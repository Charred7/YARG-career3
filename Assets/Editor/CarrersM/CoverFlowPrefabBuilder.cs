using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using YARG.Menu.Career.CoverFlow;

public class CoverFlowPrefabBuilder
{
    [MenuItem("GameObject/UI/Create Perfect CoverFlow Card", false, 10)]
    public static void CreateHierarchy()
    {
        // 1. Root Setup - Locked with AspectRatioFitter
        //    Add CoverFlowCard (state visuals) and CoverFlowCardControllerV3 (text renderer)
        GameObject root = new GameObject("CoverFlowCard", typeof(RectTransform), typeof(CanvasGroup),
            typeof(CoverFlowCard), typeof(CoverFlowCardControllerV3));
        Undo.RegisterCreatedObjectUndo(root, "Create CoverFlow Card");
        
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(460f, 720f);
        
        // CRITICAL FIX 1: AspectRatioFitter prevents the game from squishing it horizontally
        var arf = root.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = 460f / 720f; // Locks it to standard portrait proportion

        if (Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<RectTransform>() != null)
        {
            root.transform.SetParent(Selection.activeGameObject.transform, false);
        }

        // 2. Base Decorative Elements
        CreateImage(root, "WoodenFrame", true);
        
        GameObject poster = new GameObject("PosterImage", typeof(RectTransform), typeof(RawImage));
        poster.transform.SetParent(root.transform, false);
        StretchRect(poster.GetComponent<RectTransform>(), new RectOffset(15, 15, 15, 15)); // Inset for wood frame

        GameObject grayscale = CreateImage(root, "GrayscaleOverlay", true);
        grayscale.SetActive(false);

        GameObject sash = CreateImage(root, "SoldOutSash", true);
        sash.SetActive(false);

        // Accent Border — active-state glow around the card
        GameObject accentBorder = CreateImage(root, "AccentBorder", true);
        accentBorder.SetActive(false);

        // Lock Panel — shown when card is in Locked state
        GameObject lockPanel = new GameObject("LockPanel", typeof(RectTransform));
        lockPanel.transform.SetParent(root.transform, false);
        StretchRect(lockPanel.GetComponent<RectTransform>(), new RectOffset(20, 20, 20, 20));
        lockPanel.SetActive(false);

        var lockVlg = lockPanel.AddComponent<VerticalLayoutGroup>();
        lockVlg.childAlignment = TextAnchor.MiddleCenter;
        lockVlg.spacing = 12;
        lockVlg.childControlWidth = true;
        lockVlg.childControlHeight = true;
        lockVlg.childForceExpandWidth = true;
        lockVlg.childForceExpandHeight = false;

        // Lock Icon
        GameObject lockIcon = CreateImage(lockPanel, "LockIcon", false);
        var lockIconLE = lockIcon.AddComponent<LayoutElement>();
        lockIconLE.minWidth = 64;
        lockIconLE.minHeight = 64;
        lockIconLE.preferredWidth = 64;
        lockIconLE.preferredHeight = 64;

        // Unlock Requirement Text
        GameObject unlockReqText = CreateTMP(lockPanel, "UnlockRequirementText", "🔒 UNLOCK: CLEAR GIG 00");
        var unlockTmp = unlockReqText.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(unlockTmp, 22, TextAlignmentOptions.Center, true);
        unlockTmp.fontStyle = FontStyles.Bold;

        // Mystery Tracklist Text
        GameObject mysteryText = CreateTMP(lockPanel, "MysteryTracklistText", "???");
        var mysteryTmp = mysteryText.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(mysteryTmp, 18, TextAlignmentOptions.Center, true);

        // 3. Active Content Panel Setup
        GameObject activePanel = new GameObject("ActiveContentPanel", typeof(RectTransform));
        activePanel.transform.SetParent(root.transform, false);
        StretchRect(activePanel.GetComponent<RectTransform>(), new RectOffset(25, 25, 30, 30));
        
        var vlg = activePanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;       // Allow it to manage height vertically
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;  // Do not evenly divide height

        // Gig Title Text
        GameObject gigTitle = CreateTMP(activePanel, "GigTitleText", "GUITAR HERO:\nMETALLICA");
        var titleText = gigTitle.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(titleText, 32, TextAlignmentOptions.Center, true);
        titleText.fontStyle = FontStyles.Bold;
        
        var leTitle = gigTitle.AddComponent<LayoutElement>();
        leTitle.minHeight = 80;
        leTitle.flexibleHeight = 0;

        // Band Logo Image (Empty image to hold space if no logo)
        GameObject bandLogo = CreateImage(activePanel, "BandLogoImage", false);
        var leLogo = bandLogo.AddComponent<LayoutElement>();
        leLogo.minHeight = 90;
        leLogo.flexibleHeight = 0;

        // Tracklist Container
        GameObject tracklistContainer = new GameObject("TracklistContainer", typeof(RectTransform));
        tracklistContainer.transform.SetParent(activePanel.transform, false);
        
        var leTrackContainer = tracklistContainer.AddComponent<LayoutElement>();
        leTrackContainer.flexibleHeight = 1; // Explicitly pull all remaining middle space
        
        var vlgTracklist = tracklistContainer.AddComponent<VerticalLayoutGroup>();
        vlgTracklist.childAlignment = TextAnchor.UpperCenter;
        vlgTracklist.childControlWidth = true;
        vlgTracklist.childControlHeight = true;

        // Tracklist Text
        GameObject tracklistTextObj = CreateTMP(tracklistContainer, "TracklistText", "1. Am I Evil?\n2. Blood and Thunder\n3. Stone Cold Crazy");
        var trackText = tracklistTextObj.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(trackText, 22, TextAlignmentOptions.Center, true);
        trackText.lineSpacing = 10;

        // 4. Metadata Footer - Stacked Vertically
        GameObject footer = new GameObject("MetadataFooter", typeof(RectTransform));
        footer.transform.SetParent(activePanel.transform, false);
        
        var leFooter = footer.AddComponent<LayoutElement>();
        leFooter.minHeight = 70; // Hard lock space at the bottom
        leFooter.flexibleHeight = 0;
        
        var vlgFooter = footer.AddComponent<VerticalLayoutGroup>();
        vlgFooter.childAlignment = TextAnchor.MiddleCenter;
        vlgFooter.spacing = 4;
        vlgFooter.childControlWidth = true;
        vlgFooter.childControlHeight = true;
        vlgFooter.childForceExpandWidth = true;
        vlgFooter.childForceExpandHeight = false;

        // Sub-Rows Setup
        CreateRow(footer, "AvgTrackRow", "ClockIcon", "AvgTrackText", "AVG. TRACK: ~3 MINS");
        CreateRow(footer, "IntensityRow", "SunglassesIcon", "IntensityText", "SETLIST INTENSITY: CASUAL");

        // ── Auto-wire CoverFlowCardControllerV3 serialized references ──
        AutoWireV3Controller(root, poster, bandLogo, gigTitle, tracklistTextObj, footer);

        // ── Auto-wire CoverFlowCard serialized references ──
        AutoWireCoverFlowCard(root, poster, grayscale, sash, accentBorder,
            lockPanel, lockIcon, unlockReqText, mysteryText,
            gigTitle, bandLogo, activePanel, footer);

        Selection.activeGameObject = root;

        // ── Open Sprite Setup window for quick asset assignment ──
        CoverFlowCardSpriteSetup.ShowWindow(root);
    }

