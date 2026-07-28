namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Static global campaign averages used by HypeEngine Rule 6 (Stamina Mitigation).
    /// Populated by CoverFlowController.SetCareer() when a campaign loads.
    /// </summary>
    public static class CampaignMetrics
    {
        /// <summary>
        /// Average difficulty rating across all gigs in the current campaign.
        /// Used by Rule 6 to determine if a gig is "casual" relative to the campaign.
        /// </summary>
        public static float GlobalAverageDifficulty;

        /// <summary>
        /// Average note density across all gigs in the current campaign.
        /// Used by Rule 6 as a secondary comparison metric.
        /// </summary>
        public static float GlobalAverageNoteDensity;
    }
}