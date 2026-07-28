using System;
using System.Collections.Generic;

namespace YARG.Menu.Career
{
    /// <summary>
    /// How a match was determined between a career song and a library song.
    /// </summary>
    public enum MatchMethod
    {
        /// <summary>No match found.</summary>
        None,

        /// <summary>Exact SHA1 hash match — 100%.</summary>
        Hash,

        /// <summary>Exact title + artist + source — 95%.</summary>
        ExactTitleArtistSource,

        /// <summary>Exact title + artist — 90%.</summary>
        ExactTitleArtist,

        /// <summary>Fuzzy match (weighted scoring of title/artist/genre/source).</summary>
        Fuzzy,

        /// <summary>User manually confirmed this match.</summary>
        Manual
    }

    /// <summary>
    /// A single resolved match between a <see cref="GigSong"/> in the career
    /// and a <see cref="YARG.Core.Song.SongEntry"/> in the user's library.
    /// </summary>
    [Serializable]
    public class SongMatch
    {
        /// <summary>
        /// Key identifying the career song. Uses the GigSong hash if available,
        /// otherwise falls back to "title|artist".
        /// </summary>
        public string gigSongKey;

        /// <summary>Original title from the career gig data.</summary>
        public string title;

        /// <summary>Original artist from the career gig data.</summary>
        public string artist;

        /// <summary>Original source tag from the career gig data.</summary>
        public string source;

        /// <summary>
        /// The hash (as hex string) of the resolved SongEntry in the user's library.
        /// Null/empty if unresolved.
        /// </summary>
        public string resolvedHash;

        /// <summary>
        /// How this match was determined.
        /// </summary>
        public MatchMethod method = MatchMethod.None;

        /// <summary>
        /// Confidence score from 0.0 to 1.0.
        /// </summary>
        public float confidence;

        /// <summary>
        /// Whether the user has manually confirmed or overridden this match.
        /// Manual confirmations are never overwritten by auto-analysis.
        /// </summary>
        public bool manuallyConfirmed;

        /// <summary>Whether this match resolved to a song in the library.</summary>
        public bool IsResolved => !string.IsNullOrEmpty(resolvedHash) && method != MatchMethod.None;

        /// <summary>
        /// Creates a stable key for a <see cref="GigSong"/>.
        /// Prefers hash, falls back to "title|artist" (lowercased).
        /// </summary>
        public static string CreateKey(GigSong gigSong)
        {
            if (!string.IsNullOrEmpty(gigSong.hash))
            {
                return $"hash:{gigSong.hash}";
            }

            string t = (gigSong.title ?? "").ToLowerInvariant().Trim();
            string a = (gigSong.artist ?? "").ToLowerInvariant().Trim();
            return $"meta:{t}|{a}";
        }
    }

    /// <summary>
    /// Cached duration data for a single gig — used by the HypeEngine
    /// for Tier 2 micro-time efficiency rules.
    /// </summary>
    [Serializable]
    public class GigDurationData
    {
        /// <summary>Total play time of all resolved songs in the gig, in seconds.</summary>
        public double totalSeconds;

        /// <summary>Average track duration across resolved songs, in seconds.</summary>
        public double avgTrackSeconds;

        /// <summary>Number of resolved songs that contributed to this data.</summary>
        public int resolvedSongCount;
    }

    /// <summary>
    /// The full set of song matches for a single career, persisted to disk.
    /// Stored per-career (not per-band) since library matches are independent
    /// of which band is playing.
    /// </summary>
    [Serializable]
    public class CareerMatchCache
    {
        /// <summary>The career ID these matches belong to.</summary>
        public string careerId;

        /// <summary>All song matches for this career, keyed by <see cref="SongMatch.gigSongKey"/>.</summary>
        public List<SongMatch> matches = new();

        /// <summary>ISO 8601 timestamp of last analysis run.</summary>
        public string lastAnalyzed;

        /// <summary>
        /// Summary counts — stored for quick display without iterating all matches.
        /// </summary>
        public int totalSongs;
        public int hashMatches;
        public int exactMatches;
        public int fuzzyMatches;
        public int manualMatches;
        public int unmatched;

        /// <summary>
        /// Per-gig duration cache for the HypeEngine.
        /// Key is the gig name, value is total and average duration of resolved songs.
        /// Populated during analysis; queried at runtime by Tier 2 rules.
        /// </summary>
        public Dictionary<string, GigDurationData> gigDurations = new();
    }
}