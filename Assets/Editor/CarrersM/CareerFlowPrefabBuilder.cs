using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using YARG.Menu;
using YARG.Menu.Career;
using YARG.Menu.Career.CareerFlow;
using YARG.Menu.Career.CoverFlow;

/// <summary>
/// Editor automation for the CareerFlow UI system.
///
/// Provides three menu items under Tools/YARG/:
///   1. "Build Career Gear Crate Card" — creates a single CareerGearCrateCard GameObject
///      with the full crate frame / box art / dashboard hierarchy, fully wired.
///   2. "Build Career Career Menu" — creates the complete CareerCareerMenuModern
///      hierarchy with 7 card slots, bootstrapper, background, and controller.
///   3. "Register CareerCareerModern Menu" — ensures a MenuObject with
///      Menu.CareerCareerModern exists in the scene.
///
/// All operations are Undo-aware and use SerializedObject for persistent references.
/// </summary>
public class CareerFlowPrefabBuilder
{
    // ── Card Dimensions ───────────────────────────────────────────

    private const float CARD_WIDTH  = 460f;
    private const float CARD_HEIGHT = 720f;

    // ── Menu Item 1: Build Single Gear Crate Card ─────────────────

    [MenuItem("Tools/YARG/Build Career Gear Crate Card", false, 100)]
    public static void BuildCareerGearCrateCard()
    {
        // 1. Root — CareerFlowCard (subclass of CoverFlowCard) + CareerFlowController
        GameObject root = new GameObject("CareerGearCrateCard",
            typeof(RectTransform), typeof(CanvasGroup),
            typeof(CareerFlowCard), typeof(CareerFlowController));
        Undo.RegisterCreatedObjectUndo(root, "Build Career Gear Crate Card");

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);