    /// <summary>
    /// Auto-wires the CoverFlowCardControllerV3 component's serialized fields
    /// to the child GameObjects created above. Uses SerializedObject to persist
    /// references through Undo.
    /// </summary>
    private static void AutoWireV3Controller(GameObject root, GameObject poster,
        GameObject bandLogo, GameObject gigTitle, GameObject tracklistText, GameObject footer)
    {
        var v3 = root.GetComponent<CoverFlowCardControllerV3>();
        if (v3 == null) return;

        var so = new SerializedObject(v3);

        // _posterImage → PosterImage (RawImage)
        var posterRawImage = poster.GetComponent<RawImage>();
        if (posterRawImage != null)
        {
            var prop = so.FindProperty("_posterImage");
            if (prop != null) { prop.objectReferenceValue = posterRawImage; }
        }

        // _bandLogoImage → BandLogoImage (Image)
        var bandLogoImage = bandLogo.GetComponent<Image>();
        if (bandLogoImage != null)
        {
            var prop = so.FindProperty("_bandLogoImage");
            if (prop != null) { prop.objectReferenceValue = bandLogoImage; }
        }

        // _gigTitleText → GigTitleText (TextMeshProUGUI)
        var gigTitleTmp = gigTitle.GetComponent<TextMeshProUGUI>();
        if (gigTitleTmp != null)
        {
            var prop = so.FindProperty("_gigTitleText");
            if (prop != null) { prop.objectReferenceValue = gigTitleTmp; }
        }

        // _tracklistText → TracklistText (TextMeshProUGUI)
        var tracklistTmp = tracklistText.GetComponent<TextMeshProUGUI>();
        if (tracklistTmp != null)
        {
            var prop = so.FindProperty("_tracklistText");
            if (prop != null) { prop.objectReferenceValue = tracklistTmp; }
        }

        // _avgTrackText → AvgTrackRow/AvgTrackText (TextMeshProUGUI)
        var avgTrackRow = footer.transform.Find("AvgTrackRow");
        if (avgTrackRow != null)
        {
            var avgTrackText = avgTrackRow.Find("AvgTrackText");
            if (avgTrackText != null)
            {
                var avgTmp = avgTrackText.GetComponent<TextMeshProUGUI>();
                if (avgTmp != null)
                {
                    var prop = so.FindProperty("_avgTrackText");
                    if (prop != null) { prop.objectReferenceValue = avgTmp; }
                }
            }
        }

        // _intensityText → IntensityRow/IntensityText (TextMeshProUGUI)
        var intensityRow = footer.transform.Find("IntensityRow");
        if (intensityRow != null)
        {
            var intensityText = intensityRow.Find("IntensityText");
            if (intensityText != null)
            {
                var intTmp = intensityText.GetComponent<TextMeshProUGUI>();
                if (intTmp != null)
                {
                    var prop = so.FindProperty("_intensityText");
                    if (prop != null) { prop.objectReferenceValue = intTmp; }
                }
            }
        }

        so.ApplyModifiedProperties();
    }

