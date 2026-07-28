#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YARG.Menu.Career;
using YARG.Menu.MusicLibrary;
using YARG.Menu.Career.Trophy;

/// <summary>
/// Authors Song Match prefabs — vertical candidate rows + detail inspector.
/// Run: Tools → YARG Career Mod → Build Song Match Prefabs
/// </summary>
public static class SongMatchPrefabBuilder
{
    private const string Folder = "Assets/Prefabs/CareersM/Submenus";
    private const string PanelPath = Folder + "/ManualMatchPanel.prefab";
    private const string RowPrefabPath = Folder + "/MatchCandidateRow.prefab";
    private const string RowPath = Folder + "/MatchRowView.prefab";
    private const string RingPath = "Assets/Prefabs/Menu/MusicLibrary/DifficultyRing.prefab";
    private const string ArtPath = "Assets/Art/CareersM/Trophy/TrophyScreenArt.asset";
    private const string BgPath = "Assets/Art/CareersM/Trophy/Arts/01_trophy_screen_bg.png";
    private const string BarlowBlackPath = "Assets/Art/Fonts/Barlow/Barlow-Black.asset";
    private const string BarlowLightPath = "Assets/Art/Fonts/Barlow/Barlow-Light.asset";
    private const string ResourcesFolder = "Assets/Resources/CareersM";

    private static TMP_FontAsset _black;
    private static TMP_FontAsset _light;

