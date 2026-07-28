using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Career;
using YARG.Core.Audio;
using YARG.Core.Input;
using YARG.Menu;
using YARG.Menu.Career.Motivation;
using YARG.Menu.Career.Trophy;
using YARG.Menu.Navigation;
using YARG.Player;

namespace YARG.Menu.Career.CareerFlow
{
    /// <summary>
    /// TRUE 3D CareerFlow controller — 7-slot parabolic arc in WorldSpace.
    /// Displays career/tour selection cards using the Gear Crate visual motif.
    /// Uses pure Transform manipulation (localPosition, localRotation, localScale).
    /// Reuses the same parabolic arc layout and animation system as CoverFlowController.
    ///
    /// Operates on List<CareerInfo> across all installed bundles — no tier-locking,
    /// no HypeEngine, simplified Active/Completed state model.
    /// </summary>
    public class CareerFlowMenuController : MonoBehaviour
    {
        [Header("Card Pool")]
        [SerializeField]
        private CareerFlowCard[] _cardSlots = new CareerFlowCard[7];

        [Header("Background")]
        [SerializeField]
        private CoverFlow.CoverFlowBackground _background;

        [Header("Header UI")]
        [SerializeField]
        private TextMeshProUGUI _headerText;

        [SerializeField]
        private TextMeshProUGUI _bandNameText;

        [SerializeField]
        private BandSelectionPopup _bandSelectionPopup;

        [SerializeField]
        private CampaignManagePopup _campaignManagePopup;

        [Header("Canvas")]
        [SerializeField]
        private Canvas _targetCanvas;

        [Header("Animation")]
        [SerializeField]
        private AnimationCurve _easingCurve;

        [SerializeField]
        [Range(0.10f, 0.35f)]
        private float _transitionDuration = 0.18f;

        [SerializeField]
        [Range(0.0f, 0.15f)]
        private float _overshootDuration = 0.08f;

        [SerializeField]
        [Range(0.0f, 0.10f)]
        private float _overshootAmount = 0.025f;

        [Header("SFX")]
        [SerializeField]
        [Range(0.05f, 0.3f)]
        private float _scrollSfxCooldown = 0.1f;

        [Header("Position Offset")]
        [SerializeField]
        private Vector3 _positionOffset = Vector3.zero;

        /// <summary>
        /// Applies the Inspector-configured _positionOffset to any layout position.
        /// Used to shift the entire parabolic arc in local space.
        /// </summary>
        private Vector3 OffsetPosition(Vector3 layoutPos) => layoutPos + _positionOffset;

        // ── TRUE 3D Parabolic Arc Layout (7 slots, center=3) ──────
        //    Identical to CoverFlowController's LAYOUTS array.

        private struct CardLayout
        {
            public Vector3 LocalPosition;
            public float YRotation;
            public float Scale;
            public float Opacity;
        }

        private static readonly CardLayout[] LAYOUTS = new CardLayout[]
        {
            new() { LocalPosition = new Vector3(-560f, -40f, 250f), YRotation = -65f, Scale = 0.40f, Opacity = 0.20f },
            new() { LocalPosition = new Vector3(-390f, -20f, 140f), YRotation = -52f, Scale = 0.52f, Opacity = 0.32f },
            new() { LocalPosition = new Vector3(-220f, -10f,  55f), YRotation = -38f, Scale = 0.68f, Opacity = 0.58f },
            new() { LocalPosition = new Vector3(   0f,   0f,   0f), YRotation =   0f, Scale = 1.00f, Opacity = 1.00f },
            new() { LocalPosition = new Vector3( 220f, -10f,  55f), YRotation =  38f, Scale = 0.68f, Opacity = 0.58f },
            new() { LocalPosition = new Vector3( 390f, -20f, 140f), YRotation =  52f, Scale = 0.52f, Opacity = 0.32f },
            new() { LocalPosition = new Vector3( 560f, -40f, 250f), YRotation =  65f, Scale = 0.40f, Opacity = 0.20f },
        };

        private static readonly CardLayout VIRTUAL_LEFT  = new() { LocalPosition = new Vector3(-730f, -60f, 360f), YRotation = -75f, Scale = 0.28f, Opacity = 0.0f };
        private static readonly CardLayout VIRTUAL_RIGHT = new() { LocalPosition = new Vector3( 730f, -60f, 360f), YRotation =  75f, Scale = 0.28f, Opacity = 0.0f };

        // ── Runtime State ─────────────────────────────────────────

        private List<CareerInfo> _careers;
        private int _centerIndex;
        private int _visibleCareerCount;
        private bool _isAnimating;
        private CareerSortMode _sortMode = CareerSortMode.ReleaseDate;

        /// <summary>
        /// The navigation scheme this menu pushed, tracked so we only ever pop/refresh
        /// our own scheme and never clobber a child menu's (or parent's) scheme.
        /// </summary>
        private NavigationScheme _navScheme;

        private readonly Dictionary<string, Texture2D> _artCache = new();

        // ── SFX Runtime ────────────────────────────────────────────

        private float _lastScrollSfxTime = -1f;

        // ── Initialisation ─────────────────────────────────────────

        private void Awake()
        {
            if (_easingCurve == null || _easingCurve.keys.Length == 0)
            {
                _easingCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 3f),
                    new Keyframe(1f, 1f, 0f, 0f));
            }

            if (_headerText != null)
                _headerText.text = "CHOOSE A CAREER";

            if (_bandSelectionPopup != null)
                _bandSelectionPopup.OnBandChanged.AddListener(OnBandChangedHandler);

