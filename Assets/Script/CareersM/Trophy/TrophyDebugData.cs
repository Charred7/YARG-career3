using System.Collections.Generic;
using YARG.Menu.Career.Motivation;

namespace YARG.Menu.Career.Trophy
{
    /// <summary>
    /// Builds fixed preset <see cref="CampaignStatsData"/> for layout testing.
    /// Produces four full instrument cards (Guitar, Bass, Drums, Vocals) plus a
    /// Golden Vinyl band summary regardless of which profiles are logged in or
    /// whether any campaign is actually complete.
    ///
    /// Toggle via <c>CampaignTrophyController._useDebugStats</c> on the dev build.
    /// Safe to leave in the project — nothing references it unless the flag is on.
    /// </summary>
    public static class TrophyDebugData
    {
        public static CampaignStatsData Build(CareerInfo career = null)
        {
            string name = career?.name;
            if (string.IsNullOrWhiteSpace(name))
                name = "DEBUG CAMPAIGN";

            return new CampaignStatsData
            {
                CareerId = career?.id ?? "debug",
                ScreenTitle = $"CAMPAIGN CONQUERED: {name.ToUpper()} | DEBUG",
                IsFullyCompleted = true,
                Milestone = BuildMilestone(),
                Players = new List<PlayerStatsData>
                {
                    BuildGuitar(),
                    BuildBass(),
                    BuildDrums(),
                    BuildVocals(),
                }
            };
        }

        private static MilestoneData BuildMilestone() => new()
        {
            Tier = MilestoneTier.GoldenVinyl,
            BadgeTitle = "GOLDEN VINYL",
            CompletionBannerText = "100% COMPLETE",
            SubLabel = "GOLDEN VINYL",
            BandStatRows = new List<StatRowData>
            {
                new("Total Band Score", "23,166,082"),
                new("Total Stars Earned", "140"),
                new("4-Star Songs", "26"),
                new("Total Sessions", "43"),
            },
            TopSongs = new List<TopSongData>
            {
                new(1, "Through the Fire and Flames", "1,204,880"),
                new(2, "Green Grass and High Tides", "988,140"),
                new(3, "Cliffs of Dover", "902,455"),
            }
        };

        private static PlayerStatsData BuildGuitar() => new()
        {
            PlayerName = "PLAYER 1",
            InstrumentLabel = "GUITAR - PLAYER 1",
            InstrumentIndex = 0,
            Aura = PlayerAuraType.Flames,
            DiamondFrame = DiamondFrameVariant.OrnateGold_B,
            AccoladeTitle = "GOLDEN FINGERS",
            StarCount = 5,
            MaxStars = 5,
            HighScoreSong = "Through the Fire and Flames",
            Stats = new List<StatRowData>
            {
                new("99%+ Songs", "43"),
                new("Gold Star Songs", "1"),
                new("4-Star Songs", "40"),
                new("95%+ Songs", "16"),
                new("90%+ Songs", "38"),
                new("Expert Songs", "3"),
                new("High Score", "344,138"),
            }
        };

        private static PlayerStatsData BuildBass() => new()
        {
            PlayerName = "PLAYER 2",
            InstrumentLabel = "BASS - PLAYER 2",
            InstrumentIndex = 1,
            Aura = PlayerAuraType.Lightning,
            DiamondFrame = DiamondFrameVariant.Standard_A,
            AccoladeTitle = "EXPERT VETERAN",
            StarCount = 4,
            MaxStars = 5,
            HighScoreSong = "Green Grass and High Tides",
            Stats = new List<StatRowData>
            {
                new("4-Star Songs", "11"),
                new("Expert Songs", "3"),
                new("5-Star Songs", "1"),
                new("95%+ Songs", "6"),
                new("90%+ Songs", "18"),
                new("Hard Songs", "2"),
                new("High Score", "274,200"),
            }
        };

        private static PlayerStatsData BuildDrums() => new()
        {
            PlayerName = "PLAYER 3",
            InstrumentLabel = "DRUMS - PLAYER 3",
            InstrumentIndex = 2,
            Aura = PlayerAuraType.Lightning,
            DiamondFrame = DiamondFrameVariant.Standard_D,
            AccoladeTitle = "ROAD WARRIOR",
            StarCount = 5,
            MaxStars = 5,
            HighScoreSong = "Cliffs of Dover",
            Stats = new List<StatRowData>
            {
                new("4-Star Songs", "1"),
                new("Best Streak", "588"),
                new("Total Score", "119,873"),
                new("Accuracy", "90.9%"),
                new("Hard Songs", "1"),
                new("95%+ Songs", "4"),
                new("High Score", "119,873"),
            }
        };

        private static PlayerStatsData BuildVocals() => new()
        {
            PlayerName = "PLAYER 4",
            InstrumentLabel = "VOCALS - PLAYER 4",
            InstrumentIndex = 3,
            Aura = PlayerAuraType.None,
            DiamondFrame = DiamondFrameVariant.OrnateGold_C,
            AccoladeTitle = "STREAK MASTER",
            StarCount = 5,
            MaxStars = 5,
            HighScoreSong = "Bohemian Rhapsody",
            Stats = new List<StatRowData>
            {
                new("Best Streak", "1,204"),
                new("Accuracy", "98.2%"),
                new("100% Songs", "22"),
                new("95%+ Songs", "31"),
                new("Total Sessions", "43"),
                new("Hard Songs", "5"),
                new("High Score", "198,540"),
            }
        };
    }
}