    [MenuItem("Tools/YARG Career Mod/Build Song Match Prefabs", false, 120)]
    public static void Build()
    {
        EnsureFolder();
        _black = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarlowBlackPath);
        _light = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarlowLightPath);

        var setlistRow = BuildMatchRowViewPrefab();
        var candidateRow = BuildCandidateRowPrefab();

        var existingPanel = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
        bool hasRightDesk = existingPanel != null
            && existingPanel.transform.Find("RightDesk") != null;

        if (hasRightDesk)
        {
            PatchLeftColumnInExistingPanel(setlistRow);
            Debug.Log("[SongMatchPrefabBuilder] Patched LeftColumn + MatchRowView; preserved RightDesk/InspectorHost.");
        }
        else
        {
            BuildManualMatchPanelPrefab(candidateRow);
            Debug.Log("[SongMatchPrefabBuilder] Built full ManualMatchPanel (no existing RightDesk).");
        }

        EnsureResources();
        AssetDatabase.DeleteAsset(ResourcesFolder + "/ManualMatchPanel.prefab");
        AssetDatabase.CopyAsset(PanelPath, ResourcesFolder + "/ManualMatchPanel.prefab");
        AssetDatabase.DeleteAsset(ResourcesFolder + "/MatchCandidateRow.prefab");
        AssetDatabase.CopyAsset(RowPrefabPath, ResourcesFolder + "/MatchCandidateRow.prefab");
        AssetDatabase.DeleteAsset(ResourcesFolder + "/MatchRowView.prefab");
        AssetDatabase.CopyAsset(RowPath, ResourcesFolder + "/MatchRowView.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
    }

    private static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/CareersM"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "CareersM");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Prefabs/CareersM", "Submenus");
    }

    private static void EnsureResources()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(ResourcesFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "CareersM");
    }

    private static GameObject BuildCandidateRowPrefab()
    {
        var ringPrefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(RingPath);
        DifficultyRing ringComp = ringPrefabGo != null
            ? ringPrefabGo.GetComponent<DifficultyRing>()
            : null;

        var root = new GameObject("MatchCandidateRow",
            typeof(RectTransform), typeof(LayoutElement), typeof(MatchCandidateRow));
        var le = root.GetComponent<LayoutElement>();
        le.preferredHeight = MatchCandidateRow.CollapsedHeight;
        le.minHeight = MatchCandidateRow.CollapsedHeight;
        le.flexibleHeight = 0f;
        le.flexibleWidth = 1f;

        var borderOuter = Create("BorderOuter", root.transform);
        Stretch(borderOuter, 0f);
        var borderOuterImg = borderOuter.AddComponent<Image>();
        borderOuterImg.color = Color.clear;
        borderOuterImg.raycastTarget = false;
        borderOuterImg.enabled = false;

        var gap = Create("BorderGap", root.transform);
        Stretch(gap, 2f);
        var gapImg = gap.AddComponent<Image>();
        gapImg.color = new Color(0.06f, 0.07f, 0.09f, 1f);
        gapImg.raycastTarget = false;

        var borderInner = Create("BorderInner", root.transform);
        Stretch(borderInner, 3f);
        var borderInnerImg = borderInner.AddComponent<Image>();
        borderInnerImg.color = Color.clear;
        borderInnerImg.raycastTarget = false;
        borderInnerImg.enabled = false;

        var fill = Create("Fill", root.transform);
        Stretch(fill, 5f);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.10f, 0.10f, 0.12f, 0.95f);
        fillImg.raycastTarget = false;

        // Collapsed — compact single row
        var collapsed = Create("Collapsed", root.transform);
        Stretch(collapsed, 5f);

        var cThumb = Create("Thumb", collapsed.transform);
        var cThumbRt = cThumb.GetComponent<RectTransform>();
        cThumbRt.anchorMin = new Vector2(0f, 0.5f);
        cThumbRt.anchorMax = new Vector2(0f, 0.5f);
        cThumbRt.pivot = new Vector2(0f, 0.5f);
        cThumbRt.anchoredPosition = new Vector2(8f, 0f);
        cThumbRt.sizeDelta = new Vector2(30f, 30f);
        var cThumbRaw = cThumb.AddComponent<RawImage>();
        cThumbRaw.color = new Color(0.12f, 0.12f, 0.14f, 1f);

        var cTitle = CreateText("Title", collapsed.transform, 46f, 4f, -210f, -4f,
            Vector2.zero, Vector2.one, 15f, true, TextAlignmentOptions.MidlineLeft);
        cTitle.overflowMode = TextOverflowModes.Ellipsis;
        cTitle.textWrappingMode = TextWrappingModes.NoWrap;
        cTitle.characterSpacing = 0f;
        cTitle.text = "Song Title";

        var cBadge = CreateBadge("Badge", collapsed.transform, out var cBadgeBg, out var cBadgeLabel);
        var cBadgeRt = cBadge.GetComponent<RectTransform>();
        cBadgeRt.anchorMin = new Vector2(1f, 0.5f);
        cBadgeRt.anchorMax = new Vector2(1f, 0.5f);
        cBadgeRt.pivot = new Vector2(1f, 0.5f);
        cBadgeRt.anchoredPosition = new Vector2(-118f, 0f);
        cBadgeRt.sizeDelta = new Vector2(78f, 20f);
        cBadgeLabel.text = "YARG";

        var cConf = PlaceLabel("Confidence", collapsed.transform, 0f, 0f, 100f, 28f, 14f, true);
        var cConfRt = cConf.rectTransform;
        cConfRt.anchorMin = new Vector2(1f, 0.5f);
        cConfRt.anchorMax = new Vector2(1f, 0.5f);
        cConfRt.pivot = new Vector2(1f, 0.5f);
        cConfRt.anchoredPosition = new Vector2(-10f, 0f);
        cConf.alignment = TextAlignmentOptions.MidlineRight;
        cConf.text = "85% MATCH";

        // Expanded
        var expanded = Create("Expanded", root.transform);
        Stretch(expanded, 5f);
        expanded.SetActive(false);

        var eTitle = PlaceLabel("Title", expanded.transform, 12f, -8f, 380f, 26f, 20f, true);
        eTitle.text = "Song Title";
        eTitle.characterSpacing = 1f;

        var eArtist = PlaceLabel("Artist", expanded.transform, 12f, -32f, 360f, 20f, 13f, false);
        eArtist.text = "Artist";
        eArtist.color = new Color(0.58f, 0.58f, 0.64f);

        var eBadge = CreateBadge("Badge", expanded.transform, out var eBadgeBg, out var eBadgeLabel);
        var eBadgeRt = eBadge.GetComponent<RectTransform>();
        eBadgeRt.anchorMin = new Vector2(1f, 1f);
        eBadgeRt.anchorMax = new Vector2(1f, 1f);
        eBadgeRt.pivot = new Vector2(1f, 1f);
        eBadgeRt.anchoredPosition = new Vector2(-128f, -10f);
        eBadgeRt.sizeDelta = new Vector2(86f, 22f);
        eBadgeLabel.text = "RB4 DLC";

        var eConf = PlaceLabel("Confidence", expanded.transform, 0f, 0f, 110f, 28f, 18f, true);
        var eConfRt = eConf.rectTransform;
        eConfRt.anchorMin = new Vector2(1f, 1f);
        eConfRt.anchorMax = new Vector2(1f, 1f);
        eConfRt.pivot = new Vector2(1f, 1f);
        eConfRt.anchoredPosition = new Vector2(-12f, -8f);
        eConf.alignment = TextAlignmentOptions.TopRight;
        eConf.text = "100% MATCH";

        var eThumb = Create("Thumb", expanded.transform);
        var eThumbRt = eThumb.GetComponent<RectTransform>();
        eThumbRt.anchorMin = new Vector2(0f, 1f);
        eThumbRt.anchorMax = new Vector2(0f, 1f);
        eThumbRt.pivot = new Vector2(0f, 1f);
        eThumbRt.anchoredPosition = new Vector2(12f, -58f);
        eThumbRt.sizeDelta = new Vector2(88f, 88f);
        var eThumbRaw = eThumb.AddComponent<RawImage>();
        eThumbRaw.color = new Color(0.12f, 0.12f, 0.14f, 1f);

        var eSection = PlaceLabel("SectionLabel", expanded.transform, 114f, -58f, 420f, 18f, 12f, false);
        eSection.text = "Candidate Instrument Availability & Tiers";
        eSection.color = new Color(0.58f, 0.58f, 0.64f);

        var eMeta = PlaceLabel("Meta", expanded.transform, 114f, -78f, 480f, 18f, 11f, false);
        eMeta.text = "Year 2002  |  Pop-Rock  |  HASH [FDE35…]";
        eMeta.color = new Color(0.58f, 0.58f, 0.64f);

        var rings = Create("Rings", expanded.transform);
        var ringsRt = rings.GetComponent<RectTransform>();
        ringsRt.anchorMin = new Vector2(0f, 1f);
        ringsRt.anchorMax = new Vector2(1f, 1f);
        ringsRt.pivot = new Vector2(0f, 1f);
        ringsRt.anchoredPosition = new Vector2(114f, -100f);
        ringsRt.sizeDelta = new Vector2(-130f, 48f);
        var ringsHlg = rings.AddComponent<HorizontalLayoutGroup>();
        ringsHlg.childAlignment = TextAnchor.MiddleLeft;
        ringsHlg.spacing = 6f;
        ringsHlg.childControlWidth = false;
        ringsHlg.childControlHeight = false;
        ringsHlg.childForceExpandWidth = false;
        ringsHlg.childForceExpandHeight = false;

        if (ringPrefabGo != null)
        {
            for (int i = 0; i < 6; i++)
            {
                var r = (GameObject)PrefabUtility.InstantiatePrefab(ringPrefabGo, rings.transform);
                r.name = $"Ring_{i}";
                ScaleRingCompact(r);
            }
        }

        var star = Create("Star", root.transform);
        var starRt = star.GetComponent<RectTransform>();
        starRt.anchorMin = new Vector2(0f, 1f);
        starRt.anchorMax = new Vector2(0f, 1f);
        starRt.pivot = new Vector2(0f, 1f);
        starRt.anchoredPosition = new Vector2(8f, -4f);
        starRt.sizeDelta = new Vector2(18f, 18f);
        var starTmp = star.AddComponent<TextMeshProUGUI>();
        starTmp.text = "★";
        ApplyBlack(starTmp, 14f);
        starTmp.color = new Color(0.83f, 0.69f, 0.22f);
        starTmp.alignment = TextAlignmentOptions.Center;
        star.SetActive(false);

        var rowComp = root.GetComponent<MatchCandidateRow>();
        var so = new SerializedObject(rowComp);
        so.FindProperty("_rowLayoutVersion").intValue = MatchCandidateRow.LayoutVersion;
        so.FindProperty("_fill").objectReferenceValue = fillImg;
        so.FindProperty("_borderOuter").objectReferenceValue = borderOuterImg;
        so.FindProperty("_borderInner").objectReferenceValue = borderInnerImg;
        so.FindProperty("_layoutElement").objectReferenceValue = le;
        so.FindProperty("_collapsedRoot").objectReferenceValue = collapsed;
        so.FindProperty("_expandedRoot").objectReferenceValue = expanded;
        so.FindProperty("_starBadge").objectReferenceValue = star;
        so.FindProperty("_collapsedThumb").objectReferenceValue = cThumbRaw;
        so.FindProperty("_collapsedTitle").objectReferenceValue = cTitle;
        so.FindProperty("_collapsedBadgeBg").objectReferenceValue = cBadgeBg;
        so.FindProperty("_collapsedBadgeText").objectReferenceValue = cBadgeLabel;
        so.FindProperty("_collapsedConfidence").objectReferenceValue = cConf;
        so.FindProperty("_expandedThumb").objectReferenceValue = eThumbRaw;
        so.FindProperty("_expandedTitle").objectReferenceValue = eTitle;
        so.FindProperty("_expandedArtist").objectReferenceValue = eArtist;
        so.FindProperty("_expandedBadgeBg").objectReferenceValue = eBadgeBg;
        so.FindProperty("_expandedBadgeText").objectReferenceValue = eBadgeLabel;
        so.FindProperty("_expandedConfidence").objectReferenceValue = eConf;
        so.FindProperty("_expandedSectionLabel").objectReferenceValue = eSection;
        so.FindProperty("_expandedMeta").objectReferenceValue = eMeta;
        so.FindProperty("_expandedRingsRow").objectReferenceValue = rings.transform;
        if (ringComp != null)
            so.FindProperty("_difficultyRingPrefab").objectReferenceValue = ringComp;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, RowPrefabPath);
    }

    private static GameObject CreateBadge(string name, Transform parent,
        out Image bg, out TextMeshProUGUI label)
    {
        var go = Create(name, parent);
        bg = go.AddComponent<Image>();
        bg.color = new Color(0.45f, 0.22f, 0.22f, 1f);

        var textGo = Create("Label", go.transform);
        Stretch(textGo, 2f);
        label = textGo.AddComponent<TextMeshProUGUI>();
        ApplyBlack(label, 10f);
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return go;
    }

    private static void BuildManualMatchPanelPrefab(GameObject candidateRowPrefab)
    {
        var art = AssetDatabase.LoadAssetAtPath<TrophyScreenArt>(ArtPath);
        var ringPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RingPath);
        var setlistRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath);
        var bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
        if (art != null && art.Background != null)
            bgSprite = art.Background;

        var root = new GameObject("ManualMatchPanel",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(Image), typeof(ManualMatchPanel));

        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = new Vector2(0f, 56f);
        rootRt.offsetMax = Vector2.zero;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 50;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var rootImg = root.GetComponent<Image>();
        rootImg.color = new Color(0.07f, 0.07f, 0.09f, 1f);
        rootImg.raycastTarget = true;

        var bgArt = Create("BgArt", root.transform);
        Stretch(bgArt, 0f);
        var bgArtImg = bgArt.AddComponent<Image>();
        bgArtImg.sprite = bgSprite;
        bgArtImg.color = new Color(1f, 1f, 1f, 0.42f);
        bgArtImg.raycastTarget = false;
        bgArt.transform.SetAsFirstSibling();

        var vig = Create("Vignette", root.transform);
        Stretch(vig, 0f);
        var vigImg = vig.AddComponent<Image>();
        vigImg.color = new Color(0f, 0f, 0f, 0.50f);
        vigImg.raycastTarget = false;

        var titleGo = Create("ScreenTitle", root.transform);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -14f);
        titleRt.sizeDelta = new Vector2(-200f, 48f);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "SONG MATCH";
        ApplyBlack(titleTmp, 32f);
        titleTmp.color = new Color(0.83f, 0.69f, 0.22f);
        titleTmp.alignment = TextAlignmentOptions.Top;
        titleTmp.characterSpacing = 24f;
        titleTmp.raycastTarget = false;

        // ── Left column (unchanged) ──
        var left = Create("LeftColumn", root.transform);
        var leftRt = left.GetComponent<RectTransform>();
        leftRt.anchorMin = new Vector2(0.04f, 0.05f);
        leftRt.anchorMax = new Vector2(0.36f, 0.88f);
        leftRt.offsetMin = Vector2.zero;
        leftRt.offsetMax = Vector2.zero;
        var leftImg = left.AddComponent<Image>();
        leftImg.sprite = null;
        leftImg.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);

        var leftList = left.AddComponent<MatchSongListMenu>();

        var leftHeader = CreateText("LeftHeader", left.transform, 36f, -28f, -36f, 0f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), 22f, true, TextAlignmentOptions.TopLeft);
        var leftHeaderRt = leftHeader.rectTransform;
        leftHeaderRt.pivot = new Vector2(0.5f, 1f);
        leftHeaderRt.anchoredPosition = new Vector2(0f, -28f);
        leftHeaderRt.sizeDelta = new Vector2(-72f, 58f);
        leftHeader.text = "SETLIST\n<size=65%><color=#D4AF37>83 SONGS  ·  0 OPEN</color></size>";
        leftHeader.characterSpacing = 6f;

        var scrollContent = Create("LeftScrollContent", left.transform);
        var scrollRt = scrollContent.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(24f, 24f);
        scrollRt.offsetMax = new Vector2(-24f, -96f);
        var scrollVlg = scrollContent.AddComponent<VerticalLayoutGroup>();
        scrollVlg.spacing = 4f;
        scrollVlg.childAlignment = TextAnchor.MiddleCenter;
        scrollVlg.childControlWidth = true;
        scrollVlg.childControlHeight = true;
        scrollVlg.childForceExpandWidth = true;
        scrollVlg.childForceExpandHeight = false;

        var scrollbarGo = Create("Scrollbar", left.transform);
        var sbRt = scrollbarGo.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(1f, 0f);
        sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.pivot = new Vector2(1f, 0.5f);
        sbRt.anchoredPosition = new Vector2(-18f, 0f);
        sbRt.sizeDelta = new Vector2(8f, -120f);
        var scrollbar = scrollbarGo.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        var sbImg = scrollbarGo.AddComponent<Image>();
        sbImg.color = new Color(1f, 1f, 1f, 0.08f);

        var sliding = Create("Sliding Area", scrollbarGo.transform);
        Stretch(sliding, 0f);
        var handle = Create("Handle", sliding.transform);
        Stretch(handle, 0f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.83f, 0.69f, 0.22f, 0.45f);
        scrollbar.handleRect = handle.GetComponent<RectTransform>();
        scrollbar.targetGraphic = handleImg;

        var leftListSo = new SerializedObject(leftList);
        if (setlistRowPrefab != null)
        {
            var rowView = setlistRowPrefab.GetComponent<MatchViewObject>();
            leftListSo.FindProperty("_viewObjectPrefab").objectReferenceValue = rowView;
        }
        leftListSo.FindProperty("_viewObjectParent").objectReferenceValue = scrollContent.transform;
        leftListSo.FindProperty("_scrollbar").objectReferenceValue = scrollbar;
        leftListSo.ApplyModifiedPropertiesWithoutUndo();

        // ── Right desk (vertical candidates + detail) ──
        var right = Create("RightDesk", root.transform);
        var rightRt = right.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.38f, 0.05f);
        rightRt.anchorMax = new Vector2(0.96f, 0.88f);
        rightRt.offsetMin = Vector2.zero;
        rightRt.offsetMax = Vector2.zero;
        var rightImg = right.AddComponent<Image>();
        rightImg.sprite = null;
        rightImg.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);

        var rightHeader = CreateText("RightHeader", right.transform, 28f, -20f, -28f, 0f,
            new Vector2(0f, 1f), new Vector2(1f, 1f), 18f, true, TextAlignmentOptions.TopLeft);
        var rhRt = rightHeader.rectTransform;
        rhRt.pivot = new Vector2(0.5f, 1f);
        rhRt.anchoredPosition = new Vector2(0f, -20f);
        rhRt.sizeDelta = new Vector2(-56f, 56f);
        rightHeader.text = "MATCH CANDIDATES";
        rightHeader.characterSpacing = 6f;
        rightHeader.color = new Color(0.83f, 0.69f, 0.22f);

        var list = Create("CandidateList", right.transform);
        var listRt = list.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0f, 0.38f);
        listRt.anchorMax = new Vector2(1f, 0.90f);
        listRt.offsetMin = new Vector2(24f, 4f);
        listRt.offsetMax = new Vector2(-24f, -4f);
        var scroll = list.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        var viewport = Create("Viewport", list.transform);
        Stretch(viewport, 0f);
        viewport.AddComponent<RectMask2D>();
        var vpImg = viewport.AddComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.001f);

        var content = Create("Content", viewport.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(2, 2, 4, 4);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRt;

        // Detail inspector
        var inspectorHost = Create("InspectorHost", right.transform);
        var hostRt = inspectorHost.GetComponent<RectTransform>();
        hostRt.anchorMin = new Vector2(0f, 0.03f);
        hostRt.anchorMax = new Vector2(1f, 0.36f);
        hostRt.offsetMin = new Vector2(24f, 16f);
        hostRt.offsetMax = new Vector2(-24f, -4f);

        var inspectorGo = Create("DetailInspector", inspectorHost.transform);
        Stretch(inspectorGo, 0f);
        var inspImg = inspectorGo.AddComponent<Image>();
        inspImg.sprite = null;
        inspImg.color = new Color(0.07f, 0.08f, 0.10f, 1f);
        var inspector = inspectorGo.AddComponent<MatchDetailInspector>();

        var inspHeader = PlaceLabel("Header", inspectorGo.transform, 28f, -18f, 640f, 28f, 16f, true);
        inspHeader.text = "SELECTED CANDIDATE DETAILS";
        inspHeader.color = new Color(0.83f, 0.69f, 0.22f);
        inspHeader.characterSpacing = 10f;

        var album = Create("AlbumCover", inspectorGo.transform);
        var albumRt = album.GetComponent<RectTransform>();
        albumRt.anchorMin = new Vector2(0f, 1f);
        albumRt.anchorMax = new Vector2(0f, 1f);
        albumRt.pivot = new Vector2(0f, 1f);
        albumRt.anchoredPosition = new Vector2(24f, -48f);
        albumRt.sizeDelta = new Vector2(140f, 140f);
        var albumRaw = album.AddComponent<RawImage>();
        albumRaw.color = new Color(0.12f, 0.12f, 0.14f, 1f);

        var inspTitle = PlaceLabel("Title", inspectorGo.transform, 184f, -48f, 420f, 32f, 24f, true);
        inspTitle.text = "SONG TITLE";
        inspTitle.characterSpacing = 2f;

        var inspArtist = PlaceLabel("Artist", inspectorGo.transform, 184f, -80f, 360f, 22f, 15f, false);
        inspArtist.text = "Artist";
        inspArtist.color = new Color(0.58f, 0.58f, 0.64f);

        var inspMeta = PlaceLabel("Meta", inspectorGo.transform, 184f, -104f, 360f, 20f, 13f, false);
        inspMeta.text = "GENRE  ·  2023";
        inspMeta.color = new Color(0.58f, 0.58f, 0.64f);

        var inspSource = PlaceLabel("Source", inspectorGo.transform, 520f, -80f, 240f, 20f, 12f, false);
        inspSource.text = "Source: [rb4_dlc]";
        inspSource.color = new Color(0.30f, 0.82f, 0.92f);

        var inspHash = PlaceLabel("Hash", inspectorGo.transform, 520f, -104f, 280f, 36f, 10f, false);
        inspHash.text = "Full hash: …";
        inspHash.color = new Color(0.38f, 0.38f, 0.42f);
        inspHash.overflowMode = TextOverflowModes.Ellipsis;
        inspHash.textWrappingMode = TextWrappingModes.Normal;
        inspHash.maxVisibleLines = 2;

        var inspConf = PlaceLabel("Confidence", inspectorGo.transform, 0f, 0f, 180f, 36f, 22f, true);
        var confRt = inspConf.rectTransform;
        confRt.anchorMin = new Vector2(1f, 1f);
        confRt.anchorMax = new Vector2(1f, 1f);
        confRt.pivot = new Vector2(1f, 1f);
        confRt.anchoredPosition = new Vector2(-24f, -48f);
        inspConf.alignment = TextAlignmentOptions.TopRight;
        inspConf.text = "100% MATCH";

        // Single dense ring row
        var ringsTop = Create("RingsTop", inspectorGo.transform);
        SetupRingRow(ringsTop, 8f, 46f);

        DifficultyRing ringComp = null;
        if (ringPrefab != null)
        {
            ringComp = ringPrefab.GetComponent<DifficultyRing>();
            for (int i = 0; i < 10; i++)
            {
                var r = (GameObject)PrefabUtility.InstantiatePrefab(ringPrefab, ringsTop.transform);
                r.name = $"DifficultyRing_{i}";
                ScaleRingCompact(r);
            }
        }

        var inspSo = new SerializedObject(inspector);
        inspSo.FindProperty("_headerText").objectReferenceValue = inspHeader;
        inspSo.FindProperty("_albumCover").objectReferenceValue = albumRaw;
        inspSo.FindProperty("_titleText").objectReferenceValue = inspTitle;
        inspSo.FindProperty("_artistText").objectReferenceValue = inspArtist;
        inspSo.FindProperty("_metaText").objectReferenceValue = inspMeta;
        inspSo.FindProperty("_sourceText").objectReferenceValue = inspSource;
        inspSo.FindProperty("_hashText").objectReferenceValue = inspHash;
        inspSo.FindProperty("_confidenceText").objectReferenceValue = inspConf;
        inspSo.FindProperty("_ringsTopRow").objectReferenceValue = ringsTop.transform;
        if (ringComp != null)
            inspSo.FindProperty("_difficultyRingPrefab").objectReferenceValue = ringComp;
        inspSo.ApplyModifiedPropertiesWithoutUndo();

        var panel = root.GetComponent<ManualMatchPanel>();
        var panelSo = new SerializedObject(panel);
        panelSo.FindProperty("_leftList").objectReferenceValue = leftList;
        panelSo.FindProperty("_leftHeader").objectReferenceValue = leftHeader;
        panelSo.FindProperty("_rightHeader").objectReferenceValue = rightHeader;
        panelSo.FindProperty("_candidateStripContent").objectReferenceValue = content.transform;
        panelSo.FindProperty("_detailInspector").objectReferenceValue = inspector;
        panelSo.FindProperty("_backgroundImage").objectReferenceValue = rootImg;
        panelSo.FindProperty("_leftPanelImage").objectReferenceValue = leftImg;
        panelSo.FindProperty("_rightPanelImage").objectReferenceValue = rightImg;
        panelSo.FindProperty("_candidateRowPrefab").objectReferenceValue =
            candidateRowPrefab.GetComponent<MatchCandidateRow>();
        panelSo.FindProperty("_authoredLayout").boolValue = true;
        if (ringComp != null)
            panelSo.FindProperty("_difficultyRingPrefab").objectReferenceValue = ringComp;
        panelSo.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, PanelPath);
    }

    private static void ScaleRingCompact(GameObject ring)
    {
        var le = ring.GetComponent<LayoutElement>() ?? ring.AddComponent<LayoutElement>();
        le.preferredWidth = 42f;
        le.preferredHeight = 42f;
        le.minWidth = 42f;
        le.minHeight = 42f;
        ring.transform.localScale = Vector3.one * 0.58f;
    }

    private static void SetupRingRow(GameObject row, float bottom, float height = 46f)
    {
        var rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottom);
        rt.sizeDelta = new Vector2(-40f, height);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 4f;
        hlg.padding = new RectOffset(20, 20, 0, 0);
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
    }

    private static TextMeshProUGUI PlaceLabel(string name, Transform parent,
        float x, float y, float w, float h, float size, bool bold)
    {
        var go = Create(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (bold) ApplyBlack(tmp, size);
        else ApplyLight(tmp, size);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent,
        float left, float bottom, float right, float top,
        Vector2 anchorMin, Vector2 anchorMax, float size, bool bold,
        TextAlignmentOptions align)
    {
        var go = Create(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(right, top);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (bold) ApplyBlack(tmp, size);
        else ApplyLight(tmp, size);
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void ApplyBlack(TextMeshProUGUI tmp, float size)
    {
        if (_black != null) tmp.font = _black;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = new Color(0.96f, 0.96f, 0.97f);
    }

    private static void ApplyLight(TextMeshProUGUI tmp, float size)
    {
        if (_light != null) tmp.font = _light;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = new Color(0.58f, 0.58f, 0.64f);
    }

    private static GameObject Create(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(GameObject go, float inset)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject BuildMatchRowViewPrefab()
    {
        var root = new GameObject("MatchRowView",
            typeof(RectTransform), typeof(CanvasGroup), typeof(LayoutElement), typeof(MatchViewObject));

        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0f, 1f);
        rootRt.anchorMax = new Vector2(1f, 1f);
        rootRt.pivot = new Vector2(0.5f, 1f);
        rootRt.sizeDelta = new Vector2(0f, 46f);

        var le = root.GetComponent<LayoutElement>();
        le.minHeight = 46f;
        le.preferredHeight = 46f;
        le.flexibleWidth = 1f;

        var canvasGroup = root.GetComponent<CanvasGroup>();

        // Stock ListMenu backgrounds (dimmed — cyan frame is primary selection)
        var normalBg = Create("Normal Background", root.transform);
        Stretch(normalBg, 0f);
        var normalImg = normalBg.AddComponent<Image>();
        normalImg.color = new Color(1f, 1f, 1f, 0.02f);
        normalImg.raycastTarget = false;

        var selectedBg = Create("Selected Background", root.transform);
        Stretch(selectedBg, 0f);
        var selectedImg = selectedBg.AddComponent<Image>();
        selectedImg.color = new Color(0.30f, 0.82f, 0.92f, 0.10f);
        selectedImg.raycastTarget = false;
        selectedBg.SetActive(false);

        var categoryBg = Create("Category Background", root.transform);
        Stretch(categoryBg, 0f);
        var categoryImg = categoryBg.AddComponent<Image>();
        categoryImg.color = new Color(1f, 1f, 1f, 0.03f);
        categoryImg.raycastTarget = false;
        categoryBg.SetActive(false);

        // Hidden stock icon (ViewObject requires it)
        var iconGo = Create("Icon", root.transform);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(-40f, 0f);
        iconRt.sizeDelta = new Vector2(8f, 8f);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.clear;
        iconGo.SetActive(false);

        // Song chrome
        var songRoot = Create("SongRoot", root.transform);
        Stretch(songRoot, 0f);

        var borderOuter = Create("BorderOuter", songRoot.transform);
        Stretch(borderOuter, 0f);
        var borderOuterImg = borderOuter.AddComponent<Image>();
        borderOuterImg.color = Color.clear;
        borderOuterImg.raycastTarget = false;
        borderOuterImg.enabled = false;

        var gap = Create("BorderGap", songRoot.transform);
        Stretch(gap, 2f);
        var gapImg = gap.AddComponent<Image>();
        gapImg.color = new Color(0.06f, 0.07f, 0.09f, 1f);
        gapImg.raycastTarget = false;

        var borderInner = Create("BorderInner", songRoot.transform);
        Stretch(borderInner, 3f);
        var borderInnerImg = borderInner.AddComponent<Image>();
        borderInnerImg.color = Color.clear;
        borderInnerImg.raycastTarget = false;
        borderInnerImg.enabled = false;

        var fill = Create("Fill", songRoot.transform);
        Stretch(fill, 5f);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.10f, 0.10f, 0.12f, 0.92f);
        fillImg.raycastTarget = false;

        var status = PlaceLabel("Status", songRoot.transform, 12f, -8f, 28f, 30f, 16f, true);
        status.alignment = TextAlignmentOptions.MidlineLeft;
        status.text = "○";

        var title = PlaceLabel("Title", songRoot.transform, 40f, -4f, 300f, 22f, 15f, true);
        title.text = "Song Title";
        title.overflowMode = TextOverflowModes.Ellipsis;
        title.characterSpacing = 0f;

        var artist = PlaceLabel("Artist", songRoot.transform, 40f, -24f, 280f, 16f, 12f, false);
        artist.text = "Artist";
        artist.color = new Color(0.58f, 0.58f, 0.64f);
        artist.overflowMode = TextOverflowModes.Ellipsis;

        var badge = Create("Badge", songRoot.transform);
        var badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(1f, 0.5f);
        badgeRt.anchorMax = new Vector2(1f, 0.5f);
        badgeRt.pivot = new Vector2(1f, 0.5f);
        badgeRt.anchoredPosition = new Vector2(-14f, 0f);
        badgeRt.sizeDelta = new Vector2(72f, 20f);
        var badgeBg = badge.AddComponent<Image>();
        badgeBg.color = new Color(0.15f, 0.45f, 0.55f, 1f);
        var badgeLabelGo = Create("Label", badge.transform);
        Stretch(badgeLabelGo, 2f);
        var badgeLabel = badgeLabelGo.AddComponent<TextMeshProUGUI>();
        ApplyBlack(badgeLabel, 10f);
        badgeLabel.color = Color.white;
        badgeLabel.alignment = TextAlignmentOptions.Center;
        badgeLabel.text = "MANUAL";
        badge.SetActive(false);

        // Section chrome
        var sectionRoot = Create("SectionRoot", root.transform);
        Stretch(sectionRoot, 0f);
        sectionRoot.SetActive(false);
        var sectionText = PlaceLabel("SectionText", sectionRoot.transform, 16f, -12f, 400f, 22f, 12f, true);
        sectionText.color = new Color(0.83f, 0.69f, 0.22f);
        sectionText.characterSpacing = 8f;
        sectionText.text = "NEEDS MATCH";

        var view = root.GetComponent<MatchViewObject>();
        var so = new SerializedObject(view);
        so.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
        so.FindProperty("NormalBackground").objectReferenceValue = normalBg;
        so.FindProperty("SelectedBackground").objectReferenceValue = selectedBg;
        so.FindProperty("CategoryBackground").objectReferenceValue = categoryBg;
        so.FindProperty("_icon").objectReferenceValue = iconImg;
        var primaryProp = so.FindProperty("_primaryText");
        if (primaryProp != null) primaryProp.ClearArray();
        var secondaryProp = so.FindProperty("_secondaryText");
        if (secondaryProp != null) secondaryProp.ClearArray();
        so.FindProperty("_songRoot").objectReferenceValue = songRoot;
        so.FindProperty("_borderOuter").objectReferenceValue = borderOuterImg;
        so.FindProperty("_borderInner").objectReferenceValue = borderInnerImg;
        so.FindProperty("_fill").objectReferenceValue = fillImg;
        so.FindProperty("_statusText").objectReferenceValue = status;
        so.FindProperty("_titleText").objectReferenceValue = title;
        so.FindProperty("_artistText").objectReferenceValue = artist;
        so.FindProperty("_badgeBg").objectReferenceValue = badgeBg;
        so.FindProperty("_badgeText").objectReferenceValue = badgeLabel;
        so.FindProperty("_sectionRoot").objectReferenceValue = sectionRoot;
        so.FindProperty("_sectionText").objectReferenceValue = sectionText;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SavePrefab(root, RowPath);
    }

    private static void PatchLeftColumnInExistingPanel(GameObject setlistRowPrefab)
    {
        var panelRoot = PrefabUtility.LoadPrefabContents(PanelPath);
        try
        {
            var left = panelRoot.transform.Find("LeftColumn");
            if (left == null)
            {
                Debug.LogWarning("[SongMatchPrefabBuilder] LeftColumn not found — skip left patch.");
                return;
            }

            // Kill gold trophy frames
            var frame = left.Find("LeftFrame");
            if (frame != null) Object.DestroyImmediate(frame.gameObject);
            var hair = left.Find("LeftGoldHairline");
            if (hair != null) Object.DestroyImmediate(hair.gameObject);

            var leftImg = left.GetComponent<Image>() ?? left.gameObject.AddComponent<Image>();
            leftImg.sprite = null;
            leftImg.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);

            var scrollContent = left.Find("LeftScrollContent");
            if (scrollContent != null)
            {
                var scrollVlg = scrollContent.GetComponent<VerticalLayoutGroup>()
                                ?? scrollContent.gameObject.AddComponent<VerticalLayoutGroup>();
                scrollVlg.spacing = 4f;
                scrollVlg.childAlignment = TextAnchor.MiddleCenter;
                scrollVlg.childControlWidth = true;
                scrollVlg.childControlHeight = true;
                scrollVlg.childForceExpandWidth = true;
                scrollVlg.childForceExpandHeight = false;
            }

            var headerTmp = left.Find("LeftHeader")?.GetComponent<TextMeshProUGUI>();
            if (headerTmp != null)
            {
                ApplyBlack(headerTmp, 18f);
                headerTmp.color = new Color(0.83f, 0.69f, 0.22f);
                headerTmp.characterSpacing = 6f;
                headerTmp.alignment = TextAlignmentOptions.TopLeft;
                var hrt = headerTmp.rectTransform;
                hrt.anchorMin = new Vector2(0f, 1f);
                hrt.anchorMax = new Vector2(1f, 1f);
                hrt.pivot = new Vector2(0.5f, 1f);
                hrt.anchoredPosition = new Vector2(0f, -22f);
                hrt.sizeDelta = new Vector2(-56f, 56f);
            }

            var leftList = left.GetComponent<MatchSongListMenu>();
            if (leftList != null && setlistRowPrefab != null)
            {
                var leftListSo = new SerializedObject(leftList);
                leftListSo.FindProperty("_viewObjectPrefab").objectReferenceValue =
                    setlistRowPrefab.GetComponent<MatchViewObject>();
                leftListSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var panel = panelRoot.GetComponent<ManualMatchPanel>();
            if (panel != null)
            {
                var panelSo = new SerializedObject(panel);
                panelSo.FindProperty("_leftPanelImage").objectReferenceValue = leftImg;
                if (headerTmp != null)
                    panelSo.FindProperty("_leftHeader").objectReferenceValue = headerTmp;
                if (leftList != null)
                    panelSo.FindProperty("_leftList").objectReferenceValue = leftList;
                panelSo.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.SaveAsPrefabAsset(panelRoot, PanelPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(panelRoot);
        }
    }
}
#endif
