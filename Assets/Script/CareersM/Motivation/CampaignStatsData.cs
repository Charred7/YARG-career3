// ============================================================
//  CampaignStatsData.cs
//  YARG — Campaign Stats Screen  (AAA Milestone / End-of-Campaign)
//
//  Pure data container — NO MonoBehaviour, NO Unity engine calls.
//  Designed to be populated by CampaignStatsController and consumed
//  by CampaignStatsView for fully sprite-asset-driven rendering.
//
//  Namespace: YARG.Menu.Career.Motivation
// ============================================================

using System;
using System.Collections.Generic;

namespace YARG.Menu.Career.Motivation
{
    // ──────────────────────────────────────────────────────────────────────────
    //  ENUMS
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Which particle/aura overlay sprite to display behind a player's diamond badge.
    /// Maps directly to the imported sprite assets:
    ///   Flames    → player1_flames.png
    ///   Lightning → player3-lightning.png
    ///   None      → no overlay rendered
    /// </summary>
    public enum PlayerAuraType
    {
        None      = 0,
        Flames    = 1,
        Lightning = 2,
    }

    /// <summary>
    /// Milestone tier awarded at campaign completion.
    /// Drives the left-panel badge_vinyl display and completion banner text.
    /// </summary>
    public enum MilestoneTier
    {
        /// <summary>No milestone unlocked (campaign not fully complete).</summary>
        None         = 0,

        /// <summary>Campaign completed — standard bronze completion.</summary>
        Completed    = 1,

        /// <summary>Campaign completed with all songs having at least 3 stars.</summary>
        SilverRecord = 2,

        /// <summary>Campaign completed with all songs having at least 4 stars.</summary>
        GoldRecord   = 3,

        /// <summary>Campaign 100% complete — all songs 5-starred. Shows badge_vinyl.</summary>
        GoldenVinyl  = 4,
    }

    /// <summary>
    /// Which sub-sprite from the badge_crest_diamond sprite sheet to use
    /// for a player's hero diamond badge. The sheet is a 4-column × 3-row grid
    /// (indices 0–11, left-to-right, top-to-bottom as sliced by the Sprite Editor).
    ///
    ///   Row 0 (indices 0–3):  Ornate gold frames  — top-tier accolades
    ///   Row 1 (indices 4–7):  Standard frames     — mid-tier accolades
    ///   Row 2 (indices 8–11): Plain/outline frames — base / no accolade
    /// </summary>
    public enum DiamondFrameVariant
    {
        OrnateGold_A   = 0,
        OrnateGold_B   = 1,
        OrnateGold_C   = 2,
        OrnateGold_D   = 3,
        Standard_A     = 4,
        Standard_B     = 5,
        Standard_C     = 6,
        Standard_D     = 7,
        Plain_A        = 8,
        Plain_B        = 9,
        Plain_C        = 10,
        Plain_D        = 11,
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  STAT ROW  (single label + value pair for the stat list)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single display-ready stat row shown beneath a player's diamond badge.
    /// Example: Label="Best Streak:"  Value="1,200"
    /// </summary>
    public sealed class StatRowData
    {
        /// <summary>Short label shown on the left side of the row (e.g. "Best Streak:").</summary>
        public string Label;

        /// <summary>Formatted value shown on the right side (e.g. "1,200" or "99.4%").</summary>
        public string Value;

        /// <summary>
        /// Which icon to show at the start of this row.
        /// Drives the sprite selection in CampaignStatsView.
        /// </summary>
        public StatRowIconType IconType;

        public StatRowData(string label, string value, StatRowIconType iconType = StatRowIconType.Trophy)
        {
            Label    = label;
            Value    = value;
            IconType = iconType;
        }
    }

    /// <summary>
    /// Icon type for a stat row — maps to the sprite assets available.
    /// </summary>
    public enum StatRowIconType
    {
        /// <summary>trophy.png — used for score/streak achievements.</summary>
        Trophy       = 0,

        /// <summary>Star_white.png (tinted) — used for star-count stats.</summary>
        Star         = 1,

        /// <summary>CreditSprites double-note sub-sprite — used for song counts.</summary>
        MusicNote    = 2,

        /// <summary>No icon — plain text row.</summary>
        None         = 3,
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  TOP SONG DATA  (band panel — Top Songs list)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single display-ready entry in the band's "Top Songs" list.
    /// </summary>
    public sealed class TopSongData
    {
        /// <summary>1-based rank shown before the song name.</summary>
        public int Rank;

        /// <summary>Song title (may be truncated by the view).</summary>
        public string SongName;

        /// <summary>Formatted band score (e.g. "1,204,880").</summary>
        public string ScoreText;

