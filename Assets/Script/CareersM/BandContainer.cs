using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using YARG.Core.Logging;
using YARG.Helpers;

namespace YARG.Career
{
    public static class BandContainer
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            Formatting = Formatting.Indented
        };

        public static string CareerDirectory { get; private set; }

        private static string _bandsPath;

        private static BandsDatabase _bandsDatabase;

        private static int _currentBandIndex;

        public static BandProgress CurrentBand => _bandsDatabase?.Bands != null && _bandsDatabase.Bands.Count > 0
            ? _bandsDatabase.Bands[_currentBandIndex]
            : null;

        public static List<BandProgress> AllBands => _bandsDatabase?.Bands ?? new List<BandProgress>();

        public static int CurrentBandIndex => _currentBandIndex;

        private static bool _isInitialized;

        public static void Initialize()
        {
            CareerDirectory = Path.Combine(PathHelper.PersistentDataPath, "career");
            _bandsPath = Path.Combine(CareerDirectory, "bands.json");

            // Make sure the folder exists to prevent errors
            Directory.CreateDirectory(CareerDirectory);

            LoadBands();
        }

        public static void LoadBands()
        {
            if (!File.Exists(_bandsPath))
            {
                // If the bands file doesn't exist, create a new one with a default band
                _bandsDatabase = new BandsDatabase
                {
                    Bands = new List<BandProgress>
                    {
                        new BandProgress
                        {
                            BandName = "Default Band",
                            CompletedGigs = new List<string>()
                        }
                    },
                    CurrentBandIndex = 0
                };

                SaveBands();
                _currentBandIndex = 0;
                _isInitialized = true;
                return;
            }

            // Load existing bands
            try
            {
                var json = File.ReadAllText(_bandsPath);
                _bandsDatabase = JsonConvert.DeserializeObject<BandsDatabase>(json, _jsonSettings);

                if (_bandsDatabase is null || _bandsDatabase.Bands is null || _bandsDatabase.Bands.Count == 0)
                {
                    YargLogger.LogWarning("Failed to load bands, creating new database");
                    _bandsDatabase = new BandsDatabase
                    {
                        Bands = new List<BandProgress>
                        {
                            new BandProgress
                            {
                                BandName = "Default Band",
                                CompletedGigs = new List<string>()
                            }
                        },
                        CurrentBandIndex = 0
                    };
                    SaveBands();
                }

                // Ensure current band index is valid
                _currentBandIndex = _bandsDatabase.CurrentBandIndex;
                if (_currentBandIndex < 0 || _currentBandIndex >= _bandsDatabase.Bands.Count)
                {
                    _currentBandIndex = 0;
                    _bandsDatabase.CurrentBandIndex = 0;
                }

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Failed to load bands");
                _bandsDatabase = new BandsDatabase
                {
                    Bands = new List<BandProgress>
                    {
                        new BandProgress
                        {
                            BandName = "Default Band",
                            CompletedGigs = new List<string>()
                        }
                    },
                    CurrentBandIndex = 0
                };
                _currentBandIndex = 0;
                _isInitialized = true;
            }
        }

        public static void SaveBands()
        {
            if (!_isInitialized)
            {
                YargLogger.LogWarning("Bands could not be saved as they were not loaded");
                return;
            }

            try
            {
                _bandsDatabase.CurrentBandIndex = _currentBandIndex;
                var json = JsonConvert.SerializeObject(_bandsDatabase, _jsonSettings);
                File.WriteAllText(_bandsPath, json);
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, "Failed to save bands");
            }
        }

        public static void SetBandName(string name)
        {
            if (CurrentBand == null) return;

            CurrentBand.BandName = name;
            SaveBands();
        }

        public static void CompleteGig(string gigId)
        {
            if (CurrentBand == null) return;

            if (CurrentBand.CompletedGigs.Contains(gigId))
            {
                return;
            }

            CurrentBand.CompletedGigs.Add(gigId);
            SaveBands();
        }

        public static bool IsGigCompleted(string gigId)
        {
            if (CurrentBand == null) return false;

            return CurrentBand.CompletedGigs.Contains(gigId);
        }

        public static void RemoveCompletedGig(string gigId)
        {
            if (CurrentBand == null) return;

            if (CurrentBand.CompletedGigs.Remove(gigId))
            {
                SaveBands();
            }
        }

        public static void CreateBand(string bandName)
        {
            var newBand = new BandProgress
            {
                BandName = bandName,
                CompletedGigs = new List<string>()
            };

            _bandsDatabase.Bands.Add(newBand);
            SaveBands();
        }

        public static void DeleteBand(int bandIndex)
        {
            if (bandIndex < 0 || bandIndex >= _bandsDatabase.Bands.Count)
            {
                YargLogger.LogWarning($"Cannot delete band at index {bandIndex} - index out of range");
                return;
            }

            if (_bandsDatabase.Bands.Count <= 1)
            {
                YargLogger.LogWarning("Cannot delete the last band");
                return;
            }

            _bandsDatabase.Bands.RemoveAt(bandIndex);

            // Adjust current band index if necessary
            if (_currentBandIndex >= _bandsDatabase.Bands.Count)
            {
                _currentBandIndex = _bandsDatabase.Bands.Count - 1;
            }

            SaveBands();
        }

        public static void SwitchToBand(int bandIndex)
        {
            if (bandIndex < 0 || bandIndex >= _bandsDatabase.Bands.Count)
            {
                YargLogger.LogWarning($"Cannot switch to band at index {bandIndex} - index out of range");
                return;
            }

            _currentBandIndex = bandIndex;
            SaveBands();
        }

        public static void CycleBand()
        {
            if (_bandsDatabase.Bands.Count <= 1)
            {
                return;
            }

            _currentBandIndex = (_currentBandIndex + 1) % _bandsDatabase.Bands.Count;
            SaveBands();
        }

        public static void HideCampaign(string careerId)
        {
            if (CurrentBand == null) return;

            if (CurrentBand.HiddenCampaigns == null)
            {
                CurrentBand.HiddenCampaigns = new List<string>();
            }

            if (!CurrentBand.HiddenCampaigns.Contains(careerId))
            {
                CurrentBand.HiddenCampaigns.Add(careerId);
                SaveBands();
            }
        }

        public static bool IsCareerHidden(string careerId)
        {
            if (CurrentBand == null) return false;
            if (CurrentBand.HiddenCampaigns == null) return false;

            return CurrentBand.HiddenCampaigns.Contains(careerId);
        }

        public static void UnhideCampaign(string careerId)
        {
            if (CurrentBand == null) return;
            if (CurrentBand.HiddenCampaigns == null) return;

            if (CurrentBand.HiddenCampaigns.Remove(careerId))
            {
                SaveBands();
                YargLogger.LogInfo($"Unhid campaign '{careerId}' for current band");
            }
        }

        public static void ToggleCampaignHidden(string careerId)
        {
            if (string.IsNullOrEmpty(careerId)) return;

            if (IsCareerHidden(careerId))
                UnhideCampaign(careerId);
            else
                HideCampaign(careerId);
        }

        public static void UnhideAllCampaigns()
        {
            if (CurrentBand == null) return;

            if (CurrentBand.HiddenCampaigns != null && CurrentBand.HiddenCampaigns.Count > 0)
            {
                CurrentBand.HiddenCampaigns.Clear();
                SaveBands();
                YargLogger.LogInfo("Unhid all campaigns for current band");
            }
        }

        public static void SetSortMode(int mode)
        {
            if (CurrentBand == null) return;

            CurrentBand.SortMode = mode;
            SaveBands();
        }
    }

    [Serializable]
    public class BandsDatabase
    {
        [JsonProperty("bands")]
        public List<BandProgress> Bands { get; set; }

        [JsonProperty("currentBandIndex")]
        public int CurrentBandIndex { get; set; }
    }

    [Serializable]
    public class BandProgress
    {
        [JsonProperty("bandName")]
        public string BandName { get; set; }

        [JsonProperty("completedGigs")]
        public List<string> CompletedGigs { get; set; }

        [JsonProperty("hiddenCampaigns")]
        public List<string> HiddenCampaigns { get; set; } = new List<string>();

        /// <summary>
        /// Persisted <see cref="CareerSortMode"/> for the career picker (0=A-Z, 1=Date, 2=Progress).
        /// </summary>
        [JsonProperty("sortMode")]
        public int SortMode { get; set; } = 1;

        /// <summary>
        /// Maps careerId -> UTC DateTime when the campaign was first completed.
        /// Used by the trophy screen to apply a date cutoff filter so stats
        /// are frozen at the moment of completion.
        /// </summary>
        [JsonProperty("campaignCompletedDates")]
        public Dictionary<string, DateTime> CampaignCompletedDates { get; set; } = new();
    }
}