using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YARG.Career;
using YARG.Core.Input;
using YARG.Menu.ListMenu;
using YARG.Menu.Navigation;
using TMPro;
using Cysharp.Threading.Tasks;
using YARG.Menu.Persistent;
using YARG.Menu.Career.Motivation;

namespace YARG.Menu.Career
{
    public enum CareerSortMode
    {
        Alphabetical,
        ReleaseDate,
        Progress
    }

    public class CareerView : ListMenu<CareerViewType, CareerViewObject>
    {
        protected override int ExtraListViewPadding => 15;

        [SerializeField]
        private TextMeshProUGUI _bandNameText;
        [SerializeField]
        private TextMeshProUGUI _headerText;
        [SerializeField]
        private Image _artworkImage;
        [SerializeField]
        private BandSelectionPopup _bandSelectionPopup;

        private bool _isMonitoring;
        private CareerSortMode _sortMode = CareerSortMode.ReleaseDate;

        protected override void Awake()
        {
            base.Awake();

            if (_headerText != null)
            {
                _headerText.text = "CHOOSE A CAREER";
            }

            if (_bandSelectionPopup != null)
            {
                _bandSelectionPopup.OnBandChanged.AddListener(OnBandChangedHandler);
            }
        }

        private void OnDestroy()
        {
            if (_bandSelectionPopup != null)
            {
                _bandSelectionPopup.OnBandChanged.RemoveListener(OnBandChangedHandler);
            }
        }

        private void OnBandChangedHandler()
        {
            UpdateBandDisplay();
            RequestViewListUpdate();
            RebuildNavScheme();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            LoadCareerData();
            UpdateBandDisplay();
            RebuildNavScheme();

            MonitorArtworkLoading().Forget();

            // Refresh the stats panel to match the currently selected career.
            // OnSelectedIndexChanged is NOT called by RequestViewListUpdate (it sets
            // _selectedIndex directly as a field), so we must explicitly re-evaluate
            // the trophy screen when returning from a sub-menu (e.g. GigView).
            var statsController = GetComponentInChildren<CampaignStatsController>(includeInactive: true);
            statsController?.ShowForCampaign(CurrentSelection?.Career);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _isMonitoring = false;
            Navigator.Instance?.PopScheme();

            // Hide the stats panel when leaving the career selection screen.
            // The panel lives on a separate DontDestroyOnLoad overlay Canvas (sortOrder 200),
            // so it stays visible when CareerView is disabled. We must explicitly hide it
            // to prevent it from rendering on top of sub-menus (e.g. GigView).
            var statsController = GetComponentInChildren<CampaignStatsController>(includeInactive: true);
            if (statsController != null && statsController.IsVisible)
            {
                statsController.HideStats();
            }
        }

        private void Back()
        {
            // If the new stats panel is visible, dismiss it first.
            // The panel lives on a separate overlay Canvas (sortingOrder 200, DontDestroyOnLoad),
            // so PopMenu() alone won't close it. We must explicitly hide it.
            var statsController = GetComponentInChildren<CampaignStatsController>(includeInactive: true);
            if (statsController != null && statsController.IsVisible)
            {
                statsController.HideStats();
                return;
            }

            MenuManager.Instance.PopMenu();
        }

        private void LoadCareerData()
        {
            CareerManager.Instance.LoadCareerData();
            RequestViewListUpdate();
        }

        private void SelectBand()
        {
            if (_bandSelectionPopup != null)
            {
                _bandSelectionPopup.gameObject.SetActive(true);
            }
        }

        private void HideCurrentCampaign()
        {
            if (CurrentSelection == null) return;

            var career = CurrentSelection.Career;
            if (career == null) return;

            BandContainer.HideCampaign(career.id);
            Debug.Log($"CareerView: Hidden campaign '{career.name}' [{career.id}]");

            // Refresh the view to remove the hidden campaign
            RequestViewListUpdate();

            // Rebuild nav scheme in case all campaigns are now hidden
            RebuildNavScheme();
        }

