using UnityEngine;



namespace YARG.Menu.Career.Trophy

{

    /// <summary>

    /// Reference layout constants for the Campaign Trophy screen at 1920×1080.

    /// Used by prefab builders, carousel scroll math, and runtime styling.

    /// </summary>

    public static class TrophyLayoutSpec

    {

        public const float ReferenceWidth = 1920f;

        public const float ReferenceHeight = 1080f;



        // Safe area

        public const float HorizontalPadding = 80f;

        public const float ContentTopOffset = 100f;

        public const float FooterHeight = 56f;

        public const float ContentBottomInset = 80f;



        // Title

        public const float TitleTopInset = 28f;

        public const float TitleHeight = 64f;

        public const float TitleFontSize = 36f;



        // Content row split

        public const float SummaryFlex = 0.30f;

        public const float CarouselFlex = 0.70f;

        public const float SummaryMinWidth = 380f;

        public const float SummaryPreferredWidth = 420f;

        public const float CarouselMinWidth = 900f;

        public const float ContentRowSpacing = 24f;



        // Summary card

        public static readonly Vector2 SummaryCardSize = new(420f, 640f);

        public const float SummaryEmblemTopInset = 28f;

        public static readonly Vector2 SummaryEmblemSize = new(300f, 300f);

        public const float SummaryStatsBottomInset = 32f;

        public const float SummaryStatsHorizontalInset = 28f;

        public const float SummaryStatsTopGapFromEmblem = 20f;

        public const float SummaryStatRowSpacing = 12f;

        public static readonly Color EmblemWarmTint = new(1f, 0.95f, 0.85f, 1f);

        public const float SummaryOutlineWidth = 2f;



        // Instrument card

        public static readonly Vector2 InstrumentCardSize = new(360f, 600f);

        public const float HeaderBandHeight = 56f;

        public const float HeaderIconSize = 28f;

        public const float HeaderBannerPadding = 12f;

        public const float InstrumentLabelFontSize = 30f;

        public const float AccoladeFontSize = 17f;

        public const float AccoladeStripHeight = 28f;

        public const float StarsRowTopGap = 8f;

        public const float StarsRowBottomGap = 10f;

        public const float StarSize = 46f;

        public const float StarSpacing = 12f;

        public const float StarGlowScale = 1.3f;

        public const float StarGlowAlpha = 0.25f;

        public const float BodyPadding = 20f;

        public const float BodyBottomPadding = 28f;

        public const float StatColumnSpacing = 16f;

        public const float CardStatRowSpacing = 8f;

        public static readonly Vector2 WatermarkSize = new(240f, 240f);

        public const float WatermarkMargin = 16f;

        public const float WatermarkAlpha = 0.07f;

        public const float BottomGlowHeight = 32f;

        public const float FocusedGlowAlpha = 0.85f;

        public const float UnfocusedGlowAlpha = 0.45f;



        // Carousel

        public const float CardSpacing = 20f;

        public const float CarouselPeekWidth = 72f;

        public const float FocusedScale = 1f;

        public const float UnfocusedScale = 0.90f;

        public const float UnfocusedAlpha = 0.68f;

        public const float ScrollLerpDuration = 0.2f;



        // Typography — stat rows

        public const float SummaryStatLabelSize = 20f;

        public const float SummaryStatValueSize = 26f;

        public const float SummaryStatRowMinHeight = 36f;

        public const float CardStatLabelSize = 16f;

        public const float CardStatValueSize = 15f;

        public const float CardStatRowMinHeight = 36f;

        public const float StatLabelWidthFraction = 0.58f;

        // High Score anchor (bottom of instrument card)
        public const float HighScoreRowHeight = 60f;
        public const float HighScoreLabelSize = 15f;
        public const float HighScoreValueSize = 24f;
        public const float HighScoreSongSize = 12f;
        public const float HighScoreValueRowFraction = 0.6f; // top portion for label + value
        public const float HighScoreTopGap = 10f;
        public const float HighScoreDividerHeight = 1f;
        public static readonly Color HighScoreDividerColor = new(1f, 1f, 1f, 0.12f);
        public static readonly Color HighScoreBgTint = new(0f, 0f, 0f, 0.22f);
        public static readonly Color HighScoreSongColor = new(1f, 1f, 1f, 0.62f);

        public static float CardBodyBottomOffset =>
            BodyBottomPadding + HighScoreRowHeight + HighScoreTopGap;

        // Band "Top Songs" block (bottom of summary/band panel)
        public const float TopSongsSectionHeight = 150f;
        public const float TopSongsHeaderHeight = 26f;
        public const float TopSongsHeaderSize = 18f;
        public const float TopSongRowHeight = 34f;
        public const float TopSongRowSpacing = 6f;
        public const float TopSongLabelSize = 16f;
        public const float TopSongValueSize = 17f;
        public const float TopSongsSectionTopGap = 14f;
        public static readonly Color TopSongsHeaderColor = new(1f, 0.84f, 0.2f, 1f);
        public static readonly Color TopSongLabelColor = Color.white;

        // Colors

        public static readonly Color SummaryLabelWhite = Color.white;

        public static readonly Color CardLabelWhite = Color.white;

        public static readonly Color LabelGrey = new(0.604f, 0.604f, 0.604f, 1f);

        public static readonly Color ValueGold = new(1f, 0.84f, 0.2f, 1f);

        public static readonly Color HighScoreValueColor = ValueGold;

        public static readonly Color AccoladeStripColor = new(0.04f, 0.04f, 0.05f, 0.75f);

        public static readonly Color BackgroundDim = new(0f, 0f, 0f, 0.42f);

        public static readonly Color EmblemGlowColor = new(1f, 0.84f, 0.2f, 0.25f);



        public static float SummaryStatsTopOffset =>

            SummaryEmblemTopInset + SummaryEmblemSize.y + SummaryStatsTopGapFromEmblem;



        public static float BodyTopOffset =>

            HeaderBandHeight + AccoladeStripHeight + StarsRowTopGap + StarSize + StarsRowBottomGap;

    }

}


