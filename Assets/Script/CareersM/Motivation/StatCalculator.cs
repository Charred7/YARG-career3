using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YARG.Core;
using YARG.Core.Game;
using YARG.Core.Song;
using YARG.Scores;
using YARG.Song;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Computes feel-good campaign trophy stats from machine-wide scores.db data
    /// for campaign songs (up to the completion date freeze). Logged-in profiles
    /// are ignored — each instrument card uses the most impressive run per song
    /// on that instrument family.
    /// </summary>
    public static class StatCalculator
    {
        private static readonly string[] InstrumentDisplayNames =
        {
            "GUITAR", "BASS", "DRUMS", "VOCALS"
        };

        /// <summary>
        /// Computes complete campaign stats for instrument cards (not logged-in profiles).
        /// </summary>
        /// <param name="career">The campaign to compute stats for.</param>
        /// <param name="completedDate">
        /// If provided, only scores recorded on or before this date are included.
        /// </param>
        public static CampaignStats ComputeCampaignStats(CareerInfo career, DateTime? completedDate = null)
        {
            var stats = new CampaignStats
            {
                CareerId = career.id,
                CareerName = career.name,
                IsCompleted = IsCampaignComplete(career),
            };

            var resolvedSongHashes = GetResolvedSongHashes(career);
            stats.BandStats.TotalSongs = resolvedSongHashes.Count;

            if (resolvedSongHashes.Count == 0)
            {
                Debug.LogWarning($"StatCalculator: No resolved songs found for campaign '{career.id}'");
                return stats;
            }

            var allGameRecords = completedDate.HasValue
                ? ScoreContainer.GetGameRecordsUpToDate(completedDate.Value) ?? new List<GameRecord>()
                : ScoreContainer.GetAllGameRecords() ?? new List<GameRecord>();

            var gameRecordChecksums = new Dictionary<int, byte[]>();
            var gameRecordSongNames = new Dictionary<int, string>();
            var campaignGameRecordIds = new HashSet<int>();

            foreach (var record in allGameRecords)
            {
                if (!gameRecordChecksums.ContainsKey(record.Id))
                    gameRecordChecksums[record.Id] = record.SongChecksum;
                if (!gameRecordSongNames.ContainsKey(record.Id))
                    gameRecordSongNames[record.Id] = record.SongName;

                if (record.SongChecksum != null &&
                    resolvedSongHashes.Contains(HashWrapper.Create(record.SongChecksum)))
                {
                    campaignGameRecordIds.Add(record.Id);
                }
            }

            // Band panel (best band score per campaign song)
            var bandSongScores = new List<BandTopSong>();
            foreach (var hash in resolvedSongHashes)
            {
                GameRecord bandScore = completedDate.HasValue
                    ? ScoreContainer.GetBandHighScoreUpToDate(hash, completedDate.Value)
                    : ScoreContainer.GetBandHighScore(hash);

                if (bandScore != null)
                {
                    stats.BandStats.TotalBandScore += bandScore.BandScore;
                    stats.BandStats.TotalStarsEarned += (int)bandScore.BandStars;
                    if (bandScore.BandStars >= StarAmount.StarGold)
                        stats.BandStats.SongsWithGoldStars++;
                    if (bandScore.BandStars >= StarAmount.Star5)
                        stats.BandStats.SongsWithFiveStars++;
                    if (bandScore.BandStars >= StarAmount.Star4)
                        stats.BandStats.SongsWithFourStars++;
                    stats.BandStats.TotalSongsWithScores++;

                    bandSongScores.Add(new BandTopSong
                    {
                        SongName  = string.IsNullOrEmpty(bandScore.SongName) ? "Unknown Song" : bandScore.SongName,
                        BandScore = bandScore.BandScore,
                        BandStars = bandScore.BandStars,
                    });
                }
            }

            stats.BandStats.TopSongs = bandSongScores
                .OrderByDescending(s => s.BandScore)
                .Take(5)
                .ToList();

            stats.BandStats.TotalPlaySessions = campaignGameRecordIds.Count;

            // Machine-wide player scores on campaign songs (any profile), non-replay only
            var allPlayerScores = ScoreContainer.GetAllPlayerScoreRecords() ?? new List<PlayerScoreRecord>();
            var campaignScores = new List<PlayerScoreRecord>();
            foreach (var score in allPlayerScores)
            {
                if (score.IsReplay)
                    continue;
                if (!campaignGameRecordIds.Contains(score.GameRecordId))
                    continue;
                campaignScores.Add(score);
            }

            // One card per instrument family that has at least one best-per-song run
            for (int instrumentIndex = 0; instrumentIndex < 4; instrumentIndex++)
            {
                var instrumentBests = SelectBestRunPerSong(
                    campaignScores,
                    gameRecordChecksums,
                    instrumentIndex);

                if (instrumentBests.Count == 0)
                    continue;

                stats.ProfileStats.Add(ComputeInstrumentCardStats(
                    instrumentIndex,
                    instrumentBests,
                    gameRecordSongNames));
            }

            return stats;
        }

        /// <summary>
        /// Picks the most impressive non-replay run per campaign song for one instrument family.
        /// </summary>
        private static List<PlayerScoreRecord> SelectBestRunPerSong(
            List<PlayerScoreRecord> campaignScores,
            Dictionary<int, byte[]> gameRecordChecksums,
            int instrumentIndex)
        {
            // songKey -> best score
            var bestBySong = new Dictionary<string, PlayerScoreRecord>();

            foreach (var score in campaignScores)
            {
                if (MapInstrumentToIndex(score.Instrument) != instrumentIndex)
                    continue;
                if (!gameRecordChecksums.TryGetValue(score.GameRecordId, out var checksum) || checksum == null)
                    continue;

                string songKey = Convert.ToBase64String(checksum);
                if (!bestBySong.TryGetValue(songKey, out var current) ||
                    CompareImpressiveness(score, current) > 0)
                {
                    bestBySong[songKey] = score;
                }
            }

            return bestBySong.Values.ToList();
        }

        /// <summary>
        /// Returns positive if <paramref name="a"/> is more impressive than <paramref name="b"/>.
        /// Order: stars, FC, accuracy, score.
        /// </summary>
        private static int CompareImpressiveness(PlayerScoreRecord a, PlayerScoreRecord b)
        {
            int starCmp = ((int)a.Stars).CompareTo((int)b.Stars);
            if (starCmp != 0) return starCmp;

            if (a.IsFc != b.IsFc)
                return a.IsFc ? 1 : -1;

            int pctCmp = a.GetPercent().CompareTo(b.GetPercent());
            if (pctCmp != 0) return pctCmp;

            return a.Score.CompareTo(b.Score);
        }

        private static ProfileCampaignStats ComputeInstrumentCardStats(
            int instrumentIndex,
            List<PlayerScoreRecord> bestPerSong,
            Dictionary<int, string> gameRecordSongNames)
        {
            string name = InstrumentDisplayNames[instrumentIndex];
            var result = new ProfileCampaignStats
            {
                PlayerName = name,
                ProfileId = Guid.Empty,
                InstrumentIndex = instrumentIndex,
            };

            if (bestPerSong == null || bestPerSong.Count == 0)
                return result;

            int goldStarCount     = bestPerSong.Count(s => s.Stars >= StarAmount.StarGold);
            int expertFcCount     = bestPerSong.Count(s => s.IsFc && s.Difficulty == Difficulty.Expert);
            int perfectAccCount   = bestPerSong.Count(s => s.GetPercent() >= 1.0f);
            int nearPerfectCount  = bestPerSong.Count(s => s.GetPercent() >= 0.99f);
            int fcCount           = bestPerSong.Count(s => s.IsFc);

            var highScoreRecord = bestPerSong.OrderByDescending(s => s.Score).First();
            int highScore = highScoreRecord.Score;
            if (gameRecordSongNames.TryGetValue(highScoreRecord.GameRecordId, out var hsSongName))
                result.HighScoreSongName = string.IsNullOrEmpty(hsSongName) ? string.Empty : hsSongName;

            int fiveStarCount     = bestPerSong.Count(s => s.Stars >= StarAmount.Star5);
            int highAccuracyCount = bestPerSong.Count(s => s.GetPercent() >= 0.95f);
            int expertCount       = bestPerSong.Count(s => s.Difficulty == Difficulty.Expert);
            int fourStarCount     = bestPerSong.Count(s => s.Stars >= StarAmount.Star4);

            float avgAccuracy     = bestPerSong.Average(s => s.GetPercent());
            int hardPlusCount     = bestPerSong.Count(s => s.Difficulty >= Difficulty.Hard);
            int ninetyPlusCount   = bestPerSong.Count(s => s.GetPercent() >= 0.90f);
            int totalScore        = bestPerSong.Sum(s => s.Score);
            int threeStarCount    = bestPerSong.Count(s => s.Stars >= StarAmount.Star3);

            // Best-per-song: unique cleared == number of songs represented
            int uniqueSongsCleared = bestPerSong.Count;
            int songsPlayed        = uniqueSongsCleared;
            int totalNotesHit      = bestPerSong.Sum(s => s.NotesHit);
            int eightyPlusCount    = bestPerSong.Count(s => s.GetPercent() >= 0.80f);
            int seventyPlusCount   = bestPerSong.Count(s => s.GetPercent() >= 0.70f);
            int mediumCount        = bestPerSong.Count(s => s.Difficulty == Difficulty.Medium);
            int easyCount          = bestPerSong.Count(s => s.Difficulty == Difficulty.Easy);
            int avgScore           = songsPlayed > 0 ? totalScore / songsPlayed : 0;

            var bestStars = new int[4];
            int maxStar = bestPerSong.Max(s => (int)s.Stars);
            bestStars[instrumentIndex] = maxStar;
            result.BestStarsByInstrument = bestStars;

            var allStats = new List<PlayerStat>();

            if (goldStarCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.GoldStarSongs, Value = goldStarCount, Tier = StatTier.Elite, SortPriority = goldStarCount * 150 });
            if (expertFcCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.ExpertFCs, Value = expertFcCount, Tier = StatTier.Elite, SortPriority = expertFcCount * 140 });
            if (perfectAccCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.PerfectAccuracy, Value = perfectAccCount, Tier = StatTier.Elite, SortPriority = perfectAccCount * 130 });
            if (nearPerfectCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.NearPerfectAccuracy, Value = nearPerfectCount, Tier = StatTier.Elite, SortPriority = nearPerfectCount * 120 });
            if (fcCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.FullCombos, Value = fcCount, Tier = StatTier.Elite, SortPriority = fcCount * 100 });

            if (highScore > 0)
                allStats.Add(new PlayerStat { Type = StatType.HighScore, Value = highScore, Tier = StatTier.Advanced, SortPriority = Mathf.RoundToInt(highScore / 1000f) });
            if (fiveStarCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.FiveStarCount, Value = fiveStarCount, Tier = StatTier.Advanced, SortPriority = fiveStarCount * 90 });
            if (highAccuracyCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.HighAccuracy, Value = highAccuracyCount, Tier = StatTier.Advanced, SortPriority = highAccuracyCount * 80 });
            if (expertCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.ExpertSongsPlayed, Value = expertCount, Tier = StatTier.Advanced, SortPriority = expertCount * 70 });
            if (fourStarCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.FourStarCount, Value = fourStarCount, Tier = StatTier.Advanced, SortPriority = fourStarCount * 60 });

            if (avgAccuracy > 0)
                allStats.Add(new PlayerStat { Type = StatType.AverageAccuracy, Value = avgAccuracy, Tier = StatTier.Solid, SortPriority = Mathf.RoundToInt(avgAccuracy * 100f) });
            if (hardPlusCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.HardPlusSongsPlayed, Value = hardPlusCount, Tier = StatTier.Solid, SortPriority = hardPlusCount * 40 });
            if (ninetyPlusCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.NinetyPlusAccuracy, Value = ninetyPlusCount, Tier = StatTier.Solid, SortPriority = ninetyPlusCount * 50 });
            if (totalScore > 0)
                allStats.Add(new PlayerStat { Type = StatType.TotalScore, Value = totalScore, Tier = StatTier.Solid, SortPriority = totalScore / 10000 });
            if (threeStarCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.ThreeStarCount, Value = threeStarCount, Tier = StatTier.Solid, SortPriority = threeStarCount * 30 });
            if (uniqueSongsCleared > 0)
                allStats.Add(new PlayerStat { Type = StatType.UniqueSongsCleared, Value = uniqueSongsCleared, Tier = StatTier.Solid, SortPriority = uniqueSongsCleared * 45 });

            if (songsPlayed > 0)
                allStats.Add(new PlayerStat { Type = StatType.SongsPlayed, Value = songsPlayed, Tier = StatTier.Foundation, SortPriority = songsPlayed * 10 });
            if (totalNotesHit > 0)
                allStats.Add(new PlayerStat { Type = StatType.TotalNotesHit, Value = totalNotesHit, Tier = StatTier.Foundation, SortPriority = totalNotesHit / 10 });
            if (eightyPlusCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.EightyPlusAccuracy, Value = eightyPlusCount, Tier = StatTier.Foundation, SortPriority = eightyPlusCount * 8 });
            if (seventyPlusCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.SeventyPlusAccuracy, Value = seventyPlusCount, Tier = StatTier.Foundation, SortPriority = seventyPlusCount * 5 });
            if (mediumCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.MediumSongsPlayed, Value = mediumCount, Tier = StatTier.Foundation, SortPriority = mediumCount * 4 });
            if (easyCount > 0)
                allStats.Add(new PlayerStat { Type = StatType.EasySongsPlayed, Value = easyCount, Tier = StatTier.Foundation, SortPriority = easyCount * 3 });
            if (avgScore > 0)
                allStats.Add(new PlayerStat { Type = StatType.AverageScore, Value = avgScore, Tier = StatTier.Foundation, SortPriority = avgScore / 1000 });

            result.AllStats = allStats;
            result.BestStats = allStats.OrderByDescending(s => s.SortPriority).ToList();
            return result;
        }

        private static HashSet<HashWrapper> GetResolvedSongHashes(CareerInfo career)
        {
            var hashes = new HashSet<HashWrapper>();
            var gigs = CareerManager.Instance.GetGigs(career.id);

            foreach (var gig in gigs)
            {
                if (gig.songs == null) continue;

                foreach (var gigSong in gig.songs)
                {
                    bool found = false;

                    var resolved = SongMatchCache.ResolveFromCache(career.id, gigSong);
                    if (resolved != null)
                    {
                        hashes.Add(resolved.Hash);
                        found = true;
                    }

                    if (!string.IsNullOrEmpty(gigSong.hash))
                    {
                        var hashFromStr = HashWrapper.FromString(gigSong.hash);
                        if (SongContainer.SongsByHash.ContainsKey(hashFromStr))
                        {
                            hashes.Add(hashFromStr);
                            found = true;
                        }
                    }

                    if (!found)
                    {
                        var song = CareerManager.Instance.ResolveSong(gigSong, career.id);
                        if (song != null)
                            hashes.Add(song.Hash);
                    }
                }
            }

            return hashes;
        }

        public static int MapInstrumentToIndex(Instrument instrument)
        {
            return instrument switch
            {
                Instrument.FiveFretGuitar     => 0,
                Instrument.FiveFretRhythm     => 0,
                Instrument.FiveFretCoopGuitar => 0,
                Instrument.SixFretGuitar      => 0,
                Instrument.SixFretRhythm      => 0,
                Instrument.SixFretCoopGuitar  => 0,
                Instrument.ProGuitar_17Fret   => 0,
                Instrument.ProGuitar_22Fret   => 0,
                Instrument.Keys               => 0,
                Instrument.ProKeys            => 0,

                Instrument.FiveFretBass       => 1,
                Instrument.SixFretBass        => 1,
                Instrument.ProBass_17Fret     => 1,
                Instrument.ProBass_22Fret     => 1,

                Instrument.FourLaneDrums      => 2,
                Instrument.FiveLaneDrums      => 2,
                Instrument.ProDrums           => 2,
                Instrument.EliteDrums         => 2,

                Instrument.Vocals             => 3,
                Instrument.Harmony            => 3,

                _                             => 0,
            };
        }

        private static bool IsCampaignComplete(CareerInfo career)
        {
            var band = CareerManager.Instance.CurrentBand;
            if (band == null) return false;

            var gigs = CareerManager.Instance.GetGigs(career.id);
            foreach (var gig in gigs)
            {
                string gigId = $"{career.id}|{gig.name}";
                if (!CareerManager.Instance.IsGigCompleted(gigId))
                    return false;
            }

            return gigs.Count > 0;
        }
    }
}
