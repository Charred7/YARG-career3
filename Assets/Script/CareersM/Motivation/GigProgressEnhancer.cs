using UnityEngine;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Tracks gig progress data for the career campaign.
    /// Provides the next incomplete gig index and campaign completion state
    /// to other motivation/UI components.
    /// Attaches to the GigView at runtime — no prefab edits needed.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class GigProgressEnhancer : MonoBehaviour
    {
        private static GigProgressEnhancer _instance;
        public static GigProgressEnhancer Instance => _instance;

        private GigView _gigView;
        private CareerInfo _currentCareer;

        // Cached progress data
        private int _totalGigs;
        private int _completedGigs;
        private int _nextGigIndex = -1;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            _gigView = GetComponent<GigView>();
            if (_gigView == null)
                _gigView = FindObjectOfType<GigView>();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Called from GigView.SetCareer() hook.
        /// </summary>
        public void Initialize(CareerInfo career)
        {
            _currentCareer = career;
            RecalculateProgress();
        }

        /// <summary>
        /// Called when a gig is toggled or completed.
        /// </summary>
        public void OnGigListChanged()
        {
            if (_currentCareer == null) return;
            RecalculateProgress();
        }

        private void RecalculateProgress()
        {
            if (_currentCareer == null) return;

            var band = CareerManager.Instance.CurrentBand;
            var gigs = CareerManager.Instance.GetGigs(_currentCareer.id);

            _totalGigs = gigs.Count;
            _completedGigs = 0;
            _nextGigIndex = -1;

            for (int i = 0; i < gigs.Count; i++)
            {
                string gigId = $"{_currentCareer.id}|{gigs[i].name}";
                bool isCompleted = band != null && CareerManager.Instance.IsGigCompleted(gigId);

                if (isCompleted)
                {
                    _completedGigs++;
                }
                else if (_nextGigIndex < 0)
                {
                    _nextGigIndex = i;
                }
            }
        }

        /// <summary>
        /// Gets the index of the next incomplete gig.
        /// </summary>
        public int GetNextGigIndex()
        {
            return _nextGigIndex;
        }

        /// <summary>
        /// Gets whether the campaign is completed.
        /// </summary>
        public bool IsCampaignComplete()
        {
            return _totalGigs > 0 && _completedGigs >= _totalGigs;
        }
    }
}
