using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using YARG.Career;
using YARG.Core.Logging;
using YARG.Core.Song;
using YARG.Helpers;
using YARG.Song;

namespace YARG.Menu.Career
{
    public class CareerManager
    {
        private static CareerManager _instance;
        public static CareerManager Instance => _instance ??= new CareerManager();

        private CareerDatabase _careerDatabase;
        private Dictionary<string, GigDatabase> _gigDatabases = new();

        public BandProgress CurrentBand => BandContainer.CurrentBand;
        public List<BandProgress> Bands => BandContainer.AllBands;

        public void LoadCareerData()
        {
            Debug.Log("=== CareerManager: Starting to load career data ===");
            BandContainer.Initialize();
            SongMatchCache.Initialize();
            LoadCareers();
            Debug.Log($"=== CareerManager: Finished loading career data. Current Band: {CurrentBand?.BandName} ===");
        }

        private void LoadCareers()
        {
            // Scan bundles/ directory and regenerate careers.json
            _careerDatabase = CareerBundleManager.ScanAndBuild();

            if (_careerDatabase.careers.Count == 0)
            {
                Debug.LogWarning("CareerManager: No career bundles found. " +
                    $"Place bundles in: {CareerBundleManager.BundlesDirectory}");
                return;
            }

            Debug.Log($"CareerManager: Loaded {_careerDatabase.careers.Count} careers from bundles");

            foreach (var career in _careerDatabase.careers)
            {
                Debug.Log($"  - {career.name} by {career.author} [{career.id}]");
                LoadGigsForCareer(career);
            }
        }

