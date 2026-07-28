using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using YARG.Core.Logging;
using YARG.Helpers;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Scans <c>career/bundles/</c> for career bundle folders, reads each
    /// <c>careerdef.ini</c>, and generates <c>careers.json</c>.
    ///
    /// On first run, seeds example bundles from StreamingAssets into the
    /// user's bundles directory so the game isn't empty out of the box.
    /// </summary>
    public static class CareerBundleManager
    {
        private const string EXAMPLE_BUNDLES_FOLDER = "CareersM/_examplecareer";

        public static string BundlesDirectory { get; private set; }

        /// <summary>
        /// Seeds example bundles, scans all bundle folders, and returns a
        /// populated <see cref="CareerDatabase"/>. Saves the result to careers.json.
        /// </summary>
        public static CareerDatabase ScanAndBuild()
        {
            string careerDir = Path.Combine(PathHelper.PersistentDataPath, "career");
            BundlesDirectory = Path.Combine(careerDir, "bundles");

            Directory.CreateDirectory(BundlesDirectory);
            SeedExampleBundles();

            var database = new CareerDatabase();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (!Directory.Exists(BundlesDirectory))
            {
                SaveCareersJson(database, careerDir);
                return database;
            }

            foreach (string bundleDir in Directory.GetDirectories(BundlesDirectory))
            {
                string folderName = Path.GetFileName(bundleDir);
                string defPath = Path.Combine(bundleDir, "careerdef.ini");

                if (!File.Exists(defPath))
                {
                    YargLogger.LogWarning($"CareerBundle: Skipping '{folderName}' — no careerdef.ini");
                    continue;
                }

                try
                {
                    var career = ParseBundle(bundleDir, folderName, defPath);
                    if (career == null) continue;

                    string runtimeId = folderName;
                    if (seenIds.Contains(runtimeId))
                    {
                        int suffix = 2;
                        while (seenIds.Contains($"{runtimeId}_{suffix}"))
                            suffix++;

                        runtimeId = $"{runtimeId}_{suffix}";
                        YargLogger.LogWarning(
                            $"CareerBundle: Duplicate folder id '{folderName}', using '{runtimeId}'");
                    }

                    career.id = runtimeId;
                    seenIds.Add(runtimeId);

                    database.careers.Add(career);
                    YargLogger.LogInfo($"CareerBundle: Loaded '{career.name}' by {career.author} [{runtimeId}]");
                }
                catch (Exception ex)
                {
                    YargLogger.LogException(ex, $"CareerBundle: Failed to load bundle '{folderName}'");
                }
            }

            database.careers.Sort((a, b) =>
                string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            SaveCareersJson(database, careerDir);
            return database;
        }

        /// <summary>
        /// Copies example bundles from StreamingAssets into the user's bundles
        /// directory. Only copies bundles whose target folder doesn't already exist,
        /// so deleting an example is permanent (it won't be re-seeded).
        /// </summary>
        private static void SeedExampleBundles()
        {
            string exampleRoot = Path.Combine(PathHelper.StreamingAssetsPath, EXAMPLE_BUNDLES_FOLDER);

            if (!Directory.Exists(exampleRoot))
                return;

            string[] exampleBundles;
            try
            {
                exampleBundles = Directory.GetDirectories(exampleRoot);
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "CareerBundle: Failed to enumerate example bundles");
                return;
            }

            foreach (string srcBundle in exampleBundles)
            {
                string folderName = Path.GetFileName(srcBundle);
                string targetBundle = Path.Combine(BundlesDirectory, folderName);

                if (Directory.Exists(targetBundle))
                    continue;

                string defPath = Path.Combine(srcBundle, "careerdef.ini");
                if (!File.Exists(defPath))
                {
                    YargLogger.LogWarning(
                        $"CareerBundle: Example '{folderName}' has no careerdef.ini, skipping");
                    continue;
                }

                try
                {
                    CopyDirectory(srcBundle, targetBundle);
                    YargLogger.LogInfo($"CareerBundle: Seeded example bundle '{folderName}'");
                }
                catch (Exception ex)
                {
                    YargLogger.LogException(ex,
                        $"CareerBundle: Failed to seed example bundle '{folderName}'");
                }
            }
        }

        /// <summary>
        /// Installs a career bundle from a source folder into the bundles directory.
        /// Returns the runtime ID on success, null on failure.
        /// </summary>
        public static string InstallBundle(string sourcePath)
        {
            if (!Directory.Exists(sourcePath))
            {
                YargLogger.LogError($"CareerBundle: Install source not found: {sourcePath}");
                return null;
            }

            string defPath = Path.Combine(sourcePath, "careerdef.ini");
            if (!File.Exists(defPath))
            {
                YargLogger.LogError($"CareerBundle: No careerdef.ini in '{sourcePath}'");
                return null;
            }

            string folderName = Path.GetFileName(sourcePath);
            string targetDir = Path.Combine(BundlesDirectory, folderName);

            if (Directory.Exists(targetDir))
            {
                int suffix = 2;
                while (Directory.Exists($"{targetDir}_{suffix}"))
                    suffix++;

                targetDir = $"{targetDir}_{suffix}";
                folderName = Path.GetFileName(targetDir);
                YargLogger.LogWarning(
                    $"CareerBundle: Folder '{Path.GetFileName(sourcePath)}' exists, installing as '{folderName}'");
            }

            try
            {
                CopyDirectory(sourcePath, targetDir);
                YargLogger.LogInfo($"CareerBundle: Installed bundle '{folderName}' to {targetDir}");
                return folderName;
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, $"CareerBundle: Failed to install from '{sourcePath}'");
                return null;
            }
        }

        /// <summary>
        /// Removes a career bundle folder from disk.
        /// </summary>
        public static bool RemoveBundle(string runtimeId)
        {
            string bundleDir = Path.Combine(BundlesDirectory, runtimeId);
            if (!Directory.Exists(bundleDir))
            {
                YargLogger.LogWarning($"CareerBundle: Cannot remove '{runtimeId}' — not found");
                return false;
            }

            try
            {
                Directory.Delete(bundleDir, true);
                YargLogger.LogInfo($"CareerBundle: Removed bundle '{runtimeId}'");
                return true;
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, $"CareerBundle: Failed to remove '{runtimeId}'");
                return false;
            }
        }

        // ── Parsing ──────────────────────────────────────────────────────

        private static CareerInfo ParseBundle(string bundleDir, string folderName, string defPath)
        {
            var fields = ParseIni(defPath);
            if (EnsureCareerDefDefaults(defPath, folderName, fields))
                fields = ParseIni(defPath);

            if (!fields.TryGetValue("name", out string name) || string.IsNullOrWhiteSpace(name))
            {
                YargLogger.LogWarning($"CareerBundle: '{folderName}/careerdef.ini' missing 'name'");
                return null;
            }

            if (!fields.TryGetValue("gigsfile", out string gigsFile) || string.IsNullOrWhiteSpace(gigsFile))
            {
                YargLogger.LogWarning($"CareerBundle: '{folderName}/careerdef.ini' missing 'gigsFile'");
                return null;
            }

            string gigsFullPath = Path.Combine(bundleDir, gigsFile);
            if (!File.Exists(gigsFullPath))
            {
                YargLogger.LogWarning(
                    $"CareerBundle: '{folderName}' references '{gigsFile}' but it doesn't exist");
                return null;
            }

            fields.TryGetValue("id", out string bundleId);
            fields.TryGetValue("artworkpath", out string artworkPath);
            fields.TryGetValue("sourceicon", out string sourceIcon);
            fields.TryGetValue("author", out string author);
            fields.TryGetValue("releasedate", out string releaseDate);

            // Resolve artwork path relative to bundle folder
            string artworkFullPath = null;
            if (!string.IsNullOrEmpty(artworkPath))
            {
                string candidate = Path.Combine(bundleDir, artworkPath);
                if (File.Exists(candidate))
                    artworkFullPath = candidate;
            }

            // Parse CoverFlow visual config + HypeEngine overrides + poster strategy
            var coverFlowConfig = CoverFlow.CoverFlowConfig.Parse(fields);

            // Resolve background image path relative to bundle folder
            if (coverFlowConfig.UseImageBackground && !string.IsNullOrEmpty(coverFlowConfig.BackgroundImageName))
            {
                string bgCandidate = Path.Combine(bundleDir, coverFlowConfig.BackgroundImageName);
                if (!File.Exists(bgCandidate))
                {
                    YargLogger.LogWarning(
                        $"CareerBundle: '{folderName}' references bgimg '{coverFlowConfig.BackgroundImageName}' but it doesn't exist. Falling back to solid color.");
                    coverFlowConfig.UseImageBackground = false;
                }
            }

            // Resolve gig poster config path relative to bundle folder
            string gigPosterConfigPath = null;
            if (!string.IsNullOrEmpty(coverFlowConfig.GigPosterConfigPath))
            {
                string candidate = Path.Combine(bundleDir, coverFlowConfig.GigPosterConfigPath);
                if (File.Exists(candidate))
                {
                    gigPosterConfigPath = candidate;
                }
                else
                {
                    YargLogger.LogWarning(
                        $"CareerBundle: '{folderName}' references gigposterfile '{coverFlowConfig.GigPosterConfigPath}' but it doesn't exist. " +
                        "Gig posters will use fallback strategies.");
                }
            }

            // Resolve gig layout path relative to bundle folder
            string gigLayoutPath = null;
            if (!string.IsNullOrEmpty(coverFlowConfig.GigLayoutPath))
            {
                string candidate = Path.Combine(bundleDir, coverFlowConfig.GigLayoutPath);
                if (File.Exists(candidate))
                    gigLayoutPath = candidate;
                else
                    YargLogger.LogWarning(
                        $"CareerBundle: '{folderName}' references giglayoutpath '{coverFlowConfig.GigLayoutPath}' but it doesn't exist.");
            }

            // Parse tier-based progression sections: [CampaignSettings], [Tier_X]
            var progressionData = CareerDefParser.ParseProgression(defPath);

            // Auto-discover posters/ subfolder (informational)
            string postersDir = Path.Combine(bundleDir, "posters");
            bool hasPostersSubfolder = Directory.Exists(postersDir);
            if (hasPostersSubfolder && string.IsNullOrEmpty(gigPosterConfigPath))
            {
                YargLogger.LogInfo(
                    $"CareerBundle: '{folderName}' has a 'posters/' subfolder but no gigposterfile. " +
                    "Named gig posters (posters/{sanitized_gig_name}.png) will be used at runtime as fallback.");
            }

            return new CareerInfo
            {
                bundleId = bundleId ?? folderName,
                name = name,
                artworkPath = artworkFullPath,
                sourceIcon = sourceIcon ?? "",
                gigsPath = gigsFullPath,
                author = author ?? "Unknown",
                bundlePath = bundleDir,
                releaseDate = releaseDate ?? "",

                // CoverFlow fields
                CoverFlowConfig = coverFlowConfig,
                gigPosterConfigPath = gigPosterConfigPath,
                useArtAsGigPoster = coverFlowConfig.UseArtAsGigPoster,
                gigLayoutPath = gigLayoutPath,

                // Campaign progression data from section-aware parser
                ProgressionData = progressionData,
            };
        }

        /// <summary>
        /// Appends missing default keys to a careerdef.ini without overwriting
        /// existing values. Returns true if the file was modified.
        /// </summary>
        private static bool EnsureCareerDefDefaults(string defPath, string folderName,
            Dictionary<string, string> fields)
        {
            bool needsUseCoverFlow = !fields.ContainsKey("use_coverflow");
            bool needsImageBg = !fields.ContainsKey("imagebg");
            bool needsUseArtAsGigPoster = !fields.ContainsKey("useartasgigposter");
            bool needsUnlockAll = !fields.ContainsKey("unlockall");

            if (!needsUseCoverFlow && !needsImageBg && !needsUseArtAsGigPoster && !needsUnlockAll)
                return false;

            var sb = new StringBuilder();
            sb.AppendLine();
            sb.AppendLine("# Auto-appended defaults by YARG CareerBundleManager");

            if (needsUseCoverFlow || needsImageBg || needsUseArtAsGigPoster)
            {
                sb.AppendLine("[Visuals]");
                if (needsUseCoverFlow)
                    sb.AppendLine("use_coverflow = true");
                if (needsImageBg)
                    sb.AppendLine("imagebg = false");
                sb.AppendLine("#bgimg = bg\\bg-03.jpg");
                sb.AppendLine("#accent_color = #ff6600");
                sb.AppendLine("#fallback_color = #1a1a2e");
                if (needsUseArtAsGigPoster)
                    sb.AppendLine("useartasgigposter = true");
                sb.AppendLine("#gigposterfile = posters.json");
            }

            if (needsUnlockAll)
            {
                sb.AppendLine("[CampaignSettings]");
                sb.AppendLine("unlockall = false  # If true, overrides all tier locks instantly");
            }

            try
            {
                File.AppendAllText(defPath, sb.ToString());
                YargLogger.LogInfo(
                    $"CareerBundle: Appended missing defaults to '{folderName}/careerdef.ini'");
                return true;
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex,
                    $"CareerBundle: Failed to append defaults to '{folderName}/careerdef.ini'");
                return false;
            }
        }

        /// <summary>
        /// Parses a key=value INI file. Keys are lowercased. Lines starting
        /// with # or ; are comments.
        /// </summary>
        private static Dictionary<string, string> ParseIni(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line[0] == '#' || line[0] == ';')
                    continue;

                int eqIdx = line.IndexOf('=');
                if (eqIdx <= 0) continue;

                string key = line.Substring(0, eqIdx).Trim().ToLowerInvariant();
                string value = line.Substring(eqIdx + 1).Trim();

                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                    value = value.Substring(1, value.Length - 2);

                // First-wins: top-level keys take priority over section-level keys
                // that happen to share the same name (e.g. "name" in [Tier_X]).
                if (!result.ContainsKey(key))
                    result[key] = value;
            }

            return result;
        }

        // ── Persistence ──────────────────────────────────────────────────

        private static void SaveCareersJson(CareerDatabase database, string careerDir)
        {
            try
            {
                string path = Path.Combine(careerDir, "careers.json");
                string json = JsonUtility.ToJson(database, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "CareerBundle: Failed to save careers.json");
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);

            foreach (string file in Directory.GetFiles(source))
            {
                string dest = Path.Combine(target, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                string dest = Path.Combine(target, Path.GetFileName(dir));
                CopyDirectory(dir, dest);
            }
        }
    }
}