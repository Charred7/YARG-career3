using System;
using System.Collections.Generic;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Tracks momentum state for the current gaming session.
    /// "Momentum" = consecutive gigs completed without leaving the gig view.
    /// </summary>
    public class MomentumState
    {
        /// <summary>Career ID being tracked.</summary>
        public string CurrentCareerId;

        /// <summary>Number of gigs completed in this session for the current career.</summary>
        public int ConsecutiveGigsCompleted;

        /// <summary>Total gigs completed in this session across all careers.</summary>
        public int TotalSessionGigs;

        /// <summary>When this momentum session started.</summary>
        public DateTime SessionStartTime = DateTime.UtcNow;

        /// <summary>Gig IDs completed this session (to avoid double-counting).</summary>
        public HashSet<string> CompletedThisSession = new();

        /// <summary>
        /// Returns a badge/message based on streak length.
        /// </summary>
        public string GetStreakBadge()
        {
            if (ConsecutiveGigsCompleted >= 5)
                return $"🌟 UNSTOPPABLE! {ConsecutiveGigsCompleted} Gigs Crushed!";
            if (ConsecutiveGigsCompleted >= 3)
                return $"🔥 ON FIRE! {ConsecutiveGigsCompleted} Gigs Completed!";
            if (ConsecutiveGigsCompleted >= 2)
                return $"✨ On a Roll! {ConsecutiveGigsCompleted} Gigs!";
            return null;
        }

        /// <summary>
        /// Returns a completion-prompt message based on remaining gigs.
        /// </summary>
        public string GetCompletionPrompt(int totalGigs, int completedGigs)
        {
            int remaining = totalGigs - completedGigs;
            if (remaining <= 0)
                return "👑 CAMPAIGN COMPLETE! You conquered this campaign!";
            if (remaining == 1)
                return "🏁 SO CLOSE! Only 1 gig left! Finish it!";
            if (remaining <= 3)
                return $"🎯 Just {remaining} more gigs to conquer this campaign!";
            if (remaining <= 5)
                return $"💪 Only {remaining} gigs left. You've got this!";
            return null;
        }

        /// <summary>
        /// Resets consecutive count when switching careers.
        /// </summary>
        public void SwitchCareer(string careerId)
        {
            if (CurrentCareerId != careerId)
            {
                ConsecutiveGigsCompleted = 0;
                CurrentCareerId = careerId;
            }
        }

        /// <summary>
        /// Records a gig completion and returns true if it's a new completion.
        /// </summary>
        public bool RecordGig(string gigId)
        {
            if (CompletedThisSession.Add(gigId))
            {
                ConsecutiveGigsCompleted++;
                TotalSessionGigs++;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resets all session data.
        /// </summary>
        public void Reset()
        {
            CurrentCareerId = null;
            ConsecutiveGigsCompleted = 0;
            TotalSessionGigs = 0;
            CompletedThisSession.Clear();
            SessionStartTime = DateTime.UtcNow;
        }
    }
}