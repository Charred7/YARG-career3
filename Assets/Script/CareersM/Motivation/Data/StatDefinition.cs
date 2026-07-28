using System;
using System.Collections.Generic;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Categories of stats that can be displayed for a player.
    /// StatTier determines the impressiveness hierarchy.
    /// </summary>
    public enum StatType
    {
        // ── Tier 1: ELITE ──────────────────────────────────────
        /// <summary>Number of songs with Gold stars (StarGold or higher).</summary>
        GoldStarSongs,
        /// <summary>Full Combos on Expert difficulty.</summary>
        ExpertFCs,
        /// <summary>Number of songs with 100% accuracy.</summary>
        PerfectAccuracy,
        /// <summary>Number of songs with 99%+ accuracy.</summary>
        NearPerfectAccuracy,
        /// <summary>Number of Full Combos (IsFc == true).</summary>
        FullCombos,

        // ── Tier 2: ADVANCED ───────────────────────────────────
        /// <summary>Highest individual score on any song.</summary>
        HighScore,
        /// <summary>Number of 5-star performances.</summary>
        FiveStarCount,
        /// <summary>Number of songs with 95%+ accuracy.</summary>
        HighAccuracy,
        /// <summary>Number of songs played on Expert.</summary>
        ExpertSongsPlayed,
        /// <summary>Number of 4+ star performances.</summary>
        FourStarCount,

        // ── Tier 3: SOLID ──────────────────────────────────────
        /// <summary>Average accuracy percentage.</summary>
        AverageAccuracy,
        /// <summary>
        /// Unused by the trophy screen (was mislabeled MAX NotesHit).
        /// Retained so existing serialized/legacy references compile.
        /// </summary>
        LongestStreak,
        /// <summary>Number of songs played on Hard or Expert.</summary>
        HardPlusSongsPlayed,
        /// <summary>Number of songs with 90%+ accuracy.</summary>
        NinetyPlusAccuracy,
        /// <summary>Total score summed across all songs.</summary>
        TotalScore,
        /// <summary>Number of 3+ star performances.</summary>
        ThreeStarCount,

        // ── Tier 4: FOUNDATION ─────────────────────────────────
        /// <summary>Number of play sessions (not unique songs).</summary>
        SongsPlayed,
        /// <summary>Total notes hit across all songs.</summary>
        TotalNotesHit,
        /// <summary>Number of songs with 80%+ accuracy.</summary>
        EightyPlusAccuracy,
        /// <summary>Number of songs with 70%+ accuracy.</summary>
        SeventyPlusAccuracy,
        /// <summary>Number of songs played on Medium.</summary>
        MediumSongsPlayed,
        /// <summary>Number of songs played on Easy.</summary>
        EasySongsPlayed,
        /// <summary>Average score per song.</summary>
        AverageScore,
        /// <summary>Total notes missed (framed positively).</summary>
        NotesMissed,
        /// <summary>Distinct campaign songs with at least one play.</summary>
        UniqueSongsCleared,
    }

    /// <summary>
    /// Impressiveness tier for stat selection algorithm.
    /// Lower tier number = more impressive.
    /// </summary>
    public enum StatTier
    {
        Elite       = 1,
        Advanced    = 2,
        Solid       = 3,
        Foundation  = 4,
    }

    /// <summary>
    /// A single computed stat ready for display.
    /// </summary>
    public class PlayerStat
    {
        public StatType Type;
        public float Value;
        public string DisplayText;
        public StatTier Tier; // Impressiveness tier for weighted random selection
        public int SortPriority; // Higher = more impressive within same tier
    }

    /// <summary>
    /// Stats for one trophy instrument card (machine-wide best-per-song on that instrument).
    /// </summary>
    public class ProfileCampaignStats
    {
        public string PlayerName;
        public Guid ProfileId;
        /// <summary>0=Guitar, 1=Bass, 2=Drums, 3=Vocals.</summary>
        public int InstrumentIndex;
        public List<PlayerStat> AllStats = new();
        public List<PlayerStat> BestStats = new();

        /// <summary>
        /// Name of the song on which this card's high score was earned.
        /// </summary>
        public string HighScoreSongName = string.Empty;

        /// <summary>
        /// Best star rating by instrument index. For instrument cards, only
        /// <see cref="InstrumentIndex"/> is typically populated.
        /// </summary>
        public int[] BestStarsByInstrument = new int[4];
    }

    /// <summary>
    /// A single top-scoring band performance for the campaign's Top Songs list.
    /// </summary>
    public class BandTopSong
    {
        public string SongName;
        public int BandScore;
        public YARG.Core.Game.StarAmount BandStars;
    }

    /// <summary>
    /// Band-wide stats across the campaign.
    /// </summary>
    public class BandCampaignStats
    {
        public int TotalSongs;
        public int TotalSongsWithScores;
        public int TotalBandScore;
        public int TotalStarsEarned;
        public int SongsWithFiveStars;
        public int SongsWithGoldStars;
        public int SongsWithFourStars;
        public int TotalPlaySessions;

        /// <summary>
        /// Highest-scoring band performances for the campaign, sorted descending.
        /// Populated by <c>StatCalculator</c>; display layer clamps to what it needs.
        /// </summary>
        public List<BandTopSong> TopSongs = new();
    }

    /// <summary>
    /// Complete stats snapshot for a campaign.
    /// </summary>
    public class CampaignStats
    {
        public string CareerId;
        public string CareerName;
        public bool IsCompleted;
        public BandCampaignStats BandStats = new();
        public List<ProfileCampaignStats> ProfileStats = new();
    }

    /// <summary>
    /// Lookup extension for stat tiers.
    /// </summary>
    public static class StatTierExtensions
    {
        public static StatTier GetTier(this StatType type) => type switch
        {
            // Tier 1 — Elite
            StatType.GoldStarSongs       => StatTier.Elite,
            StatType.ExpertFCs           => StatTier.Elite,
            StatType.PerfectAccuracy     => StatTier.Elite,
            StatType.NearPerfectAccuracy => StatTier.Elite,
            StatType.FullCombos          => StatTier.Elite,

            // Tier 2 — Advanced
            StatType.HighScore           => StatTier.Advanced,
            StatType.FiveStarCount       => StatTier.Advanced,
            StatType.HighAccuracy        => StatTier.Advanced,
            StatType.ExpertSongsPlayed   => StatTier.Advanced,
            StatType.FourStarCount       => StatTier.Advanced,

            // Tier 3 — Solid
            StatType.AverageAccuracy     => StatTier.Solid,
            StatType.LongestStreak       => StatTier.Solid,
            StatType.HardPlusSongsPlayed => StatTier.Solid,
            StatType.NinetyPlusAccuracy  => StatTier.Solid,
            StatType.TotalScore          => StatTier.Solid,
            StatType.ThreeStarCount      => StatTier.Solid,
            StatType.UniqueSongsCleared  => StatTier.Solid,

            // Tier 4 — Foundation
            StatType.SongsPlayed         => StatTier.Foundation,
            StatType.TotalNotesHit       => StatTier.Foundation,
            StatType.EightyPlusAccuracy  => StatTier.Foundation,
            StatType.SeventyPlusAccuracy => StatTier.Foundation,
            StatType.MediumSongsPlayed   => StatTier.Foundation,
            StatType.EasySongsPlayed     => StatTier.Foundation,
            StatType.AverageScore        => StatTier.Foundation,
            StatType.NotesMissed         => StatTier.Foundation,

            _ => StatTier.Foundation,
        };
    }
}