using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Visual and behavioral configuration for the CoverFlow carousel,
    /// parsed from a campaign's <c>careerdef.ini</c>.
    /// </summary>
    public class CoverFlowConfig
    {
        // ── [Visuals] ────────────────────────────────────────────

        /// <summary>
        /// If true, use <see cref="BackgroundImageName"/> as the full-bleed
        /// background sprite. If false, use <see cref="FallbackColor"/>.
        /// </summary>
        public bool UseImageBackground = true;

        /// <summary>
        /// File name of the background sprite (relative to the bundle folder).
        /// </summary>
        public string BackgroundImageName = string.Empty;

        /// <summary>
        /// Hex colour (#rrggbb) used when <see cref="UseImageBackground"/> is false
        /// or the image asset is missing.
        /// </summary>
        public Color FallbackColor = new Color(0.1f, 0.1f, 0.18f); // #1a1a2e

        /// <summary>
        /// Hex colour (#rrggbb) for borders, active frames, and accent glows.
        /// </summary>
        public Color AccentColor = new Color(0f, 0.33f, 1f); // #0055ff

        /// <summary>
        /// If true, this campaign uses the CoverFlow carousel UI.
        /// If false, the classic vertical GigView list is used instead.
        /// </summary>
        public bool UseCoverFlow = false;

        /// <summary>
        /// If true, the career name is displayed on the CareerFlow gear crate card.
        /// Controlled by the display_name_on_crate key in careerdef.ini [Visuals].
        /// Defaults to false — box art speaks for itself unless custom art lacks a logo.
        /// </summary>
        public bool DisplayNameOnCrate = false;

        // ── [HypeEngineOverrides] ────────────────────────────────

        /// <summary>
        /// Optional override for Rule 7's default "Pure Rock & Roll" text.
        /// </summary>
        public string CustomVibeLabel = string.Empty;

        /// <summary>
        /// Comma-separated artist names checked by Rule 5 (Headliner Hook).
        /// Case-insensitive matching.
        /// </summary>
        public List<string> IconicArtists = new();

        // ── Poster Art Strategy (from top-level keys) ────────────

        /// <summary>
        /// Absolute path to the gig poster config file, or null if not provided.
        /// </summary>
        public string GigPosterConfigPath = null;

        /// <summary>
        /// If true, all gigs use the campaign's <c>artworkpath</c> as their poster.
        /// Only evaluated when <see cref="GigPosterConfigPath"/> is null.
        /// </summary>
        public bool UseArtAsGigPoster = false;

        /// <summary>
        /// Absolute or relative path to a gig layout INI file for per-gig
        /// layout overrides. Null if not provided.
        /// </summary>
        public string GigLayoutPath = null;

        // ── Helpers ──────────────────────────────────────────────

        /// <summary>
        /// Returns true if <paramref name="artistName"/> matches any artist
        /// in <see cref="IconicArtists"/> (case-insensitive).
        /// </summary>
        public bool IsIconicArtist(string artistName)
        {
            if (string.IsNullOrEmpty(artistName) || IconicArtists.Count == 0)
                return false;

            return IconicArtists.Any(a =>
                string.Equals(a.Trim(), artistName.Trim(), System.StringComparison.OrdinalIgnoreCase));
        }

        // ── Parsing ──────────────────────────────────────────────

        /// <summary>
        /// Builds a <see cref="CoverFlowConfig"/> from the flattened key-value
        /// dictionary produced by <see cref="CareerBundleManager"/>'s INI parser.
        /// Keys have already been lowercased.
        /// </summary>
        public static CoverFlowConfig Parse(Dictionary<string, string> fields)
        {
            var config = new CoverFlowConfig();

            // [Visuals]
            if (fields.TryGetValue("imagebg", out string imageBgStr))
                config.UseImageBackground = imageBgStr.ToLowerInvariant() == "true";

            if (fields.TryGetValue("bgimg", out string bgImg))
                config.BackgroundImageName = bgImg;

            if (fields.TryGetValue("accent_color", out string accentHex))
                config.AccentColor = ParseHexColor(accentHex, config.AccentColor);

            if (fields.TryGetValue("fallback_color", out string fallbackHex))
                config.FallbackColor = ParseHexColor(fallbackHex, config.FallbackColor);

            if (fields.TryGetValue("use_coverflow", out string useCfStr))
                config.UseCoverFlow = useCfStr.ToLowerInvariant() == "true";

            if (fields.TryGetValue("display_name_on_crate", out string displayNameStr))
                config.DisplayNameOnCrate = displayNameStr.ToLowerInvariant() == "true";

            // [HypeEngineOverrides]
            if (fields.TryGetValue("custom_vibe_label", out string vibeLabel))
                config.CustomVibeLabel = vibeLabel;

            if (fields.TryGetValue("iconic_artists", out string artistsStr))
            {
                config.IconicArtists = artistsStr
                    .Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();
            }

            // [Poster Art Strategy]
            if (fields.TryGetValue("gigposterfile", out string gpf))
                config.GigPosterConfigPath = gpf;

            if (fields.TryGetValue("useartasgigposter", out string ua1))
                config.UseArtAsGigPoster = ua1.ToLowerInvariant() == "true";
            else if (fields.TryGetValue("use_art_as_gig_poster", out string ua2))
                config.UseArtAsGigPoster = ua2.ToLowerInvariant() == "true";

            if (fields.TryGetValue("giglayoutpath", out string glp))
                config.GigLayoutPath = glp;

            return config;
        }

        private static Color ParseHexColor(string hex, Color fallback)
        {
            if (string.IsNullOrEmpty(hex))
                return fallback;

            hex = hex.TrimStart('#');
            if (hex.Length != 6 && hex.Length != 8)
                return fallback;

            if (ColorUtility.TryParseHtmlString("#" + hex, out Color parsed))
                return parsed;

            return fallback;
        }
    }
}