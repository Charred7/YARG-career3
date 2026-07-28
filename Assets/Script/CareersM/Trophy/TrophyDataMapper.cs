using System.Collections.Generic;
using UnityEngine;
using YARG.Menu.Career.Motivation;

namespace YARG.Menu.Career.Trophy
{
    public static class TrophyDataMapper
    {
        public static List<TrophyCardData> MapPlayerCards(CampaignStatsData data, TrophyScreenArt art = null)
        {
            var cards = new List<TrophyCardData>();
            if (data?.Players == null)
                return cards;

            foreach (var player in data.Players)
                cards.Add(MapPlayerCard(player, art));

            return cards;
        }

        public static TrophyCardData MapPlayerCard(PlayerStatsData player, TrophyScreenArt art = null)
        {
            var (gridStats, highScore) = MapStats(player.Stats);

            var card = new TrophyCardData
            {
                InstrumentLabel = StripPlayerSuffix(player.InstrumentLabel),
                AccoladeTitle = NormalizeAccolade(player.AccoladeTitle),
                ThemeColor = GetInstrumentColor(player.InstrumentIndex),
                WatermarkSprite = art?.GetWatermark(player.InstrumentIndex),
                StarCount = player.StarCount,
                InstrumentIndex = player.InstrumentIndex,
                HighScoreValue = highScore,
                HighScoreSong = player.HighScoreSong,
                Stats = gridStats
            };

            return card;
        }

        private static (List<TrophyStatEntry> gridStats, string highScore) MapStats(List<StatRowData> stats)
        {
            var entries = new List<TrophyStatEntry>();
            string highScore = string.Empty;

            if (stats == null)
                return (entries, highScore);

            var gridStats = new List<StatRowData>();
            foreach (var stat in stats)
            {
                string stripped = StripLabelColon(stat.Label).ToUpperInvariant();
                if (stripped == "HIGH SCORE")
                {
                    highScore = stat.Value;
                    continue;
                }

                gridStats.Add(stat);
            }

            for (int i = 0; i < gridStats.Count; i++)
            {
                var stat = gridStats[i];
                var column = i % 2 == 0 ? StatColumn.LeftFlex : StatColumn.RightConsistency;
                entries.Add(new TrophyStatEntry(StripLabelColon(stat.Label), stat.Value, column));
            }

            return (entries, highScore);
        }

        private static string StripPlayerSuffix(string instrumentLabel)
        {
            if (string.IsNullOrEmpty(instrumentLabel))
                return "GUITAR";

            int dashIndex = instrumentLabel.IndexOf(" - PLAYER", System.StringComparison.OrdinalIgnoreCase);
            return dashIndex >= 0 ? instrumentLabel[..dashIndex].Trim() : instrumentLabel.Trim();
        }

        private static string NormalizeAccolade(string accoladeTitle)
        {
            if (string.IsNullOrEmpty(accoladeTitle))
                return string.Empty;

            return accoladeTitle.Replace('\n', ' ').Trim();
        }

        private static string StripLabelColon(string label)
        {
            if (string.IsNullOrEmpty(label))
                return string.Empty;

            return label.TrimEnd(':', ' ');
        }

        private static Color GetInstrumentColor(int instrumentIndex) => (instrumentIndex % 4) switch
        {
            0 => IconLibrary.ColorGtrOrange,
            1 => IconLibrary.ColorBassPurple,
            2 => IconLibrary.ColorDrumsBlue,
            3 => IconLibrary.ColorVocalsGreen,
            _ => IconLibrary.ColorGtrOrange
        };
    }
}
