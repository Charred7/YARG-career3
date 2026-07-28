using System;
using System.Linq;
using UnityEngine;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Tracks session-wide momentum (consecutive gig completions).
    /// Singleton that persists across scenes via a DontDestroyOnLoad GameObject,
    /// or attaches to existing managers at runtime.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class MomentumTracker : MonoBehaviour
    {
        private static MomentumTracker _instance;
        public static MomentumTracker Instance => _instance;

        public MomentumState State { get; private set; } = new MomentumState();

        /// <summary>Fired when a gig is completed this session (for streak UI updates).</summary>
        public event Action<string> OnGigCompleted;

        /// <summary>Fired when the streak badge changes (e.g., "ON FIRE!").</summary>
        public event Action<string> OnStreakChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Records a gig completion. Updates streak and fires events.
        /// </summary>
        public void RecordGigCompletion(string gigId, string careerId)
        {
            State.SwitchCareer(careerId);

            if (State.RecordGig(gigId))
            {
                OnGigCompleted?.Invoke(gigId);

                // Check streak badge
                string badge = State.GetStreakBadge();
                if (badge != null)
                {
                    OnStreakChanged?.Invoke(badge);
                }

                Debug.Log($"MomentumTracker: Completed gig {gigId}. " +
                    $"Session total: {State.TotalSessionGigs}, " +
                    $"Streak: {State.ConsecutiveGigsCompleted}");
            }
        }

        /// <summary>
        /// Returns the current streak badge text, or null if no streak.
        /// </summary>
        public string GetStreakBadge()
        {
            return State.GetStreakBadge();
        }

        /// <summary>
        /// Returns a completion prompt for the given progress.
        /// </summary>
        public string GetCompletionPrompt(int totalGigs, int completedGigs)
        {
            return State.GetCompletionPrompt(totalGigs, completedGigs);
        }

        /// <summary>
        /// Resets all session data.
        /// </summary>
        public void ResetSession()
        {
            State.Reset();
            Debug.Log("MomentumTracker: Session reset");
        }
    }
}