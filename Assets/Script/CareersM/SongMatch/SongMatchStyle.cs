using UnityEngine;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Shared Setlist Desk chrome colors and helpers.
    /// </summary>
    public static class SongMatchStyle
    {
        public const int ChromeVersion = 6;

        public static readonly Color BgSolid = new(0.07f, 0.07f, 0.09f, 1f);
        public static readonly Color BgArtTint = new(1f, 1f, 1f, 0.40f);
        public static readonly Color Vignette = new(0f, 0f, 0f, 0.55f);

        public static readonly Color PanelGlass = new(0.10f, 0.10f, 0.12f, 1f);
        public static readonly Color PanelGlassInner = new(0.09f, 0.09f, 0.11f, 1f);
        public static readonly Color PanelGlassSelected = new(0.16f, 0.15f, 0.12f, 1f);

        public static readonly Color Gold = new(0.83f, 0.69f, 0.22f, 1f);
        public static readonly Color GoldSoft = new(0.83f, 0.69f, 0.22f, 0.35f);
        public static readonly Color AccentSelect = new(0.30f, 0.82f, 0.92f, 1f);
        public static readonly Color LabelMuted = new(0.58f, 0.58f, 0.64f, 1f);
        public static readonly Color TextWhite = new(0.96f, 0.96f, 0.97f, 1f);

        public static readonly Color StatusOpen = new(0.50f, 0.50f, 0.55f, 1f);
        public static readonly Color StatusFuzzy = new(1f, 0.78f, 0.30f, 1f);
        public static readonly Color StatusMatched = Gold;
        public static readonly Color StatusUnmatched = new(0.95f, 0.42f, 0.42f, 1f);

        public static readonly Color RowSelected = new(0.83f, 0.69f, 0.22f, 0.22f);
        public static readonly Color RowNormal = new(1f, 1f, 1f, 0.03f);

        public static readonly Color BadgeDefault = new(0.45f, 0.22f, 0.22f, 1f);
        public static readonly Color BadgeYarg = new(0.15f, 0.45f, 0.55f, 1f);
        public static readonly Color BadgeRb = new(0.72f, 0.18f, 0.22f, 1f);
        public static readonly Color BadgeGh = new(0.55f, 0.35f, 0.12f, 1f);

        public static Color ConfidenceColor(float confidence)
        {
            if (confidence >= 0.95f) return AccentSelect;
            if (confidence >= 0.80f) return new Color(0.40f, 0.92f, 0.55f);
            if (confidence >= 0.50f) return new Color(1f, 0.78f, 0.30f);
            if (confidence >= 0.20f) return new Color(1f, 0.55f, 0.28f);
            if (confidence >= 0.10f) return new Color(0.95f, 0.42f, 0.42f);
            return new Color(0.48f, 0.48f, 0.52f);
        }

        public static string Hex(Color c) =>
            $"#{ColorUtility.ToHtmlStringRGB(c)}";

        public static string FormatSourceBadge(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return "UNKNOWN";

            source = source.Trim().Trim('[', ']');
            string lower = source.ToLowerInvariant();

            if (lower is "yarg" or "official")
                return "YARG";
            if (lower.Contains("rb4"))
                return "RB4 DLC";
            if (lower.Contains("rb3"))
                return "RB3 DLC";
            if (lower.Contains("rb2"))
                return "RB2 DLC";
            if (lower.Contains("rb1") || lower == "rb")
                return "RB DLC";
            if (lower.Contains("ghwt") || lower.Contains("gh5") || lower.Contains("gha")
                || lower.Contains("gh3") || lower.Contains("gh2") || lower.Contains("gh"))
                return source.ToUpperInvariant().Replace('_', ' ');

            return source.Replace('_', ' ').ToUpperInvariant();
        }

        public static Color SourceBadgeColor(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return BadgeDefault;

            string lower = source.Trim().Trim('[', ']').ToLowerInvariant();
            if (lower is "yarg" or "official")
                return BadgeYarg;
            if (lower.Contains("rb"))
                return BadgeRb;
            if (lower.Contains("gh"))
                return BadgeGh;
            return BadgeDefault;
        }
    }
}
