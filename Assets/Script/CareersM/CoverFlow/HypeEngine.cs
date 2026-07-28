using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using YARG.Career;
using YARG.Core.Song;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Configurable thresholds for the HypeEngine decision tree.
    /// Populated from CoverFlowController's serialized Inspector fields.
    /// All values are tunable at runtime without code changes.
    ///
    /// These serve as fallback defaults when the external <c>hypeengine.ini</c>
    /// is absent or missing specific fields.
    /// </summary>
    [System.Serializable]
    public struct HypeThresholds
    {
        [Header("Tier 2 — Micro-Time Efficiency")]
        /// <summary>Rule 3 (Blitz Play): total duration must be ≤ this value (minutes).</summary>
        public float BlitzTotalMinutesMax;

        /// <summary>Rule 3 (Blitz Play): average track must be < this value (minutes).</summary>
        public float BlitzAvgTrackMinutesMax;

        /// <summary>Rule 4 (Bite-Sized): average track must be < this value (minutes).</summary>
        public float BiteSizeAvgTrackMinutesMax;

        /// <summary>Tier 2 blacklist: if total duration > this, skip entire tier.</summary>
        public float Tier2BlacklistTotalMinutes;

        /// <summary>Tier 2 blacklist: if average track > this, skip entire tier.</summary>
        public float Tier2BlacklistAvgTrackMinutes;

        [Header("Tier 3 — Aesthetic Vibe")]
        /// <summary>Rule 5 (Headliner Hook): artist names to check against (case-insensitive).</summary>
        public string[] HeadlinerArtistNames;

        [Header("Fallback")]
        /// <summary>Rule 7 (Catch-All): default text when no other rule matches.</summary>
        public string DefaultFallbackText;

        /// <summary>
        /// Returns sensible defaults for all thresholds.
        /// </summary>
        public static HypeThresholds Default => new HypeThresholds
        {
            BlitzTotalMinutesMax           = 9.0f,
            BlitzAvgTrackMinutesMax        = 3.5f,
            BiteSizeAvgTrackMinutesMax     = 3.0f,
            Tier2BlacklistTotalMinutes     = 10.0f,
            Tier2BlacklistAvgTrackMinutes  = 3.5f,
            HeadlinerArtistNames           = new[] { "METALLICA", "QUEEN", "ROCK BAND" },
            DefaultFallbackText            = "SETLIST VIBE: PURE ROCK & ROLL"
        };
    }

    /// <summary>
    /// Conditional metadata processor that evaluates the impending setlist
    /// and outputs two encouragement strings to the active card's lower region.
    ///
    /// V4 REFACTOR: Now data-driven from the external <c>hypeengine.ini</c> file.
    /// Rules are parsed once and cached in <see cref="HypeEngineCache"/> when the
    /// CoverFlow menu first opens. The hardcoded Tier 1/2/3 logic has been replaced
    /// with a generic loop over sorted rules from the cache.
    ///
    /// The <see cref="HypeThresholds"/> struct is retained for backward
    /// compatibility; its values serve as defaults when the INI is absent.
    /// </summary>
    public class HypeEngine
    {
        // ── Public API ────────────────────────────────────────────

        /// <summary>
        /// Evaluates the hype rules for the given gig in the context of the
        /// current campaign. Outputs up to two string slots.
        ///
        /// Uses the data-driven rule set from <see cref="HypeEngineCache"/>.
        /// Falls back to <see cref="HypeThresholds.Default"/> text if the cache
        /// is empty (e.g. no INI file found).
        /// </summary>
        public void Evaluate(GigInfo gig, CareerInfo career, CoverFlowConfig config,
            HypeThresholds thresholds, out string slot1, out string slot2,
            out string icon1, out string icon2)
        {
            slot1 = null;
            slot2 = null;
            icon1 = null;
            icon2 = null;

            if (gig == null || career == null || config == null)
                return;

            // 1. Build evaluation context
            var ctx = BuildContext(gig, career, config, thresholds);

            // 2. Ensure cache is loaded (first access triggers one-time parse)
            HypeEngineCache.LoadIfNeeded();

            // 3. If no external rules exist, fall back to hardcoded defaults
            if (HypeEngineCache.Rules == null || HypeEngineCache.Rules.Count == 0)
            {
                slot1 = thresholds.DefaultFallbackText;
                return;
            }

            // 4. Group rules by tier, excluding fallback (Tier 0)
            var tierGroups = HypeEngineCache.Rules
                .Where(r => r.Tier > 0)
                .GroupBy(r => r.Tier)
                .OrderBy(g => g.Key);

            HypeEngineRule matchedRule1 = null;
            HypeEngineRule matchedRule2 = null;

            foreach (var tierGroup in tierGroups)
            {
                // Check tier blacklist
                var tierCfg = HypeEngineCache.TierConfigs?.FirstOrDefault(t => t.Tier == tierGroup.Key);
                if (IsTierBlacklisted(tierCfg, ctx))
                    continue;

                bool tierHadMatch = false;

                foreach (var rule in tierGroup.OrderBy(r => r.PriorityRank))
                {
                    if (HypeEngineConditionEvaluator.EvaluateCondition(rule, ctx))
                    {
                        AssignNextSlot(ref slot1, ref slot2, FormatSlotText(rule.Slot1, rule.Slot1Param, ctx), rule, ref matchedRule1, ref matchedRule2);
                        if (!string.IsNullOrEmpty(rule.Slot2))
                            AssignNextSlot(ref slot1, ref slot2, FormatSlotText(rule.Slot2, rule.Slot2Param, ctx), rule, ref matchedRule1, ref matchedRule2);

                        tierHadMatch = true;

                        break; // First match wins — stop processing this tier
                    }
                }

                if (tierHadMatch)
                    break; // Stop processing lower tiers — first matching tier wins
            }

            // 6. Fallback — use Tier 0 rule if nothing matched, or thresholds text
            if (slot1 == null && slot2 == null)
            {
                var fallback = HypeEngineCache.Rules.FirstOrDefault(r => r.Tier == 0);
                slot1 = fallback?.Slot1 ?? thresholds.DefaultFallbackText;
                if (fallback != null)
                    matchedRule1 = fallback;
            }
            else if (slot1 == null)
            {
                slot1 = slot2;
                slot2 = null;
                matchedRule1 = matchedRule2;
                matchedRule2 = null;
            }

            // 7. Extract icon name from matched rule
            icon1 = matchedRule1?.Icon;
        }

        // ── Context Building ──────────────────────────────────────

        /// <summary>
        /// Builds the <see cref="HypeEvaluationContext"/> from gig, career, and
        /// thresholds data. This is the bridge between the existing data sources
        /// and the data-driven rule evaluator.
        /// </summary>
        private static HypeEvaluationContext BuildContext(GigInfo gig, CareerInfo career,
            CoverFlowConfig config, HypeThresholds thresholds)
        {
            int remainingGigsTotal = GetRemainingGigCount(career);

            // Determine the actual tier from careerdef.ini via CampaignProgressionManager
            var campaignGigs = CareerManager.Instance.GetGigs(career.id);
            int gigIndex = campaignGigs?.FindIndex(g => g.name == gig.name) ?? 0;
            if (gigIndex < 0) gigIndex = 0;

            int remainingGigsInTier = 0;
            int currentTier = CampaignProgressionManager.Instance.GetTierForGig(gigIndex);
            if (currentTier >= 0)
            {
                // Count incomplete gigs within the current real tier (from careerdef.ini)
                if (campaignGigs != null)
                {
                    for (int i = 0; i < campaignGigs.Count; i++)
                    {
                        if (CampaignProgressionManager.Instance.GetTierForGig(i) == currentTier)
                        {
                            string gid = $"{career.id}|{campaignGigs[i].name}";
                            if (!CareerManager.Instance.IsGigCompleted(gid))
                                remainingGigsInTier++;
                        }
                    }
                }
            }
            else
            {
                // Fallback: no tier config — count remaining gigs from current onward
                if (campaignGigs != null)
                {
                    for (int i = gigIndex; i < campaignGigs.Count; i++)
                    {
                        string gid = $"{career.id}|{campaignGigs[i].name}";
                        if (!CareerManager.Instance.IsGigCompleted(gid))
                            remainingGigsInTier++;
                    }
                }
            }

            // Resolve song entries from the matched library songs
            var resolvedSongs = CareerManager.Instance.ResolveGig(gig, career.id);
            var songEntries = resolvedSongs?
                .Where(r => r.SongEntry != null)
                .Select(r => r.SongEntry)
                .ToList() ?? new List<SongEntry>();

            // Compute duration directly from resolved SongEntry objects.
            // SongEntry.SongLengthSeconds is the authoritative source — the
            // cache-based gigDurations may be empty if analysis hasn't run yet.
            double gigTotalSeconds = 0.0;
            int resolvedCount = songEntries.Count;
            foreach (var entry in songEntries)
                gigTotalSeconds += entry.SongLengthSeconds;
            double gigAvgTrackSeconds = resolvedCount > 0 ? gigTotalSeconds / resolvedCount : 0.0;

            // Also pull cache-based GigDurationData for conditions that
            // reference DurationData directly (e.g. avg_track_below_campaign_avg
            // checks DurationData.avgTrackSeconds as a secondary source).
            var cache = SongMatchCache.GetOrLoadCache(career.id);
            GigDurationData durationData = null;
            cache?.gigDurations?.TryGetValue(gig.name, out durationData);

            // Compute campaign average duration
            double campaignAvgDuration = 0;
            if (campaignGigs != null && campaignGigs.Count > 0)
            {
                campaignAvgDuration = HypeEngineConditionEvaluator.ComputeCampaignAvgDuration(career, campaignGigs);
            }

            return new HypeEvaluationContext
            {
                Gig                    = gig,
                Career                 = career,
                Config                 = config,
                SongEntries            = songEntries,
                DurationData           = durationData,
                RemainingGigsTotal     = remainingGigsTotal,
                RemainingGigsInTier    = remainingGigsInTier,
                CampaignAvgDuration    = campaignAvgDuration,
                GigTotalSeconds        = gigTotalSeconds,
                GigAvgTrackSeconds     = gigAvgTrackSeconds,
                HeadlinerArtistNames   = thresholds.HeadlinerArtistNames
            };
        }

        // ── Tier Blacklist ────────────────────────────────────────

        /// <summary>
        /// Checks whether a tier should be skipped due to blacklist thresholds.
        /// Returns true if either blacklist threshold is exceeded.
        /// </summary>
        private static bool IsTierBlacklisted(HypeEngineTierConfig tierCfg, HypeEvaluationContext ctx)
        {
            if (tierCfg == null)
                return false;

            // No data, no resolved songs, or zero-duration songs = skip tier.
            // Zero totalSeconds occurs when songs are matched by hash/title
            // but have no length metadata — conditions like "total_minutes lt 10.0"
            // would incorrectly match with 0, producing "under 0 minutes" text.
            if (ctx.DurationData == null || ctx.DurationData.resolvedSongCount == 0 ||
                ctx.GigTotalSeconds <= 0.0)
                return true;

            double totalMin = ctx.GigTotalSeconds / 60.0;
            double avgMin = ctx.GigAvgTrackSeconds / 60.0;

            if (tierCfg.BlacklistTotalMinutes.HasValue &&
                totalMin > tierCfg.BlacklistTotalMinutes.Value)
            {
                return true;
            }

            if (tierCfg.BlacklistAvgTrackMinutes.HasValue &&
                avgMin > tierCfg.BlacklistAvgTrackMinutes.Value)
            {
                return true;
            }

            return false;
        }

        // ── Text Formatting ───────────────────────────────────────

        /// <summary>
        /// Formats a slot text template, substituting "{0}" with the param value.
        /// The param can be a literal number or a special token like "avg_track_minutes".
        /// </summary>
        private static string FormatSlotText(string template, string param, HypeEvaluationContext ctx)
        {
            if (string.IsNullOrEmpty(template))
                return string.Empty;

            if (string.IsNullOrEmpty(param))
                return template;

            string replacement = ResolveParamValue(param, ctx);
            return template.Replace("{0}", replacement);
        }

        /// <summary>
        /// Resolves a slot parameter to its display string.
        /// Literal numbers are returned as-is; special tokens are computed from context.
        /// </summary>
        private static string ResolveParamValue(string param, HypeEvaluationContext ctx)
        {
            if (string.IsNullOrEmpty(param))
                return string.Empty;

            string paramLower = param.ToLowerInvariant();

            // Special tokens
            switch (paramLower)
            {
                case "avg_track_minutes":
                    double avgMin = ctx.GigAvgTrackSeconds / 60.0;
                    // Clamp to minimum 1 to avoid "under 0 minutes" display
                    return Mathf.Max(1, Mathf.RoundToInt((float)avgMin)).ToString();

                case "total_minutes":
                    double totalMin = ctx.GigTotalSeconds / 60.0;
                    // Clamp to minimum 1 to avoid "under 0 minutes" display
                    return Mathf.Max(1, Mathf.RoundToInt((float)totalMin)).ToString();

                case "remaining_gigs_total":
                    return ctx.RemainingGigsTotal.ToString();

                case "remaining_gigs_in_tier":
                    return ctx.RemainingGigsInTier.ToString();

                default:
                    // Try parsing as a literal number
                    if (float.TryParse(param, NumberStyles.Float, CultureInfo.InvariantCulture, out float literal))
                        return Mathf.RoundToInt(literal).ToString();

                    // Return as-is (e.g. a pre-formatted string)
                    return param;
            }
        }

        // ── Helpers (retained) ────────────────────────────────────

        /// <summary>
        /// Assigns a value to the next available slot (slot1 preferred, then slot2).
        /// Also tracks which rule provided each slot's content for icon extraction.
        /// </summary>
        private static void AssignNextSlot(ref string slot1, ref string slot2, string value,
            HypeEngineRule rule, ref HypeEngineRule matchedRule1, ref HypeEngineRule matchedRule2)
        {
            if (slot1 == null)
            {
                slot1 = value;
                matchedRule1 = rule;
            }
            else if (slot2 == null)
            {
                slot2 = value;
                matchedRule2 = rule;
            }
            // Both full — discard (shouldn't happen with our rule count)
        }

        /// <summary>
        /// Returns the number of incomplete gigs remaining in the campaign.
        /// </summary>
        private static int GetRemainingGigCount(CareerInfo career)
        {
            var gigs = CareerManager.Instance.GetGigs(career.id);
            if (gigs == null) return 0;

            int remaining = 0;
            foreach (var gig in gigs)
            {
                string gid = $"{career.id}|{gig.name}";
                if (!CareerManager.Instance.IsGigCompleted(gid))
                    remaining++;
            }

            return remaining;
        }
    }
}