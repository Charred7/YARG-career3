namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// The visual state of a card in the CoverFlow carousel.
    /// </summary>
    public enum CoverFlowCardState
    {
        /// <summary>
        /// Gig has been completed. Poster is grayscale + "SOLD OUT" / "CRUSHED" stamp.
        /// </summary>
        Completed,

        /// <summary>
        /// Gig is the current focused selection. Full detail, full opacity.
        /// </summary>
        Active,

        /// <summary>
        /// Gig is locked/upcoming. Silhouette block + lock/question-mark icon.
        /// </summary>
        Locked
    }
}