        private void UnhideAllCampaignsHandler()
        {
            BandContainer.UnhideAllCampaigns();
            Debug.Log("CareerView: Unhid all campaigns");

            // Refresh the view to show all campaigns again
            RequestViewListUpdate();

            // Rebuild nav scheme to change Orange back to "Hide Campaign"
            RebuildNavScheme();
        }

        private void ToggleSort()
        {
            _sortMode = _sortMode == CareerSortMode.Alphabetical
                ? CareerSortMode.ReleaseDate
                : CareerSortMode.Alphabetical;

            RebuildNavScheme();
            RequestViewListUpdate();
        }

        private bool AreAllCampaignsHidden()
        {
            var band = BandContainer.CurrentBand;
            if (band == null) return false;

            int totalCareers = CareerManager.Instance.GetCareers().Count;
            if (totalCareers == 0) return false;

            int hiddenCount = band.HiddenCampaigns?.Count ?? 0;
            return hiddenCount >= totalCareers;
        }

        private void RebuildNavScheme()
        {
            // Pop the current scheme if one exists
            try
            {
                Navigator.Instance.PopScheme();
            }
            catch
            {
                // Ignore if no scheme to pop
            }

            string sortLabel = _sortMode == CareerSortMode.Alphabetical ? "Sort: A-Z" : "Sort: Date";

            bool allHidden = AreAllCampaignsHidden();
            string orangeLabel = allHidden ? "UNHIDE ALL" : "Hide Campaign";
            System.Action orangeAction = allHidden ? (System.Action) UnhideAllCampaignsHandler : HideCurrentCampaign;

            var entries = new List<NavigationScheme.Entry>
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
                new NavigationScheme.Entry(MenuAction.Orange, orangeLabel, orangeAction),
            };

            entries.Add(new NavigationScheme.Entry(MenuAction.Blue, "Select Band", SelectBand));
            entries.Add(new NavigationScheme.Entry(MenuAction.Select, sortLabel, ToggleSort));

            Navigator.Instance.PushScheme(new NavigationScheme(entries, false));
        }

        public void UpdateBandDisplay()
        {
            var band = CareerManager.Instance.CurrentBand;
            if (_bandNameText != null && band != null)
            {
                _bandNameText.text = band.BandName.ToUpper();
            }
        }

