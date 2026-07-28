using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YARG.Menu.Career;
using YARG.Core.Input;
using YARG.Menu;
using YARG.Menu.Career.Motivation;
using YARG.Menu.Navigation;

namespace YARG.Menu.Career.Trophy
{
    public class CampaignTrophyController : MonoBehaviour
    {
        [SerializeField] private CampaignTrophyView _view;
        [SerializeField] private CampaignStatsController _statsController;

        [Header("── Debug ──────────────────────────────")]
        [Tooltip("Dev-only: populate the screen with preset Guitar/Bass/Drums/Vocals cards " +
                 "regardless of logged-in profiles or campaign completion. Leave OFF for shipping builds.")]
        [SerializeField] private bool _useDebugStats;

        /// <summary>
        /// When true, the trophy screen uses hard-coded preset stats for layout testing.
        /// </summary>
        public bool UseDebugStats => _useDebugStats;

        private CareerInfo _currentCareer;
        private Coroutine _populateCoroutine;

        public void Initialize(CampaignTrophyView view, CampaignStatsController statsController)
        {
            _view = view;
            _statsController = statsController;
        }

        private void Awake()
        {
            if (_view == null)
                _view = GetComponentInChildren<CampaignTrophyView>(includeInactive: true);

            if (_statsController == null)
                _statsController = GetComponent<CampaignStatsController>();
            if (_statsController == null)
                _statsController = gameObject.AddComponent<CampaignStatsController>();

            if (_view != null && _statsController != null)
                Initialize(_view, _statsController);

            HideCustomFooter();
        }

        private void OnEnable()
        {
            HideCustomFooter();

            if (_currentCareer != null && _view != null)
                PushNavigationScheme();
        }

        private void OnDisable()
        {
            if (_populateCoroutine != null)
            {
                StopCoroutine(_populateCoroutine);
                _populateCoroutine = null;
            }

            try { Navigator.Instance?.PopScheme(); } catch { }
        }

        public void Open(CareerInfo career)
        {
            _currentCareer = career;

            CampaignStatsData data;
            if (_useDebugStats)
            {
                data = TrophyDebugData.Build(career);
                Debug.Log("[CampaignTrophy] Using DEBUG preset stats (Guitar/Bass/Drums/Vocals).");
            }
            else if (_statsController == null || !_statsController.TryBuildViewData(career, out data))
            {
                Debug.LogWarning($"[CampaignTrophy] Could not build stats for '{career?.name}'.");
                return;
            }

            if (_populateCoroutine != null)
                StopCoroutine(_populateCoroutine);

            _populateCoroutine = StartCoroutine(PopulateWhenReady(data));
        }

        private IEnumerator PopulateWhenReady(CampaignStatsData data)
        {
            // Wait for layout so the carousel viewport has a real size before RectMask2D clips children.
            yield return null;
            Canvas.ForceUpdateCanvases();

            var contentRow = transform.Find("ContentRow") as RectTransform;
            if (contentRow != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRow);

            _view.Populate(data);
            PushNavigationScheme();
            _populateCoroutine = null;
        }

        private void HideCustomFooter()
        {
            var footer = transform.Find("FooterBar");
            if (footer != null)
                footer.gameObject.SetActive(false);
        }

        private void PushNavigationScheme()
        {
            try { Navigator.Instance.PopScheme(); } catch { }

            var entries = new List<NavigationScheme.Entry>
            {
                new(MenuAction.Green, "Menu.Common.Confirm", Back),
                new(MenuAction.Red, "Menu.Common.Back", Back),
            };

            if (_view?.Carousel != null && _view.Carousel.CardCount > 1)
            {
                entries.Add(new NavigationScheme.Entry(MenuAction.Left, "Menu.Common.Left",
                    () => _view.Carousel.Step(-1)));
                entries.Add(new NavigationScheme.Entry(MenuAction.Right, "Menu.Common.Right",
                    () => _view.Carousel.Step(1)));
            }

            // Match CoverFlow: keep the persistent HelpBar + music player visible.
            Navigator.Instance.PushScheme(new NavigationScheme(entries, true));
        }

        private void Back()
        {
            MenuManager.Instance.PopMenu();
        }
    }
}
