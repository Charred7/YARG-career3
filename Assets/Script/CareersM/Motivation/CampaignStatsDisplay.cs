using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Core;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Trophy Case — AAA-quality campaign completion celebration.
    ///
    /// DEPRECATED: Legacy procedural overlay. Superseded by CampaignTrophyMenu prefab.
    ///
    /// Architecture:
    ///   - Renders on a dedicated overlay Canvas (sortOrder 200).
    ///   - Rigid explicit sizing: LayoutElement with preferredWidth/Height
    ///     on every icon, fixed-width value columns, TMPro auto-sizing on titles.
    ///   - Split-screen: 30% left ("BAND HIGHLIGHTS"), 70% right ("PLAYER LEGENDS").
    ///   - Frosted glass backing (ColorBgGlass) over deep charcoal (ColorBgDark).
    ///   - Luminous instrument-colored card borders.
    ///   - Geometric diamond accolade badges as the hero element.
    ///   - Card flip animation with elastic overshoot bounce (0.25s, 0.06s stagger).
    ///
    /// CRITICAL GUARDRAILS:
    ///   - Every icon/graphic gets a LayoutElement with preferredWidth/Height
    ///     matching its sizeDelta and flexibleWidth=0.
    ///   - All LayoutGroups explicitly disable childForceExpandWidth.
    ///   - Stat rows use fixed 40px height with label (flexibleWidth=1) and
    ///     value (preferredWidth=150px, right-aligned) to prevent collisions.
    ///
    /// All UI is procedurally created at runtime — no prefab editing needed.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class CampaignStatsDisplay : MonoBehaviour
    {
        private static CampaignStatsDisplay _instance;
        public static CampaignStatsDisplay Instance => _instance;

        // ─── Diagnostics ──────────────────────────────────────────────
        public bool EnableDiagnostics = true;

        // ─── References ───────────────────────────────────────────────
        private CareerView _careerView;

        // ─── UI State ──────────────────────────────────────────────────
        private Canvas _overlayCanvas;
        private GameObject _statsPanel;
        private CanvasGroup _panelCanvasGroup;
        private Transform _contentRoot;

        private CampaignStats _currentStats;
        private CareerInfo _lastSelectedCareer;
        private Coroutine _activeAnim;
        private Coroutine _borderPulseAnim;

        // Completion cache
        private readonly Dictionary<string, bool> _completionCache = new();
        private float _lastCacheCheck;

        // ─── AAA Color Palette ────────────────────────────────────────
        private static readonly Color ColorBgDark       = new Color(0.015f, 0.018f, 0.03f, 0.98f);
        private static readonly Color ColorBgGlass      = new Color(0.05f, 0.07f, 0.12f, 0.7f);
        private static readonly Color ColorBgCard       = new Color(0.08f, 0.11f, 0.18f, 0.9f);
        private static readonly Color ColorBgHeader     = new Color(0.025f, 0.030f, 0.060f, 1.00f);
        private static readonly Color ColorDivider      = new Color(0.180f, 0.160f, 0.070f, 0.55f);
        private static readonly Color ColorGoldPrimary  = new Color(1.000f, 0.780f, 0.000f, 1.00f);
        private static readonly Color ColorGoldHyper    = new Color(1.000f, 0.950f, 0.600f, 1.00f);
        private static readonly Color ColorGoldMedium   = new Color(0.750f, 0.550f, 0.100f, 1.00f);
        private static readonly Color ColorGoldDark     = new Color(0.500f, 0.350f, 0.040f, 1.00f);
        private static readonly Color ColorGtrOrange    = IconLibrary.ColorGtrOrange;
        private static readonly Color ColorBassPurple   = IconLibrary.ColorBassPurple;
        private static readonly Color ColorDrumsBlue    = IconLibrary.ColorDrumsBlue;
        private static readonly Color ColorVocalsGreen  = IconLibrary.ColorVocalsGreen;
        private static readonly Color ColorAccentBlue   = new Color(0.31f, 0.76f, 0.97f, 1f);
        private static readonly Color ColorAccentOrange = new Color(1.00f, 0.50f, 0.10f, 1f);
        private static readonly Color ColorAccentGreen  = new Color(0.18f, 0.80f, 0.44f, 1f);
        private static readonly Color ColorAccentRed    = new Color(0.91f, 0.30f, 0.24f, 1f);
        private static readonly Color ColorAccentPurple = new Color(0.70f, 0.40f, 1.00f, 1f);
        private static readonly Color ColorTextBody     = new Color(0.90f, 0.90f, 0.95f, 1f);
        private static readonly Color ColorTextMuted    = new Color(0.50f, 0.52f, 0.60f, 1f);
        private static readonly Color ColorTextLabel    = new Color(0.60f, 0.65f, 0.75f, 1f);

        // ─── Layout Constants ─────────────────────────────────────────
        private const float PanelHeightFraction = 0.70f;
        private const float PanelWidthFraction  = 0.95f;
        private const float LeftColFraction     = 0.35f;
        private const float PadOuter = 12f;
        private const float PadInner = 8f;
        private const int   MaxCards    = 4;
        private const float CardGapPx   = 10f;

        // ─── Rigid Sizing Constants ───────────────────────────────────
        private const float TitleRowHeightPx    = 70f;
        private const float IconFixedSize       = 32f;
        private const float BadgeFixedSize      = 120f;
        private const float StatValueWidthPx    = 150f;
        private const float StatRowHeightPx     = 40f;
        private const float SectionHeaderHeight = 36f;
        private const float DividerThickness    = 1.5f;

        // ─── Font Sizes ───────────────────────────────────────────────
        private float _panelHeightPx;
        private float _panelWidthPx;

        private float SectionHeaderFontPx => Mathf.Clamp(_panelHeightPx * 0.072f, 14f, 48f);
        private float PlayerNameFontPx    => Mathf.Clamp(_panelHeightPx * 0.072f, 14f, 48f);
        private float BandStatFontPx      => Mathf.Clamp(_panelHeightPx * 0.058f, 14f, 40f);
        private float CardStatFontPx      => Mathf.Clamp(_panelHeightPx * 0.052f, 12f, 32f);
        private float CardLabelFontPx     => Mathf.Clamp(_panelHeightPx * 0.040f, 9f, 20f);
        private float EmptyFontPx         => Mathf.Clamp(_panelHeightPx * 0.050f, 12f, 32f);

        // ─── Instrument Mapping ───────────────────────────────────────
        private static readonly Color[] InstrumentColors =
            { ColorGtrOrange, ColorBassPurple, ColorDrumsBlue, ColorVocalsGreen };

        private static readonly string[] InstrumentNames =
            { "GUITAR", "BASS", "DRUMS", "VOCALS" };

        // ─── Singleton ─────────────────────────────────────────────────
        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
            _careerView = GetComponent<CareerView>() ?? FindObjectOfType<CareerView>();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            StopAllCoroutines();
        }

        public bool IsPanelVisible => _statsPanel != null && _statsPanel.activeSelf;

        // ─── Public API ────────────────────────────────────────────────
        public void OnCampaignSelected(CareerInfo career)
        {
            if (EnableDiagnostics)
            {
                bool completed = career != null && IsCareerCompleted(career);
                Debug.Log($"[TrophyCase] OnCampaignSelected('{career?.name}') — " +
                          $"careerNull={career == null}, completed={completed}, " +
                          $"panelActive={_statsPanel?.activeSelf}, " +
                          $"lastSelected={_lastSelectedCareer?.id}");
            }

            if (career == null || !IsCareerCompleted(career))
            {
                HidePanel();
                _lastSelectedCareer = null;
                return;
            }
            if (_lastSelectedCareer?.id == career.id && _statsPanel != null && _statsPanel.activeSelf)
                return;

            _lastSelectedCareer = career;
            _currentStats = StatCalculator.ComputeCampaignStats(career);

            if (_currentStats == null)
            {
                Debug.Log($"[TrophyCase] ComputeCampaignStats returned null — showing simple completion");
                ShowSimpleCompletion(career);
                return;
            }
            ShowStatsPanel();
        }

        // ─── Completion Check ──────────────────────────────────────────
        private bool IsCareerCompleted(CareerInfo career)
        {
            if (_completionCache.TryGetValue(career.id, out var cached) &&
                Time.unscaledTime - _lastCacheCheck < 2f)
                return cached;

            var band = CareerManager.Instance.CurrentBand;
            if (band == null) return false;

            var gigs = CareerManager.Instance.GetGigs(career.id);
            foreach (var gig in gigs)
            {
                string gigId = $"{career.id}|{gig.name}";
                if (!CareerManager.Instance.IsGigCompleted(gigId))
                {
                    _completionCache[career.id] = false;
                    _lastCacheCheck = Time.unscaledTime;
                    return false;
                }
            }
            bool completed = gigs.Count > 0;
            _completionCache[career.id] = completed;
            _lastCacheCheck = Time.unscaledTime;
            return completed;
        }

        // ─── Panel Lifecycle ───────────────────────────────────────────
        private void EnsureOverlayCanvas()
        {
            if (_overlayCanvas != null) return;

            var canvasGo = new GameObject("TrophyCaseCanvas");
            DontDestroyOnLoad(canvasGo);
            _overlayCanvas = canvasGo.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = 200;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        private void ShowStatsPanel()
        {
            if (EnableDiagnostics)
                Debug.Log($"[TrophyCase] ShowStatsPanel — canvasExists={_overlayCanvas != null}, " +
                          $"panelNull={_statsPanel == null}, contentRootNull={_contentRoot == null}, " +
                          $"statsNull={_currentStats == null}");

            EnsureOverlayCanvas();
            if (_statsPanel == null) _statsPanel = CreatePanelRoot();
            ClearAllContent();
            BuildAllContent();
            _statsPanel.SetActive(true);
            StartPanelAnimations();

            if (EnableDiagnostics)
                DumpPanelDiagnostics();
        }

        private void ShowSimpleCompletion(CareerInfo career)
        {
            EnsureOverlayCanvas();
            if (_statsPanel == null) _statsPanel = CreatePanelRoot();
            ClearAllContent();

            var trophy = IconLibrary.CreateTrophy(_contentRoot, 80f, ColorGoldPrimary);
            var tRt = trophy.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.55f);
            tRt.anchorMax = new Vector2(0.5f, 0.55f);
            tRt.anchoredPosition = Vector2.zero;
            tRt.sizeDelta = new Vector2(80f, 80f);

            var titleGo = MakeAutoSizeTitle(_contentRoot, $"{career.name.ToUpper()} CONQUERED!",
                ColorGoldHyper, FontStyles.Bold, TextAlignmentOptions.Center);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.1f, 0.15f);
            titleRt.anchorMax = new Vector2(0.9f, 0.45f);
            titleRt.offsetMin = Vector2.zero;
            titleRt.offsetMax = Vector2.zero;

            var shine = titleGo.AddComponent<GoldShineTextEffect>();
            shine.ShineInterval = 3.0f;

            _statsPanel.SetActive(true);
            StartPanelAnimations();
        }

        private void ClearAllContent()
        {
            if (_contentRoot == null) return;
            foreach (Transform c in _contentRoot)
                Destroy(c.gameObject);
        }

        // ─── Panel Root ────────────────────────────────────────────────
        private GameObject CreatePanelRoot()
        {
            _panelHeightPx = Screen.height * PanelHeightFraction;
            _panelWidthPx  = Screen.width  * PanelWidthFraction;

            if (EnableDiagnostics)
                Debug.Log($"[TrophyCase] CreatePanelRoot — screen={Screen.width}x{Screen.height}, " +
                          $"panel={_panelWidthPx:F0}x{_panelHeightPx:F0}px");

            var panel = new GameObject("TrophyCasePanel",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panel.transform.SetParent(_overlayCanvas.transform, false);

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot     = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(_panelWidthPx, _panelHeightPx);
            rt.anchoredPosition = Vector2.zero;

            panel.GetComponent<Image>().color = ColorBgDark;
            panel.GetComponent<Image>().raycastTarget = false;

            // Frosted glass backing
            var glassGo = new GameObject("GlassBacking", typeof(RectTransform), typeof(Image));
            glassGo.transform.SetParent(panel.transform, false);
            var glassRt = glassGo.GetComponent<RectTransform>();
            glassRt.anchorMin = new Vector2(0.005f, 0.005f);
            glassRt.anchorMax = new Vector2(0.995f, 0.995f);
            glassRt.offsetMin = Vector2.zero;
            glassRt.offsetMax = Vector2.zero;
            glassGo.GetComponent<Image>().color = ColorBgGlass;
            glassGo.GetComponent<Image>().raycastTarget = false;

            // Top gold border
            AddBorderLine(panel.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), 3f, ColorGoldPrimary);

            // Content root — VLG for vertical stacking
            var contentGo = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(panel.transform, false);
            var crt = contentGo.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(PadOuter, PadOuter);
            crt.offsetMax = new Vector2(-PadOuter, -PadOuter);

            var contentVlg = contentGo.GetComponent<VerticalLayoutGroup>();
            contentVlg.childAlignment = TextAnchor.UpperLeft;
            contentVlg.spacing = 0f;
            contentVlg.childControlHeight = true;
            contentVlg.childControlWidth = true;
            contentVlg.childForceExpandHeight = false;
            contentVlg.childForceExpandWidth = false;  // CRITICAL: prevent blowout
            contentVlg.padding = new RectOffset(0, 0, 0, 0);

            _contentRoot = contentGo.transform;
            _panelCanvasGroup = panel.GetComponent<CanvasGroup>();
            _panelCanvasGroup.alpha = 0f;

            return panel;
        }

        // ─── Content Builder ───────────────────────────────────────────
        private void BuildAllContent()
        {
            if (_contentRoot == null || _currentStats == null)
            {
                Debug.LogWarning($"[TrophyCase] BuildAllContent skipped — " +
                    $"contentRootNull={_contentRoot == null}, statsNull={_currentStats == null}");
                return;
            }

            if (EnableDiagnostics)
            {
                Debug.Log($"[TrophyCase] Building for '{_currentStats.CareerName}' — " +
                    $"{_currentStats.ProfileStats.Count} profiles, " +
                    $"bandStars={_currentStats.BandStats?.TotalStarsEarned ?? -1}, " +
                    $"bandScore={_currentStats.BandStats?.TotalBandScore ?? -1}");
            }

            // ── Title Row (fixed 60px height) ─────────────────────────
            BuildTitleRow();

            // ── Body (fills remaining space) ──────────────────────────
            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            bodyGo.transform.SetParent(_contentRoot, false);
            var bodyLe = bodyGo.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1f;

            var bodyHlg = bodyGo.GetComponent<HorizontalLayoutGroup>();
            bodyHlg.childAlignment = TextAnchor.UpperLeft;
            bodyHlg.spacing = 0f;
            bodyHlg.childControlHeight = true;
            bodyHlg.childControlWidth = true;
            bodyHlg.childForceExpandHeight = true;
            bodyHlg.childForceExpandWidth = false;  // CRITICAL
            bodyHlg.padding = new RectOffset(0, 0, 0, 0);

            // Left: Band Highlights
            BuildBandHighlights(bodyGo.transform);

            // Vertical divider
            var divGo = new GameObject("VDivider", typeof(RectTransform), typeof(Image));
            divGo.transform.SetParent(bodyGo.transform, false);
            var divLe = divGo.AddComponent<LayoutElement>();
            divLe.preferredWidth = DividerThickness;
            divLe.flexibleWidth = 0f;
            divGo.GetComponent<Image>().color = ColorDivider;
            divGo.GetComponent<Image>().raycastTarget = false;

            // Right: Player Legends
            BuildPlayerLegends(bodyGo.transform);
        }

        // ─── Title Row (Fixed 60px, TMPro Auto-Sizing max 36) ─────────
        private void BuildTitleRow()
        {
            var rowGo = new GameObject("TitleRow",
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(_contentRoot, false);

            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.preferredHeight = TitleRowHeightPx;
            rowLe.flexibleHeight = 0f;

            rowGo.GetComponent<Image>().color = ColorBgHeader;
            rowGo.GetComponent<Image>().raycastTarget = false;

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = PadInner;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;  // CRITICAL
            hlg.padding = new RectOffset((int)PadInner, (int)PadInner, 0, 0);

            // Trophy icon — locked 32x32 with LayoutElement guard
            var trophy = IconLibrary.CreateTrophy(rowGo.transform, IconFixedSize, ColorGoldPrimary);
            LockIconSize(trophy, IconFixedSize);

            // Title text with auto-sizing, max font 36 to stay on one line
            var titleGo = MakeAutoSizeTitle(rowGo.transform,
                $"TROPHY CASE  ·  {_currentStats.CareerName.ToUpper()}  CONQUERED!",
                ColorGoldHyper, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;

            var shine = titleGo.AddComponent<GoldShineTextEffect>();
            shine.ShineInterval = 3.5f;
            shine.ShineDuration = 1.5f;
            shine.SetBaseColor(ColorGoldHyper);
            shine.SetShineColor(new Color(1f, 0.98f, 0.75f));
        }

        // ─── Band Highlights (Left Column 30%) ────────────────────────
        private void BuildBandHighlights(Transform body)
        {
            var colGo = new GameObject("BandCol",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
            colGo.transform.SetParent(body, false);

            var colLe = colGo.AddComponent<LayoutElement>();
            colLe.preferredWidth = _panelWidthPx * LeftColFraction;
            colLe.flexibleWidth = 0f;

            colGo.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.07f, 0.40f);
            colGo.GetComponent<Image>().raycastTarget = false;

            var vlg = colGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 4f;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;  // CRITICAL
            vlg.padding = new RectOffset((int)PadInner, (int)PadInner, (int)PadInner, (int)PadInner);

            // Section header
            MakeSectionHeader(colGo.transform, "BAND HIGHLIGHTS");

            // Underline
            AddUnderline(colGo.transform);

            // Stats list
            var listGo = new GameObject("BandStatList",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            listGo.transform.SetParent(colGo.transform, false);
            var listLe = listGo.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 1f;

            var listVlg = listGo.GetComponent<VerticalLayoutGroup>();
            listVlg.childAlignment = TextAnchor.UpperLeft;
            listVlg.spacing = 4f;
            listVlg.childControlHeight = true;
            listVlg.childControlWidth = true;
            listVlg.childForceExpandHeight = false;
            listVlg.childForceExpandWidth = false;  // CRITICAL
            listVlg.padding = new RectOffset(0, 0, 4, 0);

            var band = _currentStats.BandStats;

            if (band.TotalStarsEarned > 0)
                AddBandStatRow(listGo.transform, "STARS EARNED",
                    $"{band.TotalStarsEarned}", ColorGoldHyper);

            if (band.TotalBandScore > 0)
                AddBandStatRow(listGo.transform, "BAND SCORE",
                    $"{band.TotalBandScore:N0}", ColorGoldMedium);

            if (band.SongsWithFiveStars > 0)
                AddBandStatRow(listGo.transform, "5-STAR SONGS",
                    $"{band.SongsWithFiveStars}", ColorGoldHyper);

            if (band.TotalSongsWithScores > 0)
                AddBandStatRow(listGo.transform, "SONGS PLAYED",
                    $"{band.TotalSongsWithScores}", ColorAccentOrange);

            if (band.TotalSongs > 0)
                AddBandStatRow(listGo.transform, "CAMPAIGN SONGS",
                    $"{band.TotalSongs}", ColorAccentBlue);

            if (band.TotalSongsWithScores == 0)
            {
                var emptyGo = MakeText(listGo.transform, "Play songs to\nearn band stats!",
                    EmptyFontPx, ColorTextMuted, FontStyles.Normal, TextAlignmentOptions.TopLeft);
                var emptyLe = emptyGo.AddComponent<LayoutElement>();
                emptyLe.preferredHeight = StatRowHeightPx * 2.5f;
                emptyLe.flexibleHeight = 0f;
            }
        }

        /// <summary>
        /// Rigid stat row: [dot 32x32] [label flex] [value 150px right-aligned].
        /// Fixed 40px height. childForceExpandWidth is OFF to prevent text collisions.
        /// </summary>
        private void AddBandStatRow(Transform parent,
            string label, string value, Color valueColor)
        {
            var rowGo = new GameObject($"Row_{label}",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);

            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.preferredHeight = StatRowHeightPx;
            rowLe.flexibleHeight = 0f;

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 8f;
            hlg.childControlHeight = true;
            hlg.childControlWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;  // CRITICAL: prevents text collision
            hlg.padding = new RectOffset(0, 0, 0, 0);

            // Luminous dot — locked 32x32 with LayoutElement guard
            var dotGo = CreateLuminousDot(rowGo.transform, valueColor, IconFixedSize);

            // Label — flexible width, takes remaining space
            float labelFontSz = Mathf.Clamp(CardLabelFontPx, 10f, 24f);
            var labelGo = MakeText(rowGo.transform, label,
                labelFontSz, ColorTextLabel, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            labelGo.GetComponent<TextMeshProUGUI>().characterSpacing = 1.5f;
            labelGo.GetComponent<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.NoWrap;
            labelGo.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Truncate;

            // Value — fixed 150px width, right-aligned
            float valFontSz = Mathf.Clamp(BandStatFontPx, 14f, 40f);
            var valueGo = MakeText(rowGo.transform, value,
                valFontSz, valueColor, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            var valueLe = valueGo.AddComponent<LayoutElement>();
            valueLe.preferredWidth = StatValueWidthPx;
            valueLe.flexibleWidth = 0f;
            valueGo.GetComponent<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.NoWrap;
            valueGo.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Truncate;
        }

        // ─── Player Legends (Right Column 70%) ────────────────────────
        private void BuildPlayerLegends(Transform body)
        {
            var colGo = new GameObject("PlayerCol",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
            colGo.transform.SetParent(body, false);

            colGo.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 0.35f);
            colGo.GetComponent<Image>().raycastTarget = false;

            var colLe = colGo.AddComponent<LayoutElement>();
            colLe.flexibleWidth = 1f;

            var vlg = colGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.spacing = 4f;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;  // CRITICAL
            vlg.padding = new RectOffset((int)PadInner, (int)PadInner, (int)PadInner, (int)PadInner);

            // Section header
            MakeSectionHeader(colGo.transform, "PLAYER LEGENDS");

            // Underline
            AddUnderline(colGo.transform);

            // Cards area
            var cardsAreaGo = new GameObject("CardsArea",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardsAreaGo.transform.SetParent(colGo.transform, false);
            var cardsAreaLe = cardsAreaGo.AddComponent<LayoutElement>();
            cardsAreaLe.flexibleHeight = 1f;

            var cardsHlg = cardsAreaGo.GetComponent<HorizontalLayoutGroup>();
            cardsHlg.childAlignment = TextAnchor.UpperLeft;
            cardsHlg.spacing = CardGapPx;
            cardsHlg.childControlHeight = true;
            cardsHlg.childControlWidth = true;
            cardsHlg.childForceExpandHeight = true;
            cardsHlg.childForceExpandWidth = false;  // CRITICAL: cards don't sprawl
            cardsHlg.padding = new RectOffset(0, 0, 0, 0);

            int cardCount = Mathf.Min(_currentStats.ProfileStats.Count, MaxCards);

            if (cardCount == 0)
            {
                var emptyGo = MakeText(cardsAreaGo.transform,
                    "No player stats yet.\nPlay through songs to earn your legends!",
                    EmptyFontPx, ColorTextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
                var emptyLe = emptyGo.AddComponent<LayoutElement>();
                emptyLe.flexibleWidth = 1f;
                return;
            }

            // Calculate strict card width: (available - gaps) / count
            float availableWidth = _panelWidthPx - PadOuter * 2f
                - (_panelWidthPx * LeftColFraction) - DividerThickness
                - PadInner * 4f;
            float totalGaps = (cardCount - 1) * CardGapPx;
            float cardWidth = (availableWidth - totalGaps) / cardCount;

            // SAFETY CLAMP: prevent negative/zero card width
            if (cardWidth < 50f)
            {
                Debug.LogWarning($"[TrophyCase] Card width clamped: was {cardWidth:F1}px -> 50px. " +
                    $"availableWidth={availableWidth:F0}, totalGaps={totalGaps:F0}, count={cardCount}");
                cardWidth = 50f;
            }

            if (EnableDiagnostics)
            {
                Debug.Log($"[TrophyCase] Card layout: panelW={_panelWidthPx:F0}, " +
                    $"availableW={availableWidth:F0}, gaps={totalGaps:F0}, " +
                    $"cardCount={cardCount}, cardWidth={cardWidth:F1}px");
            }

            for (int i = 0; i < cardCount; i++)
            {
                BuildPlayerCard(cardsAreaGo.transform, _currentStats.ProfileStats[i], i, cardWidth);
            }
        }

        /// <summary>
        /// Player card with rigid sizing:
        /// - Strict calculated width via LayoutElement
        /// - Instrument-colored 4px left border
        /// - ColorBgCard background
        /// - Geometric badge as hero element (fixed 120x120)
        /// - Compact stat rows with fixed 150px value column
        /// </summary>
        private void BuildPlayerCard(Transform parent, ProfileCampaignStats profileStats,
            int index, float cardWidth)
        {
            Color instrColor = GetInstrumentColor(index);

            // Card outer wrapper with strict width
            var cardGo = new GameObject($"Card_{profileStats.PlayerName}",
                typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(parent, false);
            cardGo.transform.localScale = new Vector3(0f, 1f, 1f);  // Start collapsed for animation

            var cardLe = cardGo.AddComponent<LayoutElement>();
            cardLe.preferredWidth = cardWidth;
            cardLe.flexibleWidth = 0f;

            cardGo.GetComponent<Image>().color = ColorBgCard;
            cardGo.GetComponent<Image>().raycastTarget = false;

            // Luminous instrument-colored left border (4px)
            var accentGo = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentGo.transform.SetParent(cardGo.transform, false);
            var accentRt = accentGo.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.pivot     = new Vector2(0f, 0.5f);
            accentRt.offsetMin = Vector2.zero;
            accentRt.offsetMax = Vector2.zero;
            accentRt.sizeDelta = new Vector2(4f, 0f);
            accentGo.GetComponent<Image>().color = instrColor;
            accentGo.GetComponent<Image>().raycastTarget = false;

            // Inner content VLG
            var innerGo = new GameObject("Inner",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            innerGo.transform.SetParent(cardGo.transform, false);
            var innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(12f, 6f);
            innerRt.offsetMax = new Vector2(-6f, -6f);

            var innerVlg = innerGo.GetComponent<VerticalLayoutGroup>();
            innerVlg.childAlignment = TextAnchor.UpperCenter;
            innerVlg.spacing = 4f;
            innerVlg.childControlHeight = true;
            innerVlg.childControlWidth = true;
            innerVlg.childForceExpandHeight = false;
            innerVlg.childForceExpandWidth = false;  // CRITICAL
            innerVlg.padding = new RectOffset(0, 0, 0, 0);

            // ── Player Name Row ───────────────────────────────────────
            var nameRowGo = new GameObject("NameRow",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            nameRowGo.transform.SetParent(innerGo.transform, false);
            var nameRowLe = nameRowGo.AddComponent<LayoutElement>();
            nameRowLe.preferredHeight = StatRowHeightPx;
            nameRowLe.flexibleHeight = 0f;

            var nameHlg = nameRowGo.GetComponent<HorizontalLayoutGroup>();
            nameHlg.childAlignment = TextAnchor.MiddleCenter;
            nameHlg.spacing = 6f;
            nameHlg.childControlHeight = true;
            nameHlg.childControlWidth = false;
            nameHlg.childForceExpandHeight = true;
            nameHlg.childForceExpandWidth = false;  // CRITICAL
            nameHlg.padding = new RectOffset(0, 0, 0, 0);

            // Instrument dot — locked 32x32
            var dotGo = CreateLuminousDot(nameRowGo.transform, instrColor, IconFixedSize);

            // Player name
            float nameFontSz = Mathf.Clamp(PlayerNameFontPx, 14f, 48f);
            var nameGo = MakeText(nameRowGo.transform,
                profileStats.PlayerName.ToUpper(),
                nameFontSz, ColorGoldPrimary, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            var nameTextLe = nameGo.AddComponent<LayoutElement>();
            nameTextLe.flexibleWidth = 1f;
            var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            nameTmp.characterSpacing = 1f;
            nameTmp.textWrappingMode = TextWrappingModes.NoWrap;
            nameTmp.overflowMode = TextOverflowModes.Truncate;

            var nameShine = nameGo.AddComponent<GoldShineTextEffect>();
            nameShine.ShineInterval = 4.0f + (index * 0.7f);
            nameShine.ShineDuration = 1.0f;
            nameShine.SetBaseColor(ColorGoldPrimary);
            nameShine.SetShineColor(new Color(1f, 0.95f, 0.65f));

            // ── Gold divider ──────────────────────────────────────────
            var divGo = new GameObject("NameDivider", typeof(RectTransform), typeof(Image));
            divGo.transform.SetParent(innerGo.transform, false);
            var divLe = divGo.AddComponent<LayoutElement>();
            divLe.preferredHeight = DividerThickness;
            divLe.flexibleHeight = 0f;
            divGo.GetComponent<Image>().color = ColorGoldDark;
            divGo.GetComponent<Image>().raycastTarget = false;

            // ── GEOMETRIC BADGE — HERO element (fixed 120x120) ────────
            var accoladeTier = DetermineAccolade(profileStats);

            if (accoladeTier != AccoladeTier.None)
            {
                var badge = IconLibrary.CreateAccoladeBadge(
                    innerGo.transform, accoladeTier, BadgeFixedSize, instrColor);
                if (badge != null)
                {
                    var badgeLe = badge.AddComponent<LayoutElement>();
                    badgeLe.preferredHeight = BadgeFixedSize;
                    badgeLe.flexibleHeight = 0f;
                    // CreateContainer() already adds AspectRatioFitter — only add if missing
                    if (badge.GetComponent<AspectRatioFitter>() == null)
                    {
                        var badgeArf = badge.AddComponent<AspectRatioFitter>();
                        badgeArf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                        badgeArf.aspectRatio = 1f;
                    }
                }
            }
            else
            {
                // Fallback: luminous instrument-colored circle
                var fallbackGo = new GameObject("FallbackBadge",
                    typeof(RectTransform), typeof(Image));
                fallbackGo.transform.SetParent(innerGo.transform, false);
                var fbLe = fallbackGo.AddComponent<LayoutElement>();
                fbLe.preferredHeight = BadgeFixedSize * 0.45f;
                fbLe.flexibleHeight = 0f;
                var fbImg = fallbackGo.GetComponent<Image>();
                fbImg.sprite = GetDotSprite();
                fbImg.color = new Color(instrColor.r, instrColor.g, instrColor.b, 0.35f);
                fbImg.raycastTarget = false;
                var fbArf = fallbackGo.AddComponent<AspectRatioFitter>();
                fbArf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                fbArf.aspectRatio = 1f;
            }

            // ── Stats (compact, below badge) ──────────────────────────
            if (profileStats.AllStats == null || profileStats.AllStats.Count == 0)
            {
                var emptyGo = MakeText(innerGo.transform, "Play to earn stats!",
                    EmptyFontPx, ColorTextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
                var emptyLe = emptyGo.AddComponent<LayoutElement>();
                emptyLe.flexibleHeight = 1f;
            }
            else
            {
                var allStatsSorted = profileStats.AllStats
                    .OrderByDescending(s => s.SortPriority)
                    .ToList();

                const int densityThreshold = 2;
                const int maxStatsPerCard = 2;

                List<PlayerStat> statsToShow;
                if (allStatsSorted.Count <= densityThreshold)
                {
                    statsToShow = allStatsSorted;
                }
                else
                {
                    int poolSize = Mathf.Min(allStatsSorted.Count, maxStatsPerCard * 2);
                    var topPool = allStatsSorted.Take(poolSize).ToList();
                    int pickCount = UnityEngine.Random.Range(1, maxStatsPerCard + 1);
                    statsToShow = topPool
                        .OrderBy(_ => UnityEngine.Random.value)
                        .Take(pickCount)
                        .OrderByDescending(s => s.SortPriority)
                        .ToList();
                }

                float statFontSz = Mathf.Clamp(CardStatFontPx, 12f, 32f);
                float labelFontSz = Mathf.Clamp(CardLabelFontPx, 9f, 20f);

                foreach (var stat in statsToShow)
                {
                    AddStatRow(innerGo.transform, stat,
                        statFontSz, labelFontSz, index);
                }
            }
        }

        /// <summary>
        /// Rigid stat row for player cards: [label flex] [value 150px right].
        /// Fixed 40px height. childForceExpandWidth OFF to prevent collisions.
        /// </summary>
        private void AddStatRow(Transform parent, PlayerStat stat,
            float valueFontSz, float labelFontSz, int playerIndex)
        {
            var rowGo = new GameObject($"Stat_{stat.Type}",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);

            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.preferredHeight = StatRowHeightPx;
            rowLe.flexibleHeight = 0f;

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 6f;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth = false;  // CRITICAL
            hlg.padding = new RectOffset(4, 4, 0, 0);

            string labelText = GetStatLabel(stat.Type);
            string valueText = StatFormatter.FormatStatShort(stat);
            Color valueColor = GetStatColor(stat.Type);

            // Label — flexible width
            var labelGo = MakeText(rowGo.transform, labelText,
                labelFontSz, ColorTextLabel, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.characterSpacing = 1f;
            labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
            labelTmp.overflowMode = TextOverflowModes.Truncate;

            // Value — fixed 150px, right-aligned
            var valueGo = MakeText(rowGo.transform, valueText,
                valueFontSz, valueColor, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            var valueLe = valueGo.AddComponent<LayoutElement>();
            valueLe.preferredWidth = StatValueWidthPx;
            valueLe.flexibleWidth = 0f;
            valueGo.GetComponent<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.NoWrap;
            valueGo.GetComponent<TextMeshProUGUI>().overflowMode = TextOverflowModes.Truncate;

            // Gold shine on high-value stats
            if (IsHighValueStat(stat.Type))
            {
                var shine = valueGo.AddComponent<GoldShineTextEffect>();
                shine.ShineInterval = 5.0f + (playerIndex * 0.5f);
                shine.ShineDuration = 0.8f;
                shine.SetBaseColor(valueColor);
                shine.SetShineColor(ColorGoldHyper);
            }
        }

        // ─── Accolade System ───────────────────────────────────────────
        private static AccoladeTier DetermineAccolade(ProfileCampaignStats profile)
        {
            float fullCombos      = GetStatValue(profile, StatType.FullCombos);
            float longestStreak   = GetStatValue(profile, StatType.LongestStreak);
            float averageAccuracy = GetStatValue(profile, StatType.AverageAccuracy);

            if (fullCombos > 5)  return AccoladeTier.CampaignMVP;
            if (fullCombos > 0)  return AccoladeTier.PerfectStrings;
            if (longestStreak > 1000) return AccoladeTier.ImmortalStreak;
            if (averageAccuracy >= 0.98f) return AccoladeTier.SharpShooter;
            return AccoladeTier.None;
        }

        private static float GetStatValue(ProfileCampaignStats profile, StatType type)
        {
            if (profile.AllStats == null) return 0f;
            var stat = profile.AllStats.FirstOrDefault(s => s.Type == type);
            return stat?.Value ?? 0f;
        }

        // ─── Instrument Helpers ────────────────────────────────────────
        private static Color GetInstrumentColor(int playerIndex)
            => InstrumentColors[playerIndex % InstrumentColors.Length];

        private static string GetInstrumentName(int playerIndex)
            => InstrumentNames[playerIndex % InstrumentNames.Length];

        // ─── Stat Helpers ──────────────────────────────────────────────
        private static bool IsHighValueStat(StatType type) => type switch
        {
            StatType.FullCombos      => true,
            StatType.LongestStreak   => true,
            StatType.AverageAccuracy => true,
            StatType.HighScore       => true,
            StatType.FiveStarCount   => true,
            _                        => false,
        };

        private static string GetStatLabel(StatType type) => type switch
        {
            StatType.FullCombos      => "FULL COMBOS",
            StatType.HighScore       => "HIGH SCORE",
            StatType.HighAccuracy    => "95%+ ACCURACY",
            StatType.FiveStarCount   => "5-STAR SONGS",
            StatType.LongestStreak   => "BEST STREAK",
            StatType.TotalNotesHit   => "NOTES HIT",
            StatType.TotalScore      => "TOTAL SCORE",
            StatType.AverageAccuracy => "AVG ACCURACY",
            StatType.SongsPlayed     => "SONGS PLAYED",
            _                        => "STAT",
        };

        private Color GetStatColor(StatType type) => type switch
        {
            StatType.FullCombos      => ColorAccentOrange,
            StatType.HighScore       => ColorGoldHyper,
            StatType.HighAccuracy    => ColorAccentGreen,
            StatType.FiveStarCount   => ColorGoldHyper,
            StatType.LongestStreak   => ColorAccentRed,
            StatType.TotalNotesHit   => ColorAccentBlue,
            StatType.TotalScore      => ColorGoldMedium,
            StatType.AverageAccuracy => ColorAccentGreen,
            StatType.SongsPlayed     => ColorAccentPurple,
            _                        => ColorTextBody,
        };

        // ─── UI Helpers ────────────────────────────────────────────────

        /// <summary>
        /// Creates a TextMeshProUGUI with auto-sizing enabled to prevent
        /// vertical text wrapping. Title-safe for long campaign names.
        /// Max font size capped at 36 to guarantee single-line rendering.
        /// </summary>
        private static GameObject MakeAutoSizeTitle(Transform parent, string text,
            Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject("TitleText", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14;
            tmp.fontSizeMax = 36;  // Capped at 36 to stay on one line
            tmp.margin = new Vector4(20, 0, 20, 0);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Truncate;
            return go;
        }

        private static GameObject MakeText(Transform parent, string text, float fontSize,
            Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            return go;
        }

        private GameObject MakeSectionHeader(Transform parent, string label)
        {
            var go = MakeText(parent, label,
                SectionHeaderFontPx, ColorAccentBlue, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = SectionHeaderHeight;
            le.flexibleHeight = 0f;
            go.GetComponent<TextMeshProUGUI>().characterSpacing = 2f;
            return go;
        }

        private void AddUnderline(Transform parent)
        {
            var go = new GameObject("Underline", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = DividerThickness;
            le.flexibleHeight = 0f;
            go.GetComponent<Image>().color = ColorAccentBlue * new Color(1, 1, 1, 0.4f);
            go.GetComponent<Image>().raycastTarget = false;
        }

        /// <summary>
        /// Creates a luminous dot icon with LOCKED size and 1:1 aspect ratio.
        /// LayoutElement with preferredWidth/Height prevents stretching.
        /// </summary>
        private static GameObject CreateLuminousDot(Transform parent, Color color, float size)
        {
            var go = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.flexibleWidth = 0f;

            var arf = go.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            arf.aspectRatio = 1f;

            var img = go.GetComponent<Image>();
            img.sprite = GetDotSprite();
            img.color = color;
            img.raycastTarget = false;

            return go;
        }

        /// <summary>
        /// Locks an existing icon GameObject to a fixed size with 1:1 aspect ratio.
        /// Adds LayoutElement guard if not already present.
        /// </summary>
        private static void LockIconSize(GameObject iconGo, float size)
        {
            var le = iconGo.AddComponent<LayoutElement>();
            le.preferredWidth = size;
            le.preferredHeight = size;
            le.flexibleWidth = 0f;

            // IconLibrary.CreateContainer() already adds AspectRatioFitter,
            // so only add if not present to avoid "already added" exception
            if (iconGo.GetComponent<AspectRatioFitter>() == null)
            {
                var arf = iconGo.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
                arf.aspectRatio = 1f;
            }
        }

        private static void AddBorderLine(Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, float thicknessPx, Color color)
        {
            var go = new GameObject("Line", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, thicknessPx);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        // ─── Procedural Dot Sprite ────────────────────────────────────
        private static Sprite _dotSprite;

        private static Sprite GetDotSprite()
        {
            if (_dotSprite != null) return _dotSprite;

            int res = 32;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var pixels = new Color[res * res];
            float center = res / 2f;
            float radius = center - 1f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    pixels[y * res + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _dotSprite = Sprite.Create(tex, new Rect(0, 0, res, res),
                new Vector2(0.5f, 0.5f), 100f);
            return _dotSprite;
        }

        // ─── Animations ────────────────────────────────────────────────
        private void StartPanelAnimations()
        {
            if (_activeAnim != null) StopCoroutine(_activeAnim);
            if (_borderPulseAnim != null) StopCoroutine(_borderPulseAnim);
            _activeAnim = StartCoroutine(AnimatePanelIn());
        }

        /// <summary>
        /// Panel fade-in followed by elastic overshoot card reveal.
        /// Each card scales from scaleX=0 to scaleX=1 with an elastic
        /// bounce that overshoots to ~1.08 before settling at 1.0.
        /// Cards are staggered by 0.06s each.
        /// </summary>
        private IEnumerator AnimatePanelIn()
        {
            _panelCanvasGroup.alpha = 0f;
            float elapsed = 0f;
            float dur = 0.2f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                _panelCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / dur);
                yield return null;
            }
            _panelCanvasGroup.alpha = 1f;

            if (_contentRoot != null)
            {
                var cards = new List<Transform>();
                FindCardsRecursive(_contentRoot, cards);
                Debug.Log($"[TrophyCase] Animating {cards.Count} cards with elastic overshoot");

                for (int i = 0; i < cards.Count; i++)
                {
                    var card = cards[i];
                    if (i > 0)
                        yield return new WaitForSecondsRealtime(0.06f);

                    elapsed = 0f;
                    dur = 0.25f;
                    while (elapsed < dur)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        float t = Mathf.Clamp01(elapsed / dur);
                        float scaleX = ElasticOvershoot(t);
                        card.localScale = new Vector3(scaleX, 1f, 1f);
                        yield return null;
                    }
                    card.localScale = Vector3.one;
                }
            }

            _borderPulseAnim = StartCoroutine(BorderPulse());
        }

        /// <summary>
        /// Elastic overshoot function.
        /// f(0)=0, f(1)=1, overshoots to ~1.08 around t=0.75.
        /// Uses the standard elastic-out formula: 2^(-10t) * sin((t-s)*2pi/p) + 1
        /// </summary>
        private static float ElasticOvershoot(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            const float p = 0.3f;  // period
            float s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        private IEnumerator BorderPulse()
        {
            while (_statsPanel != null && _statsPanel.activeInHierarchy)
            {
                float t = Mathf.PingPong(Time.unscaledTime * 0.35f, 1f);
                var panelImg = _statsPanel.GetComponent<Image>();
                if (panelImg != null)
                {
                    Color c = panelImg.color;
                    c.a = Mathf.Lerp(0.94f, 0.99f, t);
                    panelImg.color = c;
                }
                yield return null;
            }
        }

        public void HidePanel()
        {
            if (_activeAnim != null) StopCoroutine(_activeAnim);
            if (_borderPulseAnim != null) StopCoroutine(_borderPulseAnim);
            if (_statsPanel != null)
                StartCoroutine(FadeOutPanel());
            _lastSelectedCareer = null;
        }

        private IEnumerator FadeOutPanel()
        {
            float elapsed = 0f;
            float dur = 0.18f;
            while (elapsed < dur && _panelCanvasGroup != null && _statsPanel != null)
            {
                elapsed += Time.unscaledDeltaTime;
                _panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / dur);
                yield return null;
            }
            if (_statsPanel != null)
                _statsPanel.SetActive(false);
        }

        private static void FindCardsRecursive(Transform parent, List<Transform> results)
        {
            foreach (Transform child in parent)
            {
                if (child.name.StartsWith("Card_"))
                    results.Add(child);
                FindCardsRecursive(child, results);
            }
        }

        // ─── Diagnostics ───────────────────────────────────────────────
        private void DumpPanelDiagnostics()
        {
            if (_statsPanel == null) return;
            var rt = _statsPanel.GetComponent<RectTransform>();
            Debug.Log($"[TrophyCase] DIAGNOSTICS — " +
                $"panelSize={rt.sizeDelta}, " +
                $"screen={Screen.width}x{Screen.height}, " +
                $"panelW={_panelWidthPx:F0}, panelH={_panelHeightPx:F0}, " +
                $"leftColW={_panelWidthPx * LeftColFraction:F0}, " +
                $"stats={_currentStats?.CareerName ?? "null"}, " +
                $"profiles={_currentStats?.ProfileStats.Count ?? 0}");

            if (_contentRoot != null)
            {
                int cardCount = 0;
                FindCardsRecursive(_contentRoot, new List<Transform>());
                foreach (Transform c in _contentRoot)
                {
                    if (c.name.StartsWith("Card_")) cardCount++;
                }
                Debug.Log($"[TrophyCase] Content children: {_contentRoot.childCount}, cards: {cardCount}");
            }
        }
    }
}
