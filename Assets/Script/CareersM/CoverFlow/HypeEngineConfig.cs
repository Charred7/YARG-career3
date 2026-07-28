using System.Collections.Generic;
using System.Linq;
using YARG.Helpers;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Reserved for future global operational settings.
    /// Currently unused — tier prioritisation is hardcoded as
    /// first-match-wins (Tier 1 priority order → Tier 2 priority order → ...).
    /// </summary>
    public class HypeEngineGlobalSettings
    {
        public static HypeEngineGlobalSettings Default => new HypeEngineGlobalSettings();
    }

    /// <summary>
    /// A single evaluated rule parsed from a <c>[Rule.*]</c> section of
    /// <c>hypeengine.ini</c>. Contains one or two conditions (AND logic),
    /// a priority rank within its tier, and the output text templates.
    /// </summary>
    public class HypeEngineRule
    {
        /// <summary>Section name, e.g. "Rule.CampaignFinale".</summary>
        public string RuleId;

        /// <summary>Tier this rule belongs to. 0 = fallback, 1-5 = normal tiers.</summary>
        public int Tier;

        /// <summary>Lower = higher priority within the tier.</summary>
        public int PriorityRank;

        // ── Primary condition ──────────────────────────────────────

        /// <summary>Condition type string, e.g. "remaining_gigs_total".</summary>
        public string ConditionType;

        /// <summary>Comparison operator: "eq", "lt", "lte", "gt", "gte".</summary>
        public string Operator;

        /// <summary>String value parsed at evaluation time.</summary>
        public string Value;

        // ── Optional secondary condition (AND logic) ───────────────

        /// <summary>Optional second condition type. Empty string = no second condition.</summary>
        public string ConditionType2 = string.Empty;

        /// <summary>Optional second operator. Empty string = no second condition.</summary>
        public string Operator2 = string.Empty;

        /// <summary>Optional second value. Empty string = no second condition.</summary>
        public string Value2 = string.Empty;

        // ── Output ─────────────────────────────────────────────────

        /// <summary>Icon name (maps to a Sprite in HypeIconSet).</summary>
        public string Icon = string.Empty;

        /// <summary>Text for slot 1. May contain a "{0}" placeholder.</summary>
        public string Slot1;

        /// <summary>Text for slot 2. May contain a "{0}" placeholder. Empty = not used.</summary>
        public string Slot2 = string.Empty;

        /// <summary>
        /// Value substituted into "{0}" in Slot1.
        /// Can be a literal number or a special token like "avg_track_minutes".
        /// </summary>
        public string Slot1Param = string.Empty;

        /// <summary>
        /// Value substituted into "{0}" in Slot2.
        /// Can be a literal number or a special token like "avg_track_minutes".
        /// </summary>
        public string Slot2Param = string.Empty;
    }

    /// <summary>
    /// Per-tier configuration parsed from <c>[Tier.*]</c> sections of
    /// <c>hypeengine.ini</c>. Currently supports blacklist thresholds
    /// that can skip an entire tier when exceeded.
    /// </summary>
    public class HypeEngineTierConfig
    {
        /// <summary>The tier number this config applies to.</summary>
        public int Tier;

        /// <summary>If set, skip the entire tier when total duration > this value (minutes).</summary>
        public float? BlacklistTotalMinutes;

        /// <summary>If set, skip the entire tier when average track duration > this value (minutes).</summary>
        public float? BlacklistAvgTrackMinutes;
    }

    /// <summary>
    /// Runtime evaluation context passed to every condition evaluator.
    /// </summary>
    public struct HypeEvaluationContext
    {
        /// <summary>The gig currently being evaluated.</summary>
        public GigInfo Gig;

        /// <summary>The career/campaign the gig belongs to.</summary>
        public CareerInfo Career;

        /// <summary>Parsed CoverFlow visual and HypeEngine override configuration.</summary>
        public CoverFlowConfig Config;

        /// <summary>Resolved song entries for this gig (non-null entries only).</summary>
        public List<YARG.Core.Song.SongEntry> SongEntries;

        /// <summary>Cached duration data for this gig (may be null).</summary>
        public GigDurationData DurationData;

        /// <summary>Number of incomplete gigs remaining in the campaign.</summary>
        public int RemainingGigsTotal;

        /// <summary>Number of incomplete gigs remaining in the current tier.</summary>
        public int RemainingGigsInTier;

        /// <summary>Campaign-wide average track duration in seconds.</summary>
        public double CampaignAvgDuration;

        /// <summary>Total duration of all resolved songs in this gig, in seconds.</summary>
        public double GigTotalSeconds;

        /// <summary>Average track duration for this gig, in seconds.</summary>
        public double GigAvgTrackSeconds;

        /// <summary>Headliner artist names from HypeThresholds, used by has_headliner_artist condition.</summary>
        public string[] HeadlinerArtistNames;
    }

    /// <summary>
    /// Static in-memory cache for the parsed <c>hypeengine.ini</c> configuration.
    /// Populated exactly once when the CoverFlow menu is first opened via
    /// <see cref="LoadIfNeeded"/>, and remains available for the session.
    /// </summary>
    public static class HypeEngineCache
    {
        /// <summary>True once the configuration has been loaded at least once this session.</summary>
        public static bool IsLoaded { get; private set; }

        /// <summary>Parsed global settings from the [Global] section.</summary>
        public static HypeEngineGlobalSettings GlobalSettings { get; private set; }

        /// <summary>
        /// All parsed rules, sorted by Tier ascending, then PriorityRank ascending.
        /// Tier 0 (fallback) always sorts last regardless of priority_rank.
        /// </summary>
        public static List<HypeEngineRule> Rules { get; private set; }

        /// <summary>Per-tier configuration objects (blacklists, etc.).</summary>
        public static List<HypeEngineTierConfig> TierConfigs { get; private set; }

        /// <summary>
        /// Ensures the cache is populated. Does nothing if already loaded.
        /// Called lazily from <see cref="CoverFlowController.SetCareer"/> when
        /// the CoverFlow menu first opens — never at global game launch.
        /// </summary>
        public static void LoadIfNeeded()
        {
            if (IsLoaded)
                return;

            var result = HypeEngineConfigParser.Parse();
            if (result != null)
            {
                GlobalSettings = result.Value.GlobalSettings ?? HypeEngineGlobalSettings.Default;
                Rules = result.Value.Rules ?? new List<HypeEngineRule>();
                TierConfigs = result.Value.TierConfigs ?? new List<HypeEngineTierConfig>();

                // Sort rules: Tier ascending, then PriorityRank ascending.
                // Tier 0 (fallback) is pushed to the very end.
                Rules = Rules
                    .OrderBy(r => r.Tier == 0 ? int.MaxValue : r.Tier)
                    .ThenBy(r => r.PriorityRank)
                    .ToList();
            }
            else
            {
                // No INI file found at all — use empty defaults
                GlobalSettings = HypeEngineGlobalSettings.Default;
                Rules = new List<HypeEngineRule>();
                TierConfigs = new List<HypeEngineTierConfig>();
            }

            IsLoaded = true;
        }

        /// <summary>
        /// Invalidates the cache so it will be re-parsed on the next access.
        /// Useful for hot-reload during development or when returning to the menu.
        /// </summary>
        public static void Invalidate()
        {
            IsLoaded = false;
            GlobalSettings = null;
            Rules = null;
            TierConfigs = null;
        }

        /// <summary>
        /// Result tuple returned by <see cref="HypeEngineConfigParser.Parse"/>.
        /// Must be public since the parser is a public static method.
        /// </summary>
        public struct ParseResult
        {
            public HypeEngineGlobalSettings GlobalSettings;
            public List<HypeEngineRule> Rules;
            public List<HypeEngineTierConfig> TierConfigs;
        }
    }
}