    /// <summary>
    /// Auto-wires the CoverFlowCard component's serialized fields
    /// to the child GameObjects created above. Uses SerializedObject to persist
    /// references through Undo.
    /// </summary>
    private static void AutoWireCoverFlowCard(GameObject root, GameObject poster,
        GameObject grayscale, GameObject sash, GameObject accentBorder,
        GameObject lockPanel, GameObject lockIcon, GameObject unlockReqText, GameObject mysteryText,
        GameObject gigTitle, GameObject bandLogo, GameObject activePanel, GameObject footer)
    {
        var card = root.GetComponent<CoverFlowCard>();
        if (card == null) return;

        var so = new SerializedObject(card);

        // _rectTransform → root RectTransform
        var rootRt = root.GetComponent<RectTransform>();
        if (rootRt != null)
        {
            var prop = so.FindProperty("_rectTransform");
            if (prop != null) { prop.objectReferenceValue = rootRt; }
        }

        // _canvasGroup → root CanvasGroup
        var rootCg = root.GetComponent<CanvasGroup>();
        if (rootCg != null)
        {
            var prop = so.FindProperty("_canvasGroup");
            if (prop != null) { prop.objectReferenceValue = rootCg; }
        }

        // _posterImage → PosterImage (RawImage)
        var posterRawImage = poster.GetComponent<RawImage>();
        if (posterRawImage != null)
        {
            var prop = so.FindProperty("_posterImage");
            if (prop != null) { prop.objectReferenceValue = posterRawImage; }
        }

        // _grayscaleOverlay → GrayscaleOverlay (Image)
        var grayscaleImg = grayscale.GetComponent<Image>();
        if (grayscaleImg != null)
        {
            var prop = so.FindProperty("_grayscaleOverlay");
            if (prop != null) { prop.objectReferenceValue = grayscaleImg; }
        }

        // _lockIcon → LockIcon (Image)
        var lockIconImg = lockIcon.GetComponent<Image>();
        if (lockIconImg != null)
        {
            var prop = so.FindProperty("_lockIcon");
            if (prop != null) { prop.objectReferenceValue = lockIconImg; }
        }

        // _accentBorder → AccentBorder (Image)
        var accentBorderImg = accentBorder.GetComponent<Image>();
        if (accentBorderImg != null)
        {
            var prop = so.FindProperty("_accentBorder");
            if (prop != null) { prop.objectReferenceValue = accentBorderImg; }
        }

        // _gigTitleText → GigTitleText (TextMeshProUGUI)
        var gigTitleTmp = gigTitle.GetComponent<TextMeshProUGUI>();
        if (gigTitleTmp != null)
        {
            var prop = so.FindProperty("_gigTitleText");
            if (prop != null) { prop.objectReferenceValue = gigTitleTmp; }
        }

        // _bandLogoImage → BandLogoImage (Image)
        var bandLogoImg = bandLogo.GetComponent<Image>();
        if (bandLogoImg != null)
        {
            var prop = so.FindProperty("_bandLogoImage");
            if (prop != null) { prop.objectReferenceValue = bandLogoImg; }
        }

        // _activeContentPanel → ActiveContentPanel (GameObject)
        if (activePanel != null)
        {
            var prop = so.FindProperty("_activeContentPanel");
            if (prop != null) { prop.objectReferenceValue = activePanel; }
        }

        // _woodenFrame → WoodenFrame (Image)
        var woodenFrame = root.transform.Find("WoodenFrame");
        if (woodenFrame != null)
        {
            var wfImg = woodenFrame.GetComponent<Image>();
            if (wfImg != null)
            {
                var prop = so.FindProperty("_woodenFrame");
                if (prop != null) { prop.objectReferenceValue = wfImg; }
            }
        }

        // _soldOutSash → SoldOutSash (Image)
        var sashImg = sash.GetComponent<Image>();
        if (sashImg != null)
        {
            var prop = so.FindProperty("_soldOutSash");
            if (prop != null) { prop.objectReferenceValue = sashImg; }
        }

        // _lockPanel → LockPanel (GameObject)
        if (lockPanel != null)
        {
            var prop = so.FindProperty("_lockPanel");
            if (prop != null) { prop.objectReferenceValue = lockPanel; }
        }

        // _unlockRequirementText → UnlockRequirementText (TextMeshProUGUI)
        var unlockTmp = unlockReqText.GetComponent<TextMeshProUGUI>();
        if (unlockTmp != null)
        {
            var prop = so.FindProperty("_unlockRequirementText");
            if (prop != null) { prop.objectReferenceValue = unlockTmp; }
        }

        // _mysteryTracklistText → MysteryTracklistText (TextMeshProUGUI)
        var mysteryTmp = mysteryText.GetComponent<TextMeshProUGUI>();
        if (mysteryTmp != null)
        {
            var prop = so.FindProperty("_mysteryTracklistText");
            if (prop != null) { prop.objectReferenceValue = mysteryTmp; }
        }

        // _hypeSlot1Text → AvgTrackText (same TMP as _avgTrackText)
        // _avgTrackText → AvgTrackRow/AvgTrackText (TextMeshProUGUI)
        var avgTrackRow = footer.transform.Find("AvgTrackRow");
        if (avgTrackRow != null)
        {
            var avgTrackText = avgTrackRow.Find("AvgTrackText");
            if (avgTrackText != null)
            {
                var avgTmp = avgTrackText.GetComponent<TextMeshProUGUI>();
                if (avgTmp != null)
                {
                    var hype1Prop = so.FindProperty("_hypeSlot1Text");
                    if (hype1Prop != null) { hype1Prop.objectReferenceValue = avgTmp; }

                    var avgProp = so.FindProperty("_avgTrackText");
                    if (avgProp != null) { avgProp.objectReferenceValue = avgTmp; }
                }
            }
        }

        // _hypeSlot2Text → IntensityText (same TMP as _intensityText)
        // _intensityText → IntensityRow/IntensityText (TextMeshProUGUI)
        var intensityRow = footer.transform.Find("IntensityRow");
        if (intensityRow != null)
        {
            var intensityText = intensityRow.Find("IntensityText");
            if (intensityText != null)
            {
                var intTmp = intensityText.GetComponent<TextMeshProUGUI>();
                if (intTmp != null)
                {
                    var hype2Prop = so.FindProperty("_hypeSlot2Text");
                    if (hype2Prop != null) { hype2Prop.objectReferenceValue = intTmp; }

                    var intProp = so.FindProperty("_intensityText");
                    if (intProp != null) { intProp.objectReferenceValue = intTmp; }
                }
            }
        }

        so.ApplyModifiedProperties();
    }