        public TopSongData(int rank, string songName, string scoreText)
        {
            Rank = rank;
            SongName = songName;
            ScoreText = scoreText;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  MILESTONE DATA  (left panel — Band Highlights)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Data for the left "Band Highlights" panel.
    /// Controls the badge_vinyl display, completion banner, and band-wide stat rows.
    /// </summary>
    public sealed class MilestoneData
    {
        // ── Badge / Banner ────────────────────────────────────────────────────

        /// <summary>
        /// Milestone tier — determines which badge graphic and banner text to show.
        /// GoldenVinyl → badge_vinyl sprite is displayed.
        /// </summary>
        public MilestoneTier Tier = MilestoneTier.Completed;

        /// <summary>
        /// Large bold title shown above the vinyl badge (e.g. "GOLDEN VINYL").
        /// Rendered with GoldShineTextEffect.
        /// </summary>
        public string BadgeTitle = "CAMPAIGN COMPLETE";

        /// <summary>
        /// Text inside the completion banner ribbon below the badge
        /// (e.g. "100% COMPLETE").
        /// </summary>
        public string CompletionBannerText = "COMPLETE";

        /// <summary>
        /// Small label shown below the banner (e.g. "GOLDEN VINYL").
        /// </summary>
        public string SubLabel = string.Empty;

        // ── Band Stat Rows ────────────────────────────────────────────────────

        /// <summary>
        /// Ordered list of band-wide stat rows shown in the lower portion
        /// of the left panel. Typically 2–4 rows.
        /// </summary>
        public List<StatRowData> BandStatRows = new List<StatRowData>();

        /// <summary>
        /// Top-scoring songs for the campaign, shown in a dedicated "Top Songs"
        /// block at the bottom of the band panel. Empty hides the block.
        /// </summary>
        public List<TopSongData> TopSongs = new List<TopSongData>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  PLAYER STATS DATA  (one entry per player card in the right panel)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// All display data for a single player's card in the "Player Legends" grid.
    /// </summary>
    public sealed class PlayerStatsData
    {
        // ── Identity ──────────────────────────────────────────────────────────

        /// <summary>
        /// Player display name (e.g. "PLAYER 1").
        /// Shown in the card header.
        /// </summary>
        public string PlayerName = "PLAYER";

        /// <summary>
        /// Full instrument + player label shown in the card header chip
        /// (e.g. "GUITAR - PLAYER 1").
        /// </summary>
        public string InstrumentLabel = "GUITAR - PLAYER 1";

        /// <summary>
        /// Zero-based instrument index (0=Guitar, 1=Bass, 2=Drums, 3=Vocals).
        /// Used to select the header chip background color and team flavor tint.
        /// </summary>
        public int InstrumentIndex = 0;

        // ── Visual Flavor ─────────────────────────────────────────────────────

        /// <summary>
        /// Which aura overlay sprite to render behind the diamond badge.
        /// Flames (orange team) or Lightning (blue team).
        /// </summary>
        public PlayerAuraType Aura = PlayerAuraType.None;

        /// <summary>
        /// Which sub-sprite from the badge_crest_diamond sheet to use
        /// as the hero diamond frame for this player.
        /// </summary>
        public DiamondFrameVariant DiamondFrame = DiamondFrameVariant.Plain_A;

        // ── Accolade ──────────────────────────────────────────────────────────

        /// <summary>
        /// Title text rendered inside the diamond badge
        /// (e.g. "FULL COMBO KING" or "STREAK MASTER").
        /// Two lines are supported — use '\n' to split.
        /// </summary>
        public string AccoladeTitle = string.Empty;

        /// <summary>
        /// Name of the song on which this player earned their campaign high score.
        /// Shown as a subtitle under the High Score anchor. Empty hides the subtitle.
        /// </summary>
        public string HighScoreSong = string.Empty;

        // ── Star Meter ────────────────────────────────────────────────────────

        /// <summary>
        /// Number of filled stars to display above the card (0–5).
        /// Uses Star_white sprites tinted to the player's team color.
        /// </summary>
        public int StarCount = 0;

        /// <summary>
        /// Total possible stars (used to render empty star slots). Default 5.
        /// </summary>
        public int MaxStars = 5;

        // ── Stat Rows ─────────────────────────────────────────────────────────

        /// <summary>
        /// Ordered list of stat rows shown below the diamond badge.
        /// Typically 3 rows: Best Streak, Accuracy, Full Combos.
        /// </summary>
        public List<StatRowData> Stats = new List<StatRowData>();
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  CAMPAIGN STATS DATA  (root data object pushed to CampaignStatsView)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Root data container for the entire Campaign Stats screen.
    /// Populated by CampaignStatsController and passed to CampaignStatsView.Populate().
    ///
    /// Design contract:
    ///   - This class is a pure data bag — no logic, no Unity calls.
    ///   - CampaignStatsController is responsible for mapping CampaignStats
    ///     (the engine-side model) into this view-friendly structure.
    ///   - CampaignStatsView reads this and builds all UI from it.
    /// </summary>
    public sealed class CampaignStatsData
    {
        // ── Screen Header ─────────────────────────────────────────────────────

        /// <summary>
        /// Main screen title shown in the top banner
        /// (e.g. "CAMPAIGN CONQUERED: NEON FESTIVAL 2026").
        /// </summary>
        public string ScreenTitle = "CAMPAIGN CONQUERED";

        /// <summary>
        /// Internal career ID — used for caching / deduplication.
        /// </summary>
        public string CareerId = string.Empty;

        // ── Left Panel ────────────────────────────────────────────────────────

        /// <summary>
        /// Data for the "Band Highlights" left panel including the vinyl badge
        /// and band-wide stat rows.
        /// </summary>
        public MilestoneData Milestone = new MilestoneData();

        // ── Right Panel ───────────────────────────────────────────────────────

        /// <summary>
        /// Per-player card data for the "Player Legends" right panel.
        /// Supports 1–4 players. The view will clamp to MaxPlayerCards.
        /// </summary>
        public List<PlayerStatsData> Players = new List<PlayerStatsData>();

        // ── Metadata ──────────────────────────────────────────────────────────

        /// <summary>UTC timestamp when this data snapshot was computed.</summary>
        public DateTime ComputedAt = DateTime.UtcNow;

        /// <summary>
        /// True if the campaign was fully completed (all gigs done).
        /// False means the screen is showing a partial/preview state.
        /// </summary>
        public bool IsFullyCompleted = true;
    }
}
