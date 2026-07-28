using System;
using System.Text;
using YARG.Core.Logging;
using YARG.Menu.Career;

namespace YARG.Career
{
    /// <summary>
    /// Singleton state manager for campaign tier-based progression.
    ///
    /// Consumes <see cref="CampaignProgressionData"/> (parsed from <c>careerdef.ini</c>)
    /// and provides O(1) lookups for lock-state evaluation based on the player's
    /// cumulative completed-gig count.
    ///
    /// Also handles "orphaned" gig detection: if the total number of gigs defined
    /// across all <c>[Tier_N]</c> blocks is less than the actual number of gigs
    /// present in the campaign, the surplus gigs are automatically appended to
    /// the final tier so no content is stranded.
    /// </summary>
    public class CampaignProgressionManager
    {
        private static CampaignProgressionManager _instance;
        public static CampaignProgressionManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new CampaignProgressionManager();
                return _instance;
            }
        }

        // ── Internal State ──────────────────────────────────────────────

        private CampaignProgressionData _progressionData;
        private int _totalGigCount;

        /// <summary>
        /// Maps gigIndex → tierIndex. Built once during <see cref="Initialize"/>.
        /// Allocates an array of size <paramref name="totalGigCount"/>.
        /// </summary>
        private int[] _gigToTierMap;

        /// <summary>
        /// Whether any tier configuration exists. If false, all gigs are unlocked.
        /// </summary>
        private bool _hasTierConfig;

        // ── Properties ─────────────────────────────────────────────────

        /// <summary>
        /// The currently loaded progression data, or null if none is configured.
        /// </summary>
        public CampaignProgressionData ProgressionData => _progressionData;

        /// <summary>
        /// Total number of tiers defined in the current campaign's progression data.
        /// Returns 0 if no tier config is present (all gigs unlocked).
        /// </summary>
        public int TierCount => _progressionData?.Tiers?.Count ?? 0;

        // ── Initialization ──────────────────────────────────────────────

        /// <summary>
        /// Call once when a campaign is opened (e.g., from
        /// <see cref="CoverFlowController.SetCareer"/> or
        /// <see cref="GigView.SetCareer"/>).
        ///
        /// Builds the internal gig-to-tier mapping and detects orphaned gigs.
        /// </summary>
        /// <param name="career">The career info containing progression data.</param>
        /// <param name="totalGigCount">Total number of gigs in the campaign's gigs file.</param>
        public void Initialize(CareerInfo career, int totalGigCount)
        {
            _progressionData = career?.ProgressionData;
            _totalGigCount = totalGigCount;
            _gigToTierMap = null;
            _hasTierConfig = false;

            // ── DIAGNOSTIC: Log whether ProgressionData loaded ──
            UnityEngine.Debug.Log($"[CampaignProgressionManager] Initialize: career='{career?.name}' (id={career?.id}), " +
                $"ProgressionData==null={_progressionData == null}, totalGigCount={totalGigCount}, " +
                $"UnlockAll={_progressionData?.UnlockAll}, Tiers.Count={_progressionData?.Tiers?.Count}");

            if (_progressionData != null && _progressionData.Tiers != null)
            {
                foreach (var t in _progressionData.Tiers)
                {
                    UnityEngine.Debug.Log($"[CampaignProgressionManager]   Tier[{t.TierIndex}]: name='{t.Name}', " +
                        $"gigsInTier={t.GigsInTier}, requiredToUnlock={t.GigsRequiredToUnlock}");
                }
            }

            // No progression data or no tiers defined → no gating
            if (_progressionData == null || _progressionData.Tiers == null || _progressionData.Tiers.Count == 0)
            {
                YargLogger.LogDebug("CampaignProgressionManager: No tier config — all gigs unlocked.");
                return;
            }

            if (totalGigCount <= 0)
            {
                YargLogger.LogDebug("CampaignProgressionManager: No gigs in campaign — nothing to map.");
                return;
            }

            _hasTierConfig = true;
            _gigToTierMap = new int[totalGigCount];

            int nextSlot = 0;
            int lastTierIndex = 0;

            // 1. Assign gigs to tiers based on declared GigsInTier
            foreach (var tier in _progressionData.Tiers)
            {
                int slotsToAssign = Math.Max(0, tier.GigsInTier);
                int endSlot = Math.Min(nextSlot + slotsToAssign, totalGigCount);

                for (int i = nextSlot; i < endSlot; i++)
                {
                    _gigToTierMap[i] = tier.TierIndex;
                }

                nextSlot = endSlot;
                lastTierIndex = tier.TierIndex;

                if (nextSlot >= totalGigCount)
                    break;
            }

            // 2. Detect and handle orphaned gigs
            if (nextSlot < totalGigCount)
            {
                int orphanCount = totalGigCount - nextSlot;

                YargLogger.LogWarning(
                    $"CampaignProgressionManager: {orphanCount} orphaned gig(s) detected " +
                    $"(gigs_in_tier sum = {nextSlot}, total gigs = {totalGigCount}). " +
                    $"Appending to final tier (Tier_{lastTierIndex}).");

                for (int i = nextSlot; i < totalGigCount; i++)
                {
                    _gigToTierMap[i] = lastTierIndex;
                }
            }

            LogMappingSummary();
        }

        /// <summary>
        /// Logs a summary of the gig-to-tier mapping for debugging.
        /// </summary>
        private void LogMappingSummary()
        {
            if (!_hasTierConfig || _gigToTierMap == null)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("CampaignProgressionManager: Gig-to-Tier mapping:");

            for (int i = 0; i < _totalGigCount; i++)
            {
                int tierIdx = _gigToTierMap[i];
                var tier = GetTierData(tierIdx);
                string tierName = tier?.Name ?? $"Tier_{tierIdx}";
                sb.AppendLine($"  Gig[{i}] → {tierName} (tier {tierIdx})");
            }

            YargLogger.LogDebug(sb.ToString().TrimEnd());
        }

        // ── Queries ─────────────────────────────────────────────────────

        /// <summary>
        /// Returns <c>true</c> if the gig at the given index is locked
        /// based on the player's total completed gig count and the tier
        /// thresholds defined in the campaign config.
        ///
        /// Short-circuits on <c>unlockall = true</c> (returns unlocked). 
        /// </summary>
        /// <param name="gigIndex">Zero-based gig index in the campaign's gig list.</param>
        /// <param name="totalCompletedGigs">The cumulative number of gigs the player has completed.</param>
        public bool IsGigLocked(int gigIndex, int totalCompletedGigs)
        {
            // Bounds check
            if (gigIndex < 0 || gigIndex >= _totalGigCount)
                return true;

            // No tier config → all unlocked
            if (!_hasTierConfig || _gigToTierMap == null)
                return false;

            // Global override
            if (_progressionData.UnlockAll)
                return false;

            // Look up tier for this gig
            int tierIndex = _gigToTierMap[gigIndex];
            var tier = GetTierData(tierIndex);
            if (tier == null)
                return false;

            // Evaluate cumulative threshold
            return totalCompletedGigs < tier.GigsRequiredToUnlock;
        }

        /// <summary>
        /// Returns the tier index that the given gig belongs to.
        /// Returns -1 if no tier configuration is present.
        /// </summary>
        public int GetTierForGig(int gigIndex)
        {
            if (gigIndex < 0 || gigIndex >= _totalGigCount || _gigToTierMap == null)
                return -1;

            return _gigToTierMap[gigIndex];
        }

        /// <summary>
        /// Returns the <see cref="TierData"/> for a given tier index,
        /// or null if the tier doesn't exist or no config is loaded.
        /// </summary>
        public TierData GetTierData(int tierIndex)
        {
            if (_progressionData?.Tiers == null)
                return null;

            foreach (var tier in _progressionData.Tiers)
            {
                if (tier.TierIndex == tierIndex)
                    return tier;
            }

            return null;
        }

        /// <summary>
        /// Returns the zero-based index of the last gig that should be
        /// visible in the carousel, applying the horizon rule:
        ///   - All gigs in unlocked tiers are visible.
        ///   - All gigs in the first locked tier (immediate next) are visible.
        ///   - Gigs in any tier beyond that are hidden (beyond the horizon).
        ///
        /// Returns <c>(totalGigCount - 1)</c> if no tier config exists,
        /// unlockall is active, or all tiers are unlocked (everything visible).
        ///
        /// Returns -1 if totalGigCount is 0.
        /// </summary>
        /// <param name="totalGigCount">Total gigs in the campaign file.</param>
        /// <param name="totalCompletedGigs">Cumulative gigs completed by the player.</param>
        public int GetHorizonGigIndex(int totalGigCount, int totalCompletedGigs)
        {
            if (totalGigCount <= 0)
                return -1;

            // ── DIAGNOSTIC ──
            UnityEngine.Debug.Log($"[CampaignProgressionManager] GetHorizonGigIndex: totalGigCount={totalGigCount}, " +
                $"totalCompletedGigs={totalCompletedGigs}, _hasTierConfig={_hasTierConfig}, " +
                $"UnlockAll={_progressionData?.UnlockAll}");

            // No tier config or unlockall → everything visible
            if (!_hasTierConfig || _progressionData.UnlockAll)
            {
                UnityEngine.Debug.Log($"[CampaignProgressionManager]   => returning totalGigCount-1 = {totalGigCount - 1} (no tier config or unlockall)");
                return totalGigCount - 1;
            }

            // Walk tiers in order. Tiers are already sorted by TierIndex ascending.
            int cumulativeSlots = 0;
            int lastUnlockedEndIndex = -1;

            foreach (var tier in _progressionData.Tiers)
            {
                int tierSlotCount = Math.Max(0, tier.GigsInTier);
                if (tierSlotCount == 0)
                    continue;

                int tierStart = cumulativeSlots;
                int tierEnd = Math.Min(cumulativeSlots + tierSlotCount, totalGigCount) - 1;

                UnityEngine.Debug.Log($"[CampaignProgressionManager]   Tier[{tier.TierIndex}]: slots [{tierStart}-{tierEnd}], " +
                    $"required={tier.GigsRequiredToUnlock}, completed={totalCompletedGigs}, " +
                    $"locked={totalCompletedGigs < tier.GigsRequiredToUnlock}");

                if (totalCompletedGigs >= tier.GigsRequiredToUnlock)
                {
                    // Tier is unlocked — mark its end index
                    lastUnlockedEndIndex = tierEnd;
                }
                else
                {
                    // This is the first locked tier — include it, then stop
                    UnityEngine.Debug.Log($"[CampaignProgressionManager]   => first locked tier, horizon end = {tierEnd}");
                    return tierEnd;
                }

                cumulativeSlots = tierEnd + 1;
                if (cumulativeSlots >= totalGigCount)
                    break;
            }

            // If we got here, all tiers are unlocked
            UnityEngine.Debug.Log($"[CampaignProgressionManager]   => all tiers unlocked, horizon end = {totalGigCount - 1}");
            return totalGigCount - 1;
        }

        /// <summary>
        /// Returns the zero-based gig index of the FIRST gig in the first locked tier,
        /// given a completed gig count. This is the opposite boundary of
        /// <see cref="GetHorizonGigIndex"/>: instead of returning the last gig of the
        /// first locked tier (the horizon end), this returns the first gig of that tier.
        ///
        /// Used by the tier-unlock cinematic sequence to determine where the camera
        /// should sweep — the first newly unlocked gig after a completion crosses a
        /// tier threshold.
        ///
        /// Returns -1 if no tier config, unlockall is active, or all tiers are unlocked.
        /// </summary>
        public int GetFirstLockedTierStartIndex(int totalCompletedGigs)
        {
            if (!_hasTierConfig || _progressionData.UnlockAll)
                return -1;

            int cumulativeSlots = 0;

            foreach (var tier in _progressionData.Tiers)
            {
                int tierSlotCount = Math.Max(0, tier.GigsInTier);
                if (tierSlotCount == 0)
                    continue;

                int tierStart = cumulativeSlots;

                if (totalCompletedGigs < tier.GigsRequiredToUnlock)
                {
                    // This is the first locked tier — return its start index
                    return tierStart;
                }

                cumulativeSlots += tierSlotCount;
                if (cumulativeSlots >= _totalGigCount)
                    break;
            }

            // All tiers unlocked
            return -1;
        }

        /// <summary>
        /// Resets the manager. Call when leaving a campaign to free memory.
        /// </summary>
        public void Reset()
        {
            _progressionData = null;
            _gigToTierMap = null;
            _totalGigCount = 0;
            _hasTierConfig = false;
        }
    }
}