using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Serializable data class for the gig poster mapping JSON file.
    /// Referenced by <c>gigposterfile</c> in <c>careerdef.ini</c>.
    ///
    /// Format:
    /// <code>
    /// {
    ///   "posters": {
    ///     "Gig Name One": "posters/first_gig.png",
    ///     "Another Gig":  "posters/second.jpg"
    ///   },
    ///   "fallback": "posters/missing_poster.png"
    /// }
    /// </code>
    ///
    /// Paths in the value are relative to the config file's own directory
    /// (which is the bundle directory by convention).
    ///
    /// The <c>fallback</c> key is optional. When set, it is used as the poster
    /// image for any gig whose name is not found in the <c>posters</c>
    /// dictionary, provided <c>useArtAsGigPoster</c> is <c>false</c>.
    /// </summary>
    public class GigPosterConfig
    {
        /// <summary>
        /// Dictionary mapping gig display names → poster file paths (relative
        /// to the config file's parent directory).
        /// </summary>
        public Dictionary<string, string> posters = new();

        /// <summary>
        /// Optional fallback poster path used when a gig name is not found
        /// in <see cref="posters"/>. Relative to the config file's parent
        /// directory. Ignored if null or empty.
        /// </summary>
        public string fallback = null;

        /// <summary>
        /// Loads and deserialises a <see cref="GigPosterConfig"/> from a JSON file.
        /// Returns <c>null</c> if the file doesn't exist or fails to parse.
        /// </summary>
        public static GigPosterConfig Load(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath) || !File.Exists(jsonPath))
                return null;

            try
            {
                string json = File.ReadAllText(jsonPath);
                var config = JsonConvert.DeserializeObject<GigPosterConfig>(json);
                return config?.posters != null ? config : null;
            }
            catch (Exception ex)
            {
                YARG.Core.Logging.YargLogger.LogError($"GigPosterConfig: Failed to load '{jsonPath}': {ex.Message}");
                return null;
            }
        }
    }
}