        private void LoadGigsForCareer(CareerInfo career)
        {
            if (string.IsNullOrEmpty(career.gigsPath))
            {
                Debug.LogWarning($"CareerManager: No gigs path for career {career.name}");
                return;
            }

            if (!File.Exists(career.gigsPath))
            {
                Debug.LogError($"CareerManager: Gigs file not found: {career.gigsPath}");
                return;
            }

            try
            {
                string json = File.ReadAllText(career.gigsPath);
                var gigDatabase = JsonUtility.FromJson<GigDatabase>(json);

                if (gigDatabase != null && gigDatabase.gigs != null)
                {
                    _gigDatabases[career.id] = gigDatabase;
                    Debug.Log($"CareerManager: Loaded {gigDatabase.gigs.Count} gigs for {career.name}");
                }
                else
                {
                    Debug.LogError($"CareerManager: Failed to deserialize gigs for {career.name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"CareerManager: Exception loading gigs for {career.name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces a rescan of the bundles directory and reloads all career data.
        /// Call after installing or removing a bundle.
        /// </summary>
        public void ReloadCareers()
        {
            _gigDatabases.Clear();
            LoadCareers();
        }

        /// <summary>
        /// Installs a career bundle from a source folder and reloads.
        /// Returns the runtime ID of the installed bundle, or null on failure.
        /// </summary>
        public string InstallCareerBundle(string sourcePath)
        {
            string runtimeId = CareerBundleManager.InstallBundle(sourcePath);
            if (runtimeId != null)
            {
                ReloadCareers();
            }
            return runtimeId;
        }

        /// <summary>
        /// Removes a career bundle and reloads.
        /// </summary>
        public bool RemoveCareerBundle(string careerId)
        {
            bool removed = CareerBundleManager.RemoveBundle(careerId);
            if (removed)
            {
                ReloadCareers();
            }
            return removed;
        }

        public void SaveCurrentBand()
        {
            if (CurrentBand == null)
            {
                Debug.LogWarning("CareerManager: Cannot save - no current band");
                return;
            }

            BandContainer.SaveBands();
            Debug.Log($"CareerManager: Saved band progress for {CurrentBand.BandName}");
        }

        public void CycleBand()
        {
            if (Bands.Count <= 1)
            {
                Debug.Log("CareerManager: Only one band available, cannot cycle");
                return;
            }

            BandContainer.CycleBand();
            Debug.Log($"CareerManager: Cycled to band {CurrentBand.BandName}");
        }

        public void SwitchToBand(int bandIndex)
        {
            BandContainer.SwitchToBand(bandIndex);
            Debug.Log($"CareerManager: Switched to band {CurrentBand.BandName}");
        }

        public void CreateNewBand(string bandName)
        {
            BandContainer.CreateBand(bandName);
            Debug.Log($"CareerManager: Created new band {bandName}");
        }

        public void DeleteBand(int bandIndex)
        {
            BandContainer.DeleteBand(bandIndex);
            Debug.Log($"CareerManager: Deleted band at index {bandIndex}");
        }

        public List<CareerInfo> GetCareers()
        {
            return _careerDatabase?.careers ?? new List<CareerInfo>();
        }

        public List<GigInfo> GetGigs(string careerId)
        {
            if (_gigDatabases.TryGetValue(careerId, out var gigDatabase))
            {
                return gigDatabase.gigs;
            }

            Debug.LogWarning($"CareerManager: No gigs found for career {careerId}");
            return new List<GigInfo>();
        }

        public CareerInfo GetCareerById(string careerId)
        {
            return _careerDatabase?.careers?.FirstOrDefault(c => c.id == careerId);
        }

        public void CompleteGig(string gigId)
        {
            BandContainer.CompleteGig(gigId);
            Debug.Log($"CareerManager: Completed gig {gigId}");

            // NEW: Check if this gig completed a campaign. gigId format: "{careerId}|{gigName}"
            int separatorIndex = gigId.IndexOf('|');
            if (separatorIndex > 0)
            {
                string careerId = gigId.Substring(0, separatorIndex);
                TryRecordCampaignCompletion(careerId);
            }
        }

        /// <summary>
        /// If all gigs in the campaign are now completed and no completion date
        /// is already recorded, stamp the current UTC time.
        /// This freezes trophy screen stats to this moment.
        /// </summary>
        private void TryRecordCampaignCompletion(string careerId)
        {
            var band = CurrentBand;
            if (band == null) return;

            // Already recorded — don't overwrite the original completion date
            if (band.CampaignCompletedDates.ContainsKey(careerId))
                return;

            // Check if all gigs are done
            var gigs = GetGigs(careerId);
            if (gigs.Count == 0) return;

            foreach (var gig in gigs)
            {
                string gId = $"{careerId}|{gig.name}";
                if (!IsGigCompleted(gId))
                    return; // not all gigs done yet
            }

            // All gigs complete — record the timestamp
            band.CampaignCompletedDates[careerId] = DateTime.UtcNow;
            BandContainer.SaveBands();
            Debug.Log($"CareerManager: Campaign '{careerId}' completed at {band.CampaignCompletedDates[careerId]:O}");
        }

        /// <summary>
        /// Returns the DateTime when the campaign was first completed, or null if not yet completed.
        /// </summary>
        public DateTime? GetCampaignCompletedDate(string careerId)
        {
            var band = CurrentBand;
            if (band?.CampaignCompletedDates == null) return null;

            if (band.CampaignCompletedDates.TryGetValue(careerId, out DateTime date))
                return date;

            return null;
        }

        public bool IsGigCompleted(string gigId)
        {
            return BandContainer.IsGigCompleted(gigId);
        }

        public void SetBandName(string name)
        {
            BandContainer.SetBandName(name);
            Debug.Log($"CareerManager: Changed band name to {name}");
        }

        /// <summary>
        /// Resolves a <see cref="GigSong"/> to a <see cref="SongEntry"/>.
        /// </summary>
        public SongEntry ResolveSong(GigSong gigSong, string careerId = null)
        {
            if (gigSong == null) return null;

            if (!string.IsNullOrEmpty(careerId))
            {
                var cached = SongMatchCache.ResolveFromCache(careerId, gigSong);
                if (cached != null) return cached;
            }

            if (!string.IsNullOrEmpty(gigSong.hash))
            {
                var hash = HashWrapper.FromString(gigSong.hash);
                if (SongContainer.SongsByHash.TryGetValue(hash, out var songs))
                {
                    return songs[0];
                }
            }

            if (!string.IsNullOrEmpty(gigSong.title) && !string.IsNullOrEmpty(gigSong.artist))
            {
                SongEntry bestMatch = null;
                bool bestMatchHasSource = false;

                foreach (var song in SongContainer.Songs)
                {
                    bool titleMatch = string.Equals(
                        song.Name.ToString(), gigSong.title,
                        StringComparison.OrdinalIgnoreCase);
                    bool artistMatch = string.Equals(
                        song.Artist.ToString(), gigSong.artist,
                        StringComparison.OrdinalIgnoreCase);

                    if (titleMatch && artistMatch)
                    {
                        bool sourceMatch = !string.IsNullOrEmpty(gigSong.source) &&
                            string.Equals(song.Source.ToString(), gigSong.source,
                                StringComparison.OrdinalIgnoreCase);

                        if (bestMatch == null || (sourceMatch && !bestMatchHasSource))
                        {
                            bestMatch = song;
                            bestMatchHasSource = sourceMatch;
                        }

                        if (bestMatchHasSource) break;
                    }
                }

                if (bestMatch != null) return bestMatch;
            }

            return null;
        }

        public List<(GigSong GigSong, SongEntry SongEntry)> ResolveGig(GigInfo gig, string careerId = null)
        {
            var results = new List<(GigSong, SongEntry)>();
            if (gig?.songs == null) return results;

            foreach (var gigSong in gig.songs)
            {
                results.Add((gigSong, ResolveSong(gigSong, careerId)));
            }
            return results;
        }

        public CareerMatchCache AnalyzeCareer(string careerId)
        {
            var gigs = GetGigs(careerId);
            return SongMatchAnalyzer.AnalyzeCareer(careerId, gigs);
        }

        public int GetUniqueSongCount(string careerId)
        {
            var gigs = GetGigs(careerId);
            var uniqueKeys = new HashSet<string>();

            foreach (var gig in gigs)
            {
                if (gig.songs == null) continue;
                foreach (var song in gig.songs)
                {
                    uniqueKeys.Add(SongMatch.CreateKey(song));
                }
            }

            return uniqueKeys.Count;
        }
    }
}