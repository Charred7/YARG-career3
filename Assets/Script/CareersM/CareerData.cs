using System;
using System.Collections.Generic;
using YARG.Career;

namespace YARG.Menu.Career
{
    [Serializable]
    public class CareerDatabase
    {
        public List<CareerInfo> careers = new();
    }

    [Serializable]
    public class CareerInfo
    {
        /// <summary>Unique ID derived from the bundle folder name, e.g. "yarg1-authorname".</summary>
        public string id;

        /// <summary>Display name from careerdef.ini.</summary>
        public string name;

        /// <summary>Absolute path to the artwork image file.</summary>
        public string artworkPath;

        /// <summary>Source icon key for the UI (e.g. "yarg1", "yarn2").</summary>
        public string sourceIcon;

        /// <summary>Absolute path to the gigs JSON file.</summary>
        public string gigsPath;

        /// <summary>Bundle author name.</summary>
        public string author;

        /// <summary>The id field from careerdef.ini (not necessarily unique across bundles).</summary>
        public string bundleId;

        /// <summary>Absolute path to the bundle folder on disk.</summary>
        public string bundlePath;

        /// <summary>Release date from careerdef.ini (YYYY-MM-DD). Empty if unspecified.</summary>
        public string releaseDate;

        // ── CoverFlow Configuration ─────────────────────────────────

        /// <summary>
        /// Parsed CoverFlow visual and HypeEngine configuration.
        /// Populated by CareerBundleManager during scan.
        /// Null if careerdef.ini has no CoverFlow sections.
        /// </summary>
        public CoverFlow.CoverFlowConfig CoverFlowConfig;

        /// <summary>
        /// Absolute path to the gig poster config file (JSON/INI mapping gig names
        /// to poster image filenames). Null if not provided.
        /// </summary>
        public string gigPosterConfigPath;

        /// <summary>
        /// If true, all gigs use <see cref="artworkPath"/> as their poster.
        /// Only used when <see cref="gigPosterConfigPath"/> is null.
        /// </summary>
        public bool useArtAsGigPoster;

        /// <summary>
        /// Absolute path to a gig layout INI file for per-gig layout overrides.
        /// Null if not provided.
        /// </summary>
        public string gigLayoutPath;

        /// <summary>
        /// Tier-based progression configuration parsed from <c>careerdef.ini</c>
        /// sections <c>[CampaignSettings]</c> and <c>[Tier_N]</c>.
        ///
        /// Null for legacy bundles that do not define any tier sections.
        /// When non-null, consumed by <see cref="CampaignProgressionManager"/>
        /// to gate gig access based on cumulative completed-gig thresholds.
        /// </summary>
        public CampaignProgressionData ProgressionData;
    }

    [Serializable]
    public class GigDatabase
    {
        public List<GigInfo> gigs = new();
    }

    [Serializable]
    public class GigSong
    {
        /// <summary>SHA1 hex string. Primary lookup key.</summary>
        public string hash;

        /// <summary>Song title for fallback matching.</summary>
        public string title;

        /// <summary>Artist name for fallback matching.</summary>
        public string artist;

        /// <summary>Source tag (e.g. "yarg", "yarn") for match prioritization.</summary>
        public string source;

        /// <summary>Genre tag for candidate ranking.</summary>
        public string genre;
    }

    [Serializable]
    public class GigInfo
    {
        public string name;

        public List<GigSong> songs = new();

        /// <summary>Index of the encore song, or -1 if none.</summary>
        public int encoreIndex = -1;

        public bool HasEncore => encoreIndex >= 0 && encoreIndex < songs.Count;
    }
}