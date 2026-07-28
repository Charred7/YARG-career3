using System.Collections.Generic;
using UnityEngine;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Pure data-transfer struct that bridges campaign/gig data to the card's visual components.
    /// Built by CoverFlowController and consumed by CoverFlowCardControllerV3.PopulateCard().
    ///
    /// Contains ALL data the card needs to render — the card makes zero decisions.
    /// </summary>
    public struct GigCardData
    {
        /// <summary>Unique gig identifier (format: "{careerId}|{gigName}").</summary>
        public string gigID;

        /// <summary>Display title for the gig (shown in uppercase on the card).</summary>
        public string gigTitle;

        /// <summary>Poster sprite for the card's main image area.</summary>
        public Sprite posterSprite;

        /// <summary>Optional band/game logo sprite. Null = hidden.</summary>
        public Sprite bandLogoSprite;

        /// <summary>Resolved tracklist with per-song metadata.</summary>
        public List<SongChartData> tracklist;

        /// <summary>Number of gigs remaining in the current venue tier (for Rule 1).</summary>
        public int remainingGigsInTier;

        /// <summary>Total gigs remaining in the entire campaign file (for Rule 2).</summary>
        public int totalRemainingGigsInFile;

        /// <summary>Whether this gig is locked and cannot be played yet.</summary>
        public bool isLocked;

        /// <summary>Whether this gig has been completed.</summary>
        public bool isCompleted;

        /// <summary>
        /// True when the gig has a flagged encore song. The encore itself is excluded
        /// from <see cref="tracklist"/>; the card shows a +ENCORE! teaser instead.
        /// </summary>
        public bool hasEncore;

        /// <summary>Pre-computed HypeEngine slot 1 string (AvgTrackText).</summary>
        public string hypeSlot1;

        /// <summary>Pre-computed HypeEngine slot 2 string (IntensityText).</summary>
        public string hypeSlot2;

        /// <summary>Icon name (resolved via HypeIconSet).</summary>
        public string hypeIcon;

        /// <summary>
        /// Campaign accent colour parsed from careerdef.ini (e.g. #ffb947 for World Tour).
        /// Passed through to CoverFlowCardControllerV3 for the headliner dual-pulse animation.
        /// </summary>
        public Color accentColor;
    }
}