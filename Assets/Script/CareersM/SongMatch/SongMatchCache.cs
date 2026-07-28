using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using YARG.Career;
using YARG.Core.Logging;
using YARG.Core.Song;
using YARG.Helpers;
using YARG.Song;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Manages loading, saving, and querying of <see cref="CareerMatchCache"/> files.
    /// One cache file per career, stored in career/matches/{careerId}.json.
    /// </summary>
    public static class SongMatchCache
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            Formatting = Formatting.Indented
        };

        private static string _matchesDirectory;
        private static readonly Dictionary<string, CareerMatchCache> _caches = new();

        /// <summary>
        /// Initializes the matches directory. Call after <see cref="BandContainer.Initialize"/>.
        /// </summary>
        public static void Initialize()
        {
            _matchesDirectory = Path.Combine(
                PathHelper.PersistentDataPath, "career", "matches");
            Directory.CreateDirectory(_matchesDirectory);
        }

        /// <summary>
        /// Returns the cached match for a specific <see cref="GigSong"/>, or null if not cached.
        /// </summary>
        public static SongMatch GetMatch(string careerId, GigSong gigSong)
        {
            var cache = GetOrLoadCache(careerId);
            if (cache == null)
            {
                return null;
            }

            string key = SongMatch.CreateKey(gigSong);
            return cache.matches.FirstOrDefault(m => m.gigSongKey == key);
        }

        /// <summary>
        /// Resolves a <see cref="GigSong"/> to a <see cref="SongEntry"/> using the cache.
        /// Returns null if not cached or the cached hash no longer exists in the library.
        /// </summary>
        public static SongEntry ResolveFromCache(string careerId, GigSong gigSong)
        {
            var match = GetMatch(careerId, gigSong);
            if (match == null || !match.IsResolved)
            {
                return null;
            }

            // Look up the resolved hash in the song library
            var hash = HashWrapper.FromString(match.resolvedHash);
            if (SongContainer.SongsByHash.TryGetValue(hash, out var songs))
            {
                return songs[0];
            }

            // Hash was cached but song is no longer in the library
            return null;
        }

        /// <summary>
        /// Returns the full <see cref="CareerMatchCache"/> for a career,
        /// loading from disk if not already in memory.
        /// </summary>
        public static CareerMatchCache GetOrLoadCache(string careerId)
        {
            if (_caches.TryGetValue(careerId, out var cached))
            {
                return cached;
            }

            string path = GetCachePath(careerId);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                var cache = JsonConvert.DeserializeObject<CareerMatchCache>(json, _jsonSettings);

                if (cache != null)
                {
                    _caches[careerId] = cache;
                    return cache;
                }
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, $"Failed to load match cache for career '{careerId}'");
            }

            return null;
        }

        /// <summary>
        /// Stores a match result for a single song. Creates the cache if it doesn't exist.
        /// Does NOT overwrite manually confirmed matches unless <paramref name="force"/> is true.
        /// </summary>
        public static void SetMatch(string careerId, SongMatch match, bool force = false)
        {
            var cache = GetOrCreateCache(careerId);

            int existingIndex = cache.matches.FindIndex(m => m.gigSongKey == match.gigSongKey);
            if (existingIndex >= 0)
            {
                // Don't overwrite manual confirmations unless forced
                if (cache.matches[existingIndex].manuallyConfirmed && !force)
                {
                    return;
                }

                cache.matches[existingIndex] = match;
            }
            else
            {
                cache.matches.Add(match);
            }
        }

        /// <summary>
        /// Computes and caches per-gig duration data from resolved song matches.
        /// Called after all matches have been set for a career.
        /// Populates <c>cache.gigDurations</c> for the HypeEngine Tier 2 rules.
        /// </summary>
        /// <param name="careerId">The career ID.</param>
        /// <param name="gigs">The full list of gigs in this career.</param>
        public static void ComputeGigDurations(string careerId, List<GigInfo> gigs)
        {
            var cache = GetOrLoadCache(careerId);
            if (cache == null)
            {
                YargLogger.LogWarning($"SongMatchCache: No cache found for career '{careerId}', cannot compute durations");
                return;
            }

            cache.gigDurations.Clear();

            foreach (var gig in gigs)
            {
                if (gig.songs == null || gig.songs.Count == 0)
                    continue;

                double totalSeconds = 0;
                int resolvedCount = 0;

                foreach (var gigSong in gig.songs)
                {
                    var entry = ResolveFromCache(careerId, gigSong);
                    if (entry != null)
                    {
                        totalSeconds += entry.SongLengthSeconds;
                        resolvedCount++;
                    }
                }

                if (resolvedCount > 0)
                {
                    cache.gigDurations[gig.name] = new GigDurationData
                    {
                        totalSeconds = totalSeconds,
                        avgTrackSeconds = totalSeconds / resolvedCount,
                        resolvedSongCount = resolvedCount
                    };
                }
            }

            YargLogger.LogInfo(
                $"SongMatchCache: Computed durations for {cache.gigDurations.Count} gigs in career '{careerId}'");
        }

        /// <summary>
        /// Saves the in-memory cache for a career to disk and recalculates summary counts.
        /// </summary>
        public static void SaveCache(string careerId)
        {
            if (!_caches.TryGetValue(careerId, out var cache))
            {
                return;
            }

            // Recalculate summary counts
            cache.totalSongs = cache.matches.Count;
            cache.hashMatches = 0;
            cache.exactMatches = 0;
            cache.fuzzyMatches = 0;
            cache.manualMatches = 0;
            cache.unmatched = 0;

            foreach (var match in cache.matches)
            {
                switch (match.method)
                {
                    case MatchMethod.Hash:
                        cache.hashMatches++;
                        break;
                    case MatchMethod.ExactTitleArtist:
                    case MatchMethod.ExactTitleArtistSource:
                        cache.exactMatches++;
                        break;
                    case MatchMethod.Fuzzy:
                        cache.fuzzyMatches++;
                        break;
                    case MatchMethod.Manual:
                        cache.manualMatches++;
                        break;
                    default:
                        cache.unmatched++;
                        break;
                }
            }

            cache.lastAnalyzed = DateTime.UtcNow.ToString("o");

            try
            {
                string json = JsonConvert.SerializeObject(cache, _jsonSettings);
                string path = GetCachePath(careerId);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                YargLogger.LogException(ex, $"Failed to save match cache for career '{careerId}'");
            }
        }

        /// <summary>
        /// Removes the cache for a career from memory and disk.
        /// Used when re-analyzing from scratch.
        /// </summary>
        public static void ClearCache(string careerId)
        {
            _caches.Remove(careerId);

            string path = GetCachePath(careerId);
            if (File.Exists(path))
            {
                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    YargLogger.LogException(ex, $"Failed to delete match cache for career '{careerId}'");
                }
            }
        }

        /// <summary>
        /// Clears ALL caches from memory. Does not delete files.
        /// Call when the song library is rescanned.
        /// </summary>
        public static void InvalidateAll()
        {
            _caches.Clear();
        }

        private static CareerMatchCache GetOrCreateCache(string careerId)
        {
            var cache = GetOrLoadCache(careerId);
            if (cache != null)
            {
                return cache;
            }

            cache = new CareerMatchCache
            {
                careerId = careerId,
                matches = new List<SongMatch>()
            };

            _caches[careerId] = cache;
            return cache;
        }

        private static string GetCachePath(string careerId)
        {
            return Path.Combine(_matchesDirectory, $"{careerId}_matches.json");
        }
    }
}