        protected override List<CareerViewType> CreateViewList()
        {
            var viewList = new List<CareerViewType>();
            var careers = CareerManager.Instance.GetCareers();

            // Sort based on current mode
            var sorted = new List<CareerInfo>(careers);
            switch (_sortMode)
            {
                case CareerSortMode.Alphabetical:
                    sorted.Sort((a, b) =>
                        string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
                    break;
                case CareerSortMode.ReleaseDate:
                    sorted.Sort((a, b) =>
                    {
                        bool aHasDate = DateTime.TryParse(a.releaseDate, out var aDate);
                        bool bHasDate = DateTime.TryParse(b.releaseDate, out var bDate);

                        // Both have dates — compare chronologically
                        if (aHasDate && bHasDate) return aDate.CompareTo(bDate);
                        // One missing — push it to the bottom
                        if (aHasDate) return -1;
                        if (bHasDate) return 1;
                        // Neither has a date — fall back to alphabetical
                        return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
                    });
                    break;
            }

            foreach (var career in sorted)
            {
                // Skip hidden campaigns
                if (BandContainer.IsCareerHidden(career.id))
                {
                    continue;
                }

                viewList.Add(new CareerViewType(career, OnCareerSelected));
            }

            return viewList;
        }

        protected override void OnSelectedIndexChanged()
        {
            base.OnSelectedIndexChanged();
            UpdateArtworkDisplay();

            // HOOK: Notify the new Campaign Stats system about campaign selection change.
            // CampaignStatsController lives on CampaignStatsHost (child of this GameObject).
            var statsController = GetComponentInChildren<CampaignStatsController>(includeInactive: true);
            statsController?.ShowForCampaign(CurrentSelection?.Career);
        }

        private void UpdateArtworkDisplay()
        {
            if (CurrentSelection != null && _artworkImage != null)
            {
                var artwork = CurrentSelection.GetArtwork();
                if (artwork != null)
                {
                    _artworkImage.sprite = artwork;
                    _artworkImage.enabled = true;
                    UpdateArtworkOverlay();
                }
                else
                {
                    _artworkImage.enabled = false;
                    RemoveArtworkOverlay();
                }
            }
            else if (_artworkImage != null)
            {
                _artworkImage.enabled = false;
                RemoveArtworkOverlay();
            }
        }

        /// <summary>
        /// Shows/hides a "CONQUERED!" overlay badge on the artwork
        /// if the selected campaign is fully completed.
        /// </summary>
        private void UpdateArtworkOverlay()
        {
            var career = CurrentSelection?.Career;
            if (career == null)
            {
                RemoveArtworkOverlay();
                return;
            }

            bool completed = IsCampaignFullyCompleted(career);
            if (!completed)
            {
                RemoveArtworkOverlay();
                return;
            }

            // Find or create overlay
            var existing = _artworkImage.transform.Find("ArtworkOverlay")?.gameObject;
            if (existing != null)
            {
                existing.SetActive(true);
                return;
            }

            var overlayObj = new GameObject("ArtworkOverlay", typeof(RectTransform));
            overlayObj.transform.SetParent(_artworkImage.transform);

            var rt = overlayObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Semi-transparent gold tint overlay
            var overlayImg = overlayObj.AddComponent<UnityEngine.UI.Image>();
            overlayImg.color = new Color(0.3f, 0.2f, 0.05f, 0.15f);
            overlayImg.raycastTarget = false;

            // Crown badge at top-right
            var crownObj = new GameObject("CrownBadge", typeof(RectTransform));
            crownObj.transform.SetParent(overlayObj.transform);
            var crownTmp = crownObj.AddComponent<TMPro.TextMeshProUGUI>();
            crownTmp.text = "👑";
            crownTmp.fontSize = 40;
            crownTmp.alignment = TMPro.TextAlignmentOptions.Right;
            var crt = crownObj.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.6f, 0.65f);
            crt.anchorMax = new Vector2(0.95f, 0.95f);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            // CONQUERED! badge at bottom
            var badgeObj = new GameObject("ConqueredBadge", typeof(RectTransform));
            badgeObj.transform.SetParent(overlayObj.transform);
            var badgeImg = badgeObj.AddComponent<UnityEngine.UI.Image>();
            badgeImg.color = new Color(0.1f, 0.08f, 0.02f, 0.7f);
            var brt = badgeObj.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.1f, 0.08f);
            brt.anchorMax = new Vector2(0.9f, 0.20f);
            brt.offsetMin = Vector2.zero;
            brt.offsetMax = Vector2.zero;