        var arf = root.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = CARD_WIDTH / CARD_HEIGHT;

        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.GetComponent<RectTransform>() != null &&
            !PrefabUtility.IsPartOfPrefabAsset(Selection.activeGameObject))
        {
            root.transform.SetParent(Selection.activeGameObject.transform, false);
        }

        // 2. Crate Frame — rugged black flight-case metallic border
        GameObject crateFrame = CreateImage(root, "CrateFrame", true);
        var crateFrameImg = crateFrame.GetComponent<Image>();
        crateFrameImg.color = new Color(0.08f, 0.08f, 0.10f); // near-black metallic

        // 3. Box Art — clean, unobstructed, top ~80% of card
        GameObject boxArt = new GameObject("BoxArt", typeof(RectTransform), typeof(RawImage));
        boxArt.transform.SetParent(root.transform, false);
        // Inset 18px from edges for the crate frame border
        StretchRect(boxArt.GetComponent<RectTransform>(),
            new RectOffset(18, 18, 18, 100)); // bottom inset leaves room for dashboard

        var boxArtRawImage = boxArt.GetComponent<RawImage>();
        boxArtRawImage.color = Color.white;

        // 4. Bottom Gradient Overlay — subtle fade behind status text
        GameObject bottomGradient = CreateImage(root, "BottomGradient", false);
        var gradRt = bottomGradient.GetComponent<RectTransform>();
        gradRt.anchorMin = new Vector2(0f, 0f);
        gradRt.anchorMax = new Vector2(1f, 0.22f);
        gradRt.offsetMin = new Vector2(18f, 18f);
        gradRt.offsetMax = new Vector2(-18f, 0f);
        var gradImg = bottomGradient.GetComponent<Image>();
        gradImg.color = new Color(0f, 0f, 0f, 0.5f);

        // 5. Dashboard Panel — integrated into lower rim
        GameObject dashboardPanel = new GameObject("DashboardPanel", typeof(RectTransform));
        dashboardPanel.transform.SetParent(root.transform, false);
        var dashRt = dashboardPanel.GetComponent<RectTransform>();
        dashRt.anchorMin = new Vector2(0f, 0f);
        dashRt.anchorMax = new Vector2(1f, 0.14f);
        dashRt.offsetMin = new Vector2(18f, 4f);
        dashRt.offsetMax = new Vector2(-18f, -4f);

        var dashVlg = dashboardPanel.AddComponent<VerticalLayoutGroup>();
        dashVlg.childAlignment = TextAnchor.MiddleCenter;
        dashVlg.spacing = 2;
        dashVlg.childControlWidth = true;
        dashVlg.childControlHeight = true;
        dashVlg.childForceExpandWidth = true;
        dashVlg.childForceExpandHeight = false;

        // 5a. Status Line — narrative text (e.g. "• 5 GIGS REMAINING •")
        GameObject statusLine = CreateTMP(dashboardPanel, "StatusLine", "• 5 GIGS REMAINING •");
        var statusTmp = statusLine.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(statusTmp, 16, TextAlignmentOptions.Center, false);
        statusTmp.fontStyle = FontStyles.Bold;

        // 5b. Progress Bar Row
        GameObject progressRow = new GameObject("ProgressRow", typeof(RectTransform),
            typeof(HorizontalLayoutGroup));
        progressRow.transform.SetParent(dashboardPanel.transform, false);
        var prHlg = progressRow.GetComponent<HorizontalLayoutGroup>();
        prHlg.childAlignment = TextAnchor.MiddleCenter;
        prHlg.spacing = 6;
        prHlg.childControlWidth = true;
        prHlg.childControlHeight = true;
        prHlg.childForceExpandWidth = false;
        prHlg.childForceExpandHeight = false;

        // Progress Bar Background
        GameObject progressBg = CreateImage(progressRow, "ProgressBarBg", false);
        var pbgRt = progressBg.GetComponent<RectTransform>();
        pbgRt.sizeDelta = new Vector2(200f, 10f);
        var pbgLE = progressBg.AddComponent<LayoutElement>();
        pbgLE.minWidth = 200f;
        pbgLE.preferredWidth = 200f;
        var pbgImg = progressBg.GetComponent<Image>();
        pbgImg.color = new Color(0.2f, 0.2f, 0.25f);

        // Progress Bar Fill (child of background for fill masking)
        GameObject progressFill = CreateImage(progressBg, "ProgressBarFill", false);
        var pfillRt = progressFill.GetComponent<RectTransform>();
        pfillRt.anchorMin = Vector2.zero;
        pfillRt.anchorMax = Vector2.one;
        pfillRt.offsetMin = Vector2.zero;
        pfillRt.offsetMax = Vector2.zero;
        var pfillImg = progressFill.GetComponent<Image>();
        pfillImg.type = Image.Type.Filled;
        pfillImg.fillMethod = Image.FillMethod.Horizontal;
        pfillImg.fillOrigin = 0;
        pfillImg.fillAmount = 0.22f;
        pfillImg.color = new Color(0f, 0.6f, 1f); // accent blue

        // Progress Label
        GameObject progressLabel = CreateTMP(progressRow, "ProgressLabel", "COMPLETION: 22%");
        var plTmp = progressLabel.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(plTmp, 14, TextAlignmentOptions.Left, false);

        // 6. Career Name Text — optional, hidden by default
        GameObject careerName = CreateTMP(root, "CareerNameText", "WORLD TOUR");
        var cnRt = careerName.GetComponent<RectTransform>();
        cnRt.anchorMin = new Vector2(0f, 0.82f);
        cnRt.anchorMax = new Vector2(1f, 0.95f);
        cnRt.offsetMin = new Vector2(20f, 0f);
        cnRt.offsetMax = new Vector2(-20f, 0f);
        var cnTmp = careerName.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(cnTmp, 28, TextAlignmentOptions.Center, true);
        cnTmp.fontStyle = FontStyles.Bold;
        careerName.SetActive(false); // hidden by default

        // 7. Completion Stamp — "TOUR COMPLETE" overlay
        GameObject completionStamp = CreateImage(root, "CompletionStamp", true);
        var csImg = completionStamp.GetComponent<Image>();
        csImg.color = new Color(1f, 0.84f, 0f, 0.7f); // gold, semi-transparent
        completionStamp.SetActive(false);

        // ── Auto-wire CareerFlowController serialized references ──
        AutoWireCareerFlowController(root, boxArt, crateFrame, statusLine,
            progressFill, progressLabel, careerName, bottomGradient);

        // ── Auto-wire CareerFlowCard serialized references ──
        AutoWireCareerFlowCard(root, crateFrame, completionStamp, dashboardPanel);

        Selection.activeGameObject = root;
        Debug.Log("[CareerFlowPrefabBuilder] CareerGearCrateCard built and wired. " +
            "Assign sprites/materials to CrateFrame, BoxArt, and CompletionStamp in the Inspector.");
    }

    // ── Menu Item 2: Build Complete Career Career Menu ────────────

    [MenuItem("Tools/YARG/Build Career Career Menu", false, 101)]
    public static void BuildCareerCareerMenu()
    {
        // 1. Root Menu GameObject — NOT a Canvas (plain RectTransform).
        //    Canvas is on CoverFlowUI child, matching CareerGigMenuModern architecture.
        GameObject rootMenu = new GameObject("CareerCareerMenuModern",
            typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(rootMenu, "Build Career Career Menu");

        // Add CareerFlowBootstrapper to root
        var bootstrapper = rootMenu.AddComponent<CareerFlowBootstrapper>();

        // 2. CoverFlowBackground Child (reused as-is)
        GameObject bgGo = new GameObject("CoverFlowBackground",
            typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(rootMenu.transform, false);
        StretchRect(bgGo.GetComponent<RectTransform>());
        var backgroundComp = bgGo.AddComponent<CoverFlowBackground>();

        // 3. Header Text
        GameObject headerTextGo = CreateTMP(rootMenu, "HeaderText", "CHOOSE A CAREER");
        var headerRt = headerTextGo.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 0.88f);
        headerRt.anchorMax = new Vector2(1f, 0.98f);
        headerRt.offsetMin = Vector2.zero;
        headerRt.offsetMax = Vector2.zero;
        var headerTmp = headerTextGo.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(headerTmp, 36, TextAlignmentOptions.Center, true);
        headerTmp.fontStyle = FontStyles.Bold;

        GameObject bandNameGo = CreateTMP(rootMenu, "BandNameText", "MY BAND");
        var bandNameRt = bandNameGo.GetComponent<RectTransform>();
        bandNameRt.anchorMin = new Vector2(0f, 0.82f);
        bandNameRt.anchorMax = new Vector2(1f, 0.88f);
        bandNameRt.offsetMin = Vector2.zero;
        bandNameRt.offsetMax = Vector2.zero;
        var bandNameTmp = bandNameGo.GetComponent<TextMeshProUGUI>();
        ConfigureTextElement(bandNameTmp, 24, TextAlignmentOptions.Center, true);
        bandNameTmp.fontStyle = FontStyles.Bold;

        var bandPopupPrefab = AssetDatabase.LoadAssetAtPath<BandSelectionPopup>(
            "Assets/Prefabs/CareersM/Submenus/BandSelectionPopupv2.prefab");
        BandSelectionPopup bandPopupInstance = null;
        if (bandPopupPrefab != null)
        {
            bandPopupInstance = (BandSelectionPopup)PrefabUtility.InstantiatePrefab(bandPopupPrefab, rootMenu.transform);
            bandPopupInstance.gameObject.SetActive(false);
        }

        // 4. CareerFlowMenuController Child
        GameObject controllerGo = new GameObject("CareerFlowMenuController");
        controllerGo.transform.SetParent(rootMenu.transform, false);
        var menuController = controllerGo.AddComponent<CareerFlowMenuController>();

        // 5. CoverFlowUI Layer Container (holds the 7 cards)
        //    This child has the Canvas (WorldSpace) scaled 0.01 to convert
        //    1920x1080 pixel coords to world-space units — matching CareerGigMenuModern.
        GameObject uiContainer = new GameObject("CoverFlowUI",
            typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        uiContainer.transform.SetParent(rootMenu.transform, false);

        // Canvas: WorldSpace with sorting above default UI
        Canvas cfCanvas = uiContainer.GetComponent<Canvas>();
        cfCanvas.renderMode = RenderMode.WorldSpace;
        cfCanvas.sortingOrder = 10;

        // RectTransform: 1920x1080 at scale 0.01 → effective 19.2 x 10.8 world units
        RectTransform cfRt = uiContainer.GetComponent<RectTransform>();
        cfRt.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        cfRt.anchorMin = new Vector2(0.5f, 0.5f);
        cfRt.anchorMax = new Vector2(0.5f, 0.5f);
        cfRt.pivot = new Vector2(0.5f, 0.5f);
        cfRt.sizeDelta = new Vector2(1920f, 1080f);

        // 6. Instantiate 7 card slots
        //    Try to load the CareerGearCrateCard prefab, or create from scratch
        GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/CareersM/01_CareerViews/CareerFlow/CareerGearCrateCard.prefab");

        CareerFlowCard[] spawnedCards = new CareerFlowCard[7];

        for (int i = 0; i < 7; i++)
        {
            GameObject cardInstance;
            if (cardPrefab != null)
            {
                cardInstance = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, uiContainer.transform);
            }
            else
            {
                // Fallback: create from scratch if prefab doesn't exist yet
                cardInstance = new GameObject($"CareerGearCrateCard_Slot{i}",
                    typeof(RectTransform), typeof(CanvasGroup),
                    typeof(CareerFlowCard), typeof(CareerFlowController));
                cardInstance.transform.SetParent(uiContainer.transform, false);
                cardInstance.GetComponent<RectTransform>().sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);
            }

            cardInstance.name = $"CareerGearCrateCard_Slot{i}";

            var cfCard = cardInstance.GetComponent<CareerFlowCard>();
            if (cfCard == null)
                cfCard = cardInstance.AddComponent<CareerFlowCard>();

            spawnedCards[i] = cfCard;
        }

        // ── Wire Bootstrapper ──
        var bootSo = new SerializedObject(bootstrapper);
        SetFieldValue(bootSo, "_careerFlowMenuController", menuController);
        SetFieldValue(bootSo, "_coverFlowBackground", backgroundComp);
        SetFieldValue(bootSo, "_bandSelectionPopupPrefab", bandPopupPrefab);
        SetArrayValues(bootSo, "_cardSlots", spawnedCards);
        bootSo.ApplyModifiedProperties();

        // ── Wire Menu Controller ──
        var mcSo = new SerializedObject(menuController);
        SetFieldValue(mcSo, "_background", backgroundComp);
        SetFieldValue(mcSo, "_headerText", headerTextGo.GetComponent<TextMeshProUGUI>());
        SetFieldValue(mcSo, "_bandNameText", bandNameGo.GetComponent<TextMeshProUGUI>());
        SetFieldValue(mcSo, "_bandSelectionPopup", bandPopupInstance);
        SetFieldValue(mcSo, "_targetCanvas", cfCanvas);
        SetArrayValues(mcSo, "_cardSlots", spawnedCards);
        SetNumericField(mcSo, "_transitionDuration", 0.18f);
        SetNumericField(mcSo, "_overshootDuration", 0.08f);
        SetNumericField(mcSo, "_overshootAmount", 0.025f);
        SetNumericField(mcSo, "_scrollSfxCooldown", 0.1f);

        // Set default position offset (centered for World Space canvas)
        var posOffsetProp = mcSo.FindProperty("_positionOffset");
        if (posOffsetProp != null)
        {
            posOffsetProp.vector3Value = Vector3.zero;
        }

        // Setup default easing curve
        var curveProp = mcSo.FindProperty("_easingCurve");
        if (curveProp != null)
        {
            curveProp.animationCurveValue = new AnimationCurve(
                new Keyframe(0f, 0f, 0f, 3f),
                new Keyframe(1f, 1f, 0f, 0f));
        }
        mcSo.ApplyModifiedProperties();

        Selection.activeGameObject = rootMenu;
        Debug.Log("[CareerFlowPrefabBuilder] CareerCareerMenuModern built and fully wired with 7 card slots. " +
            "Save as prefab: Assets/Prefabs/CareersM/01_CareerViews/CareerFlow/CareerCareerMenuModern.prefab");
    }

    // ── Menu Item 3: Register Menu Object ─────────────────────────

    [MenuItem("Tools/YARG/Register CareerCareerModern Menu", false, 102)]
    public static void RegisterCareerCareerModernMenu()
    {
        // Find or create the MenuManager in the scene
        var menuManager = Object.FindObjectOfType<MenuManager>();
        if (menuManager == null)
        {
            Debug.LogError("[CareerFlowPrefabBuilder] No MenuManager found in scene. " +
                "Open the Menu scene first.");
            return;
        }

        // Check if CareerCareerModern is already registered
        var existing = menuManager.GetComponentsInChildren<MenuObject>(true);
        foreach (var mo in existing)
        {
            if (mo.Menu == MenuManager.Menu.CareerCareerModern)
            {
                Debug.Log("[CareerFlowPrefabBuilder] CareerCareerModern menu is already registered.");
                Selection.activeGameObject = mo.gameObject;
                return;
            }
        }

        // Create a new MenuObject child
        GameObject menuObjGo = new GameObject("CareerCareerModern_MenuObject",
            typeof(RectTransform), typeof(MenuObject));
        Undo.RegisterCreatedObjectUndo(menuObjGo, "Register CareerCareerModern Menu");
        menuObjGo.transform.SetParent(menuManager.transform, false);

        var menuObject = menuObjGo.GetComponent<MenuObject>();

        // Set the Menu enum via SerializedObject
        var so = new SerializedObject(menuObject);
        var menuProp = so.FindProperty("_menu");
        if (menuProp != null)
        {
            menuProp.enumValueIndex = (int)MenuManager.Menu.CareerCareerModern;
        }
        so.ApplyModifiedProperties();

        // Try to load and assign the prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/CareersM/01_CareerViews/CareerFlow/CareerCareerMenuModern.prefab");
        if (prefab != null)
        {
            var prefabProp = so.FindProperty("_prefab");
            if (prefabProp != null)
            {
                // Instantiate the prefab as a child
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, menuObjGo.transform);
                instance.name = "CareerCareerMenuModern_Instance";
            }
        }

        Selection.activeGameObject = menuObjGo;
        Debug.Log("[CareerFlowPrefabBuilder] CareerCareerModern MenuObject registered. " +
            "Assign the CareerCareerMenuModern prefab in the Inspector if not auto-assigned.");
    }

    // ── Auto-Wire Helpers ─────────────────────────────────────────

    private static void AutoWireCareerFlowController(GameObject root,
        GameObject boxArt, GameObject crateFrame, GameObject statusLine,
        GameObject progressFill, GameObject progressLabel, GameObject careerName,
        GameObject bottomGradient)
    {
        var cfController = root.GetComponent<CareerFlowController>();
        if (cfController == null) return;

        var so = new SerializedObject(cfController);

        // _boxArtImage → BoxArt (RawImage)
        var boxArtRi = boxArt.GetComponent<RawImage>();
        if (boxArtRi != null)
            SetFieldValue(so, "_boxArtImage", boxArtRi);

        // _crateFrame → CrateFrame (Image)
        var crateFrameImg = crateFrame.GetComponent<Image>();
        if (crateFrameImg != null)
            SetFieldValue(so, "_crateFrame", crateFrameImg);

        // _statusLineText → StatusLine (TMP)
        var statusTmp = statusLine.GetComponent<TextMeshProUGUI>();
        if (statusTmp != null)
            SetFieldValue(so, "_statusLineText", statusTmp);

        // _progressBarFill → ProgressBarFill (Image)
        var pfillImg = progressFill.GetComponent<Image>();
        if (pfillImg != null)
            SetFieldValue(so, "_progressBarFill", pfillImg);

        // _progressLabel → ProgressLabel (TMP)
        var plTmp = progressLabel.GetComponent<TextMeshProUGUI>();
        if (plTmp != null)
            SetFieldValue(so, "_progressLabel", plTmp);

        // _careerNameText → CareerNameText (TMP)
        var cnTmp = careerName.GetComponent<TextMeshProUGUI>();
        if (cnTmp != null)
            SetFieldValue(so, "_careerNameText", cnTmp);

        // _bottomGradientOverlay → BottomGradient (Image)
        var gradImg = bottomGradient.GetComponent<Image>();
        if (gradImg != null)
            SetFieldValue(so, "_bottomGradientOverlay", gradImg);

        so.ApplyModifiedProperties();
    }

    private static void AutoWireCareerFlowCard(GameObject root,
        GameObject crateFrame, GameObject completionStamp, GameObject dashboardPanel)
    {
        var cfCard = root.GetComponent<CareerFlowCard>();
        if (cfCard == null) return;

        var so = new SerializedObject(cfCard);

        // _rectTransform → root RectTransform (inherited from CoverFlowCard)
        var rootRt = root.GetComponent<RectTransform>();
        if (rootRt != null)
            SetFieldValue(so, "_rectTransform", rootRt);

        // _canvasGroup → root CanvasGroup (inherited from CoverFlowCard)
        var rootCg = root.GetComponent<CanvasGroup>();
        if (rootCg != null)
            SetFieldValue(so, "_canvasGroup", rootCg);

        // _crateFrame → CrateFrame (Image)
        var crateFrameImg = crateFrame.GetComponent<Image>();
        if (crateFrameImg != null)
            SetFieldValue(so, "_crateFrame", crateFrameImg);

        // _completionStamp → CompletionStamp (Image)
        var csImg = completionStamp.GetComponent<Image>();
        if (csImg != null)
            SetFieldValue(so, "_completionStamp", csImg);

        // _dashboardPanel → DashboardPanel (GameObject)
        if (dashboardPanel != null)
            SetFieldValue(so, "_dashboardPanel", dashboardPanel);

        so.ApplyModifiedProperties();
    }

    // ── SerializedObject Helpers ──────────────────────────────────

    private static void SetFieldValue(SerializedObject so, string fieldName, Object value)
    {
        var prop = so.FindProperty(fieldName);
        if (prop != null) prop.objectReferenceValue = value;
    }

    private static void SetNumericField(SerializedObject so, string fieldName, float value)
    {
        var prop = so.FindProperty(fieldName);
        if (prop != null) prop.floatValue = value;
    }

    private static void SetArrayValues(SerializedObject so, string fieldName, Component[] elements)
    {
        var arrayProp = so.FindProperty(fieldName);
        if (arrayProp == null || !arrayProp.isArray) return;

        arrayProp.ClearArray();
        for (int i = 0; i < elements.Length; i++)
        {
            arrayProp.InsertArrayElementAtIndex(i);
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = elements[i];
        }
    }

    // ── GameObject Creation Helpers ───────────────────────────────

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

    private static void ConfigureTextElement(TextMeshProUGUI tmp, float size,
        TextAlignmentOptions align, bool wrap)
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
    }
}