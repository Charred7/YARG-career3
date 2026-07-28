// ============================================================
//  CampaignStatsView.cs
//  YARG — Campaign Stats Screen  (AAA Milestone / End-of-Campaign)
//
//  DEPRECATED: Superseded by the prefab-based Campaign Trophy menu
//  (CampaignTrophyMenu + CampaignTrophyView). Retained for CareersViewClassic
//  overlay usage until that path is removed.
//
//  Responsibilities:
//    • Owns the overlay Canvas and all UI GameObjects for the stats screen.
//    • Reads a CampaignStatsData snapshot and builds the full layout from it.
//    • Uses ONLY the provided sprite assets — no procedural VertexHelper drawing.
//    • Scales cleanly from 1080p to 4K via CanvasScaler (ScaleWithScreenSize).
//    • Exposes Populate(data) + Show() / Hide() as the public API.
//
//  Architecture guardrails (same as CampaignStatsDisplay):
//    • Every icon Image gets a LayoutElement with preferredWidth/Height + flexibleWidth=0.
//    • All LayoutGroups have childForceExpandWidth = false.
//    • Stat value columns are fixed-width (StatValueColWidth) and right-aligned.
//    • No hardcoded player counts — driven entirely by CampaignStatsData.Players.
//
//  Namespace: YARG.Menu.Career.Motivation
// ============================================================

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Main view MonoBehaviour for the Campaign Stats / Milestone screen.
    ///
    /// Attach to any persistent GameObject (e.g. the CareerView host).
    /// Assign all sprite references in the Inspector before entering Play mode.
    /// Call <see cref="Populate"/> then <see cref="Show"/> to display the screen.
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class CampaignStatsView : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        //  INSPECTOR — Sprite Assets
        // ─────────────────────────────────────────────────────────────────────

        [Header("── Badge Assets ──────────────────────────────")]

        [Tooltip("badge_vinyl.png — the large golden completion record badge.")]
        [SerializeField] private Sprite _badgeVinyl;

        [Tooltip("All 12 sub-sprites from badge_crest_diamond (sliced 4x3 grid). " +
                 "Assign in order: badge_crest_diamond_0 through badge_crest_diamond_11.")]
        [SerializeField] private Sprite[] _diamondSubSprites = new Sprite[12];

        [Header("── Aura / Particle Overlays ───────────────────")]

        [Tooltip("player1_flames.png — flame aura overlay for orange-team players.")]
        [SerializeField] private Sprite _flamesAura;

        [Tooltip("player3-lightning.png — lightning aura overlay for blue-team players.")]
        [SerializeField] private Sprite _lightningAura;

        [Header("── Star Meter Assets ──────────────────────────")]

        [Tooltip("Star_white.png — white star silhouette, tinted at runtime.")]
        [SerializeField] private Sprite _starWhite;

        [Tooltip("StarProgressEmpty.png — empty star progress ring background.")]
        [SerializeField] private Sprite _starProgressEmpty;

        [Tooltip("StarProgressFill.png — fill layer for the star progress bar.")]
        [SerializeField] private Sprite _starProgressFill;

        [Tooltip("StarProgressGold.png — fully-earned gold star state.")]
        [SerializeField] private Sprite _starProgressGold;

        [Header("── Stat Row Icons ──────────────────────────────")]

        [Tooltip("trophy.png — gold trophy icon for score/streak stat rows.")]
        [SerializeField] private Sprite _trophyIcon;

        [Tooltip("Sub-sprite from CreditSprites containing the double music note icon.")]
        [SerializeField] private Sprite _musicNoteIcon;

        [Header("── Layout Tuning ─────────────────────────────")]

        [Tooltip("Canvas sort order. Keep above the career menu (default 200).")]
        [SerializeField] private int _canvasSortOrder = 200;

        [Tooltip("Fraction of screen height the main panel occupies (0-1).")]
        [SerializeField] [Range(0.5f, 0.95f)] private float _panelHeightFraction = 0.72f;

        [Tooltip("Fraction of screen width the main panel occupies (0-1).")]
        [SerializeField] [Range(0.7f, 1.0f)]  private float _panelWidthFraction  = 0.95f;

        [Tooltip("Fraction of panel width allocated to the left 'Band Highlights' column.")]
        [SerializeField] [Range(0.2f, 0.50f)] private float _leftColFraction     = 0.38f;

        [Tooltip("Maximum number of player cards shown in the right panel.")]
        [SerializeField] [Range(1, 4)]         private int   _maxPlayerCards      = 4;

        // ─────────────────────────────────────────────────────────────────────
        //  PRIVATE — Color Palette  (matches CampaignStatsDisplay)
        // ─────────────────────────────────────────────────────────────────────

        // Backgrounds
        private static readonly Color ColBgDark    = new Color(0.015f, 0.018f, 0.030f, 0.98f);
        private static readonly Color ColBgGlass   = new Color(0.050f, 0.070f, 0.120f, 0.70f);
        private static readonly Color ColBgCard    = new Color(0.060f, 0.085f, 0.150f, 0.92f);
        private static readonly Color ColBgHeader  = new Color(0.025f, 0.030f, 0.060f, 1.00f);
        private static readonly Color ColBgLeft    = new Color(0.030f, 0.040f, 0.070f, 0.40f);
        private static readonly Color ColBgRight   = new Color(0.060f, 0.080f, 0.140f, 0.35f);

        // Gold
        private static readonly Color ColGoldHyper   = new Color(1.000f, 0.950f, 0.600f, 1.00f);
        private static readonly Color ColGoldPrimary = new Color(1.000f, 0.780f, 0.000f, 1.00f);
        private static readonly Color ColGoldMedium  = new Color(0.750f, 0.550f, 0.100f, 1.00f);
        private static readonly Color ColGoldDark    = new Color(0.500f, 0.350f, 0.040f, 1.00f);

        // Accents
        private static readonly Color ColAccentBlue   = new Color(0.310f, 0.760f, 0.970f, 1.00f);
        private static readonly Color ColAccentOrange = new Color(1.000f, 0.500f, 0.100f, 1.00f);

        // Text
        private static readonly Color ColTextLabel = new Color(0.60f, 0.65f, 0.75f, 1.00f);
        private static readonly Color ColTextMuted = new Color(0.50f, 0.52f, 0.60f, 1.00f);

        // Divider
        private static readonly Color ColDivider = new Color(0.180f, 0.160f, 0.070f, 0.55f);

        // Instrument colors (index 0-3: Guitar, Bass, Drums, Vocals)
        private static readonly Color[] ColInstrument =
        {
            IconLibrary.ColorGtrOrange,   // 0 Guitar  — orange
            IconLibrary.ColorBassPurple,  // 1 Bass    — purple
            IconLibrary.ColorDrumsBlue,   // 2 Drums   — blue
            IconLibrary.ColorVocalsGreen, // 3 Vocals  — green
        };

        // Team flavor tints for star meter
        private static readonly Color ColStarOrangeTeam = new Color(1.00f, 0.72f, 0.10f, 1f);
        private static readonly Color ColStarBlueTeam   = new Color(0.40f, 0.80f, 1.00f, 1f);

        // ─────────────────────────────────────────────────────────────────────
        //  PRIVATE — Layout Constants
        // ─────────────────────────────────────────────────────────────────────

        private const float PadOuter           = 14f;
        private const float PadInner           = 10f;
        private const float CardGapPx          = 10f;
        private const float DividerThickPx     = 1.5f;
        private const float TitleBarHeightPx   = 72f;
        private const float SectionLabelHPx    = 38f;
        private const float StatRowHPx         = 40f;
        private const float StatValueColWidth   = 140f;
        private const float IconSizePx         = 30f;
        private const float VinylBadgeSizePx   = 350f;
        private const float DiamondBadgeSizePx = 140f;
        private const float StarSizePx         = 26f;
        private const float AuraSizeMultiplier = 1.55f;

        // ─────────────────────────────────────────────────────────────────────
        //  PRIVATE — Runtime State
        // ─────────────────────────────────────────────────────────────────────

        private Canvas      _canvas;
        private GameObject  _panel;
        private CanvasGroup _panelCg;
        private Transform   _contentRoot;

        private float _panelW;
        private float _panelH;

        private CampaignStatsData _data;

        private Coroutine _showAnim;
        private Coroutine _hideAnim;

        // Tracks card GameObjects for the staggered reveal animation
        private readonly List<Transform> _cardTransforms = new List<Transform>();

        // ─────────────────────────────────────────────────────────────────────
        //  LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_badgeVinyl == null)
                Debug.LogWarning("[CampaignStatsView] _badgeVinyl sprite not assigned in Inspector.");
            if (_trophyIcon == null)
                Debug.LogWarning("[CampaignStatsView] _trophyIcon sprite not assigned in Inspector.");
            if (_diamondSubSprites == null || _diamondSubSprites.Length < 12)
                Debug.LogWarning("[CampaignStatsView] _diamondSubSprites array should have 12 entries " +
                                 "(badge_crest_diamond_0 through badge_crest_diamond_11).");
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private void OnDisable()
        {
            // When this component is disabled (e.g. parent CareerView deactivated),
            // immediately deactivate the panel. The Hide() method uses a coroutine
            // (AnimateOut) which is killed when the MonoBehaviour is disabled, so
            // the fade-out would never complete and _panel.SetActive(false) would
            // never execute — leaving the panel stuck visible on screen.
            if (_panel != null && _panel.activeSelf)
            {
                _panel.SetActive(false);
            }

            if (_panelCg != null)
            {
                _panelCg.alpha = 1f;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PUBLIC API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Returns true if the stats panel is currently visible.</summary>
        public bool IsVisible => _panel != null && _panel.activeSelf;

        /// <summary>
        /// Stores the data snapshot and rebuilds all UI content.
        /// Must be called before <see cref="Show"/>.
        /// </summary>
        public void Populate(CampaignStatsData data)
        {
            if (data == null)
            {
                Debug.LogError("[CampaignStatsView] Populate() called with null data.");
                return;
            }

            _data = data;
            EnsureCanvas();
            EnsurePanel();
            RebuildContent();
        }

        /// <summary>
        /// Fades the panel in with an elastic card-reveal animation.
        /// <see cref="Populate"/> must be called first.
        /// </summary>
        public void Show()
        {
            if (_panel == null)
            {
                Debug.LogWarning("[CampaignStatsView] Show() called before Populate().");
                return;
            }

            if (_hideAnim != null) { StopCoroutine(_hideAnim); _hideAnim = null; }
            if (_showAnim != null) { StopCoroutine(_showAnim); }

            _panel.SetActive(true);
            _showAnim = StartCoroutine(AnimateIn());
        }

        /// <summary>Fades the panel out and deactivates it.</summary>
        public void Hide()
        {
            if (_panel == null || !_panel.activeSelf) return;

            if (_showAnim != null) { StopCoroutine(_showAnim); _showAnim = null; }
            if (_hideAnim != null) { StopCoroutine(_hideAnim); }

            _hideAnim = StartCoroutine(AnimateOut());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CANVAS / PANEL BOOTSTRAP
        // ─────────────────────────────────────────────────────────────────────

        private void EnsureCanvas()
        {
            if (_canvas != null) return;

            var go = new GameObject("CampaignStatsCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            DontDestroyOnLoad(go);

            _canvas = go.GetComponent<Canvas>();
            _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = _canvasSortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;
        }

        private void EnsurePanel()
        {
            // CRITICAL: Use the canvas RectTransform rect (in reference-resolution units)
            // instead of Screen.width/height (raw pixels). The CanvasScaler remaps the canvas
            // to reference resolution (1920x1080), so Screen.width on a 4K display (3840)
            // would make the panel 2x too large. Using canvasRect ensures consistent sizing
            // across 1080p, 1440p, and 4K.
            var canvasRect = _canvas.GetComponent<RectTransform>().rect;
            _panelW = canvasRect.width  * _panelWidthFraction;
            _panelH = canvasRect.height * _panelHeightFraction;

            // If the panel already exists, update its sizeDelta to match the
            // recalculated dimensions. This is essential because the panel is
            // created once and reused — without this, a panel created with old
            // (broken) dimensions would persist forever.
            if (_panel != null)
            {
                var existingRt = _panel.GetComponent<RectTransform>();
                existingRt.sizeDelta = new Vector2(_panelW, _panelH);
                return;
            }

            // ── Outer dark panel ──────────────────────────────────────────────
            _panel = new GameObject("CampaignStatsPanel",
                typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            _panel.transform.SetParent(_canvas.transform, false);

            // Center the panel horizontally and vertically on screen.
            // The render shows the panel floating centered, not pinned to the bottom.
            var rt = _panel.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0.5f, 0.5f);
            rt.anchorMax        = new Vector2(0.5f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(_panelW, _panelH);
            rt.anchoredPosition = Vector2.zero;

            // Apply a subtle vertical gradient for depth (darker at edges, lighter center)
            var panelBgImg = _panel.GetComponent<Image>();
            panelBgImg.sprite         = GetOrCreatePanelGradientSprite();
            panelBgImg.type           = Image.Type.Sliced;
            panelBgImg.color          = Color.white; // sprite carries the gradient
            panelBgImg.raycastTarget  = false;

            _panelCg       = _panel.GetComponent<CanvasGroup>();
            _panelCg.alpha = 0f;

            // ── Frosted glass inner backing ───────────────────────────────────
            var glass = new GameObject("GlassBacking", typeof(RectTransform), typeof(Image));
            glass.transform.SetParent(_panel.transform, false);
            var glassRt = glass.GetComponent<RectTransform>();
            glassRt.anchorMin = new Vector2(0.004f, 0.004f);
            glassRt.anchorMax = new Vector2(0.996f, 0.996f);
            glassRt.offsetMin = Vector2.zero;
            glassRt.offsetMax = Vector2.zero;
            glass.GetComponent<Image>().color         = ColBgGlass;
            glass.GetComponent<Image>().raycastTarget = false;

            // ── Top gold accent border ────────────────────────────────────────
            AddBorderLine(_panel.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), 3f, ColGoldPrimary);

            // ── Content root (VLG: title bar stacked above body) ──────────────
            var contentGo = new GameObject("Content",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            contentGo.transform.SetParent(_panel.transform, false);

            var crt = contentGo.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(PadOuter, PadOuter);
            crt.offsetMax = new Vector2(-PadOuter, -PadOuter);

            var cvlg = contentGo.GetComponent<VerticalLayoutGroup>();
            cvlg.childAlignment         = TextAnchor.UpperLeft;
            cvlg.spacing                = 0f;
            cvlg.childControlHeight     = true;
            cvlg.childControlWidth      = true;
            cvlg.childForceExpandHeight = false;
            cvlg.childForceExpandWidth  = false;
            cvlg.padding                = new RectOffset(0, 0, 0, 0);

            _contentRoot = contentGo.transform;
            _panel.SetActive(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  CONTENT BUILDER — TOP LEVEL
        // ─────────────────────────────────────────────────────────────────────

        private void RebuildContent()
        {
            _cardTransforms.Clear();

            foreach (Transform child in _contentRoot)
                Destroy(child.gameObject);

            if (_data == null) return;

            BuildTitleBar();
            BuildBody();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TITLE BAR
        // ─────────────────────────────────────────────────────────────────────

        private void BuildTitleBar()
        {
            var bar = new GameObject("TitleBar",
                typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            bar.transform.SetParent(_contentRoot, false);

            var le = bar.AddComponent<LayoutElement>();
            le.preferredHeight = TitleBarHeightPx;
            le.flexibleHeight  = 0f;

            bar.GetComponent<Image>().color         = ColBgHeader;
            bar.GetComponent<Image>().raycastTarget = false;

            var hlg = bar.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.spacing                = 0f;
            hlg.childControlHeight     = true;
            hlg.childControlWidth      = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;
            hlg.padding                = new RectOffset((int)PadInner, (int)PadInner, 0, 0);

            // Title text — auto-sizing, max 38pt, single line, gold shine
            var titleGo = MakeText(bar.transform, _data.ScreenTitle,
                38f, ColGoldHyper, FontStyles.Bold, TextAlignmentOptions.Center);
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.flexibleWidth = 1f;

            var tmp = titleGo.GetComponent<TextMeshProUGUI>();
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin      = 16f;
            tmp.fontSizeMax      = 38f;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode     = TextOverflowModes.Truncate;
            tmp.characterSpacing = 2f;

            var shine = titleGo.AddComponent<GoldShineTextEffect>();
            shine.ShineInterval = 4.0f;
            shine.ShineDuration = 1.8f;
            shine.SetBaseColor(ColGoldHyper);
            shine.SetShineColor(new Color(1f, 0.99f, 0.80f));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BODY  (left column + divider + right column)
        // ─────────────────────────────────────────────────────────────────────

        private void BuildBody()
        {
            var body = new GameObject("Body",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            body.transform.SetParent(_contentRoot, false);

            var bodyLe = body.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1f;

            var hlg = body.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.UpperLeft;
            hlg.spacing                = 0f;
            hlg.childControlHeight     = true;
            hlg.childControlWidth      = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;
            hlg.padding                = new RectOffset(0, 0, 0, 0);

            BuildLeftPanel(body.transform);
            AddVerticalDivider(body.transform);
            BuildRightPanel(body.transform);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LEFT PANEL — Band Highlights
        // ─────────────────────────────────────────────────────────────────────

        private void BuildLeftPanel(Transform body)
        {
            float leftW = _panelW * _leftColFraction;

            var col = new GameObject("LeftPanel",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
            col.transform.SetParent(body, false);

            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = leftW;
            le.flexibleWidth  = 0f;

            col.GetComponent<Image>().color         = ColBgLeft;
            col.GetComponent<Image>().raycastTarget = false;

            // Cyan inner border for premium panel depth
            var leftOutline = col.AddComponent<Outline>();
            leftOutline.effectColor    = new Color(0.31f, 0.76f, 0.97f, 0.35f);
            leftOutline.effectDistance = new Vector2(1.5f, 0f);
            leftOutline.useGraphicAlpha = true;

            var vlg = col.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperLeft;
            vlg.spacing                = 6f;
            vlg.childControlHeight     = true;
            vlg.childControlWidth      = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = false;
            vlg.padding                = new RectOffset((int)PadInner, (int)PadInner,
                                                         (int)PadInner, (int)PadInner);

            BuildSectionLabel(col.transform, "Band Highlights");
            AddUnderline(col.transform, ColAccentBlue);
            BuildBandHighlightsCard(col.transform);
        }

        /// <summary>
        /// Builds the frosted card containing the badge_vinyl image, completion
        /// banner, milestone label, and band-wide stat rows.
        /// </summary>
        private void BuildBandHighlightsCard(Transform parent)
        {
            var card = new GameObject("BandHighlightsCard",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
            card.transform.SetParent(parent, false);

            var le = card.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;

            card.GetComponent<Image>().color         = ColBgCard;
            card.GetComponent<Image>().raycastTarget = false;

            var vlg = card.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperCenter;
            vlg.spacing                = 6f;
            vlg.childControlHeight     = true;
            vlg.childControlWidth      = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = false;
            vlg.padding                = new RectOffset((int)PadInner, (int)PadInner,
                                                         (int)PadInner, (int)PadInner);

            var milestone = _data?.Milestone;
            if (milestone == null) return;

            // ── "GOLDEN VINYL" title ──────────────────────────────────────────
            var titleGo = MakeText(card.transform, milestone.BadgeTitle,
                22f, ColGoldHyper, FontStyles.Bold, TextAlignmentOptions.Center);
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 32f;
            titleLe.flexibleHeight  = 0f;
            titleGo.GetComponent<TextMeshProUGUI>().characterSpacing = 2f;

            var titleShine = titleGo.AddComponent<GoldShineTextEffect>();
            titleShine.ShineInterval = 5.0f;
            titleShine.ShineDuration = 1.4f;
            titleShine.SetBaseColor(ColGoldHyper);
            titleShine.SetShineColor(new Color(1f, 0.99f, 0.85f));

            // ── badge_vinyl image (any completed campaign) ────────────────────
            if (_badgeVinyl != null)
            {
                // Wrapper container so the glow can sit behind the vinyl
                var vinylWrapper = new GameObject("VinylWrapper",
                    typeof(RectTransform));
                vinylWrapper.transform.SetParent(card.transform, false);

                var wrapLe = vinylWrapper.AddComponent<LayoutElement>();
                wrapLe.preferredWidth  = VinylBadgeSizePx;
                wrapLe.preferredHeight = VinylBadgeSizePx;
                wrapLe.flexibleWidth   = 0f;
                wrapLe.flexibleHeight  = 0f;

                // ── Radial glow behind the vinyl ──────────────────────────────
                var glowGo = new GameObject("VinylGlow",
                    typeof(RectTransform), typeof(Image));
                glowGo.transform.SetParent(vinylWrapper.transform, false);

                float glowSize = VinylBadgeSizePx * 1.35f;
                var glowRt = glowGo.GetComponent<RectTransform>();
                glowRt.anchorMin        = new Vector2(0.5f, 0.5f);
                glowRt.anchorMax        = new Vector2(0.5f, 0.5f);
                glowRt.pivot            = new Vector2(0.5f, 0.5f);
                glowRt.sizeDelta        = new Vector2(glowSize, glowSize);
                glowRt.anchoredPosition = Vector2.zero;

                var glowImg = glowGo.GetComponent<Image>();
                glowImg.sprite         = GetOrCreateRadialGlowSprite();
                glowImg.preserveAspect = true;
                glowImg.raycastTarget  = false;

                // ── Vinyl badge on top ────────────────────────────────────────
                var vinylGo = new GameObject("VinylBadge",
                    typeof(RectTransform), typeof(Image));
                vinylGo.transform.SetParent(vinylWrapper.transform, false);

                var vinylRt = vinylGo.GetComponent<RectTransform>();
                vinylRt.anchorMin        = new Vector2(0.5f, 0.5f);
                vinylRt.anchorMax        = new Vector2(0.5f, 0.5f);
                vinylRt.pivot            = new Vector2(0.5f, 0.5f);
                vinylRt.sizeDelta        = new Vector2(VinylBadgeSizePx, VinylBadgeSizePx);
                vinylRt.anchoredPosition = Vector2.zero;

                var vinylImg = vinylGo.GetComponent<Image>();
                vinylImg.sprite         = _badgeVinyl;
                vinylImg.preserveAspect = true;
                vinylImg.raycastTarget  = false;
            }

            // ── Completion banner ribbon ──────────────────────────────────────
            BuildCompletionBanner(card.transform, milestone.CompletionBannerText);

            // ── Sub-label (e.g. "GOLDEN VINYL") ──────────────────────────────
            if (!string.IsNullOrEmpty(milestone.SubLabel))
            {
                var subGo = MakeText(card.transform, milestone.SubLabel,
                    16f, ColGoldPrimary, FontStyles.Bold, TextAlignmentOptions.Center);
                var subLe = subGo.AddComponent<LayoutElement>();
                subLe.preferredHeight = 24f;
                subLe.flexibleHeight  = 0f;
                subGo.GetComponent<TextMeshProUGUI>().characterSpacing = 1.5f;
            }

            // ── Thin gold divider ─────────────────────────────────────────────
            AddUnderline(card.transform, ColGoldDark);

            // ── Band stat rows ────────────────────────────────────────────────
            if (milestone.BandStatRows != null && milestone.BandStatRows.Count > 0)
            {
                var statList = new GameObject("BandStatList",
                    typeof(RectTransform), typeof(VerticalLayoutGroup));
                statList.transform.SetParent(card.transform, false);

                var slLe = statList.AddComponent<LayoutElement>();
                slLe.flexibleHeight = 1f;

                var slVlg = statList.GetComponent<VerticalLayoutGroup>();
                slVlg.childAlignment         = TextAnchor.UpperLeft;
                slVlg.spacing                = 4f;
                slVlg.childControlHeight     = true;
                slVlg.childControlWidth      = true;
                slVlg.childForceExpandHeight = false;
                slVlg.childForceExpandWidth  = false;
                slVlg.padding                = new RectOffset(0, 0, 4, 0);

                foreach (var row in milestone.BandStatRows)
                    BuildStatRow(statList.transform, row, ColGoldMedium);
            }
        }

        /// <summary>
        /// Builds the "100% COMPLETE" banner ribbon — dark background with
        /// gold border lines top and bottom, centered bold text.
        /// </summary>
        private void BuildCompletionBanner(Transform parent, string text)
        {
            var banner = new GameObject("CompletionBanner",
                typeof(RectTransform), typeof(Image));
            banner.transform.SetParent(parent, false);

            var le = banner.AddComponent<LayoutElement>();
            le.preferredHeight = 36f;
            le.flexibleHeight  = 0f;

            banner.GetComponent<Image>().color         = new Color(0.04f, 0.06f, 0.12f, 0.95f);
            banner.GetComponent<Image>().raycastTarget = false;

            // Top gold border
            AddBorderLine(banner.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), 2f, ColGoldPrimary);
            // Bottom gold border
            AddBorderLine(banner.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), 2f, ColGoldPrimary);

            // Banner text — stretched to fill the banner
            var textGo = new GameObject("BannerText", typeof(RectTransform));
            textGo.transform.SetParent(banner.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8f, 2f);
            textRt.offsetMax = new Vector2(-8f, -2f);

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = 18f;
            tmp.color            = ColGoldPrimary;
            tmp.fontStyle        = FontStyles.Bold;
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.raycastTarget    = false;
            tmp.characterSpacing = 1.5f;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  RIGHT PANEL — Player Legends
        // ─────────────────────────────────────────────────────────────────────

        private void BuildRightPanel(Transform body)
        {
            var col = new GameObject("RightPanel",
                typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(Image));
            col.transform.SetParent(body, false);

            var le = col.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            col.GetComponent<Image>().color         = ColBgRight;
            col.GetComponent<Image>().raycastTarget = false;

            // Subtle warm inner border for right panel depth
            var rightOutline = col.AddComponent<Outline>();
            rightOutline.effectColor    = new Color(0.75f, 0.55f, 0.10f, 0.25f);
            rightOutline.effectDistance = new Vector2(1.5f, 0f);
            rightOutline.useGraphicAlpha = true;

            var vlg = col.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment         = TextAnchor.UpperLeft;
            vlg.spacing                = 6f;
            vlg.childControlHeight     = true;
            vlg.childControlWidth      = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth  = false;
            vlg.padding                = new RectOffset((int)PadInner, (int)PadInner,
                                                         (int)PadInner, (int)PadInner);

            BuildSectionLabel(col.transform, "Player Legends");
            AddUnderline(col.transform, ColAccentBlue);
            BuildPlayerCardsArea(col.transform);
        }

        /// <summary>
        /// Builds the HorizontalLayoutGroup that holds all player cards.
        /// Card width is calculated strictly: (available - gaps) / count.
        /// </summary>
        private void BuildPlayerCardsArea(Transform parent)
        {
            var area = new GameObject("CardsArea",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            area.transform.SetParent(parent, false);

            var areaLe = area.AddComponent<LayoutElement>();
            areaLe.flexibleHeight = 1f;

            var hlg = area.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.UpperLeft;
            hlg.spacing                = CardGapPx;
            hlg.childControlHeight     = true;
            hlg.childControlWidth      = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;  // CRITICAL: cards must not sprawl
            hlg.padding                = new RectOffset(0, 0, 0, 0);

            if (_data?.Players == null || _data.Players.Count == 0)
            {
                var emptyGo = MakeText(area.transform,
                    "No player stats yet.\nPlay through songs to earn your legends!",
                    16f, ColTextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
                var emptyLe = emptyGo.AddComponent<LayoutElement>();
                emptyLe.flexibleWidth = 1f;
                return;
            }

            int cardCount = Mathf.Min(_data.Players.Count, _maxPlayerCards);

            // Cards use flexibleWidth=1 so they expand proportionally to fill
            // the available space. No manual pixel math — the HorizontalLayoutGroup
            // handles distribution automatically. This eliminates dead space on the
            // right side that occurred with fixed preferredWidth calculations.
            for (int i = 0; i < cardCount; i++)
                BuildPlayerCard(area.transform, _data.Players[i], i);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PLAYER CARD
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a single player card:
        ///   [Header chip: instrument icon + player label]
        ///   [Star meter: 0-5 Star_white sprites tinted to team color]
        ///   [Aura overlay: player1_flames or player3-lightning, semi-transparent]
        ///   [Diamond badge: badge_crest_diamond sub-sprite + accolade text]
        ///   [Stat rows: trophy/star/note icon + label + value]
        /// </summary>
        private void BuildPlayerCard(Transform parent, PlayerStatsData player,
            int index)
        {
            Color instrColor = ColInstrument[player.InstrumentIndex % ColInstrument.Length];

            // ── Card outer wrapper ────────────────────────────────────────────
            var card = new GameObject($"Card_{player.PlayerName}",
                typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);
            card.transform.localScale = new Vector3(0f, 1f, 1f); // collapsed for reveal anim

            // Use flexibleWidth=1 so cards expand proportionally to fill the
            // CardsArea HorizontalLayoutGroup. No fixed preferredWidth — the
            // layout group distributes space evenly across all cards.
            var cardLe = card.AddComponent<LayoutElement>();
            cardLe.flexibleWidth  = 1f;
            cardLe.minWidth       = 120f;  // prevent cards from collapsing below readability

            card.GetComponent<Image>().color         = ColBgCard;
            card.GetComponent<Image>().raycastTarget = false;

            // Glowing instrument-colored card outline for premium depth
            var cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor    = new Color(instrColor.r, instrColor.g, instrColor.b, 0.55f);
            cardOutline.effectDistance = new Vector2(2f, -2f);
            cardOutline.useGraphicAlpha = true;

            // Instrument-colored left accent border (4px)
            var accent = new GameObject("AccentBorder", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(card.transform, false);
            var accentRt = accent.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.pivot     = new Vector2(0f, 0.5f);
            accentRt.offsetMin = Vector2.zero;
            accentRt.offsetMax = Vector2.zero;
            accentRt.sizeDelta = new Vector2(4f, 0f);
            accent.GetComponent<Image>().color         = instrColor;
            accent.GetComponent<Image>().raycastTarget = false;

            // ── Inner VLG (offset from accent border) ────────────────────────
            var inner = new GameObject("Inner",
                typeof(RectTransform), typeof(VerticalLayoutGroup));
            inner.transform.SetParent(card.transform, false);
            var innerRt = inner.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(10f, 6f);
            innerRt.offsetMax = new Vector2(-6f, -6f);

            var innerVlg = inner.GetComponent<VerticalLayoutGroup>();
            innerVlg.childAlignment         = TextAnchor.UpperCenter;
            innerVlg.spacing                = 4f;
            innerVlg.childControlHeight     = true;
            innerVlg.childControlWidth      = true;
            innerVlg.childForceExpandHeight = false;
            innerVlg.childForceExpandWidth  = false;
            innerVlg.padding                = new RectOffset(0, 0, 0, 0);

            // ── Header chip: instrument icon + player label ───────────────────
            BuildCardHeader(inner.transform, player, instrColor);

            // ── Star meter (0-5 stars above the badge) ────────────────────────
            BuildStarMeter(inner.transform, player, index);

            // ── Aura + Diamond badge (layered via absolute-positioned children) ─
            BuildBadgeWithAura(inner.transform, player, instrColor);

            // ── Stat rows ─────────────────────────────────────────────────────
            if (player.Stats != null && player.Stats.Count > 0)
            {
                foreach (var row in player.Stats)
                    BuildStatRow(inner.transform, row, ColGoldMedium);
            }
            else
            {
                var emptyGo = MakeText(inner.transform, "Play to earn stats!",
                    13f, ColTextMuted, FontStyles.Normal, TextAlignmentOptions.Center);
                var emptyLe = emptyGo.AddComponent<LayoutElement>();
                emptyLe.flexibleHeight = 1f;
            }

            // Register card for staggered reveal animation
            _cardTransforms.Add(card.transform);
        }

        /// <summary>
        /// Builds the colored header chip at the top of a player card.
        /// Contains a small instrument icon (tinted) and the player label text.
        /// </summary>
        private void BuildCardHeader(Transform parent, PlayerStatsData player, Color instrColor)
        {
            var header = new GameObject("CardHeader",
                typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(Image));
            header.transform.SetParent(parent, false);

            var le = header.AddComponent<LayoutElement>();
            le.preferredHeight = StatRowHPx;
            le.flexibleHeight  = 0f;

            // Instrument-colored header background (darkened)
            header.GetComponent<Image>().color =
                new Color(instrColor.r * 0.35f, instrColor.g * 0.35f, instrColor.b * 0.35f, 0.90f);
            header.GetComponent<Image>().raycastTarget = false;

            var hlg = header.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.spacing                = 6f;
            hlg.childControlHeight     = true;
            hlg.childControlWidth      = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;
            hlg.padding                = new RectOffset(6, 6, 0, 0);

            // Instrument icon — uses _musicNoteIcon if assigned, else tinted dot
            if (_musicNoteIcon != null)
            {
                var iconGo = new GameObject("InstrIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(header.transform, false);
                var iconLe = iconGo.AddComponent<LayoutElement>();
                iconLe.preferredWidth  = IconSizePx;
                iconLe.preferredHeight = IconSizePx;
                iconLe.flexibleWidth   = 0f;
                var iconArf = iconGo.AddComponent<AspectRatioFitter>();
                iconArf.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
                iconArf.aspectRatio = 1f;
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite         = _musicNoteIcon;
                iconImg.color          = instrColor;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget  = false;
            }
            else
            {
                // Fallback: tinted circle dot
                BuildIconDot(header.transform, instrColor, IconSizePx);
            }

            // Player label text
            var labelGo = MakeText(header.transform, player.InstrumentLabel,
                13f, Color.white, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.characterSpacing = 0.5f;
            labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
            labelTmp.overflowMode     = TextOverflowModes.Truncate;
        }

        /// <summary>
        /// Builds the star meter row above the diamond badge.
        /// Uses Star_white sprites tinted to the player's team color.
        /// Filled stars use _starProgressGold; empty slots use _starProgressEmpty.
        /// Falls back to tinted Star_white if progress sprites are not assigned.
        /// </summary>
        private void BuildStarMeter(Transform parent, PlayerStatsData player, int playerIndex)
        {
            if (player.MaxStars <= 0) return;

            // Team color: even indices = orange team, odd = blue team
            Color starFillColor  = (playerIndex % 2 == 0) ? ColStarOrangeTeam : ColStarBlueTeam;
            Color starEmptyColor = new Color(starFillColor.r, starFillColor.g, starFillColor.b, 0.25f);

            var meterRow = new GameObject("StarMeter",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            meterRow.transform.SetParent(parent, false);

            var meterLe = meterRow.AddComponent<LayoutElement>();
            meterLe.preferredHeight = StarSizePx + 4f;
            meterLe.flexibleHeight  = 0f;

            var hlg = meterRow.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleCenter;
            hlg.spacing                = 3f;
            hlg.childControlHeight     = true;
            hlg.childControlWidth      = true;
            hlg.childForceExpandHeight = false;
            hlg.childForceExpandWidth  = false;
            hlg.padding                = new RectOffset(0, 0, 2, 2);

            int filled = Mathf.Clamp(player.StarCount, 0, player.MaxStars);

            for (int i = 0; i < player.MaxStars; i++)
            {
                bool isFilled = i < filled;

                // Choose sprite: gold for filled, empty for unfilled
                Sprite starSprite = isFilled
                    ? (_starProgressGold != null ? _starProgressGold : _starWhite)
                    : (_starProgressEmpty != null ? _starProgressEmpty : _starWhite);

                var starGo = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(meterRow.transform, false);

                var starLe = starGo.AddComponent<LayoutElement>();
                starLe.preferredWidth  = StarSizePx;
                starLe.preferredHeight = StarSizePx;
                starLe.flexibleWidth   = 0f;

                var starArf = starGo.AddComponent<AspectRatioFitter>();
                starArf.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
                starArf.aspectRatio = 1f;

                var starImg = starGo.GetComponent<Image>();
                starImg.sprite         = starSprite;
                starImg.color          = isFilled ? starFillColor : starEmptyColor;
                starImg.preserveAspect = true;
                starImg.raycastTarget  = false;
            }
        }

        /// <summary>
        /// Builds the layered badge area: aura overlay behind the diamond badge.
        ///
        /// Layout trick: a fixed-height container holds both the aura Image and
        /// the diamond badge Image as absolute-positioned children (anchored to
        /// fill the container), so the aura bleeds outside the badge bounds
        /// without affecting the VerticalLayoutGroup flow.
        /// </summary>
        private void BuildBadgeWithAura(Transform parent, PlayerStatsData player, Color instrColor)
        {
            // Container — fixed height, acts as the layout slot
            var container = new GameObject("BadgeContainer",
                typeof(RectTransform));
            container.transform.SetParent(parent, false);

            var containerLe = container.AddComponent<LayoutElement>();
            containerLe.preferredHeight = DiamondBadgeSizePx;
            containerLe.flexibleHeight  = 0f;

            var containerRt = container.GetComponent<RectTransform>();
            // Width will be controlled by parent VLG; height is fixed above

            // ── Aura overlay (behind badge, larger) ───────────────────────────
            Sprite auraSprite = player.Aura switch
            {
                PlayerAuraType.Flames    => _flamesAura,
                PlayerAuraType.Lightning => _lightningAura,
                _                        => null,
            };

            if (auraSprite != null)
            {
                float auraSize = DiamondBadgeSizePx * AuraSizeMultiplier;
                float auraOffset = (auraSize - DiamondBadgeSizePx) * 0.5f;

                var auraGo = new GameObject("AuraOverlay", typeof(RectTransform), typeof(Image));
                auraGo.transform.SetParent(container.transform, false);

                var auraRt = auraGo.GetComponent<RectTransform>();
                auraRt.anchorMin        = new Vector2(0.5f, 0.5f);
                auraRt.anchorMax        = new Vector2(0.5f, 0.5f);
                auraRt.pivot            = new Vector2(0.5f, 0.5f);
                auraRt.sizeDelta        = new Vector2(auraSize, auraSize);
                auraRt.anchoredPosition = Vector2.zero;

                var auraImg = auraGo.GetComponent<Image>();
                auraImg.sprite         = auraSprite;
                auraImg.color          = new Color(1f, 1f, 1f, 0.72f);
                auraImg.preserveAspect = true;
                auraImg.raycastTarget  = false;
            }

            // ── Diamond badge (badge_crest_diamond sub-sprite) ────────────────
            int spriteIdx = (int)player.DiamondFrame;
            Sprite diamondSprite = (_diamondSubSprites != null
                                    && spriteIdx < _diamondSubSprites.Length)
                ? _diamondSubSprites[spriteIdx]
                : null;

            var badgeGo = new GameObject("DiamondBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(container.transform, false);

            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.anchorMin        = new Vector2(0.5f, 0.5f);
            badgeRt.anchorMax        = new Vector2(0.5f, 0.5f);
            badgeRt.pivot            = new Vector2(0.5f, 0.5f);
            badgeRt.sizeDelta        = new Vector2(DiamondBadgeSizePx, DiamondBadgeSizePx);
            badgeRt.anchoredPosition = Vector2.zero;

            var badgeImg = badgeGo.GetComponent<Image>();
            if (diamondSprite != null)
            {
                badgeImg.sprite         = diamondSprite;
                badgeImg.color          = Color.white;  // sprite carries its own color
                badgeImg.preserveAspect = true;
            }
            else
            {
                // Fallback: tinted instrument-colored circle when sprite not assigned
                badgeImg.sprite = null;
                badgeImg.color  = new Color(instrColor.r, instrColor.g, instrColor.b, 0.30f);
            }
            badgeImg.raycastTarget = false;

            // ── Accolade title text (centered over the badge) ─────────────────
            if (!string.IsNullOrEmpty(player.AccoladeTitle))
            {
                var textGo = new GameObject("AccoladeText", typeof(RectTransform));
                textGo.transform.SetParent(container.transform, false);

                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin        = new Vector2(0.5f, 0.5f);
                textRt.anchorMax        = new Vector2(0.5f, 0.5f);
                textRt.pivot            = new Vector2(0.5f, 0.5f);
                textRt.sizeDelta        = new Vector2(DiamondBadgeSizePx * 0.72f,
                                                       DiamondBadgeSizePx * 0.55f);
                textRt.anchoredPosition = Vector2.zero;

                var tmp = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text             = player.AccoladeTitle;
                tmp.fontSize         = 16f;
                tmp.color            = Color.white;
                tmp.fontStyle        = FontStyles.Bold;
                tmp.alignment        = TextAlignmentOptions.Center;
                tmp.raycastTarget    = false;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin      = 9f;
                tmp.fontSizeMax      = 18f;
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode     = TextOverflowModes.Truncate;
                tmp.characterSpacing = 1f;

                // Subtle shadow for legibility over the badge sprite
                var shadow = textGo.AddComponent<Shadow>();
                shadow.effectColor    = new Color(0f, 0f, 0f, 0.75f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  STAT ROW  (shared by both left panel and player cards)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a single stat row:
        ///   [icon 30x30] [label flex] [value 140px right-aligned]
        ///
        /// Fixed 40px height. childForceExpandWidth = false prevents text collision.
        /// Icon sprite is chosen from the StatRowIconType enum.
        /// </summary>
        private void BuildStatRow(Transform parent, StatRowData row, Color defaultValueColor)
        {
            var rowGo = new GameObject($"StatRow_{row.Label}",
                typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGo.transform.SetParent(parent, false);

            var rowLe = rowGo.AddComponent<LayoutElement>();
            rowLe.preferredHeight = StatRowHPx;
            rowLe.flexibleHeight  = 0f;

            var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.spacing                = 8f;
            hlg.childControlHeight     = true;
            hlg.childControlWidth      = true;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;  // CRITICAL
            hlg.padding                = new RectOffset(2, 2, 0, 0);

            // ── Icon ──────────────────────────────────────────────────────────
            BuildStatRowIcon(rowGo.transform, row.IconType);

            // ── Label (flexible width) ────────────────────────────────────────
            var labelGo = MakeText(rowGo.transform, row.Label,
                13f, ColTextLabel, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
            var labelLe = labelGo.AddComponent<LayoutElement>();
            labelLe.flexibleWidth = 1f;
            var labelTmp = labelGo.GetComponent<TextMeshProUGUI>();
            labelTmp.characterSpacing = 0.5f;
            labelTmp.textWrappingMode = TextWrappingModes.NoWrap;
            labelTmp.overflowMode     = TextOverflowModes.Truncate;

            // ── Value (fixed 140px, right-aligned) ────────────────────────────
            var valueGo = MakeText(rowGo.transform, row.Value,
                16f, defaultValueColor, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
            var valueLe = valueGo.AddComponent<LayoutElement>();
            valueLe.preferredWidth = StatValueColWidth;
            valueLe.flexibleWidth  = 0f;
            var valueTmp = valueGo.GetComponent<TextMeshProUGUI>();
            valueTmp.textWrappingMode = TextWrappingModes.NoWrap;
            valueTmp.overflowMode     = TextOverflowModes.Truncate;
        }

        /// <summary>
        /// Builds the icon at the start of a stat row.
        /// Uses the actual sprite assets when assigned; falls back to a tinted dot.
        /// </summary>
        private void BuildStatRowIcon(Transform parent, StatRowIconType iconType)
        {
            Sprite sprite = iconType switch
            {
                StatRowIconType.Trophy    => _trophyIcon,
                StatRowIconType.Star      => _starWhite,
                StatRowIconType.MusicNote => _musicNoteIcon,
                _                         => null,
            };

            Color tint = iconType switch
            {
                StatRowIconType.Trophy    => ColGoldPrimary,
                StatRowIconType.Star      => ColGoldHyper,
                StatRowIconType.MusicNote => ColAccentBlue,
                _                         => Color.clear,
            };

            if (iconType == StatRowIconType.None)
            {
                // Spacer to keep alignment consistent
                var spacer = new GameObject("IconSpacer", typeof(RectTransform));
                spacer.transform.SetParent(parent, false);
                var spacerLe = spacer.AddComponent<LayoutElement>();
                spacerLe.preferredWidth  = IconSizePx;
                spacerLe.preferredHeight = IconSizePx;
                spacerLe.flexibleWidth   = 0f;
                return;
            }

            if (sprite != null)
            {
                var iconGo = new GameObject("RowIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(parent, false);

                var iconLe = iconGo.AddComponent<LayoutElement>();
                iconLe.preferredWidth  = IconSizePx;
                iconLe.preferredHeight = IconSizePx;
                iconLe.flexibleWidth   = 0f;

                var iconArf = iconGo.AddComponent<AspectRatioFitter>();
                iconArf.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
                iconArf.aspectRatio = 1f;

                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite         = sprite;
                iconImg.color          = tint;
                iconImg.preserveAspect = true;
                iconImg.raycastTarget  = false;
            }
            else
            {
                // Fallback: tinted dot
                BuildIconDot(parent, tint, IconSizePx);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SHARED UI HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds a section label (e.g. "Band Highlights", "Player Legends").
        /// Fixed height, accent-blue, bold, letter-spaced.
        /// </summary>
        private void BuildSectionLabel(Transform parent, string text)
        {
            var go = MakeText(parent, text,
                20f, ColAccentBlue, FontStyles.Bold, TextAlignmentOptions.TopLeft);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = SectionLabelHPx;
            le.flexibleHeight  = 0f;
            go.GetComponent<TextMeshProUGUI>().characterSpacing = 1.5f;
        }

        /// <summary>
        /// Adds a thin horizontal underline (1.5px) in the given color.
        /// </summary>
        private static void AddUnderline(Transform parent, Color color)
        {
            var go = new GameObject("Underline", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = DividerThickPx;
            le.flexibleHeight  = 0f;
            go.GetComponent<Image>().color         = new Color(color.r, color.g, color.b, 0.45f);
            go.GetComponent<Image>().raycastTarget = false;
        }

        /// <summary>
        /// Adds a thin vertical divider between the left and right panels.
        /// </summary>
        private static void AddVerticalDivider(Transform parent)
        {
            // Glow layer (wider, more transparent) behind the main divider
            var glowGo = new GameObject("VDividerGlow", typeof(RectTransform), typeof(Image));
            glowGo.transform.SetParent(parent, false);
            var glowLe = glowGo.AddComponent<LayoutElement>();
            glowLe.preferredWidth = DividerThickPx * 4f;
            glowLe.flexibleWidth  = 0f;
            glowGo.GetComponent<Image>().color         = new Color(0.75f, 0.55f, 0.10f, 0.18f);
            glowGo.GetComponent<Image>().raycastTarget = false;

            // Main divider line
            var go = new GameObject("VDivider", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = DividerThickPx * 2f;
            le.flexibleWidth  = 0f;
            go.GetComponent<Image>().color         = new Color(0.75f, 0.55f, 0.10f, 0.65f);
            go.GetComponent<Image>().raycastTarget = false;
        }

        /// <summary>
        /// Adds an absolute-positioned border line (top or bottom) to a parent.
        /// anchorMin/anchorMax define which edge; thicknessPx is the height.
        /// </summary>
        private static void AddBorderLine(Transform parent,
            Vector2 anchorMin, Vector2 anchorMax, float thicknessPx, Color color)
        {
            var go = new GameObject("BorderLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = new Vector2(0f, thicknessPx);
            go.GetComponent<Image>().color         = color;
            go.GetComponent<Image>().raycastTarget = false;
        }

        /// <summary>
        /// Creates a tinted circle dot icon with locked size and 1:1 aspect ratio.
        /// Used as a fallback when the real sprite is not assigned in the Inspector.
        /// Generates a soft-edged circle texture procedurally (cached after first call).
        /// </summary>
        private static void BuildIconDot(Transform parent, Color color, float size)
        {
            var go = new GameObject("IconDot", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = size;
            le.preferredHeight = size;
            le.flexibleWidth   = 0f;

            var arf = go.AddComponent<AspectRatioFitter>();
            arf.aspectMode  = AspectRatioFitter.AspectMode.WidthControlsHeight;
            arf.aspectRatio = 1f;

            var img = go.GetComponent<Image>();
            img.sprite        = GetOrCreateDotSprite();
            img.color         = color;
            img.raycastTarget = false;
        }

        // Cached procedural dot sprite (32×32 soft circle, white, reused across all dots)
        private static Sprite _dotSprite;

        // Cached radial glow sprite (256×256 warm gold radial gradient, reused)
        private static Sprite _radialGlowSprite;

        /// <summary>
        /// Generates a warm gold radial gradient sprite for use as a glow behind
        /// the vinyl badge. Center is bright gold, fading to transparent at edges.
        /// Cached after first call for performance.
        /// </summary>
        private static Sprite GetOrCreateRadialGlowSprite()
        {
            if (_radialGlowSprite != null) return _radialGlowSprite;

            const int res    = 256;
            var tex          = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var pixels       = new Color[res * res];
            float center     = res / 2f;
            float maxRadius  = center;

            // Warm gold center color, fading to transparent
            Color glowCenter = new Color(1.0f, 0.85f, 0.25f, 0.9f);
            Color glowEdge   = new Color(1.0f, 0.70f, 0.10f, 0.0f);

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx   = x - center;
                    float dy   = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) / maxRadius;

                    // Smooth falloff using smoothstep for a soft glow
                    float t    = 1f - Mathf.Clamp01(dist);
                    float alpha = t * t * (3f - 2f * t); // smoothstep
                    alpha      *= 0.85f; // cap max alpha so it blends nicely

                    pixels[y * res + x] = new Color(
                        Mathf.Lerp(glowEdge.r, glowCenter.r, t),
                        Mathf.Lerp(glowEdge.g, glowCenter.g, t),
                        Mathf.Lerp(glowEdge.b, glowCenter.b, t),
                        alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode   = TextureWrapMode.Clamp;

            _radialGlowSprite = Sprite.Create(tex,
                new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
            return _radialGlowSprite;
        }

        // Cached panel background gradient sprite (64×64 vertical gradient, reused)
        private static Sprite _panelGradientSprite;

        /// <summary>
        /// Generates a subtle vertical gradient sprite for the main panel background.
        /// Darker at top/bottom edges, slightly lighter in the center for depth.
        /// Cached after first call for performance.
        /// </summary>
        private static Sprite GetOrCreatePanelGradientSprite()
        {
            if (_panelGradientSprite != null) return _panelGradientSprite;

            const int resH = 64;
            const int resW = 8; // thin — sliced horizontally
            var tex        = new Texture2D(resW, resH, TextureFormat.RGBA32, false);
            var pixels     = new Color[resW * resH];

            Color topColor    = new Color(0.010f, 0.012f, 0.025f, 1f);  // very dark at edges
            Color centerColor = new Color(0.025f, 0.030f, 0.050f, 1f);  // slightly lighter center

            for (int y = 0; y < resH; y++)
            {
                float t = y / (float)(resH - 1);
                // Smooth gradient: darker at 0 and 1, lighter at 0.5
                float distFromCenter = Mathf.Abs(t - 0.5f) * 2f; // 0 at center, 1 at edges
                float smoothT = distFromCenter * distFromCenter; // quadratic falloff
                Color rowColor = Color.Lerp(centerColor, topColor, smoothT);

                for (int x = 0; x < resW; x++)
                    pixels[y * resW + x] = rowColor;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode   = TextureWrapMode.Clamp;

            _panelGradientSprite = Sprite.Create(tex,
                new Rect(0, 0, resW, resH), new Vector2(0.5f, 0.5f), 100f);
            return _panelGradientSprite;
        }

        private static Sprite GetOrCreateDotSprite()
        {
            if (_dotSprite != null) return _dotSprite;

            const int res    = 32;
            var tex          = new Texture2D(res, res, TextureFormat.RGBA32, false);
            var pixels       = new Color[res * res];
            float center     = res / 2f;
            float radius     = center - 1f;

            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    float dx    = x - center;
                    float dy    = y - center;
                    float dist  = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - dist + 1f);
                    pixels[y * res + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;

            _dotSprite = Sprite.Create(tex,
                new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), 100f);
            return _dotSprite;
        }

        /// <summary>
        /// Creates a TextMeshProUGUI GameObject with the given parameters.
        /// Does NOT add a LayoutElement — callers must add their own sizing constraints.
        /// Gold-colored text (r > 0.7, g > 0.5) automatically gets a subtle dark
        /// shadow for depth, matching the premium goal-render aesthetic.
        /// </summary>
        private static GameObject MakeText(Transform parent, string text, float fontSize,
            Color color, FontStyles style, TextAlignmentOptions alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text          = text;
            tmp.fontSize      = fontSize;
            tmp.color         = color;
            tmp.fontStyle     = style;
            tmp.alignment     = alignment;
            tmp.raycastTarget = false;

            // Add subtle shadow to gold/bright text for depth
            bool isGold = color.r > 0.7f && color.g > 0.5f && color.b < 0.5f;
            if (isGold)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor    = new Color(0.15f, 0.08f, 0f, 0.55f);
                shadow.effectDistance = new Vector2(1.5f, -1.5f);
                shadow.useGraphicAlpha = true;
            }

            return go;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ANIMATIONS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Fade-in (0.2s) followed by staggered elastic card reveal.
        /// Each card scales from scaleX=0 to scaleX=1 with an elastic overshoot
        /// that peaks at ~1.08 before settling. Cards stagger by 0.06s each.
        /// </summary>
        private IEnumerator AnimateIn()
        {
            // Phase 1: panel fade-in
            _panelCg.alpha = 0f;
            float elapsed  = 0f;
            const float fadeDur = 0.20f;

            while (elapsed < fadeDur)
            {
                elapsed        += Time.unscaledDeltaTime;
                _panelCg.alpha  = Mathf.SmoothStep(0f, 1f, elapsed / fadeDur);
                yield return null;
            }
            _panelCg.alpha = 1f;

            // Phase 2: staggered elastic card reveal
            for (int i = 0; i < _cardTransforms.Count; i++)
            {
                var card = _cardTransforms[i];
                if (card == null) continue;

                if (i > 0)
                    yield return new WaitForSecondsRealtime(0.06f);

                elapsed = 0f;
                const float revealDur = 0.25f;

                while (elapsed < revealDur)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t  = Mathf.Clamp01(elapsed / revealDur);
                    float sx = ElasticOvershoot(t);
                    if (card != null)
                        card.localScale = new Vector3(sx, 1f, 1f);
                    yield return null;
                }

                if (card != null)
                    card.localScale = Vector3.one;
            }

            _showAnim = null;
        }

        /// <summary>
        /// Fade-out (0.18s) then deactivate the panel.
        /// </summary>
        private IEnumerator AnimateOut()
        {
            float elapsed  = 0f;
            const float dur = 0.18f;

            while (elapsed < dur && _panelCg != null)
            {
                elapsed        += Time.unscaledDeltaTime;
                _panelCg.alpha  = Mathf.Lerp(1f, 0f, elapsed / dur);
                yield return null;
            }

            if (_panelCg != null) _panelCg.alpha = 0f;
            if (_panel   != null) _panel.SetActive(false);

            _hideAnim = null;
        }

        /// <summary>
        /// Elastic overshoot easing function.
        /// f(0)=0, f(1)=1, overshoots to ~1.08 around t=0.75.
        /// Formula: 2^(-10t) * sin((t - s) * 2π / p) + 1
        /// </summary>
        private static float ElasticOvershoot(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            const float p = 0.3f;
            const float s = p / 4f;
            return Mathf.Pow(2f, -10f * t)
                   * Mathf.Sin((t - s) * (2f * Mathf.PI) / p)
                   + 1f;
        }
    }
}