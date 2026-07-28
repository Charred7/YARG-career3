namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Lightweight per-song metadata struct consumed by CoverFlowCardControllerV3
    /// for tracklist display and HypeEngine calculations.
    /// </summary>
    [System.Serializable]
    public struct SongChartData
    {
        /// <summary>Song title for display.</summary>
        public string title;

        /// <summary>Artist name for headliner checks.</summary>
        public string artist;

        /// <summary>Track length in seconds.</summary>
        public float durationSeconds;

        /// <summary>Numerical difficulty tier (0 = easiest).</summary>
        public int difficultyRating;

        /// <summary>Total note count for density calculations.</summary>
        public int noteCount;

        /// <summary>
        /// True if this song's artist matches a headliner/iconic artist
        /// for this campaign. Used by CoverFlowCardControllerV3 to enable
        /// the dual-pulse vertex-color animation on headliner tracks.
        /// </summary>
        public bool isHeadliner;
    }
}