using System;
using System.Collections.Generic;

namespace YARG.Career
{
    /// <summary>
    /// Tier-based campaign progression configuration parsed from
    /// <c>careerdef.ini</c> section blocks <c>[CampaignSettings]</c>
    /// and <c>[Tier_N]</c>.
    ///
    /// Null (or empty <see cref="Tiers"/>) means no tier-based gating
    /// is configured — all gigs are treated as unlocked (backward
    /// compatible with legacy bundles).
    /// </summary>
    [Serializable]
    public class CampaignProgressionData
    {
        /// <summary>
        /// When <c>true</c>, all tiers are unlocked regardless of
        /// the player's completed-gig count. Parsed from the
        /// <c>unlockall</c> key in <c>[CampaignSettings]</c>.
        /// </summary>
        public bool UnlockAll;

        /// <summary>
        /// Tier definitions parsed from <c>[Tier_0]</c>, <c>[Tier_1]</c>,
        /// etc., sorted by <see cref="TierData.TierIndex"/> ascending.
        /// Populated by <see cref="Menu.Career.CareerDefParser"/>.
        /// </summary>
        public List<TierData> Tiers = new();
    }

    /// <summary>
    /// A single tier block parsed from a <c>[Tier_N]</c> section in
    /// <c>careerdef.ini</c>.
    /// </summary>
    [Serializable]
    public class TierData
    {
        /// <summary>Parsed from the section header, e.g. <c>0</c> for <c>[Tier_0]</c>.</summary>
        public int TierIndex;

        /// <summary>Display name from the <c>name</c> key, e.g. "Opening Acts".</summary>
        public string Name;

        /// <summary>Number of gigs declared in this tier via <c>gigs_in_tier</c>.</summary>
        public int GigsInTier;

        /// <summary>
        /// Cumulative total of completed gigs required across the entire
        /// campaign before this tier unlocks. Specified directly in the
        /// INI via <c>gigs_required_to_unlock</c> (not derived by the parser).
        /// Tier 0 defaults to 0 if omitted.
        /// </summary>
        public int GigsRequiredToUnlock;
    }
}