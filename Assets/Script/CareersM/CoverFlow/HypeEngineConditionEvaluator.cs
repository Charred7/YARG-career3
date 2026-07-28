using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using YARG.Core.Song;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Static dispatch that maps condition type strings to strongly-typed
    /// evaluation delegates. Each delegate receives the runtime context and
    /// compares against the rule's configured value using the specified operator.
    ///
    /// Adding a new condition type requires only:
    /// 1. Adding a new case in <see cref="EvaluateCondition"/>
    /// 2. Defining the evaluation delegate
    ///
    /// No structural code changes needed elsewhere.
    /// </summary>
    public static class HypeEngineConditionEvaluator
    {
        /// <summary>
        /// Evaluates a single rule's condition(s) against the given runtime context.
        /// Supports primary + optional secondary condition with AND logic.
        /// </summary>
        /// <param name="rule">The rule to evaluate.</param>
        /// <param name="ctx">Runtime context with gig, career, and duration data.</param>
        /// <returns>True if all conditions are satisfied.</returns>
        public static bool EvaluateCondition(HypeEngineRule rule, HypeEvaluationContext ctx)
        {
            if (rule == null)
                return false;

            // Evaluate primary condition
            bool primaryResult = EvaluateSingleCondition(
                rule.ConditionType, rule.Operator, rule.Value, ctx);

            if (!primaryResult)
                return false;

            // Evaluate optional secondary condition (AND logic)
            if (!string.IsNullOrEmpty(rule.ConditionType2) &&
                !string.IsNullOrEmpty(rule.Operator2) &&
                !string.IsNullOrEmpty(rule.Value2))
            {
                bool secondaryResult = EvaluateSingleCondition(
                    rule.ConditionType2, rule.Operator2, rule.Value2, ctx);

                if (!secondaryResult)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Evaluates a single condition (type + operator + value) against the context.
        /// </summary>
        private static bool EvaluateSingleCondition(
            string conditionType, string op, string value, HypeEvaluationContext ctx)
        {
            switch (conditionType)
            {
                case "always":
                    return EvaluateAlways(op, value);

                case "remaining_gigs_total":
                    return EvaluateIntCondition(ctx.RemainingGigsTotal, op, value);

                case "remaining_gigs_in_tier":
                    return EvaluateIntCondition(ctx.RemainingGigsInTier, op, value);

                case "total_minutes":
                    return EvaluateFloatCondition(ctx.GigTotalSeconds / 60.0, op, value);

                case "avg_track_minutes":
                    return EvaluateFloatCondition(ctx.GigAvgTrackSeconds / 60.0, op, value);

                case "has_headliner_artist":
                    return EvaluateHeadlinerCondition(ctx, value);

                case "avg_track_below_campaign_avg":
                    return EvaluateBelowCampaignAvg(ctx);

                default:
                    YARG.Core.Logging.YargLogger.LogWarning(
                        $"HypeEngineConditionEvaluator: Unknown condition type '{conditionType}'.");
                    return false;
            }
        }

        // ── Individual Condition Evaluators ──────────────────────────────

        /// <summary>
        /// "always" condition: matches only when value == "true".
        /// </summary>
        private static bool EvaluateAlways(string op, string value)
        {
            return op == "eq" && value == "true";
        }

        /// <summary>
        /// Compares an integer value from context against the rule's configured value.
        /// </summary>
        private static bool EvaluateIntCondition(int contextValue, string op, string valueStr)
        {
            if (!int.TryParse(valueStr, out int threshold))
                return false;

            return op switch
            {
                "eq"  => contextValue == threshold,
                "lt"  => contextValue < threshold,
                "lte" => contextValue <= threshold,
                "gt"  => contextValue > threshold,
                "gte" => contextValue >= threshold,
                _     => false
            };
        }

        /// <summary>
        /// Compares a double value from context against the rule's configured value.
        /// </summary>
        private static bool EvaluateFloatCondition(double contextValue, string op, string valueStr)
        {
            if (!double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double threshold))
                return false;

            return op switch
            {
                "eq"  => Math.Abs(contextValue - threshold) < 0.001,
                "lt"  => contextValue < threshold,
                "lte" => contextValue <= threshold,
                "gt"  => contextValue > threshold,
                "gte" => contextValue >= threshold,
                _     => false
            };
        }

        /// <summary>
        /// "has_headliner_artist": checks if any song in the setlist matches
        /// a headliner artist name. The rule's value should be "true" or "false".
        /// Also checks <see cref="CoverFlowConfig.IconicArtists"/> for backward compatibility.
        /// </summary>
        private static bool EvaluateHeadlinerCondition(HypeEvaluationContext ctx, string value)
        {
            bool expected = value == "true";
            if (!expected)
                return false; // Only "true" is meaningful for this condition

            if (ctx.SongEntries == null || ctx.SongEntries.Count == 0)
                return false;

            // Check embedded headliner artist names from HypeThresholds
            bool hasHeadliner = ctx.SongEntries.Any(s =>
                IsHeadlinerArtist(s.Artist.ToString(), ctx.HeadlinerArtistNames));

            if (hasHeadliner)
                return true;

            // Also check config's IconicArtists (backward compat)
            if (ctx.Config != null && ctx.Config.IconicArtists.Count > 0)
            {
                hasHeadliner = ctx.SongEntries.Any(s =>
                    ctx.Config.IsIconicArtist(s.Artist.ToString()));
            }

            return hasHeadliner;
        }

        /// <summary>
        /// "avg_track_below_campaign_avg": compares gig average track duration
        /// against campaign-wide average. The rule's value should be "true".
        /// </summary>
        private static bool EvaluateBelowCampaignAvg(HypeEvaluationContext ctx)
        {
            if (ctx.DurationData == null || ctx.DurationData.resolvedSongCount == 0)
                return false;

            if (ctx.CampaignAvgDuration <= 0)
                return false;

            return ctx.DurationData.avgTrackSeconds < ctx.CampaignAvgDuration;
        }

        // ── Shared Helpers (moved from HypeEngine.cs) ────────────────────

        /// <summary>
        /// Returns the campaign-wide average track duration in seconds.
        /// Used by the "avg_track_below_campaign_avg" condition.
        /// </summary>
        public static double ComputeCampaignAvgDuration(CareerInfo career,
            List<GigInfo> campaignGigs)
        {
            var cache = SongMatchCache.GetOrLoadCache(career.id);
            if (cache?.gigDurations == null || cache.gigDurations.Count == 0)
                return 0;

            double total = 0;
            int count = 0;

            foreach (var gig in campaignGigs)
            {
                if (cache.gigDurations.TryGetValue(gig.name, out var dd))
                {
                    total += dd.avgTrackSeconds;
                    count++;
                }
            }

            return count > 0 ? total / count : 0;
        }

        /// <summary>
        /// Case-insensitive comparison of an artist name against a list of headliner names.
        /// </summary>
        private static bool IsHeadlinerArtist(string artistName, string[] headlinerNames)
        {
            if (string.IsNullOrEmpty(artistName) || headlinerNames == null || headlinerNames.Length == 0)
                return false;

            foreach (var name in headlinerNames)
            {
                if (string.Equals(name.Trim(), artistName.Trim(), StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}