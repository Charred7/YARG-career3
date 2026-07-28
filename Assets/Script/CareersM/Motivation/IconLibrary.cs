using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Procedurally generates ALL icons for the motivation system.
    /// NO EMOJI DEPENDENCIES - everything drawn with Unity UI primitives.
    /// All icons render perfectly on all platforms - no font/emoji issues.
    ///
    /// CRITICAL GUARDRAIL: Every container created via CreateContainer()
    /// receives a LayoutElement with preferredWidth/Height matching its
    /// sizeDelta, and flexibleWidth=0. This prevents parent LayoutGroups
    /// from stretching icons into eggs or causing layout blowouts.
    /// </summary>
    public static class IconLibrary
    {
        // Standard icon sizes (in Unity units)
        public const float SizeSmall  = 18f;
        public const float SizeMedium = 28f;
        public const float SizeLarge  = 40f;
        public const float SizeHuge   = 52f;

        // ─── Instrument Color Tokens ──────────────────────────────────
        public static readonly Color ColorGtrOrange   = new Color(1.000f, 0.450f, 0.050f, 1.00f);
        public static readonly Color ColorBassPurple  = new Color(0.650f, 0.150f, 1.000f, 1.00f);
        public static readonly Color ColorDrumsBlue   = new Color(0.000f, 0.650f, 1.000f, 1.00f);
        public static readonly Color ColorVocalsGreen = new Color(0.100f, 0.950f, 0.400f, 1.00f);

        // Cached sprites for performance
        private static Sprite _circleSprite;
        private static Sprite _starSprite;
        private static Sprite _ringSprite;

        // ──────────────────────────────────────────────────────────────
        //  PUBLIC ICON CREATORS
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a trophy icon for campaign completion celebration.
        /// </summary>
        public static GameObject CreateTrophy(Transform parent, float size, Color color)
        {
            var container = CreateContainer(parent, "Trophy", size);

            // Cup body (main bowl shape)
            var cup = CreateRect(container.transform,
                new Vector2(size * 0.55f, size * 0.50f), color, 0, size * 0.12f);

            // Handles (left and right) - thin rectangles rotated
            var leftHandle = CreateRect(container.transform,
                new Vector2(size * 0.10f, size * 0.25f), color * 0.9f,
                -size * 0.32f, size * 0.15f);
            leftHandle.transform.rotation = Quaternion.Euler(0, 0, -15);

            var rightHandle = CreateRect(container.transform,
                new Vector2(size * 0.10f, size * 0.25f), color * 0.9f,
                size * 0.32f, size * 0.15f);
            rightHandle.transform.rotation = Quaternion.Euler(0, 0, 15);

            // Base pedestal
            CreateRect(container.transform, new Vector2(size * 0.50f, size * 0.20f),
                color * 0.85f, 0, -size * 0.30f);

            // Shine highlight (diagonal white overlay)
            var shine = CreateRect(container.transform,
                new Vector2(size * 0.20f, size * 0.40f),
                new Color(1, 1, 1, 0.25f), -size * 0.13f, size * 0.15f);
            shine.transform.rotation = Quaternion.Euler(0, 0, -30);

            return container;
        }

        /// <summary>
        /// Creates a star icon for ratings and achievements.
        /// </summary>
        public static GameObject CreateStar(Transform parent, float size, Color color)
        {
            var container = CreateContainer(parent, "Star", size);

            // Star shape using TextMeshPro character (reliable across all platforms)
            var text = container.AddComponent<TextMeshProUGUI>();
            text.text = "\u2605"; // ★ Unicode star
            text.fontSize = size * 0.8f;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;

            // Glow effect behind star
            var glow = CreateCircle(container.transform, size * 1.5f,
                new Color(color.r, color.g, color.b, 0.12f), 0, 0);
            glow.transform.SetAsFirstSibling();

            return container;
        }

        /// <summary>
        /// Creates a flame/fire icon for streaks.
        /// </summary>
        public static GameObject CreateFlame(Transform parent, float size, Color baseColor)
        {
            var container = CreateContainer(parent, "Flame", size);

            Color outerColor = new Color(1f, 0.35f, 0.05f);  // Red-orange
            Color innerColor = new Color(1f, 0.85f, 0.15f);  // Yellow-gold

            // Outer flame (larger, behind)
            var outer = CreateCircle(container.transform, size * 0.75f, outerColor, 0, 0);
            outer.transform.localScale = new Vector3(1f, 1.35f, 1f);

            // Inner flame (smaller, brighter)
            var inner = CreateCircle(container.transform, size * 0.45f, innerColor, 0, -size * 0.04f);
            inner.transform.localScale = new Vector3(1f, 1.4f, 1f);

            return container;
        }

        /// <summary>
        /// Creates a crown icon for conquered campaigns.
        /// </summary>
        public static GameObject CreateCrown(Transform parent, float size, Color goldColor)
        {
            var container = CreateContainer(parent, "Crown", size);

            // Base band
            CreateRect(container.transform, new Vector2(size * 0.82f, size * 0.22f),
                goldColor, 0, -size * 0.30f);

            // Three peaks (stretched circles)
            float[] peakX = { -size * 0.27f, 0f, size * 0.27f };
            Color[] peakColors = {
                goldColor * 0.95f,
                goldColor * 1.1f,  // Center peak brighter
                goldColor * 0.95f
            };

            for (int i = 0; i < 3; i++)
            {
                var peak = CreateCircle(container.transform, size * 0.24f,
                    peakColors[i], peakX[i], size * 0.06f);
                peak.transform.localScale = new Vector3(0.85f, 1.35f, 1f);
            }

            // Jewels on peaks
            Color[] jewelColors = {
                new Color(0.3f, 0.6f, 1f),    // Blue
                new Color(0.8f, 0.3f, 0.9f),  // Purple
                new Color(0.3f, 0.6f, 1f)     // Blue
            };

            for (int i = 0; i < 3; i++)
            {
                var jewel = CreateCircle(container.transform, size * 0.10f,
                    jewelColors[i], peakX[i], size * 0.19f);

                // Jewel shine dot
                CreateCircle(jewel.transform, size * 0.035f,
                    new Color(1, 1, 1, 0.7f), -size * 0.02f, size * 0.02f);
            }

            return container;
        }

        /// <summary>
        /// Creates a guitar icon for instrument/music stats.
        /// </summary>
        public static GameObject CreateGuitar(Transform parent, float size, Color color)
        {
            var container = CreateContainer(parent, "Guitar", size);

            // Body (ellipse)
            var body = CreateCircle(container.transform, size * 0.52f, color, 0, -size * 0.10f);
            body.transform.localScale = new Vector3(0.95f, 1.15f, 1f);

            // Sound hole
            CreateCircle(container.transform, size * 0.18f, color * 0.35f, 0, -size * 0.10f);

            // Neck
            CreateRect(container.transform, new Vector2(size * 0.14f, size * 0.55f),
                color * 0.9f, 0, size * 0.22f);

            // Headstock
            var head = CreateRect(container.transform, new Vector2(size * 0.24f, size * 0.18f),
                color * 0.88f, 0, size * 0.43f);
            head.transform.rotation = Quaternion.Euler(0, 0, 5);

            // Tuning pegs
            for (int i = 0; i < 3; i++)
            {
                float pegX = (i - 1) * size * 0.07f;
                CreateCircle(container.transform, size * 0.04f,
                    color * 0.75f, pegX, size * 0.38f);
            }

            return container;
        }

        /// <summary>
        /// Creates a target/bullseye icon for accuracy.
        /// </summary>
        public static GameObject CreateTarget(Transform parent, float size, Color color)
        {
            var container = CreateContainer(parent, "Target", size);

            // Outer ring
            CreateCircle(container.transform, size * 0.90f, color * 0.55f, 0, 0);
            CreateCircle(container.transform, size * 0.72f, new Color(0, 0, 0, 0.75f), 0, 0);

            // Middle ring
            CreateCircle(container.transform, size * 0.62f, color * 0.75f, 0, 0);
            CreateCircle(container.transform, size * 0.48f, new Color(0, 0, 0, 0.75f), 0, 0);

            // Inner circle (bullseye)
            CreateCircle(container.transform, size * 0.38f, color, 0, 0);

            // Center dot (red)
            CreateCircle(container.transform, size * 0.13f,
                new Color(0.9f, 0.25f, 0.15f), 0, 0);

            return container;
        }

        /// <summary>
        /// Creates a checkmark icon.
        /// </summary>
        public static GameObject CreateCheckmark(Transform parent, float size, Color color)
        {
            var container = CreateContainer(parent, "Checkmark", size);

            // Short arm (bottom-left to center)
            var shortArm = CreateRect(container.transform,
                new Vector2(size * 0.13f, size * 0.34f), color,
                -size * 0.14f, -size * 0.08f);
            shortArm.transform.rotation = Quaternion.Euler(0, 0, -48);

            // Long arm (center to top-right)
            var longArm = CreateRect(container.transform,
                new Vector2(size * 0.13f, size * 0.60f), color,
                size * 0.06f, size * 0.06f);
            longArm.transform.rotation = Quaternion.Euler(0, 0, 42);

            return container;
        }

        /// <summary>
        /// Creates a lightning/energy icon.
        /// </summary>
        public static GameObject CreateLightning(Transform parent, float size, Color color)
        {
            var container = CreateContainer(parent, "Lightning", size);

            // Top bolt section
            var top = CreateRect(container.transform,
                new Vector2(size * 0.35f, size * 0.40f), color,
                -size * 0.08f, size * 0.20f);

            // Bottom bolt section
            var bottom = CreateRect(container.transform,
                new Vector2(size * 0.35f, size * 0.40f), color,
                size * 0.08f, -size * 0.20f);

            // Glow aura
            var glow = CreateCircle(container.transform, size * 1.4f,
                new Color(color.r, color.g, color.b, 0.08f), 0, 0);
            glow.transform.SetAsFirstSibling();

            return container;
        }

        /// <summary>
        /// Creates a music note icon.
        /// </summary>
        public static GameObject CreateMusicNote(Transform parent, float size, Color color)
        {
            var container = CreateContainer(parent, "MusicNote", size);

            // Note head (tilted circle)
            var head = CreateCircle(container.transform, size * 0.24f, color,
                -size * 0.14f, -size * 0.22f);
            head.transform.localScale = new Vector3(1.2f, 0.85f, 1f);
            head.transform.rotation = Quaternion.Euler(0, 0, -18);

            // Stem
            CreateRect(container.transform, new Vector2(size * 0.09f, size * 0.55f),
                color, size * 0.04f, size * 0.12f);

            // Flag (small arc at top of stem)
            CreateCircle(container.transform, size * 0.15f, color,
                size * 0.16f, size * 0.34f);

            return container;
        }

        /// <summary>
        /// Creates a star-filled display (multiple stars) for star counts.
        /// </summary>
        public static GameObject CreateStarDisplay(Transform parent, int starCount, float starSize, Color color)
        {
            int displayCount = Mathf.Min(starCount, 5);
            float totalWidth = displayCount * starSize + (displayCount - 1) * 2f;
            float startX = -totalWidth / 2f + starSize / 2f;

            var container = new GameObject("StarDisplay", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            var rt = container.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(totalWidth, starSize);
            rt.anchoredPosition = Vector2.zero;

            for (int i = 0; i < displayCount; i++)
            {
                var star = CreateStar(container.transform, starSize, color);
                var starRt = star.GetComponent<RectTransform>();
                starRt.anchoredPosition = new Vector2(startX + i * (starSize + 2f), 0);
            }

            return container;
        }

        // ──────────────────────────────────────────────────────────────
        //  GEOMETRIC BADGE — CRISP DIAMOND FRAME
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a beautiful, rigid diamond-frame badge using nested
        /// square UI Images rotated at 45 degrees, featuring a crisp
        /// Outline component. Absolutely no blurred, unscaled soft-circle
        /// drop sprites.
        ///
        /// The badge is locked to its exact size via LayoutElement with
        /// forcedMinimum/preferred matching the sizeDelta, so parent
        /// LayoutGroups cannot stretch it.
        /// </summary>
        /// <param name="parent">Parent transform to attach to.</param>
        /// <param name="size">Overall badge size in Unity units (e.g. 120).</param>
        /// <param name="accentColor">Accent color for the diamond layers.</param>
        /// <param name="title">Text label centered inside the badge.</param>
        public static GameObject CreateGeometricBadge(Transform parent, float size,
            Color accentColor, string title)
        {
            var container = CreateContainer(parent, "GeometricBadge", size);

            // ── Outer diamond (largest, most transparent) ─────────────
            var outerDiamond = CreateRect(container.transform,
                new Vector2(size * 0.85f, size * 0.85f),
                accentColor * new Color(1f, 1f, 1f, 0.25f), 0, 0);
            outerDiamond.name = "OuterDiamond";
            outerDiamond.transform.rotation = Quaternion.Euler(0, 0, 45);

            // ── Middle diamond ─────────────────────────────────────────
            var middleDiamond = CreateRect(container.transform,
                new Vector2(size * 0.70f, size * 0.70f),
                accentColor * new Color(1f, 1f, 1f, 0.55f), 0, 0);
            middleDiamond.name = "MiddleDiamond";
            middleDiamond.transform.rotation = Quaternion.Euler(0, 0, 45);

            // ── Inner diamond (solid, with crisp Outline) ──────────────
            var innerDiamond = CreateRect(container.transform,
                new Vector2(size * 0.55f, size * 0.55f),
                accentColor, 0, 0);
            innerDiamond.name = "InnerDiamond";
            innerDiamond.transform.rotation = Quaternion.Euler(0, 0, 45);

            // Crisp outline on the inner diamond for geometric flair
            var outline = innerDiamond.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.30f);
            outline.effectDistance = new Vector2(2f, -2f);

            // ── Title text centered ────────────────────────────────────
            var textGo = new GameObject("BadgeTitle", typeof(RectTransform));
            textGo.transform.SetParent(container.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(size * 0.65f, size * 0.30f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = size * 0.18f;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;

            return container;
        }

        // ──────────────────────────────────────────────────────────────
        //  ACCOLADE BADGE SYSTEM
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a multi-layered geometric accolade badge.
        /// All graphics are procedurally built from Unity UI primitives —
        /// no external file dependencies.
        /// </summary>
        /// <param name="parent">Parent transform to attach to.</param>
        /// <param name="tier">The accolade tier determines the visual theme.</param>
        /// <param name="size">Overall badge size in Unity units.</param>
        /// <param name="instrumentColor">Instrument color for themed badges (PerfectStrings).</param>
        public static GameObject CreateAccoladeBadge(Transform parent, AccoladeTier tier,
            float size, Color instrumentColor)
        {
            if (tier == AccoladeTier.None) return null;

            var container = CreateContainer(parent, $"Accolade_{tier}", size);

            switch (tier)
            {
                case AccoladeTier.CampaignMVP:
                    BuildCampaignMvpBadge(container.transform, size);
                    break;
                case AccoladeTier.PerfectStrings:
                    BuildPerfectStringsBadge(container.transform, size, instrumentColor);
                    break;
                case AccoladeTier.ImmortalStreak:
                    BuildImmortalStreakBadge(container.transform, size);
                    break;
                case AccoladeTier.SharpShooter:
                    BuildSurgicalPrecisionBadge(container.transform, size);
                    break;
            }

            return container;
        }

        // ─── Accolade: Campaign MVP ───────────────────────────────────

        private static void BuildCampaignMvpBadge(Transform parent, float size)
        {
            Color goldBright = new Color(1f, 0.95f, 0.6f, 1f);
            Color goldPrimary = new Color(1f, 0.78f, 0f, 1f);
            Color goldDark    = new Color(0.7f, 0.5f, 0.08f, 1f);

            // Glow aura behind everything
            var glow = CreateCircle(parent, size * 0.95f,
                new Color(goldBright.r, goldBright.g, goldBright.b, 0.14f), 0, 0);
            glow.transform.SetAsFirstSibling();

            // Outer crown ring (thin circle outline)
            CreateRingOutline(parent, size * 0.78f, size * 0.04f, goldDark, 0, 0);

            // Crown base band
            CreateRect(parent, new Vector2(size * 0.62f, size * 0.15f),
                goldPrimary, 0, -size * 0.18f);

            // Three crown peaks (small stretched circles)
            float[] peakX = { -size * 0.20f, 0f, size * 0.20f };
            for (int i = 0; i < 3; i++)
            {
                var peak = CreateCircle(parent, size * 0.18f,
                    i == 1 ? goldBright : goldPrimary, peakX[i], size * 0.04f);
                peak.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
            }

            // Jewel dots on peaks
            for (int i = 0; i < 3; i++)
            {
                CreateCircle(parent, size * 0.07f,
                    i == 1
                        ? new Color(1f, 0.4f, 0.3f)    // Center: ruby
                        : new Color(0.3f, 0.6f, 1f),    // Sides: sapphire
                    peakX[i], size * 0.14f);
            }

            // "MVP" text with outline
            var textGo = new GameObject("MvpText", typeof(RectTransform));
            textGo.transform.SetParent(parent, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0, -size * 0.03f);
            textRt.sizeDelta = new Vector2(size * 0.70f, size * 0.35f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "MVP";
            tmp.fontSize = size * 0.38f;
            tmp.color = new Color(1f, 0.97f, 0.80f, 1f);  // White-gold
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            // Outline on text for depth
            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = goldPrimary;
            outline.effectDistance = new Vector2(size * 0.03f, -size * 0.03f);
        }

        // ─── Accolade: Perfect Strings ────────────────────────────────

        private static void BuildPerfectStringsBadge(Transform parent, float size,
            Color instrumentColor)
        {
            Color fillColor   = instrumentColor;
            Color borderColor = instrumentColor * 0.7f;

            // Trail circles behind the shield (staggered, fading)
            float[] trailOffsets = { -size * 0.16f, size * 0.16f, 0f };
            float[] trailAlphas  = { 0.15f, 0.15f, 0.25f };
            for (int i = 0; i < 3; i++)
            {
                var trail = CreateCircle(parent, size * 0.55f,
                    new Color(instrumentColor.r, instrumentColor.g,
                        instrumentColor.b, trailAlphas[i]),
                    trailOffsets[i], size * 0.04f);
                trail.transform.SetAsFirstSibling();
            }

            // Shield body (diamond-ish shape via crossed rotated rects)
            var shield = CreateShieldShape(parent, size * 0.65f, fillColor, borderColor);

            // Inner highlight stripe
            var highlight = CreateRect(parent,
                new Vector2(size * 0.08f, size * 0.35f),
                new Color(1, 1, 1, 0.2f), -size * 0.13f, size * 0.02f);
            highlight.transform.rotation = Quaternion.Euler(0, 0, -20);

            // Small star in center
            var star = new GameObject("ShieldStar", typeof(RectTransform));
            star.transform.SetParent(parent, false);
            var starRt = star.GetComponent<RectTransform>();
            starRt.anchoredPosition = Vector2.zero;
            starRt.sizeDelta = new Vector2(size * 0.28f, size * 0.28f);
            var starTmp = star.AddComponent<TextMeshProUGUI>();
            starTmp.text = "\u2605";
            starTmp.fontSize = size * 0.25f;
            starTmp.color = new Color(1, 1, 1, 0.9f);
            starTmp.alignment = TextAlignmentOptions.Center;
            starTmp.raycastTarget = false;
        }

        // ─── Accolade: Immortal Streak ────────────────────────────────

        private static void BuildImmortalStreakBadge(Transform parent, float size)
        {
            Color magmaOuter = new Color(1f, 0.25f, 0.05f, 1f);
            Color magmaInner = new Color(1f, 0.55f, 0.10f, 1f);
            Color magmaCore  = new Color(1f, 0.85f, 0.20f, 1f);

            // Outer magma glow ring
            var outerGlow = CreateCircle(parent, size * 0.90f,
                new Color(magmaOuter.r, magmaOuter.g, magmaOuter.b, 0.12f), 0, 0);
            outerGlow.transform.SetAsFirstSibling();

            // Ring outline with magma color
            CreateRingOutline(parent, size * 0.78f, size * 0.05f, magmaOuter, 0, 0);

            // Inner ring
            CreateRingOutline(parent, size * 0.58f, size * 0.035f, magmaInner, 0, 0);

            // Flame shapes (3 stretched ellipses at different angles)
            for (int i = 0; i < 3; i++)
            {
                float angle = (i - 1) * 35f;
                float scaleY = 1.3f + i * 0.2f;
                var flame = CreateCircle(parent, size * 0.32f,
                    i == 0 ? magmaOuter : (i == 1 ? magmaInner : magmaCore),
                    Mathf.Sin(angle * Mathf.Deg2Rad) * size * 0.12f,
                    Mathf.Cos(angle * Mathf.Deg2Rad) * size * 0.12f);
                flame.transform.localScale = new Vector3(0.55f, scaleY, 1f);
                flame.transform.rotation = Quaternion.Euler(0, 0, angle);
            }

            // "IMMORTAL" text with shadow
            var textGo = new GameObject("ImmortalText", typeof(RectTransform));
            textGo.transform.SetParent(parent, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchoredPosition = Vector2.zero;
            textRt.sizeDelta = new Vector2(size * 0.70f, size * 0.28f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "IMMORTAL";
            tmp.fontSize = size * 0.16f;
            tmp.color = magmaCore;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.characterSpacing = 3f;

            var shadow = textGo.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.6f, 0.08f, 0f, 0.7f);
            shadow.effectDistance = new Vector2(size * 0.025f, -size * 0.025f);
        }

        // ─── Accolade: Surgical Precision ─────────────────────────────

        private static void BuildSurgicalPrecisionBadge(Transform parent, float size)
        {
            Color cyanBright  = new Color(0.15f, 0.95f, 0.95f, 1f);
            Color cyanMid     = new Color(0.10f, 0.75f, 0.85f, 1f);
            Color cyanDim     = new Color(0.08f, 0.50f, 0.60f, 0.8f);

            // Outermost ring
            CreateRingOutline(parent, size * 0.82f, size * 0.025f, cyanDim, 0, 0);

            // Middle ring
            CreateRingOutline(parent, size * 0.62f, size * 0.025f, cyanMid, 0, 0);

            // Inner ring (thicker, brighter)
            CreateRingOutline(parent, size * 0.42f, size * 0.032f, cyanBright, 0, 0);

            // Crosshair lines (+ shape)
            CreateCrosshair(parent, size * 0.78f, size * 0.025f, cyanMid);

            // Precision dots at ring intersections (N, S, E, W)
            float dotSize = size * 0.05f;
            float dotDist = size * 0.39f;
            CreateCircle(parent, dotSize, cyanBright, 0, dotDist);       // Top
            CreateCircle(parent, dotSize, cyanBright, 0, -dotDist);      // Bottom
            CreateCircle(parent, dotSize, cyanBright, dotDist, 0);       // Right
            CreateCircle(parent, dotSize, cyanBright, -dotDist, 0);      // Left

            // Center target dot
            CreateCircle(parent, size * 0.10f, cyanBright, 0, 0);

            // Small digital "99%" text
            var textGo = new GameObject("PrecisionText", typeof(RectTransform));
            textGo.transform.SetParent(parent, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchoredPosition = new Vector2(0, -size * 0.32f);
            textRt.sizeDelta = new Vector2(size * 0.55f, size * 0.18f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "99%";
            tmp.fontSize = size * 0.18f;
            tmp.color = cyanBright;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.characterSpacing = 2f;
        }

        // ──────────────────────────────────────────────────────────────
        //  PRIVATE PRIMITIVE HELPERS
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Creates a container GameObject with a locked size.
        /// CRITICAL: Adds a LayoutElement with preferredWidth/Height
        /// matching the sizeDelta and flexibleWidth=0 so parent
        /// LayoutGroups CANNOT stretch this icon.
        /// </summary>
        private static GameObject CreateContainer(Transform parent, string name, float size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;

            // ── CRITICAL GUARDRAIL: LayoutElement prevents stretching ──
            var le = obj.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.flexibleWidth = 0f;

            // Lock aspect ratio so layout groups don't stretch icons into eggs
            var arf = obj.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            arf.aspectRatio = 1f;

            return obj;
        }

        private static GameObject CreateRect(Transform parent, Vector2 size, Color color, float x, float y)
        {
            var obj = new GameObject("Rect", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);

            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = new Vector2(x, y);

            var img = obj.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            return obj;
        }

        private static GameObject CreateCircle(Transform parent, float diameter, Color color, float x, float y)
        {
            var obj = CreateRect(parent, new Vector2(diameter, diameter), color, x, y);
            obj.name = "Circle";

            var img = obj.GetComponent<Image>();
            img.sprite = GetCircleSprite();

            return obj;
        }

        /// <summary>
        /// Creates a ring/outline — a circle with a hollow center.
        /// Built as a circle Image using a ring sprite.
        /// </summary>
        private static GameObject CreateRingOutline(Transform parent, float diameter,
            float thickness, Color color, float x, float y)
        {
            var obj = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);

            var rt = obj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(diameter, diameter);
            rt.anchoredPosition = new Vector2(x, y);

            var img = obj.GetComponent<Image>();
            img.sprite = GetRingSprite(thickness / diameter);
            img.color = color;
            img.raycastTarget = false;

            return obj;
        }

        /// <summary>
        /// Creates a crosshair (+ shape) from four thin rectangles.
        /// </summary>
        private static GameObject CreateCrosshair(Transform parent, float size,
            float thickness, Color color)
        {
            var container = CreateContainer(parent, "Crosshair", size);

            // Vertical line
            CreateRect(container.transform, new Vector2(thickness, size), color, 0, 0);
            // Horizontal line
            CreateRect(container.transform, new Vector2(size, thickness), color, 0, 0);

            return container;
        }

        /// <summary>
        /// Creates a shield/diamond shape from crossed rotated rectangles.
        /// </summary>
        private static GameObject CreateShieldShape(Transform parent, float size,
            Color fillColor, Color borderColor)
        {
            var container = CreateContainer(parent, "Shield", size);

            // Two thick crossed rectangles rotated 45° produce a diamond
            var hStrip = CreateRect(container.transform,
                new Vector2(size * 0.95f, size * 0.55f), fillColor, 0, 0);
            hStrip.transform.rotation = Quaternion.Euler(0, 0, 45);

            var vStrip = CreateRect(container.transform,
                new Vector2(size * 0.55f, size * 0.95f), fillColor * 0.85f, 0, 0);
            vStrip.transform.rotation = Quaternion.Euler(0, 0, 45);

            // Border strips (slightly larger, behind)
            var hBorder = CreateRect(container.transform,
                new Vector2(size * 1.00f, size * 0.60f), borderColor, 0, 0);
            hBorder.transform.rotation = Quaternion.Euler(0, 0, 45);
            hBorder.transform.SetAsFirstSibling();

            var vBorder = CreateRect(container.transform,
                new Vector2(size * 0.60f, size * 1.00f), borderColor, 0, 0);
            vBorder.transform.rotation = Quaternion.Euler(0, 0, 45);
            vBorder.transform.SetAsFirstSibling();

            return container;
        }

        // ──────────────────────────────────────────────────────────────
        //  PROCEDURAL SPRITES
        // ──────────────────────────────────────────────────────────────

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            int res = 64;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var pixels = new Color[res * res];

            float center = res / 2f;
            float radius = center - 1f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    pixels[y * res + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f), 100f);

            return _circleSprite;
        }

        /// <summary>
        /// Generates a ring sprite where only pixels within a certain
        /// thickness band of the outer edge are opaque.
        /// </summary>
        private static Sprite GetRingSprite(float thicknessFraction)
        {
            // Clamp thickness to reasonable range
            float tf = Mathf.Clamp(thicknessFraction, 0.02f, 0.25f);

            int res = 64;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var pixels = new Color[res * res];

            float center = res / 2f;
            float outerRadius = center - 1f;
            float innerRadius = outerRadius * (1f - tf);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Anti-aliased ring: 1 inside the band, fade at edges
                    float outerAlpha = Mathf.Clamp01(outerRadius - dist + 0.5f);
                    float innerAlpha = Mathf.Clamp01(dist - innerRadius + 0.5f);
                    float alpha = outerAlpha * innerAlpha;

                    pixels[y * res + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            var sprite = Sprite.Create(tex, new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "RingSprite";

            return sprite;
        }
    }

    /// <summary>
    /// Accolade tiers displayed on player cards.
    /// Priority-ordered: higher values = checked first, more impressive.
    /// </summary>
    public enum AccoladeTier
    {
        /// <summary>No accolade earned.</summary>
        None = 0,

        // ── Tier 1: LEGENDARY ──────────────────────────────────
        /// <summary>FullCombos >= 5 — OrnateGold_A + Flames + "FULL COMBO KING"</summary>
        CampaignMVP = 1,
        /// <summary>At least 1 Gold star song — OrnateGold_B + Flames + "GOLDEN FINGERS"</summary>
        GoldenFingers = 2,
        /// <summary>At least 1 song with 100% accuracy — OrnateGold_C + Flames + "FLAWLESS"</summary>
        Perfectionist = 3,

        // ── Tier 2: ELITE ──────────────────────────────────────
        /// <summary>FullCombos >= 1 — OrnateGold_D + Flames + "PERFECT STRINGS"</summary>
        PerfectStrings = 4,
        /// <summary>At least 3 Expert songs played — Standard_A + Lightning + "EXPERT VETERAN"</summary>
        ExpertVeteran = 5,
        /// <summary>AverageAccuracy >= 0.95 — Standard_B + Lightning + "SHARP SHOOTER"</summary>
        SharpShooter = 6,

        // ── Tier 3: SOLID ──────────────────────────────────────
        /// <summary>LongestStreak >= 1000 — Standard_C + Lightning + "STREAK MASTER"</summary>
        ImmortalStreak = 7,
        /// <summary>All campaign songs played — Standard_D + Lightning + "ROAD WARRIOR"</summary>
        RoadWarrior = 8,
        /// <summary>At least 50% of songs 4★+ — Plain_A + None + "RISING STAR"</summary>
        RisingStar = 9,

        // ── Tier 4: PARTICIPATION ──────────────────────────────
        /// <summary>At least 50% of songs played — Plain_B + None + "ROCK ENTHUSIAST"</summary>
        RockEnthusiast = 10,
        /// <summary>At least 1 song played — Plain_C + None + "SHOW OPENER"</summary>
        ShowOpener = 11,
    }
}