    private static GameObject CreateImage(GameObject parent, string name, bool stretch)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);
        if (stretch) StretchRect(go.GetComponent<RectTransform>());
        return go;
    }

    private static GameObject CreateTMP(GameObject parent, string name, string text)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        go.GetComponent<TextMeshProUGUI>().text = text;
        return go;
    }

    private static void CreateRow(GameObject parent, string rowName, string iconName, string textName, string defaultText)
    {
        GameObject row = new GameObject(rowName, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(parent.transform, false);
        
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 8;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        GameObject icon = CreateImage(row, iconName, false);
        var iconLE = icon.AddComponent<LayoutElement>();
        iconLE.minWidth = 24;
        iconLE.minHeight = 24;
        iconLE.preferredWidth = 24;
        iconLE.preferredHeight = 24;

        GameObject textObj = CreateTMP(row, textName, defaultText);
        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        
        // CRITICAL FIX 2: Auto-Sizing ensures long text shrinks instead of blowing out the layout
        tmp.enableAutoSizing = true;
        tmp.fontSizeMin = 12;
        tmp.fontSizeMax = 18; 
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = false;
        
        var textLE = textObj.AddComponent<LayoutElement>();
        textLE.flexibleWidth = 1; // Allow text to take remaining horizontal space safely
    }

    private static void ConfigureTextElement(TextMeshProUGUI tmp, float size, TextAlignmentOptions align, bool wrap)
    {
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.enableWordWrapping = wrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
    }

    private static void StretchRect(RectTransform rt, RectOffset offset = null)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        if (offset != null)
        {
            rt.offsetMin = new Vector2(offset.left, offset.bottom);
            rt.offsetMax = new Vector2(-offset.right, -offset.top);
        }
        else
        {
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        rt.anchoredPosition = Vector2.zero;
    }
}