            // Badge text
            var textObj = new GameObject("BadgeText", typeof(RectTransform));
            textObj.transform.SetParent(badgeObj.transform);
            var textTmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            textTmp.text = "CONQUERED!";
            textTmp.fontSize = 22;
            textTmp.fontStyle = TMPro.FontStyles.Bold;
            textTmp.alignment = TMPro.TextAlignmentOptions.Center;
            textTmp.color = new Color(1f, 0.84f, 0f);
            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
        }

        private void RemoveArtworkOverlay()
        {
            if (_artworkImage == null) return;
            var existing = _artworkImage.transform.Find("ArtworkOverlay")?.gameObject;
            if (existing != null)
                existing.SetActive(false);
        }

        /// <summary>
        /// Quick completion check for the artwork overlay.
        /// </summary>
        private static bool IsCampaignFullyCompleted(CareerInfo career)
        {
            var band = CareerManager.Instance.CurrentBand;
            if (band == null) return false;

            var gigs = CareerManager.Instance.GetGigs(career.id);
            if (gigs == null || gigs.Count == 0) return false;

            foreach (var gig in gigs)
            {
                string gigId = $"{career.id}|{gig.name}";
                if (!CareerManager.Instance.IsGigCompleted(gigId))
                    return false;
            }
            return true;
        }

        private async UniTaskVoid MonitorArtworkLoading()
        {
            _isMonitoring = true;

            while (_isMonitoring && this != null && gameObject.activeInHierarchy)
            {
                if (CurrentSelection != null && CurrentSelection.IsArtworkLoaded())
                {
                    UpdateArtworkDisplay();
                }

                await UniTask.Delay(100);
            }
        }

        private void OnCareerSelected(CareerInfo career)
        {
            Debug.Log($"[CareerFlow] OnCareerSelected() called with career='{career?.name ?? "NULL"}' (id='{career?.id ?? "NULL"}')");

            // ── PATH A: CareerFlow Carousel (top-level career browser) ──
            try
            {
                Debug.Log("[CareerFlow] OnCareerSelected() — attempting PushMenu(CareerCareerModern)...");
                var careerFlowMenu = MenuManager.Instance.PushMenu(MenuManager.Menu.CareerCareerModern);
                Debug.Log($"[CareerFlow] OnCareerSelected() — PushMenu returned {(careerFlowMenu != null ? $"'{careerFlowMenu.name}'" : "NULL")}");

                if (careerFlowMenu != null)
                {
                    var cfBootstrapper = careerFlowMenu.GetComponent<CareerFlow.CareerFlowBootstrapper>();
                    if (cfBootstrapper == null)
                        cfBootstrapper = careerFlowMenu.GetComponentInChildren<CareerFlow.CareerFlowBootstrapper>(includeInactive: true);

                    Debug.Log($"[CareerFlow] OnCareerSelected() — cfBootstrapper={(cfBootstrapper != null ? $"'{cfBootstrapper.name}'" : "NULL")}, " +
                        $"IsAvailable={cfBootstrapper?.IsAvailable}");

                    if (cfBootstrapper != null && cfBootstrapper.IsAvailable)
                    {
                        var allCareers = CareerManager.Instance.GetCareers();
                        Debug.Log($"[CareerFlow] OnCareerSelected() — GetCareers() returned {(allCareers != null ? allCareers.Count : 0)} careers. " +
                            $"_careerDatabase==null? (check CareerManager)");
                        cfBootstrapper.OpenCareerFlow(allCareers);
                        Debug.Log($"CareerView: Opening CareerFlow carousel with {allCareers?.Count ?? 0} careers");
                        return;
                    }
                    else
                    {
                        Debug.LogWarning($"[CareerFlow] OnCareerSelected() — CareerFlow NOT available. " +
                            $"cfBootstrapper==null={cfBootstrapper == null}, IsAvailable={cfBootstrapper?.IsAvailable}. Falling to Path B.");
                    }
                }
                else
                {
                    Debug.LogWarning("[CareerFlow] OnCareerSelected() — PushMenu returned NULL. Falling to Path B.");
                }
            }
            catch (System.InvalidOperationException ex)
            {
                Debug.LogWarning($"[CareerFlow] OnCareerSelected() — PushMenu threw InvalidOperationException: {ex.Message}. " +
                    "CareerCareerModern menu not registered — falling through to Path B.");
            }

            // ── PATH B: Direct-to-Gig CoverFlow ──
            // CareerFlow not available. Always route to CoverFlow (classic GigView is disabled).
            var gigMenu = MenuManager.Instance.PushMenu(MenuManager.Menu.CareerGigModern);

            var bootstrapper = gigMenu.GetComponent<CoverFlow.CoverFlowBootstrapper>();
            if (bootstrapper == null)
                bootstrapper = gigMenu.GetComponentInChildren<CoverFlow.CoverFlowBootstrapper>(includeInactive: true);

            if (bootstrapper != null && !bootstrapper.IsAvailable)
            {
                Debug.LogWarning(
                    "CareerView: CoverFlowBootstrapper found but IsAvailable=false. " +
                    "Wire CoverFlowController and 5 Card Slots in the Bootstrapper Inspector. " +
                    $"Controller={bootstrapper.name} exists but references incomplete.");
            }

            if (bootstrapper != null && bootstrapper.IsAvailable)
            {
                bootstrapper.OpenCampaign(career);
                Debug.Log($"CareerView: Opening '{career.name}' with CoverFlow");
            }
            else
            {
                Debug.LogError(
                    "CareerView: CoverFlowBootstrapper not available on CareerGigModern menu. " +
                    "Cannot open campaign. Wire CoverFlowController and card slots on the prefab.");
                if (gigMenu != null)
                    MenuManager.Instance.PopMenu();
            }
        }
    }
}
