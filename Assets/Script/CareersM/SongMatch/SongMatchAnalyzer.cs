using System;
using System.Collections.Generic;
using UnityEngine;
using YARG.Core.Song;
using YARG.Song;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Analyzes a career's songs against the user's library, producing
    /// <see cref="SongMatch"/> results with confidence scores.
    ///
    /// Match hierarchy (auto-resolve):
    ///   1. Hash                              → 100%
    ///   2. Title + Artist + Source (exact)    →  95%
    ///
    /// Manual match candidate scoring:
    ///   Title exact + artist + source         →  90%
    ///   Title exact + artist                  →  85%
    ///   Title variant + artist + source       →  75%  (e.g. "One (Live)")
    ///   Title variant + artist                →  70%
    ///   Title exact (no artist)               →  60%
    ///   Title variant (no artist)             →  50%
    ///   Artist + Genre                        →  30%
    ///   Genre only                            →  15%
    ///
    /// Fallback padding (always fills to maxResults):
    ///   Artist only                           →   5%
    ///   Genre only (no scored match)          →   3%
    ///   Source only                           →   1%
    /// </summary>
    public static class SongMatchAnalyzer
    {
        // ── Deterministic confidence tiers ────────────────────────────────
        private const float CONF_HASH                 = 1.00f;
        private const float CONF_TITLE_ARTIST_SOURCE  = 0.95f;

        // ── AnalyzeCareer ────────────────────────────────────────────────

        public static CareerMatchCache AnalyzeCareer(string careerId, List<GigInfo> gigs)
        {
            var allSongs = new Dictionary<string, GigSong>();
            foreach (var gig in gigs)
            {
                if (gig.songs == null) continue;
                foreach (var song in gig.songs)
                {
                    string key = SongMatch.CreateKey(song);
                    if (!allSongs.ContainsKey(key))
                    {
                        allSongs[key] = song;
                    }
                }
            }

            foreach (var kvp in allSongs)
            {
                var gigSong = kvp.Value;

                var existing = SongMatchCache.GetMatch(careerId, gigSong);
                if (existing != null && existing.manuallyConfirmed)
                {
                    continue;
                }

                var match = MatchSong(gigSong);
                SongMatchCache.SetMatch(careerId, match);
            }

            // Compute and cache per-gig song durations for the HypeEngine
            SongMatchCache.ComputeGigDurations(careerId, gigs);

            SongMatchCache.SaveCache(careerId);
            return SongMatchCache.GetOrLoadCache(careerId);
        }

        // ── MatchSong ────────────────────────────────────────────────────

        public static SongMatch MatchSong(GigSong gigSong)
        {
            var match = new SongMatch
            {
                gigSongKey = SongMatch.CreateKey(gigSong),
                title = gigSong.title,
                artist = gigSong.artist,
                source = gigSong.source
            };

            // ── 1. Hash (100%) ───────────────────────────────────────────
            if (!string.IsNullOrEmpty(gigSong.hash))
            {
                var hash = HashWrapper.FromString(gigSong.hash);
                if (SongContainer.SongsByHash.TryGetValue(hash, out var songs))
                {
                    match.resolvedHash = songs[0].Hash.ToString();
                    match.method = MatchMethod.Hash;
                    match.confidence = CONF_HASH;
                    return match;
                }
            }

            // ── 2. Exact title + artist + source (95%) ───────────────────
            if (!string.IsNullOrEmpty(gigSong.title) && !string.IsNullOrEmpty(gigSong.artist))
            {
                SongEntry bestExact = null;
                bool hasSourceMatch = false;

                foreach (var song in SongContainer.Songs)
                {
                    bool titleEq = string.Equals(
                        song.Name.ToString(), gigSong.title,
                        StringComparison.OrdinalIgnoreCase);
                    bool artistEq = string.Equals(
                        song.Artist.ToString(), gigSong.artist,
                        StringComparison.OrdinalIgnoreCase);

                    if (titleEq && artistEq)
                    {
                        bool sourceEq = !string.IsNullOrEmpty(gigSong.source) &&
                            string.Equals(song.Source.ToString(), gigSong.source,
                                StringComparison.OrdinalIgnoreCase);

                        if (bestExact == null || (sourceEq && !hasSourceMatch))
                        {
                            bestExact = song;
                            hasSourceMatch = sourceEq;
                        }

                        if (hasSourceMatch) break;
                    }
                }

                if (bestExact != null && hasSourceMatch)
                {
                    match.resolvedHash = bestExact.Hash.ToString();
                    match.method = MatchMethod.ExactTitleArtistSource;
                    match.confidence = CONF_TITLE_ARTIST_SOURCE;
                    return match;
                }
            }

            match.method = MatchMethod.None;
            match.confidence = 0f;
            return match;
        }

        // ── FindFuzzyMatches (manual match UI) ───────────────────────────

        /// <summary>
        /// Finds candidates for the manual match UI, always filling to maxResults.
        /// Phase 1: Score all songs (exact/variant title, artist, genre, source).
        /// Phase 2–4: Pad with artist-only, genre-only, source-only fallbacks.
        /// </summary>
        public static List<(SongEntry Song, float Confidence)> FindFuzzyMatches(
            GigSong gigSong, int maxResults = 15)
        {
            var scored = ScoreCandidates(gigSong);

            var result = new List<(SongEntry Song, float Confidence)>();
            var seen = new HashSet<string>();

            // Take scored results that have any signal
            foreach (var candidate in scored)
            {
                if (result.Count >= maxResults) break;
                if (candidate.Confidence <= 0f) continue;

                string h = candidate.Song.Hash.ToString();
                if (seen.Contains(h)) continue;
                seen.Add(h);
                result.Add(candidate);
            }

            // Phase 2: Pad with artist-only matches
            if (result.Count < maxResults)
            {
                string targetArtist = (gigSong.artist ?? "").ToLowerInvariant().Trim();
                if (!string.IsNullOrEmpty(targetArtist))
                {
                    foreach (var song in SongContainer.Songs)
                    {
                        if (result.Count >= maxResults) break;
                        string h = song.Hash.ToString();
                        if (seen.Contains(h)) continue;

                        string songArtist = song.Artist.ToString().ToLowerInvariant().Trim();
                        if (string.Equals(targetArtist, songArtist, StringComparison.Ordinal))
                        {
                            seen.Add(h);
                            result.Add((song, 0.05f));
                        }
                    }
                }
            }

            // Phase 3: Pad with genre-only matches
            if (result.Count < maxResults)
            {
                string targetGenre = (gigSong.genre ?? "").ToLowerInvariant().Trim();
                if (!string.IsNullOrEmpty(targetGenre))
                {
                    foreach (var song in SongContainer.Songs)
                    {
                        if (result.Count >= maxResults) break;
                        string h = song.Hash.ToString();
                        if (seen.Contains(h)) continue;

                        string songGenre = song.Genre.ToString().ToLowerInvariant().Trim();
                        if (string.Equals(targetGenre, songGenre, StringComparison.Ordinal))
                        {
                            seen.Add(h);
                            result.Add((song, 0.03f));
                        }
                    }
                }
            }

            // Phase 4: Pad with same-source matches
            if (result.Count < maxResults)
            {
                string targetSource = (gigSong.source ?? "").ToLowerInvariant().Trim();
                if (!string.IsNullOrEmpty(targetSource))
                {
                    foreach (var song in SongContainer.Songs)
                    {
                        if (result.Count >= maxResults) break;
                        string h = song.Hash.ToString();
                        if (seen.Contains(h)) continue;

                        string songSource = song.Source.ToString().ToLowerInvariant().Trim();
                        if (string.Equals(targetSource, songSource, StringComparison.Ordinal))
                        {
                            seen.Add(h);
                            result.Add((song, 0.01f));
                        }
                    }
                }
            }

            return result;
        }

        // ── Core Scoring ─────────────────────────────────────────────────

        /// <summary>
        /// Scores every song in the library against a GigSong.
        /// Title matching is STRICT: exact match or variant match only.
        /// A "variant" is when one title contains the other after stripping
        /// parenthetical suffixes like (Live), (Remastered), (WaveGroup), etc.
        /// No Levenshtein fuzzy — "Mother" will NOT match "Monsters".
        /// </summary>
        private static List<(SongEntry Song, float Confidence)> ScoreCandidates(GigSong gigSong)
        {
            var candidates = new List<(SongEntry Song, float Confidence)>();

            string targetTitle = (gigSong.title ?? "").ToLowerInvariant().Trim();
            string targetArtist = (gigSong.artist ?? "").ToLowerInvariant().Trim();
            string targetGenre = (gigSong.genre ?? "").ToLowerInvariant().Trim();
            string targetSource = (gigSong.source ?? "").ToLowerInvariant().Trim();

            bool hasTitle = !string.IsNullOrEmpty(targetTitle);
            bool hasArtist = !string.IsNullOrEmpty(targetArtist);
            bool hasGenre = !string.IsNullOrEmpty(targetGenre);
            bool hasSource = !string.IsNullOrEmpty(targetSource);

            string targetTitleBase = hasTitle ? StripParenthetical(targetTitle) : "";

            foreach (var song in SongContainer.Songs)
            {
                string songTitle = song.Name.ToString().ToLowerInvariant().Trim();
                string songArtist = song.Artist.ToString().ToLowerInvariant().Trim();
                string songGenre = song.Genre.ToString().ToLowerInvariant().Trim();
                string songSource = song.Source.ToString().ToLowerInvariant().Trim();

                bool artistExact = hasArtist &&
                    string.Equals(targetArtist, songArtist, StringComparison.Ordinal);
                bool genreExact = hasGenre &&
                    string.Equals(targetGenre, songGenre, StringComparison.Ordinal);
                bool sourceExact = hasSource &&
                    string.Equals(targetSource, songSource, StringComparison.Ordinal);

                float score = 0f;

                // ── Title matching (strict) ──────────────────────────────
                bool titleExact = false;
                bool titleVariant = false;

                if (hasTitle)
                {
                    if (string.Equals(targetTitle, songTitle, StringComparison.Ordinal))
                    {
                        titleExact = true;
                    }
                    else
                    {
                        // Check if base titles match after stripping (Live), (Remastered), etc.
                        string songTitleBase = StripParenthetical(songTitle);
                        if (!string.IsNullOrEmpty(targetTitleBase) &&
                            !string.IsNullOrEmpty(songTitleBase) &&
                            string.Equals(targetTitleBase, songTitleBase, StringComparison.Ordinal))
                        {
                            titleVariant = true;
                        }
                    }
                }

                // ── Score based on what matched ──────────────────────────

                if (titleExact)
                {
                    // Title exact + artist + source = 90%
                    // Title exact + artist          = 85%
                    // Title exact alone             = 60%
                    if (artistExact && sourceExact)     score = 0.90f;
                    else if (artistExact)               score = 0.85f;
                    else                                score = 0.60f;
                }
                else if (titleVariant)
                {
                    // Title variant + artist + source = 75%
                    // Title variant + artist          = 70%
                    // Title variant alone             = 50%
                    if (artistExact && sourceExact)     score = 0.75f;
                    else if (artistExact)               score = 0.70f;
                    else                                score = 0.50f;
                }

                // ── Non-title paths ──────────────────────────────────────

                // Artist + Genre (no title match)
                if (score < 0.30f && artistExact && genreExact)
                {
                    score = Mathf.Max(score, 0.30f);
                }

                // Genre only
                if (score < 0.15f && genreExact)
                {
                    score = Mathf.Max(score, 0.15f);
                }

                if (score > 0f)
                {
                    candidates.Add((song, score));
                }
            }

            candidates.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
            return candidates;
        }

        // ── Title Variant Helpers ────────────────────────────────────────

        /// <summary>
        /// Strips parenthetical suffixes from a title.
        /// "One (Live)" → "one"
        /// "Hit Me With Your Best Shot (Remastered)" → "hit me with your best shot"
        /// "Bark at the Moon" → "bark at the moon" (unchanged, no parens)
        /// </summary>
        private static string StripParenthetical(string title)
        {
            if (string.IsNullOrEmpty(title)) return title;

            int parenIdx = title.IndexOf('(');
            if (parenIdx > 0)
            {
                return title.Substring(0, parenIdx).Trim();
            }

            return title;
        }

        // ── Manual Match ─────────────────────────────────────────────────

        public static void SetManualMatch(string careerId, GigSong gigSong, SongEntry songEntry, float originalConfidence)
        {
            var match = new SongMatch
            {
                gigSongKey = SongMatch.CreateKey(gigSong),
                title = gigSong.title,
                artist = gigSong.artist,
                source = gigSong.source,
                resolvedHash = songEntry.Hash.ToString(),
                method = MatchMethod.Manual,
                confidence = originalConfidence,
                manuallyConfirmed = true
            };

            SongMatchCache.SetMatch(careerId, match, force: true);
            SongMatchCache.SaveCache(careerId);
        }

        public static void ClearManualMatch(string careerId, GigSong gigSong)
        {
            var match = SongMatchCache.GetMatch(careerId, gigSong);
            if (match != null && match.manuallyConfirmed)
            {
                match.manuallyConfirmed = false;
                match.method = MatchMethod.None;
                match.resolvedHash = null;
                match.confidence = 0f;
                SongMatchCache.SaveCache(careerId);
            }
        }
    }
}