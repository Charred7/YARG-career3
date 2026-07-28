using System;
using System.Collections.Generic;
using UnityEngine;
using YARG.Core;
using YARG.Scores;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Formats stats into engaging display strings.
    /// Icons are now added visually via IconLibrary, not via emoji text.
    /// </summary>
    public static class StatFormatter
    {
        /// <summary>
        /// Formats a single stat into its display text (text only, no emoji).
        /// Icons are rendered separately via IconLibrary.
        /// </summary>
        public static string FormatStat(PlayerStat stat)
        {
            return stat.Type switch
            {
                // Tier 1
                StatType.GoldStarSongs       => $"{stat.Value:F0} Gold Star Songs",
                StatType.ExpertFCs           => $"{stat.Value:F0} Expert Full Combos",
                StatType.PerfectAccuracy     => $"{stat.Value:F0} Perfect Songs",
                StatType.NearPerfectAccuracy => $"{stat.Value:F0} Songs 99%+",
                StatType.FullCombos          => $"{stat.Value:F0} Full Combos",

                // Tier 2
                StatType.HighScore           => $"High Score: {FormatScore((int)stat.Value)}",
                StatType.FiveStarCount       => $"5-Star Master ({stat.Value:F0})",
                StatType.HighAccuracy        => $"95%+ on {stat.Value:F0} Songs",
                StatType.ExpertSongsPlayed   => $"{stat.Value:F0} Expert Songs",
                StatType.FourStarCount       => $"{stat.Value:F0} Songs 4★+",

                // Tier 3
                StatType.AverageAccuracy     => $"Average: {stat.Value:P1}",
                StatType.LongestStreak       => $"Longest Streak: {stat.Value:F0}",
                StatType.HardPlusSongsPlayed => $"{stat.Value:F0} Hard+ Songs",
                StatType.NinetyPlusAccuracy  => $"{stat.Value:F0} Songs 90%+",
                StatType.TotalScore          => $"Total Score: {FormatScore((int)stat.Value)}",
                StatType.ThreeStarCount      => $"{stat.Value:F0} Songs 3★+",
                StatType.UniqueSongsCleared  => $"{stat.Value:F0} Songs Cleared",

                // Tier 4
                StatType.SongsPlayed         => $"{stat.Value:F0} Sessions",
                StatType.TotalNotesHit       => $"{FormatNumber((int)stat.Value)} Notes Hit",
                StatType.EightyPlusAccuracy  => $"{stat.Value:F0} Songs 80%+",
                StatType.SeventyPlusAccuracy => $"{stat.Value:F0} Songs 70%+",
                StatType.MediumSongsPlayed   => $"{stat.Value:F0} Medium Songs",
                StatType.EasySongsPlayed     => $"{stat.Value:F0} Easy Songs",
                StatType.AverageScore        => $"Avg Score: {FormatScore((int)stat.Value)}",
                StatType.NotesMissed         => $"{FormatNumber((int)stat.Value)} Notes Missed",

                _ => $"Unknown Stat: {stat.Value}",
            };
        }

        /// <summary>
        /// Short format for player card display (compact, no emoji).
        /// </summary>
        public static string FormatStatShort(PlayerStat stat)
        {
            return stat.Type switch
            {
                // Tier 1
                StatType.GoldStarSongs       => $"{stat.Value:F0}",
                StatType.ExpertFCs           => $"{stat.Value:F0}",
                StatType.PerfectAccuracy     => $"{stat.Value:F0}",
                StatType.NearPerfectAccuracy => $"{stat.Value:F0}",
                StatType.FullCombos          => $"{stat.Value:F0}",

                // Tier 2
                StatType.HighScore           => FormatScore((int)stat.Value),
                StatType.FiveStarCount       => $"{stat.Value:F0}",
                StatType.HighAccuracy        => $"{stat.Value:F0}",
                StatType.ExpertSongsPlayed   => $"{stat.Value:F0}",
                StatType.FourStarCount       => $"{stat.Value:F0}",

                // Tier 3
                StatType.AverageAccuracy     => $"{stat.Value * 100f:F1}%",
                StatType.LongestStreak       => FormatNumber((int)stat.Value),
                StatType.HardPlusSongsPlayed => $"{stat.Value:F0}",
                StatType.NinetyPlusAccuracy  => $"{stat.Value:F0}",
                StatType.TotalScore          => FormatScore((int)stat.Value),
                StatType.ThreeStarCount      => $"{stat.Value:F0}",
                StatType.UniqueSongsCleared  => $"{stat.Value:F0}",

                // Tier 4
                StatType.SongsPlayed         => $"{stat.Value:F0}",
                StatType.TotalNotesHit       => FormatNumber((int)stat.Value),
                StatType.EightyPlusAccuracy  => $"{stat.Value:F0}",
                StatType.SeventyPlusAccuracy => $"{stat.Value:F0}",
                StatType.MediumSongsPlayed   => $"{stat.Value:F0}",
                StatType.EasySongsPlayed     => $"{stat.Value:F0}",
                StatType.AverageScore        => FormatScore((int)stat.Value),
                StatType.NotesMissed         => FormatNumber((int)stat.Value),

                _ => stat.Value.ToString("G4"),
            };
        }

        /// <summary>
        /// Formats band stats text without emoji.
        /// </summary>
        public static string FormatBandStat(string label, object value)
        {
            return $"  {label}: {value}";
        }

        /// <summary>
        /// Formats a star count display text (compact).
        /// </summary>
        public static string FormatStarsText(int totalStars)
        {
            int fullStars = Mathf.Clamp(totalStars / 5, 0, 20);
            string d = "";
            for (int i = 0; i < Mathf.Min(fullStars, 5); i++)
                d += "\u2605 ";  // ★
            if (fullStars > 5)
                d += $"({totalStars})";
            else if (fullStars == 0 && totalStars > 0)
                d = $"{totalStars} stars";
            return d;
        }

        /// <summary>
        /// Formats a score number with commas.
        /// </summary>
        private static string FormatScore(int score)
        {
            return score.ToString("N0");
        }

        /// <summary>
        /// Formats a generic number (e.g. notes hit) with K/M suffixes.
        /// </summary>
        private static string FormatNumber(int number)
        {
            if (number >= 1000000)
                return $"{(number / 1000000f):F1}M";
            if (number >= 1000)
                return $"{(number / 1000f):F1}K";
            return number.ToString("N0");
        }

        /// <summary>
        /// Returns a trophy/congratulations header based on completion metrics.
        /// </summary>
        public static string GetTitle(CampaignStats stats)
        {
            if (!stats.IsCompleted) return "CAMPAIGN STATS";

            int profileCount = stats.ProfileStats.Count;
            if (profileCount >= 4)
                return "CAMPAIGN CONQUERED!";
            if (profileCount >= 2)
                return "CAMPAIGN CONQUERED!";
            return "CAMPAIGN COMPLETE!";
        }

        /// <summary>
        /// Returns a random encouraging subtitle.
        /// </summary>
        public static string GetRandomSubtitle()
        {
            string[] subtitles =
            {
                "What an incredible run!",
                "Legends never die!",
                "That was epic!",
                "Rock and roll!",
                "Now THAT'S a band!",
                "Stage domination!",
                "Absolute legends!",
                "Unforgettable performance!",
                "Pure rock greatness!",
                "History in the making!",
            };

            return subtitles[UnityEngine.Random.Range(0, subtitles.Length)];
        }
    }
}