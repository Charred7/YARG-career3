using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using YARG.Career;
using YARG.Core.Logging;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Section-aware INI parser for campaign tier and settings data in
    /// <c>careerdef.ini</c>.
    ///
    /// This parser handles <c>[CampaignSettings]</c> and <c>[Tier_X]</c>
    /// sections. Unlike <see cref="CareerBundleManager"/>'s flattened parser
    /// (which ignores section headers), this one tracks the current section
    /// and routes key-value pairs into the appropriate data structures.
    ///
    /// File resolution is left to the caller (<see cref="CareerBundleManager"/>).
    /// This parser works solely with the file path it receives.
    ///
    /// Design mirrors <c>HypeEngineConfigParser</c> in the CoverFlow namespace.
    /// </summary>
    public static class CareerDefParser
    {
        // Regex: Tier_N where N is an integer.
        // Note: no brackets — the section header [Tier_N] has its brackets
        // stripped before it reaches FlushSection.
        private static readonly Regex TierSectionRegex =
            new Regex(@"^Tier_(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private const string CampaignSettingsSection = "campaignsettings";

        /// <summary>
        /// Parses the given <c>careerdef.ini</c> file for <c>[CampaignSettings]</c>
        /// and <c>[Tier_X]</c> sections.
        ///
        /// Returns a populated <see cref="CampaignProgressionData"/> on success,
        /// or <c>null</c> if the file cannot be read.
        ///
        /// If the file contains no <c>[Tier_X]</c> sections, the returned object's
        /// <see cref="CampaignProgressionData.Tiers"/> list will be empty (callers
        /// should treat this as "no tier gating configured").
        /// </summary>
        public static CampaignProgressionData ParseProgression(string iniPath)
        {
            if (string.IsNullOrEmpty(iniPath) || !File.Exists(iniPath))
            {
                YargLogger.LogWarning($"CareerDefParser: File not found: '{iniPath}'");
                return null;
            }

            try
            {
                var result = new CampaignProgressionData();
                var tiers = new List<TierData>();

                string currentSection = null;
                var sectionKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (string rawLine in File.ReadAllLines(iniPath))
                {
                    string line = rawLine.Trim();

                    // Skip empty lines and comments
                    if (string.IsNullOrEmpty(line) || line[0] == '#' || line[0] == ';')
                        continue;

                    // Section header
                    if (line[0] == '[' && line[line.Length - 1] == ']')
                    {
                        // Flush previous section before switching
                        FlushSection(currentSection, sectionKeys, result, tiers);

                        currentSection = line.Substring(1, line.Length - 2).Trim();
                        sectionKeys.Clear();
                        continue;
                    }

                    // Key-value pair
                    int eqIdx = line.IndexOf('=');
                    if (eqIdx <= 0) continue;

                    string key = line.Substring(0, eqIdx).Trim().ToLowerInvariant();
                    string value = line.Substring(eqIdx + 1).Trim();

                    // Strip inline comments (everything from # or ; onward)
                    int commentIdx = value.IndexOf('#');
                    if (commentIdx < 0)
                        commentIdx = value.IndexOf(';');
                    if (commentIdx >= 0)
                        value = value.Substring(0, commentIdx).Trim();

                    // Strip surrounding quotes
                    if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                        value = value.Substring(1, value.Length - 2);

                    sectionKeys[key] = value;
                }

                // Flush the final section
                FlushSection(currentSection, sectionKeys, result, tiers);

                // Sort tiers by index ascending
                tiers.Sort((a, b) => a.TierIndex.CompareTo(b.TierIndex));
                result.Tiers = tiers;

                return result;
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, $"CareerDefParser: Failed to parse '{iniPath}'");
                return null;
            }
        }

        /// <summary>
        /// Flushes accumulated key-value pairs for the current section into
        /// the appropriate data structure.
        /// </summary>
        private static void FlushSection(string section,
            Dictionary<string, string> keys,
            CampaignProgressionData result,
            List<TierData> tiers)
        {
            if (string.IsNullOrEmpty(section) || keys.Count == 0)
                return;

            string sectionLower = section.ToLowerInvariant();

            if (sectionLower == CampaignSettingsSection)
            {
                ParseCampaignSettings(keys, result);
            }
            else
            {
                // Check if this is a [Tier_N] section
                var match = TierSectionRegex.Match(section);
                if (match.Success)
                {
                    if (int.TryParse(match.Groups[1].Value, out int tierIndex))
                    {
                        var tier = ParseTierSection(tierIndex, keys);
                        if (tier != null)
                            tiers.Add(tier);
                    }
                }
                // Any other section is silently ignored (forward-compatible).
            }
        }

        /// <summary>
        /// Parses the <c>[CampaignSettings]</c> block.
        /// </summary>
        private static void ParseCampaignSettings(Dictionary<string, string> keys,
            CampaignProgressionData result)
        {
            if (keys.TryGetValue("unlockall", out string unlockStr))
            {
                result.UnlockAll = unlockStr.ToLowerInvariant() == "true";
            }
        }

        /// <summary>
        /// Parses a single <c>[Tier_N]</c> block.
        /// </summary>
        private static TierData ParseTierSection(int tierIndex, Dictionary<string, string> keys)
        {
            // Prefer "tiername" to avoid collision with the career-level "name" key.
            // Fall back to "name" for backward compatibility with legacy .ini files.
            string name = null;
            if (!keys.TryGetValue("tiername", out name))
                keys.TryGetValue("name", out name);

            int gigsInTier = 0;
            if (keys.TryGetValue("gigs_in_tier", out string gigsStr))
            {
                int.TryParse(gigsStr, out gigsInTier);
            }

            int gigsRequiredToUnlock = 0;
            if (keys.TryGetValue("gigs_required_to_unlock", out string reqStr))
            {
                int.TryParse(reqStr, out gigsRequiredToUnlock);
            }

            return new TierData
            {
                TierIndex = tierIndex,
                Name = name ?? $"Tier {tierIndex}",
                GigsInTier = gigsInTier,
                GigsRequiredToUnlock = gigsRequiredToUnlock
            };
        }
    }
}