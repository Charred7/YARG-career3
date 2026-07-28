using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YARG.Core.Input;
using YARG.Core.Song;
using YARG.Menu.Navigation;
using YARG.Menu.ListMenu;
using YARG.Menu.Persistent;
using YARG.Player;
using YARG.Career;
using YARG.Menu.Career.Motivation;

namespace YARG.Menu.Career
{
    public class GigView : ListMenu<GigViewType, GigViewObject>
    {
        protected override int ExtraListViewPadding => 15;

        [SerializeField]
        private TextMeshProUGUI _bandNameText;
        [SerializeField]
        private TextMeshProUGUI _headerText;
        [SerializeField]
        private TextMeshProUGUI _careerNameText;
        [SerializeField]
        private TextMeshProUGUI _progressText;
        [SerializeField]
        private GigSongListPanel _songListPanel;
        [SerializeField]
        private BandSelectionPopup _bandSelectionPopup;
        [SerializeField]
        private CareerSetupPanel _careerSetupPanel;

        private CareerInfo _currentCareer;

        protected override void Awake()
        {
            base.Awake();

            if (_bandSelectionPopup != null)
            {
                _bandSelectionPopup.OnBandChanged.AddListener(OnBandChangedHandler);
            }
            else
            {
                Debug.LogError("GigView.Awake: _bandSelectionPopup is NULL! Please assign it in the Inspector!");
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Navigator.Instance.PushScheme(new NavigationScheme(new()
            {
                new NavigationScheme.Entry(MenuAction.Up, "Menu.Common.Up", () => {
                    SelectedIndex--;
                }),
                new NavigationScheme.Entry(MenuAction.Down, "Menu.Common.Down", () => {
                    SelectedIndex++;
                }),
                new NavigationScheme.Entry(MenuAction.Green, "Menu.Common.Confirm", () => {
                    CurrentSelection?.PrimaryButtonClick();
                }),
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", Back),
                new NavigationScheme.Entry(MenuAction.Orange, "Toggle Complete", ToggleGigCompletion),
                new NavigationScheme.Entry(MenuAction.Blue, "Select Band", SelectBand),
                new NavigationScheme.Entry(MenuAction.Select, "Song Matching", OpenCareerSetup)
            }, false));

            UpdateDisplay();
            RequestViewListUpdate();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            try
            {
                Navigator.Instance?.PopScheme();
            }
            catch
            {
                // Scheme may have already been popped by CoverFlowController.
                // This is expected when CoverFlowBootstrapper is present.
            }
        }

        private void OnDestroy()
        {
            if (_bandSelectionPopup != null)
            {
                _bandSelectionPopup.OnBandChanged.RemoveListener(OnBandChangedHandler);
            }
        }

        public void SetCareer(CareerInfo career)
        {
            _currentCareer = career;

            if (_careerNameText != null)
            {
                _careerNameText.text = career.name.ToUpper();
            }

            if (_headerText != null)
            {
                _headerText.text = "CHOOSE A GIG";
            }

            // Auto-analyze on first open if no cache exists
            var existingCache = SongMatchCache.GetOrLoadCache(career.id);
            if (existingCache == null)
            {
                Debug.Log($"GigView: No match cache for '{career.id}', running auto-analysis...");
                CareerManager.Instance.AnalyzeCareer(career.id);
            }

            UpdateDisplay();
            RequestViewListUpdate();

            // HOOK: Initialize motivation system components for this career
            EnsureGigMotivationComponents();
            GigProgressEnhancer.Instance?.Initialize(career);
            TeaserPanel.Instance?.Initialize(career);
        }

        /// <summary>
        /// Ensures motivation components are attached to this view.
        /// Self-injection pattern — no prefab editing required.
        /// </summary>
        private void EnsureGigMotivationComponents()
        {
            if (GetComponent<GigProgressEnhancer>() == null)
                gameObject.AddComponent<GigProgressEnhancer>();
            if (GetComponent<TeaserPanel>() == null)
                gameObject.AddComponent<TeaserPanel>();
            if (GetComponent<CompletionCelebration>() == null)
                gameObject.AddComponent<CompletionCelebration>();
        }

        private void OnBandChangedHandler()
        {
            Debug.Log("GigView.OnBandChangedHandler: Band was changed!");
            UpdateDisplay();
            RequestViewListUpdate();
        }

        public void UpdateDisplay()
        {
            var band = CareerManager.Instance.CurrentBand;
            if (_bandNameText != null && band != null)
            {
                _bandNameText.text = band.BandName.ToUpper();
            }

            if (_progressText != null && _currentCareer != null)
            {
                var gigs = CareerManager.Instance.GetGigs(_currentCareer.id);
                int completedCount = 0;

                foreach (var gig in gigs)
                {
                    string gigId = $"{_currentCareer.id}|{gig.name}";
                    if (band != null && CareerManager.Instance.IsGigCompleted(gigId))
                    {
                        completedCount++;
                    }
                }

                _progressText.text = $"{completedCount}/{gigs.Count} COMPLETE";
            }
        }

        protected override List<GigViewType> CreateViewList()
        {
            var viewList = new List<GigViewType>();

            if (_currentCareer == null)
            {
                Debug.LogWarning("GigView: No career set, cannot create gig list");
                return viewList;
            }

            var gigs = CareerManager.Instance.GetGigs(_currentCareer.id);
            Debug.Log($"GigView: Creating views for {gigs.Count} gigs in {_currentCareer.name}");

            foreach (var gig in gigs)
            {
                string gigId = $"{_currentCareer.id}|{gig.name}";
                bool isCompleted = CareerManager.Instance.IsGigCompleted(gigId);
                viewList.Add(new GigViewType(_currentCareer.id, gig, isCompleted, OnGigSelected));
            }

            return viewList;
        }

        protected override void OnSelectedIndexChanged()
        {
            base.OnSelectedIndexChanged();
            UpdateSongListDisplay();
        }

        private void UpdateSongListDisplay()
        {
            if (_songListPanel != null && CurrentSelection != null)
            {
                _songListPanel.SetGig(CurrentSelection.Gig);
            }
            else if (_songListPanel != null)
            {
                _songListPanel.Clear();
            }
        }

        private void Back()
        {
            MenuManager.Instance.PopMenu();
        }

        private void ToggleGigCompletion()
        {
            if (CurrentSelection != null && _currentCareer != null)
            {
                string gigId = $"{_currentCareer.id}|{CurrentSelection.Gig.name}";

                if (CareerManager.Instance.IsGigCompleted(gigId))
                {
                    BandContainer.RemoveCompletedGig(gigId);
                }
                else
                {
                    CareerManager.Instance.CompleteGig(gigId);
                }

                // HOOK: Track gig completion for momentum system
                if (CareerManager.Instance.IsGigCompleted(gigId))
                {
                    MomentumTracker.Instance?.RecordGigCompletion(gigId, _currentCareer.id);
                    CompletionCelebration.Instance?.PlayGigCompleteEffect(transform);

                    // Check if campaign is now fully completed
                    if (GigProgressEnhancer.Instance?.IsCampaignComplete() == true)
                    {
                        CompletionCelebration.Instance?.PlayCampaignCompleteEffect(transform);
                        Debug.Log($"GigView: Campaign '{_currentCareer.name}' fully completed!");
                    }
                }

                UpdateDisplay();
                RequestViewListUpdate();
                GigProgressEnhancer.Instance?.OnGigListChanged();
                TeaserPanel.Instance?.UpdateTicker();
            }
        }

        private void OnGigSelected(string careerId, GigInfo gig)
        {
            Debug.Log($"Gig selected: {gig.name} from career {careerId}");

            if (PlayerContainer.Players.Count <= 0)
            {
                Debug.LogWarning("GigView: No players connected, cannot start gig.");
                return;
            }

            // Resolve using the cache-aware pipeline
            var resolvedSongs = CareerManager.Instance.ResolveGig(gig, careerId);
            var songEntries = resolvedSongs
                .Where(r => r.SongEntry != null)
                .Select(r => r.SongEntry)
                .ToList();

            int missingCount = resolvedSongs.Count(r => r.SongEntry == null);
            if (missingCount > 0)
            {
                Debug.LogWarning($"GigView: {missingCount} song(s) could not be resolved for gig '{gig.name}'");
            }

            if (songEntries.Count == 0)
            {
                Debug.LogError($"GigView: No songs could be resolved for gig '{gig.name}'!");
                return;
            }

            // Open the real MusicLibrary in playlist mode
            MusicLibraryWrapper.OpenGigInMusicLibrary(gig.name, songEntries);
        }

        private void OpenCareerSetup()
        {
            if (_currentCareer == null) return;

            if (_careerSetupPanel != null)
            {
                _careerSetupPanel.Show(_currentCareer.id, _currentCareer.name);
            }
            else
            {
                Debug.LogWarning("GigView: _careerSetupPanel is not assigned in the Inspector!");
            }
        }

        private void SelectBand()
        {
            if (_bandSelectionPopup != null)
            {
                _bandSelectionPopup.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogError("GigView.SelectBand: _bandSelectionPopup is NULL!");
            }
        }
    }
}