            if (_campaignManagePopup != null)
                _campaignManagePopup.OnVisibilityChanged.AddListener(OnManageVisibilityChanged);

            AutoWireUIReferences();
        }

        public void AssignBandSelectionPopup(BandSelectionPopup popup)
        {
            if (popup == null)
                return;

            if (_bandSelectionPopup != null)
                _bandSelectionPopup.OnBandChanged.RemoveListener(OnBandChangedHandler);

            _bandSelectionPopup = popup;

            if (_bandSelectionPopup != null)
                _bandSelectionPopup.OnBandChanged.AddListener(OnBandChangedHandler);
        }

        public void AssignCampaignManagePopup(CampaignManagePopup popup)
        {
            if (popup == null)
                return;

            if (_campaignManagePopup != null)
                _campaignManagePopup.OnVisibilityChanged.RemoveListener(OnManageVisibilityChanged);

            _campaignManagePopup = popup;

            if (_campaignManagePopup != null)
                _campaignManagePopup.OnVisibilityChanged.AddListener(OnManageVisibilityChanged);
        }

        private void AutoWireUIReferences()
        {
            var root = GetComponentInParent<CareerFlowBootstrapper>()?.transform ?? transform.parent;
            if (root == null)
                return;

            if (_bandSelectionPopup == null)
                _bandSelectionPopup = root.GetComponentInChildren<BandSelectionPopup>(true);

            if (_campaignManagePopup == null)
                _campaignManagePopup = root.GetComponentInChildren<CampaignManagePopup>(true);

            if (_bandNameText == null)
            {
                var bandNameTransform = root.Find("BandNameText");
                if (bandNameTransform != null)
                    _bandNameText = bandNameTransform.GetComponent<TextMeshProUGUI>();
            }

            EnsureBandNameText();
        }

        private void EnsureBandNameText()
        {
            if (_bandNameText != null || _headerText == null)
                return;

            var go = new GameObject("BandNameText", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(_headerText.transform.parent, false);

            var rt = go.GetComponent<RectTransform>();
            var headerRt = _headerText.rectTransform;
            rt.anchorMin = headerRt.anchorMin;
            rt.anchorMax = headerRt.anchorMax;
            rt.pivot = headerRt.pivot;
            rt.localScale = headerRt.localScale;
            rt.anchoredPosition = headerRt.anchoredPosition + new Vector2(0f, -0.35f);
            rt.sizeDelta = headerRt.sizeDelta;

            _bandNameText = go.GetComponent<TextMeshProUGUI>();
            _bandNameText.font = _headerText.font;
            _bandNameText.fontSharedMaterial = _headerText.fontSharedMaterial;
            _bandNameText.fontSize = 24;
            _bandNameText.fontStyle = FontStyles.Bold;
            _bandNameText.alignment = TextAlignmentOptions.Center;
            _bandNameText.color = new Color(0.62352943f, 0.6392157f, 0.78431374f);
        }

        private void OnDestroy()
        {
            if (_bandSelectionPopup != null)
                _bandSelectionPopup.OnBandChanged.RemoveListener(OnBandChangedHandler);

            if (_campaignManagePopup != null)
                _campaignManagePopup.OnVisibilityChanged.RemoveListener(OnManageVisibilityChanged);
        }

        private void OnEnable()
        {
            if (_careers != null && _careers.Count > 0)
                RebuildNavScheme();
        }

        private void OnDisable()
        {
            // Guarded: only pop our own scheme. MenuManager.PushMenu disables this menu
            // before enabling the next, so our scheme is still on top here.
            if (_navScheme != null)
            {
                Navigator.Instance?.PopScheme(_navScheme);
                _navScheme = null;
            }
        }

        public void AssignCardSlots(CareerFlowCard[] slots)
        {
            if (slots == null || slots.Length < 7) return;
            if (_cardSlots == null || _cardSlots.Length < 7 || _cardSlots[0] == null)
                _cardSlots = slots;
        }

        /// <summary>
        /// Loads career data from CareerManager, applies sort/filter, and initializes the carousel.
        /// </summary>
        public void LoadCareers()
        {
            LoadSortModeFromBand();
            CareerManager.Instance.LoadCareerData();
            ApplyCareerList(BuildCareerList());
        }

        /// <summary>
        /// Primary entry point. Called by CareerFlowBootstrapper with a pre-built career list.
        /// </summary>
        public void SetCareers(List<CareerInfo> careers)
        {
            LoadSortModeFromBand();
            ApplyCareerList(careers ?? BuildCareerList());
        }

        private void LoadSortModeFromBand()
        {
            var band = BandContainer.CurrentBand;
            if (band == null) return;

            int mode = band.SortMode;
            if (Enum.IsDefined(typeof(CareerSortMode), mode))
                _sortMode = (CareerSortMode)mode;
        }

        private void ApplyCareerList(List<CareerInfo> careers)
        {
            Debug.Log($"[CareerFlow] ApplyCareerList() called. _cardSlots valid={_cardSlots != null && _cardSlots.Length >= 7 && _cardSlots[0] != null}, " +
                $"careers={(careers != null ? careers.Count : 0)} items");

            if (_cardSlots == null || _cardSlots.Length < 7 || _cardSlots[0] == null)
            {
                Debug.LogError("[CareerFlow] ApplyCareerList() ABORTED — _cardSlots not ready. " +
                    $"_cardSlots==null={_cardSlots == null}, length={_cardSlots?.Length}, [0]==null={_cardSlots?[0] == null}");
                return;
            }

            _careers = careers ?? new List<CareerInfo>();
            _visibleCareerCount = _careers.Count;

            if (_sortMode == CareerSortMode.Progress)
                _centerIndex = FindHighestUnfinishedIndex(_careers);
            else
                _centerIndex = Mathf.Clamp(_centerIndex, 0, Mathf.Max(0, _visibleCareerCount - 1));

            Debug.Log($"[CareerFlow] ApplyCareerList() assigned _careers with {_careers.Count} items. _visibleCareerCount={_visibleCareerCount}");

            _artCache.Clear();
            UpdateBandDisplay();

            foreach (var card in _cardSlots)
            {
                if (card != null)
                    card.Initialize();
            }

            SnapCarouselToIndex(_centerIndex);
            RebuildNavScheme();
        }

        private List<CareerInfo> BuildCareerList()
        {
            var careers = CareerManager.Instance.GetCareers();
            var sorted = new List<CareerInfo>(careers);

            switch (_sortMode)
            {
                case CareerSortMode.Alphabetical:
                    sorted.Sort((a, b) =>
                        string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
                    break;
                case CareerSortMode.ReleaseDate:
                    sorted.Sort(CompareByReleaseDateOldestFirst);
                    break;
                case CareerSortMode.Progress:
                    sorted.Sort((a, b) =>
                    {
                        int pctCmp = GetCompletionPercent(b).CompareTo(GetCompletionPercent(a));
                        if (pctCmp != 0) return pctCmp;
                        return CompareByReleaseDateOldestFirst(a, b);
                    });
                    break;
            }

            var result = new List<CareerInfo>();
            foreach (var career in sorted)
            {
                if (!BandContainer.IsCareerHidden(career.id))
                    result.Add(career);
            }

            return result;
        }

        private static int CompareByReleaseDateOldestFirst(CareerInfo a, CareerInfo b)
        {
            bool aHasDate = DateTime.TryParse(a.releaseDate, out var aDate);
            bool bHasDate = DateTime.TryParse(b.releaseDate, out var bDate);

            if (aHasDate && bHasDate) return aDate.CompareTo(bDate);
            if (aHasDate) return -1;
            if (bHasDate) return 1;
            return string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
        }

        private static float GetCompletionPercent(CareerInfo career)
        {
            var gigs = CareerManager.Instance.GetGigs(career.id);
            if (gigs == null || gigs.Count == 0) return 0f;

            int done = gigs.Count(g => CareerManager.Instance.IsGigCompleted($"{career.id}|{g.name}"));
            return (float)done / gigs.Count;
        }

        /// <summary>
        /// Returns the index of the first campaign under 100% in a progress-sorted list,
        /// so the carousel lands on the highest-unfinished campaign (finished ones sit to the left).
        /// </summary>
        private static int FindHighestUnfinishedIndex(IReadOnlyList<CareerInfo> careers)
        {
            if (careers == null || careers.Count == 0) return 0;

            for (int i = 0; i < careers.Count; i++)
            {
                if (GetCompletionPercent(careers[i]) < 1f)
                    return i;
            }

            return 0;
        }

        private CareerInfo GetCenterCareer()
        {
            if (_careers == null || _centerIndex < 0 || _centerIndex >= _careers.Count)
                return null;
            return _careers[_centerIndex];
        }

        private void RefreshCareerList()
        {
            string previousCenterId = GetCenterCareer()?.id;
            ApplyCareerList(BuildCareerList());

            if (_sortMode != CareerSortMode.Progress &&
                !string.IsNullOrEmpty(previousCenterId) && _careers != null)
            {
                int restoredIndex = _careers.FindIndex(c => c.id == previousCenterId);
                if (restoredIndex >= 0)
                    SnapCarouselToIndex(restoredIndex);
            }
        }

        private void OnBandChangedHandler()
        {
            UpdateBandDisplay();
            RefreshCareerList();
        }

        public void UpdateBandDisplay()
        {
            var band = CareerManager.Instance.CurrentBand;
            if (_bandNameText != null && band != null)
                _bandNameText.text = band.BandName.ToUpper();
        }

        private void SelectBand()
        {
            if (_bandSelectionPopup == null)
            {
                AutoWireUIReferences();

                if (_bandSelectionPopup == null)
                {
                    var foundPopup = GetComponentInParent<CareerFlowBootstrapper>()
                        ?.GetComponentInChildren<BandSelectionPopup>(true);
                    if (foundPopup != null)
                        AssignBandSelectionPopup(foundPopup);
                }
            }

            if (_bandSelectionPopup == null)
            {
                Debug.LogWarning("[CareerFlow] SelectBand() — no BandSelectionPopup wired or found in hierarchy.");
                return;
            }

            Debug.Log($"[CareerFlow] SelectBand() — opening '{_bandSelectionPopup.name}'.");
            _bandSelectionPopup.gameObject.SetActive(true);
        }

        private void OpenManageCampaigns()
        {
            if (_campaignManagePopup == null)
            {
                AutoWireUIReferences();

                if (_campaignManagePopup == null)
                {
                    var foundPopup = GetComponentInParent<CareerFlowBootstrapper>()
                        ?.GetComponentInChildren<CampaignManagePopup>(true);
                    if (foundPopup != null)
                        AssignCampaignManagePopup(foundPopup);
                }
            }

            if (_campaignManagePopup == null)
            {
                Debug.LogWarning("[CareerFlow] OpenManageCampaigns() — no CampaignManagePopup wired or found.");
                return;
            }

            Debug.Log($"[CareerFlow] OpenManageCampaigns() — opening '{_campaignManagePopup.name}'.");
            _campaignManagePopup.gameObject.SetActive(true);
        }

        private void OnManageVisibilityChanged()
        {
            RefreshCareerList();
        }

        private void ToggleSort()
        {
            _sortMode = (CareerSortMode)(((int)_sortMode + 1) % 3);
            BandContainer.SetSortMode((int)_sortMode);
            RefreshCareerList();
        }

        private static string GetSortLabel(CareerSortMode mode) => mode switch
        {
            CareerSortMode.Alphabetical => "Sort: A-Z",
            CareerSortMode.ReleaseDate  => "Sort: Date",
            CareerSortMode.Progress     => "Sort: %",
            _ => "Sort"
        };

        private void Back()
        {
            GetComponentInParent<CareerFlowBootstrapper>()?.CloseCareerFlow();
            MenuManager.Instance.PopMenu();
        }

        private void OpenTrophyScreen()
        {
            var career = GetCenterCareer();

            var menu = MenuManager.Instance.PushMenu(MenuManager.Menu.CareerTrophy);
            var trophyController = menu.GetComponentInChildren<CampaignTrophyController>(includeInactive: true);

            if (trophyController == null || !CampaignStatsController.IsCampaignCompleted(career))
            {
                MenuManager.Instance.PopMenu();
                return;
            }

            trophyController.Open(career);
        }

        /// <summary>
        /// Sets a static background for the CareerFlow menu.
        /// Called by CareerFlowBootstrapper after opening.
        /// </summary>
        public void SetStaticBackground(Color fallbackColor)
        {
            if (_background != null)
            {
                // Use a minimal config with just the fallback colour
                var config = new CoverFlow.CoverFlowConfig
                {
                    UseImageBackground = false,
                    FallbackColor = fallbackColor
                };
                _background.ApplyConfig(config, string.Empty);
            }
        }

        private void SnapCarouselToIndex(int centerIndex)
        {
            _centerIndex = Mathf.Clamp(centerIndex, 0, Mathf.Max(0, _visibleCareerCount - 1));
            LayoutCards();
            UpdateActiveCardContent();
        }

        public void StepCarousel(int direction)
        {
            if (_isAnimating || _careers == null || _careers.Count == 0) return;
            int newIndex = _centerIndex + direction;
            if (newIndex < 0 || newIndex >= _visibleCareerCount)
            {
                PlayScrollSound(SfxSample.ScrollCantScroll);
                return;
            }
            _centerIndex = newIndex;
            PlayScrollSound(SfxSample.ScrollMain);
            StartCoroutine(AnimateTransition(direction));
        }

        // ── Animation System ───────────────────────────────────────
        //    Identical to CoverFlowController's AnimateTransition.

        private IEnumerator AnimateTransition(int direction, bool resetAnimatingFlag = true)
        {
            _isAnimating = true;

            var startPos = new Vector3[7];
            var startRot = new Quaternion[7];
            var startScl = new Vector3[7];
            var startAlpha = new float[7];

            var targetPos = new Vector3[7];
            var targetRot = new Quaternion[7];
            var targetScl = new Vector3[7];
            var targetAlpha = new float[7];

            var movingCards = new CareerFlowCard[7];
            Array.Copy(_cardSlots, movingCards, 7);

            if (direction == 1)
            {
                for (int i = 1; i < 7; i++)
                {
                    if (movingCards[i] == null) continue;
                    startPos[i] = LAYOUTS[i].LocalPosition;
                    startRot[i] = Quaternion.Euler(0, LAYOUTS[i].YRotation, 0);
                    startScl[i] = Vector3.one * LAYOUTS[i].Scale;
                    startAlpha[i] = LAYOUTS[i].Opacity;

                    int targetSlot = i - 1;
                    targetPos[i] = LAYOUTS[targetSlot].LocalPosition;
                    targetRot[i] = Quaternion.Euler(0, LAYOUTS[targetSlot].YRotation, 0);
                    targetScl[i] = Vector3.one * LAYOUTS[targetSlot].Scale;
                    targetAlpha[i] = LAYOUTS[targetSlot].Opacity;
                }

                if (movingCards[0] != null)
                {
                    startPos[0] = LAYOUTS[0].LocalPosition;
                    startRot[0] = Quaternion.Euler(0, LAYOUTS[0].YRotation, 0);
                    startScl[0] = Vector3.one * LAYOUTS[0].Scale;
                    startAlpha[0] = LAYOUTS[0].Opacity;

                    targetPos[0] = VIRTUAL_LEFT.LocalPosition;
                    targetRot[0] = Quaternion.Euler(0, VIRTUAL_LEFT.YRotation, 0);
                    targetScl[0] = Vector3.one * VIRTUAL_LEFT.Scale;
                    targetAlpha[0] = VIRTUAL_LEFT.Opacity;
                }

                int incomingRightCareer = _centerIndex + 3;
                var recycledRight = movingCards[0];
                if (incomingRightCareer >= 0 && incomingRightCareer < _visibleCareerCount && recycledRight != null)
                {
                    PopulateCard(recycledRight, incomingRightCareer);
                    recycledRight.gameObject.SetActive(true);

                    recycledRight.transform.localPosition = OffsetPosition(VIRTUAL_RIGHT.LocalPosition);
                    recycledRight.transform.localRotation = Quaternion.Euler(0, VIRTUAL_RIGHT.YRotation, 0);
                    recycledRight.transform.localScale = Vector3.one * VIRTUAL_RIGHT.Scale;
                    recycledRight.CanvasGroup.alpha = VIRTUAL_RIGHT.Opacity;

                    startPos[0] = VIRTUAL_RIGHT.LocalPosition;
                    startRot[0] = Quaternion.Euler(0, VIRTUAL_RIGHT.YRotation, 0);
                    startScl[0] = Vector3.one * VIRTUAL_RIGHT.Scale;
                    startAlpha[0] = VIRTUAL_RIGHT.Opacity;

                    targetPos[0] = LAYOUTS[6].LocalPosition;
                    targetRot[0] = Quaternion.Euler(0, LAYOUTS[6].YRotation, 0);
                    targetScl[0] = Vector3.one * LAYOUTS[6].Scale;
                    targetAlpha[0] = LAYOUTS[6].Opacity;
                }
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    if (movingCards[i] == null) continue;
                    startPos[i] = LAYOUTS[i].LocalPosition;
                    startRot[i] = Quaternion.Euler(0, LAYOUTS[i].YRotation, 0);
                    startScl[i] = Vector3.one * LAYOUTS[i].Scale;
                    startAlpha[i] = LAYOUTS[i].Opacity;

                    int targetSlot = i + 1;
                    targetPos[i] = LAYOUTS[targetSlot].LocalPosition;
                    targetRot[i] = Quaternion.Euler(0, LAYOUTS[targetSlot].YRotation, 0);
                    targetScl[i] = Vector3.one * LAYOUTS[targetSlot].Scale;
                    targetAlpha[i] = LAYOUTS[targetSlot].Opacity;
                }

                if (movingCards[6] != null)
                {
                    startPos[6] = LAYOUTS[6].LocalPosition;
                    startRot[6] = Quaternion.Euler(0, LAYOUTS[6].YRotation, 0);
                    startScl[6] = Vector3.one * LAYOUTS[6].Scale;
                    startAlpha[6] = LAYOUTS[6].Opacity;

                    targetPos[6] = VIRTUAL_RIGHT.LocalPosition;
                    targetRot[6] = Quaternion.Euler(0, VIRTUAL_RIGHT.YRotation, 0);
                    targetScl[6] = Vector3.one * VIRTUAL_RIGHT.Scale;
                    targetAlpha[6] = VIRTUAL_RIGHT.Opacity;
                }

                int incomingLeftCareer = _centerIndex - 3;
                var recycledLeft = movingCards[6];
                if (incomingLeftCareer >= 0 && incomingLeftCareer < _visibleCareerCount && recycledLeft != null)
                {
                    PopulateCard(recycledLeft, incomingLeftCareer);
                    recycledLeft.gameObject.SetActive(true);

                    recycledLeft.transform.localPosition = OffsetPosition(VIRTUAL_LEFT.LocalPosition);
                    recycledLeft.transform.localRotation = Quaternion.Euler(0, VIRTUAL_LEFT.YRotation, 0);
                    recycledLeft.transform.localScale = Vector3.one * VIRTUAL_LEFT.Scale;
                    recycledLeft.CanvasGroup.alpha = VIRTUAL_LEFT.Opacity;

                    startPos[6] = VIRTUAL_LEFT.LocalPosition;
                    startRot[6] = Quaternion.Euler(0, VIRTUAL_LEFT.YRotation, 0);
                    startScl[6] = Vector3.one * VIRTUAL_LEFT.Scale;
                    startAlpha[6] = VIRTUAL_LEFT.Opacity;

                    targetPos[6] = LAYOUTS[0].LocalPosition;
                    targetRot[6] = Quaternion.Euler(0, LAYOUTS[0].YRotation, 0);
                    targetScl[6] = Vector3.one * LAYOUTS[0].Scale;
                    targetAlpha[6] = LAYOUTS[0].Opacity;
                }
            }

            float totalAnimationTime = _transitionDuration + _overshootDuration;
            float elapsed = 0f;

            while (elapsed < totalAnimationTime)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / totalAnimationTime);

                float t;
                if (normalizedTime < 0.72f)
                {
                    float travelTime = normalizedTime / 0.72f;
                    t = _easingCurve.Evaluate(travelTime) * (1f + _overshootAmount);
                }
                else
                {
                    float settleTime = (normalizedTime - 0.72f) / 0.28f;
                    float settleEase = 1f - Mathf.Pow(1f - settleTime, 3f);
                    t = Mathf.Lerp(1f + _overshootAmount, 1f, settleEase);
                }

                for (int i = 0; i < 7; i++)
                {
                    var c = movingCards[i];
                    if (c == null || !c.gameObject.activeSelf) continue;

                    c.transform.localPosition = OffsetPosition(Vector3.LerpUnclamped(startPos[i], targetPos[i], t));
                    c.transform.localRotation = Quaternion.SlerpUnclamped(startRot[i], targetRot[i], t);
                    c.transform.localScale = Vector3.LerpUnclamped(startScl[i], targetScl[i], t);
                    c.CanvasGroup.alpha = Mathf.LerpUnclamped(startAlpha[i], targetAlpha[i], Mathf.Clamp01(t));
                }

                UpdateLiveDepthSorting(movingCards);
                yield return null;
            }

            if (direction == 1)
            {
                if (!((_centerIndex + 3) >= 0 && (_centerIndex + 3) < _visibleCareerCount) && movingCards[0] != null)
                    movingCards[0].gameObject.SetActive(false);

                _cardSlots[0] = movingCards[1]; _cardSlots[1] = movingCards[2]; _cardSlots[2] = movingCards[3];
                _cardSlots[3] = movingCards[4]; _cardSlots[4] = movingCards[5]; _cardSlots[5] = movingCards[6]; _cardSlots[6] = movingCards[0];
            }
            else
            {
                if (!((_centerIndex - 3) >= 0 && (_centerIndex - 3) < _visibleCareerCount) && movingCards[6] != null)
                    movingCards[6].gameObject.SetActive(false);

                _cardSlots[6] = movingCards[5]; _cardSlots[5] = movingCards[4]; _cardSlots[4] = movingCards[3];
                _cardSlots[3] = movingCards[2]; _cardSlots[2] = movingCards[1]; _cardSlots[1] = movingCards[0]; _cardSlots[0] = movingCards[6];
            }

            int[] postTransitionIndices = GetSlotCareerIndices(_centerIndex);
            for (int i = 0; i < 7; i++)
            {
                var c = _cardSlots[i];
                if (c == null || !c.gameObject.activeSelf) continue;

                int ci = postTransitionIndices[i];
                if (ci >= 0 && ci < _visibleCareerCount)
                    c.SetCareer(_careers[ci], GetCardState(ci));

                var finalLayout = LAYOUTS[i];
                c.transform.localPosition = OffsetPosition(finalLayout.LocalPosition);
                c.transform.localRotation = Quaternion.Euler(0, finalLayout.YRotation, 0);
                c.transform.localScale = Vector3.one * finalLayout.Scale;
                c.CanvasGroup.alpha = finalLayout.Opacity;
                c.ApplyTextOpacityForSlot(i == 3);
            }

            UpdateCardSortingOrder();
            UpdateActiveCardContent();
            if (resetAnimatingFlag)
                _isAnimating = false;
        }

        private void LayoutCards()
        {
            Debug.Log($"[CareerFlow] LayoutCards() — _centerIndex={_centerIndex}, _visibleCareerCount={_visibleCareerCount}");
            int[] indices = GetSlotCareerIndices(_centerIndex);
            for (int i = 0; i < 7; i++)
            {
                var card = _cardSlots[i];
                if (card == null)
                {
                    Debug.LogWarning($"[CareerFlow] LayoutCards() — _cardSlots[{i}] is NULL, skipping.");
                    continue;
                }
                int ci = indices[i];
                if (ci < 0 || ci >= _visibleCareerCount)
                {
                    Debug.Log($"[CareerFlow] LayoutCards() — Slot[{i}] index={ci} out of range [0,{_visibleCareerCount}), hiding card.");
                    card.gameObject.SetActive(false);
                    continue;
                }

                card.gameObject.SetActive(true);
                Debug.Log($"[CareerFlow] LayoutCards() — Slot[{i}] populating with career index={ci} " +
                    $"(id='{_careers[ci].id}', name='{_careers[ci].name}')");
                PopulateCard(card, ci);

                var layout = LAYOUTS[i];
                card.transform.localPosition = OffsetPosition(layout.LocalPosition);
                card.transform.localRotation = Quaternion.Euler(0, layout.YRotation, 0);
                card.transform.localScale = Vector3.one * layout.Scale;
                card.CanvasGroup.alpha = layout.Opacity;
                card.ApplyTextOpacityForSlot(i == 3);
            }
            UpdateCardSortingOrder();
        }

        private void PopulateCard(CareerFlowCard card, int careerIndex)
        {
            var career = _careers[careerIndex];
            var state = GetCardState(careerIndex);

            Debug.Log($"[CareerFlow] PopulateCard() — careerIndex={careerIndex}, " +
                $"career.id='{career?.id ?? "NULL"}', career.name='{career?.name ?? "NULL"}', " +
                $"state={state}, card.name='{card?.name ?? "NULL"}'");

            card.SetCareer(career, state);

            if (state == CareerFlowCardState.Active || state == CareerFlowCardState.Completed)
            {
                var tex = LoadArtForCareer(career);
                Debug.Log($"[CareerFlow] PopulateCard() — LoadArtForCareer('{career.id}') returned " +
                    $"{(tex != null ? $"Texture2D {tex.width}x{tex.height}" : "NULL")}");
                card.SetPoster(tex);
            }

            var cfController = card.GetComponent<CareerFlowController>();
            if (cfController != null)
            {
                var data = BuildCareerCardData(career, state);
                Debug.Log($"[CareerFlow] PopulateCard() — BuildCareerCardData: " +
                    $"careerName='{data.careerName}', gigCount={data.gigCount}, " +
                    $"completedGigs={data.completedGigs}, completionPercent={data.completionPercent:F2}, " +
                    $"statusText='{data.statusText}', boxArtSprite={(data.boxArtSprite != null ? "present" : "NULL")}");
                cfController.PopulateCard(data);
            }
            else
            {
                Debug.LogWarning($"[CareerFlow] PopulateCard() — card '{card.name}' has NO CareerFlowController component!");
            }
        }

        private CareerCardData BuildCareerCardData(CareerInfo career, CareerFlowCardState state)
        {
            int totalGigs = 0;
            int completedGigs = 0;

            var gigs = CareerManager.Instance.GetGigs(career.id);
            if (gigs != null)
            {
                totalGigs = gigs.Count;
                completedGigs = gigs.Count(g => CareerManager.Instance.IsGigCompleted($"{career.id}|{g.name}"));
            }

            float completionPercent = totalGigs > 0 ? (float)completedGigs / totalGigs : 0f;
            bool isCompleted = totalGigs > 0 && completedGigs >= totalGigs;

            // Build narrative status text
            string statusText;
            if (isCompleted)
                statusText = "• TOUR COMPLETED •";
            else if (completedGigs == 0)
                statusText = $"• {totalGigs} GIGS AWAIT •";
            else
            {
                int remaining = totalGigs - completedGigs;
                statusText = $"• {remaining} GIG{(remaining == 1 ? "" : "S")} REMAINING •";
            }

            // Load box art sprite
            Sprite boxArtSprite = null;
            var tex = LoadArtForCareer(career);
            if (tex != null)
                boxArtSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));

            // Get accent colour from career config
            Color accentColor = career.CoverFlowConfig?.AccentColor ?? new Color(0f, 0.33f, 1f);

            // Get displayNameOnCrate from career config
            bool displayNameOnCrate = career.CoverFlowConfig?.DisplayNameOnCrate ?? false;

            // Get useCoverFlow from career config
            bool useCoverFlow = career.CoverFlowConfig?.UseCoverFlow ?? false;

            return new CareerCardData
            {
                careerId = career.id,
                careerName = career.name,
                boxArtSprite = boxArtSprite,
                sourceIcon = career.sourceIcon ?? string.Empty,
                author = career.author ?? string.Empty,
                gigCount = totalGigs,
                completedGigs = completedGigs,
                completionPercent = completionPercent,
                isCompleted = isCompleted,
                statusText = statusText,
                accentColor = accentColor,
                displayNameOnCrate = displayNameOnCrate,
                useCoverFlow = useCoverFlow
            };
        }

        private CareerFlowCardState GetCardState(int careerIndex)
        {
            if (careerIndex < 0 || careerIndex >= _careers.Count)
                return CareerFlowCardState.Active;

            var career = _careers[careerIndex];
            var gigs = CareerManager.Instance.GetGigs(career.id);
            if (gigs == null || gigs.Count == 0)
                return CareerFlowCardState.Active;

            int completedGigs = gigs.Count(g => CareerManager.Instance.IsGigCompleted($"{career.id}|{g.name}"));
            return completedGigs >= gigs.Count ? CareerFlowCardState.Completed : CareerFlowCardState.Active;
        }

        private int[] GetSlotCareerIndices(int c) => new[] { c - 3, c - 2, c - 1, c, c + 1, c + 2, c + 3 };

        private void UpdateCardSortingOrder()
        {
            for (int i = 0; i < 7; i++)
            {
                int idx = i < 3 ? i : 6 - (i - 3);
                var c = _cardSlots[idx];
                if (c != null && c.gameObject.activeSelf)
                    c.transform.SetAsLastSibling();
            }
        }

        private void UpdateLiveDepthSorting(CareerFlowCard[] cards)
        {
            var sortedActive = cards
                .Where(c => c != null && c.gameObject.activeSelf)
                .OrderByDescending(c => c.transform.localPosition.z)
                .ToList();
            foreach (var card in sortedActive)
                card.transform.SetAsLastSibling();
        }

        private void UpdateActiveCardContent()
        {
            var cc = _cardSlots[3];
            if (cc == null || !cc.gameObject.activeSelf || cc.Career == null) return;
            var state = GetCardState(_centerIndex);
            cc.SetCareer(cc.Career, state);
            cc.ApplyTextOpacityForSlot(true);
            var cfController = cc.GetComponent<CareerFlowController>();
            if (cfController != null)
                cfController.PopulateCard(BuildCareerCardData(cc.Career, state));
        }

        // ── Art Loading ────────────────────────────────────────────

        private Texture2D LoadArtForCareer(CareerInfo career)
        {
            string key = career.id;
            if (_artCache.TryGetValue(key, out var cached))
                return cached;

            Texture2D tex = null;
            if (!string.IsNullOrEmpty(career.artworkPath))
                tex = LoadTextureFromFile(career.artworkPath);

            _artCache[key] = tex;
            return tex;
        }

        private static Texture2D LoadTextureFromFile(string path)
        {
            if (!System.IO.File.Exists(path)) return null;
            try
            {
                var t = new Texture2D(2, 2);
                if (t.LoadImage(System.IO.File.ReadAllBytes(path)))
                    return t;
            }
            catch (Exception e)
            {
                Debug.LogError($"CareerFlowMenuController: LoadTexture failed: {path}: {e.Message}");
            }
            return null;
        }

        // ── SFX ────────────────────────────────────────────────────

        private void PlayScrollSound(SfxSample sample)
        {
            if (Time.unscaledTime - _lastScrollSfxTime < _scrollSfxCooldown) return;
            _lastScrollSfxTime = Time.unscaledTime;
            GlobalAudioHandler.PlaySoundEffect(sample);
        }

        // ── Navigation ─────────────────────────────────────────────

        private void RebuildNavScheme()
        {
            var nav = Navigator.Instance;
            if (nav == null) return;

            // If our scheme is currently buried under a child menu's scheme (a gig or a
            // popup is open), leave the stack untouched — popping/pushing here would
            // clobber the child's scheme. It refreshes once we're the active menu again.
            if (_navScheme != null && !ReferenceEquals(nav.PeekScheme(), _navScheme))
                return;

            // Remove only our own scheme (guarded no-op if it isn't on top).
            if (_navScheme != null)
            {
                nav.PopScheme(_navScheme);
                _navScheme = null;
            }

            string sortLabel = GetSortLabel(_sortMode);

            // Handlers are stable and evaluate live state at invoke-time, so the scheme
            // never needs to be rebuilt while scrolling — only when the list itself
            // changes (sort/hide/unhide/band) via ApplyCareerList, or on enable.
            var entries = new List<NavigationScheme.Entry>
            {
                new(MenuAction.Up,     "Menu.Common.Up",      () => StepCarousel(-1)),
                new(MenuAction.Down,   "Menu.Common.Down",    () => StepCarousel(1)),
                new(MenuAction.Green,  "Menu.Common.Confirm", () => OnCareerSelected()),
                new(MenuAction.Red,    "Menu.Common.Back",    Back),
                new(MenuAction.Orange, "Manage",              OpenManageCampaigns),
                new(MenuAction.Yellow, "View Trophy",         OnTrophyButton),
                new(MenuAction.Blue,   "Band",                SelectBand),
                new(MenuAction.Select, sortLabel,             ToggleSort),
            };

            // allowsMusicPlayer: true keeps the music player available (matching the Gig CoverFlow).
            _navScheme = new NavigationScheme(entries, true);
            nav.PushScheme(_navScheme);
        }

        private void OnTrophyButton()
        {
            if (!CampaignStatsController.IsCampaignCompleted(GetCenterCareer()))
            {
                // Feedback: the trophy is locked until the campaign is fully completed.
                GlobalAudioHandler.PlaySoundEffect(SfxSample.ScrollCantScroll);
                return;
            }

            OpenTrophyScreen();
        }

        private void OnCareerSelected()
        {
            var cc = _cardSlots[3];
            Debug.Log($"[CareerFlow] OnCareerSelected() — center card slot[3]={(cc != null ? $"'{cc.name}'" : "NULL")}, " +
                $"Career={(cc?.Career != null ? $"'{cc.Career.id}'/'{cc.Career.name}'" : "NULL")}, " +
                $"PlayerCount={PlayerContainer.Players.Count}");

            if (cc?.Career == null)
            {
                Debug.LogWarning("[CareerFlow] OnCareerSelected() — center card has no Career data. Aborting.");
                return;
            }

            if (PlayerContainer.Players.Count <= 0)
            {
                Debug.LogWarning("[CareerFlow] OnCareerSelected() — no players in PlayerContainer. " +
                    "Transitioning anyway (career selection doesn't require active players).");
                // Fall through — career selection should work even without players
            }

            var career = cc.Career;

            Debug.Log($"[CareerFlow] OnCareerSelected() — routing career '{career.name}' to CoverFlow");

            // Route to Gig CoverFlow (classic GigView is disabled)
            Debug.Log("[CareerFlow] OnCareerSelected() — pushing CareerGigModern menu...");

            CoverFlow.CoverFlowBootstrapper bootstrapper = null;
            Menu.MenuObject gigMenu = null;

            try
            {
                gigMenu = Menu.MenuManager.Instance.PushMenu(Menu.MenuManager.Menu.CareerGigModern);
                Debug.Log($"[CareerFlow] OnCareerSelected() — CareerGigModern menu returned: {(gigMenu != null ? $"'{gigMenu.name}'" : "NULL")}");

                if (gigMenu != null)
                {
                    bootstrapper = gigMenu.GetComponent<CoverFlow.CoverFlowBootstrapper>();
                    if (bootstrapper == null)
                        bootstrapper = gigMenu.GetComponentInChildren<CoverFlow.CoverFlowBootstrapper>(includeInactive: true);
                }
            }
            catch (System.InvalidOperationException ex)
            {
                Debug.LogWarning($"[CareerFlow] OnCareerSelected() — PushMenu(CareerGigModern) threw: {ex.Message}. " +
                    "Scanning for any active CoverFlowBootstrapper in the scene.");
            }

            // If the registered menu returned the wrong GameObject, or PushMenu threw,
            // scan ALL children of MenuManager for the CoverFlowBootstrapper.
            if (bootstrapper == null || !bootstrapper.IsAvailable)
            {
                Debug.Log($"[CareerFlow] OnCareerSelected() — scanning MenuManager children for CoverFlowBootstrapper...");
                var allBootstrappers = Menu.MenuManager.Instance.GetComponentsInChildren<CoverFlow.CoverFlowBootstrapper>(includeInactive: true);
                Debug.Log($"[CareerFlow] OnCareerSelected() — found {allBootstrappers.Length} CoverFlowBootstrapper(s) in scene.");

                foreach (var candidate in allBootstrappers)
                {
                    Debug.Log($"[CareerFlow]   Candidate: '{candidate.name}' on '{candidate.gameObject.name}', " +
                        $"IsAvailable={candidate.IsAvailable}, activeInHierarchy={candidate.gameObject.activeInHierarchy}");
                    if (candidate.IsAvailable)
                    {
                        bootstrapper = candidate;
                        gigMenu = candidate.GetComponentInParent<Menu.MenuObject>();
                        Debug.Log($"[CareerFlow] OnCareerSelected() — using bootstrapper '{candidate.name}' " +
                            $"under MenuObject '{(gigMenu != null ? gigMenu.name : "NULL")}'.");
                        break;
                    }
                }
            }

            if (bootstrapper != null && bootstrapper.IsAvailable)
            {
                Debug.Log($"[CareerFlow] OnCareerSelected() — CoverFlowBootstrapper available, opening campaign '{career.name}'.");
                bootstrapper.OpenCampaign(career);
            }
            else
            {
                Debug.LogError($"[CareerFlow] OnCareerSelected() — NO CoverFlowBootstrapper available for '{career.name}'. " +
                    "Cannot open campaign. Wire CoverFlowController and card slots on the CareerGigModern prefab.");
                if (gigMenu != null)
                    Menu.MenuManager.Instance.PopMenu();
            }
        }
    }
}