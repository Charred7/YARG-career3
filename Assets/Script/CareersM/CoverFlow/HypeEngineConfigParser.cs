using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using YARG.Core.Logging;
using YARG.Helpers;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Section-aware INI parser for <c>hypeengine.ini</c>.
    /// Unlike <see cref="CareerBundleManager"/>'s flattened parser, this one
    /// tracks section headers (<c>[Global]</c>, <c>[Rule.*]</c>, <c>[Tier.*]</c>)
    /// and builds structured configuration objects.
    ///
    /// File resolution mirrors <see cref="CareerBundleManager"/>:
    /// persistent path first, StreamingAssets fallback.
    /// </summary>
    public static class HypeEngineConfigParser
    {
        private const string CONFIG_FILE_NAME = "hypeengine.ini";
        private const string STREAMING_ASSETS_SUBPATH = "CareersM";

        /// <summary>
        /// Parses the <c>hypeengine.ini</c> file from disk.
        /// Returns null if no config file can be found at either location.
        /// </summary>
        public static HypeEngineCache.ParseResult? Parse()
        {
            string configPath = ResolveConfigPath();
            if (configPath == null)
            {
                YargLogger.LogWarning("HypeEngineConfig: No hypeengine.ini found at persistent or StreamingAssets path.");
                return null;
            }

            if (!File.Exists(configPath))
            {
                YargLogger.LogWarning($"HypeEngineConfig: hypeengine.ini resolved to '{configPath}' but does not exist.");
                return null;
            }

            return ParseFile(configPath);
        }

        /// <summary>
        /// Resolves the hypeengine.ini path. Checks the persistent career directory
        /// first, then falls back to StreamingAssets. This mirrors the path resolution
        /// methodology used by <see cref="CareerBundleManager"/>.
        /// </summary>
        private static string ResolveConfigPath()
        {
            // Persistent data path (user-writable, mirrors CareerBundleManager)
            string careerDir = Path.Combine(PathHelper.PersistentDataPath, "career");
            string persistentPath = Path.Combine(careerDir, CONFIG_FILE_NAME);
            if (File.Exists(persistentPath))
                return persistentPath;

            // Fallback: StreamingAssets (shipped with the game)
            string streamingPath = Path.Combine(PathHelper.StreamingAssetsPath, STREAMING_ASSETS_SUBPATH, CONFIG_FILE_NAME);
            if (File.Exists(streamingPath))
                return streamingPath;

            return null;
        }

        /// <summary>
        /// Reads and parses the INI file at the given path.
        /// </summary>
        private static HypeEngineCache.ParseResult ParseFile(string path)
        {
            var globalSettings = HypeEngineGlobalSettings.Default;
            var rules = new List<HypeEngineRule>();
            var tierConfigs = new List<HypeEngineTierConfig>();

            string currentSection = null;
            var sectionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();

                // Skip empty lines and comments
                if (string.IsNullOrEmpty(line) || line[0] == '#' || line[0] == ';')
                    continue;

                // Section header
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    // Flush previous section before switching
                    FlushSection(currentSection, sectionKeys, globalSettings, rules, tierConfigs);

                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    sectionKeys.Clear();
                    continue;
                }

                // Key-value pair
                int eqIdx = line.IndexOf('=');
                if (eqIdx <= 0) continue;

                string key = line.Substring(0, eqIdx).Trim().ToLowerInvariant();
                string value = line.Substring(eqIdx + 1).Trim();

                // Strip surrounding quotes
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                    value = value.Substring(1, value.Length - 2);

                sectionKeys[key] = value;
            }

            // Flush the final section
            FlushSection(currentSection, sectionKeys, globalSettings, rules, tierConfigs);

            return new HypeEngineCache.ParseResult
            {
                GlobalSettings = globalSettings,
                Rules = rules,
                TierConfigs = tierConfigs
            };
        }

        /// <summary>
        /// Flushes the accumulated key-value pairs for the current section into
        /// the appropriate configuration object.
        /// </summary>
        private static void FlushSection(string section,
            Dictionary<string, string> keys,
            HypeEngineGlobalSettings globalSettings,
            List<HypeEngineRule> rules,
            List<HypeEngineTierConfig> tierConfigs)
        {
            if (string.IsNullOrEmpty(section) || keys.Count == 0)
                return;

            string sectionLower = section.ToLowerInvariant();

            if (sectionLower == "global")
            {
                // [Global] section is reserved for future settings.
                // Currently unused — tier prioritisation is hardcoded.
            }
            else if (sectionLower.StartsWith("rule."))
            {
                var rule = ParseRuleSection(section, keys);
                if (rule != null)
                    rules.Add(rule);
            }
            else if (sectionLower.StartsWith("tier."))
            {
                var tierCfg = ParseTierSection(section, keys);
                if (tierCfg != null)
                    tierConfigs.Add(tierCfg);
            }
        }

        // ── Section Parsers ──────────────────────────────────────────────

        private static HypeEngineRule ParseRuleSection(string sectionName, Dictionary<string, string> keys)
        {
            var rule = new HypeEngineRule
            {
                RuleId = sectionName
            };

            // Tier (required)
            if (!keys.TryGetValue("tier", out string tierStr) || !int.TryParse(tierStr, out int tier))
            {
                YargLogger.LogWarning($"HypeEngineConfig: Rule '{sectionName}' missing or invalid 'tier'. Skipping.");
                return null;
            }
            rule.Tier = tier;

            // PriorityRank (required)
            if (!keys.TryGetValue("priority_rank", out string rankStr) || !int.TryParse(rankStr, out int rank))
            {
                YargLogger.LogWarning($"HypeEngineConfig: Rule '{sectionName}' missing or invalid 'priority_rank'. Skipping.");
                return null;
            }
            rule.PriorityRank = rank;

            // Condition (required)
            if (!keys.TryGetValue("condition", out string condition))
            {
                YargLogger.LogWarning($"HypeEngineConfig: Rule '{sectionName}' missing 'condition'. Skipping.");
                return null;
            }
            rule.ConditionType = condition.ToLowerInvariant();

            // Operator (required)
            if (!keys.TryGetValue("operator", out string op))
            {
                YargLogger.LogWarning($"HypeEngineConfig: Rule '{sectionName}' missing 'operator'. Skipping.");
                return null;
            }
            rule.Operator = op.ToLowerInvariant();

            // Value (required)
            if (!keys.TryGetValue("value", out string val))
            {
                YargLogger.LogWarning($"HypeEngineConfig: Rule '{sectionName}' missing 'value'. Skipping.");
                return null;
            }
            rule.Value = val.ToLowerInvariant();

            // Optional second condition
            if (keys.TryGetValue("condition2", out string cond2))
                rule.ConditionType2 = cond2.ToLowerInvariant();
            if (keys.TryGetValue("operator2", out string op2))
                rule.Operator2 = op2.ToLowerInvariant();
            if (keys.TryGetValue("value2", out string val2))
                rule.Value2 = val2.ToLowerInvariant();

            // Output
            if (!keys.TryGetValue("slot1", out string slot1) || string.IsNullOrEmpty(slot1))
            {
                YargLogger.LogWarning($"HypeEngineConfig: Rule '{sectionName}' missing or empty 'slot1'. Skipping.");
                return null;
            }
            rule.Slot1 = slot1;

            if (keys.TryGetValue("slot2", out string slot2))
                rule.Slot2 = slot2;

            if (keys.TryGetValue("slot1_param", out string s1p))
                rule.Slot1Param = s1p;

            if (keys.TryGetValue("slot2_param", out string s2p))
                rule.Slot2Param = s2p;

            // Optional icon name (maps to HypeIconSet sprite)
            if (keys.TryGetValue("icon", out string icon))
                rule.Icon = icon.ToLowerInvariant();

            return rule;
        }

        private static HypeEngineTierConfig ParseTierSection(string sectionName, Dictionary<string, string> keys)
        {
            // Extract tier number from section name, e.g. "Tier.2" → 2
            string tierPart = sectionName.Substring("tier.".Length).Trim();
            if (!int.TryParse(tierPart, out int tier))
            {
                YargLogger.LogWarning($"HypeEngineConfig: Invalid tier section name '{sectionName}'. Skipping.");
                return null;
            }

            var tierCfg = new HypeEngineTierConfig
            {
                Tier = tier
            };

            if (keys.TryGetValue("blacklist_total_minutes", out string totalMin) &&
                float.TryParse(totalMin, NumberStyles.Float, CultureInfo.InvariantCulture, out float totalVal))
            {
                tierCfg.BlacklistTotalMinutes = totalVal;
            }

            if (keys.TryGetValue("blacklist_avg_track_minutes", out string avgMin) &&
                float.TryParse(avgMin, NumberStyles.Float, CultureInfo.InvariantCulture, out float avgVal))
            {
                tierCfg.BlacklistAvgTrackMinutes = avgVal;
            }

            return tierCfg;
        }
    }
}