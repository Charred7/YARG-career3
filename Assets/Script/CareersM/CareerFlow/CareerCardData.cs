using UnityEngine;

namespace YARG.Menu.Career.CareerFlow
{
    /// <summary>
    /// Pure data-transfer struct that bridges career/tour data to the card's visual components.
    /// Built by CareerFlowMenuController and consumed by CareerFlowController.PopulateCard().
    ///
    /// Contains ALL data the card needs to render — the card makes zero decisions.
    /// </summary>
    public struct CareerCardData
    {
        /// <summary>Unique career identifier (e.g. "yarg1-authorname").</summary>
        public string careerId;

        /// <summary>Display name for the career/tour (shown on crate if displayNameOnCrate is true).</summary>
        public string careerName;

        /// <summary>Box art sprite loaded from career.artworkPath.</summary>
        public Sprite boxArtSprite;

        /// <summary>Source icon key for the UI (e.g. "yarg1", "yarn2").</summary>
        public string sourceIcon;

        /// <summary>Bundle author name.</summary>
        public string author;

        /// <summary>Total number of gigs in this career.</summary>
        public int gigCount;

        /// <summary>Number of gigs the player has completed.</summary>
        public int completedGigs;

        /// <summary>Completion percentage (0.0–1.0).</summary>
        public float completionPercent;

        /// <summary>True if all gigs in this career have been completed.</summary>
        public bool isCompleted;

        /// <summary>
        /// Pre-computed narrative status line displayed at the bottom of the art area.
        /// Examples: "• 5 GIGS REMAINING •", "• TOUR COMPLETED •", "• NOT STARTED •".
        /// </summary>
        public string statusText;

        /// <summary>
        /// Campaign accent colour parsed from careerdef.ini (e.g. #ffb947 for World Tour).
        /// Used for the crate rim accent and progress bar fill.
        /// </summary>
        public Color accentColor;

        /// <summary>
        /// If true, the career name text is displayed on the crate card.
        /// Controlled by the display_name_on_crate key in careerdef.ini.
        /// Defaults to false (name hidden — box art speaks for itself).
        /// </summary>
        public bool displayNameOnCrate;

        /// <summary>
        /// Whether this career has CoverFlow enabled (use_coverflow = true in careerdef.ini).
        /// Used by CareerFlowMenuController to determine the Green-confirm routing target.
        /// </summary>
        public bool useCoverFlow;
    }
}