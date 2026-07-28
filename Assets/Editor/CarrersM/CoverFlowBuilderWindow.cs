using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TRUE 3D CoverFlow scene generator.
/// Creates a standalone WorldSpace scene with dedicated camera, layer masking,
/// studio lighting, and 7 cards in a parabolic 3D arc.
///
/// IMPORTANT: The generated scene is a STANDALONE root GameObject.
/// Do NOT place it as a child of any ScreenSpaceOverlay canvas.
/// Place it as a sibling of GigView under your Menu Manager.
/// </summary>
public class CoverFlowBuilderWindow : EditorWindow
{
    private GameObject cardPrefab;
    private float canvasWorldScale = 0.005f;
    private const string COVERFLOW_LAYER = "CoverFlow";

    [MenuItem("Tools/CoverFlow V3/Scene Generator")]
    public static void ShowWindow()
    {
        GetWindow<CoverFlowBuilderWindow>("CoverFlow Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("CoverFlow V3 — TRUE 3D Scene Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        cardPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Card Prefab (optional)", cardPrefab, typeof(GameObject), false);

        if (cardPrefab == null)
        {
            EditorGUILayout.HelpBox(
                "No prefab — cards auto-created inline.\n" +
                "For reusable prefab: GameObject > UI > Create Perfect CoverFlow Card",
                MessageType.Info);
        }

        EditorGUILayout.Space();
        canvasWorldScale = EditorGUILayout.Slider("Canvas World Scale", canvasWorldScale, 0.001f, 0.02f);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "GENERATED SCENE IS STANDALONE — place as SIBLING of GigView, NOT as a child!\n" +
            "The CoverFlow camera renders only the 'CoverFlow' layer.",
            MessageType.Warning);

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.3f);
        if (GUILayout.Button("GENERATE TRUE 3D SCENE", GUILayout.Height(40)))
        {
            BuildScene();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
        if (GUILayout.Button("DELETE EXISTING SCENE", GUILayout.Height(25)))
        {
            var existing = GameObject.Find("CareerGigMenu");
            if (existing != null) { Undo.DestroyObjectImmediate(existing); }
        }
        GUI.backgroundColor = Color.white;
    }

    private void BuildScene()
    {
        // Ensure CoverFlow layer exists
        EnsureLayerExists(COVERFLOW_LAYER);
        int cfLayer = LayerMask.NameToLayer(COVERFLOW_LAYER);

        // Clean up
        var existing = GameObject.Find("CareerGigMenu");
        if (existing != null) DestroyImmediate(existing);

        // ── 1. Root ──
        GameObject root = new GameObject("CareerGigMenu");
        Undo.RegisterCreatedObjectUndo(root, "Generate TRUE 3D CoverFlow");
        SetLayerRecursive(root, cfLayer);

        // ── 2. Dedicated Camera ──
        GameObject camGO = new GameObject("CoverFlow_RenderCamera");
        camGO.transform.SetParent(root.transform);
        camGO.transform.localPosition = new Vector3(0, 0, -7.5f);
        camGO.layer = cfLayer;
        Camera cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
        cam.fieldOfView = 32f;
        cam.cullingMask = 1 << cfLayer; // ONLY render CoverFlow layer
        cam.depth = 10; // Render on top of main UI camera

        // ── 3. Studio Lighting ──
        GameObject lightGroup = new GameObject("StudioLightingRig");
        lightGroup.transform.SetParent(root.transform);
        lightGroup.layer = cfLayer;

        GameObject keyLight = new GameObject("KeySpotlight_Hero");
        keyLight.transform.SetParent(lightGroup.transform);
        keyLight.transform.localPosition = new Vector3(0, 4f, -3f);
        keyLight.transform.localRotation = Quaternion.Euler(45, 0, 0);
        keyLight.layer = cfLayer;
        Light spot = keyLight.AddComponent<Light>();
        spot.type = LightType.Spot; spot.intensity = 3.5f; spot.spotAngle = 55f;
        spot.cullingMask = 1 << cfLayer;

        GameObject rimLight = new GameObject("MetallicBlue_RimLight");
        rimLight.transform.SetParent(lightGroup.transform);
        rimLight.transform.localPosition = new Vector3(-5, 3, 4);
        rimLight.layer = cfLayer;
        Light rim = rimLight.AddComponent<Light>();
        rim.type = LightType.Point; rim.color = new Color(0.15f, 0.45f, 1f); rim.intensity = 2f;
        rim.cullingMask = 1 << cfLayer;

        // ── 4. Background ──
        Component bgComp = CreateBackground(root, cfLayer);

        // ── 5. Header UI ──
        var headerTexts = CreateHeaderUI(root, cfLayer);

        // ── 6. Controller ──
        var (ctrlComp, inputComp) = CreateController(root, cfLayer);

        // ── 7. WorldSpace Canvas + 7 Cards ──
        Canvas canvas;
        Component[] cards;
        CreateCanvasAndCards(root, cfLayer, out canvas, out cards);

        // ── 8. Bootstrapper ──
        Component bootComp = CreateBootstrapper(root, cfLayer);

        // ── 9. Wire Everything ──
        WireController(ctrlComp, bgComp, canvas, inputComp,
            headerTexts.careerName, headerTexts.bandName, headerTexts.progress, cards);
        WireBootstrapper(bootComp, ctrlComp, bgComp, cards);

        // ── 10. Done ──
        Selection.activeGameObject = root;
        EditorUtility.DisplayDialog("TRUE 3D Scene Generated!",
            "Standalone CoverFlow scene ready.\n\n" +
            "• 7 cards in parabolic 3D arc\n" +
            "• Dedicated camera (FOV=32, CoverFlow layer only)\n" +
            "• Studio lighting (key spot + blue rim)\n" +
            "• WorldSpace canvas (1920×1080, scale=" + canvasWorldScale + ")\n\n" +
            "=== INTEGRATION INSTRUCTIONS ===\n\n" +
            "1. REMOVE CareerGigMenu from inside your GigView prefab\n" +
            "2. Place CareerGigMenu as a SIBLING of GigView\n" +
            "   (both under Menu Manager or Canvas root)\n" +
            "3. Set your main UI camera to EXCLUDE the 'CoverFlow' layer\n" +
            "4. The CoverFlow camera auto-renders on top (depth=10)\n" +
            "5. When use_coverflow=true: activate CareerGigMenu, deactivate GigView",
            "Got it!");
    }

    // ═══════════════════════════════════════════════════════════════
    //  SUBSYSTEMS
    // ═══════════════════════════════════════════════════════════════

    private Component CreateBackground(GameObject root, int layer)
    {
        GameObject bg = new GameObject("CoverFlowBackground", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(root.transform, false);
        bg.transform.localPosition = new Vector3(0, 0, 4f);
        bg.layer = layer;
        RectTransform rt = bg.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        bg.AddComponent<CanvasGroup>();
        var t = ResolveType("YARG.Menu.Career.CoverFlow.CoverFlowBackground");
        Component c = bg.AddComponent(t ?? typeof(MonoBehaviour));
        if (t != null)
        {
            var so = new SerializedObject(c);
            var ip = so.FindProperty("_backgroundImage"); if (ip != null) ip.objectReferenceValue = bg.GetComponent<Image>();
            var cp = so.FindProperty("_canvasGroup"); if (cp != null) cp.objectReferenceValue = bg.GetComponent<CanvasGroup>();
            so.ApplyModifiedProperties();
        }
        return c;
    }

    private (TextMeshProUGUI careerName, TextMeshProUGUI bandName, TextMeshProUGUI progress)
        CreateHeaderUI(GameObject root, int layer)
    {
        GameObject panel = new GameObject("HeaderPanel", typeof(RectTransform));
        panel.transform.SetParent(root.transform, false);
        panel.layer = layer;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.85f); rt.anchorMax = new Vector2(1, 1);
        rt.sizeDelta = Vector2.zero;

        var cn = MakeTMP(panel, "CareerNameText", "FIRSTBAND", 28, TextAlignmentOptions.Left, layer);
        cn.rectTransform.anchoredPosition = new Vector2(50, -30);
        var pr = MakeTMP(panel, "ProgressText", "0/18 COMPLETE", 28, TextAlignmentOptions.Right, layer);
        pr.rectTransform.anchoredPosition = new Vector2(-50, -30);
        var bn = MakeTMP(panel, "BandNameText", "YARG CHAMPION II", 42, TextAlignmentOptions.Center, layer);
        bn.rectTransform.anchoredPosition = new Vector2(0, -40);
        return (cn, bn, pr);
    }

    private (Component ctrl, Component input) CreateController(GameObject root, int layer)
    {
        GameObject go = new GameObject("CoverFlowController");
        go.transform.SetParent(root.transform, false);
        go.layer = layer;
        var ct = ResolveType("YARG.Menu.Career.CoverFlow.CoverFlowController");
        Component cc = go.AddComponent(ct ?? typeof(MonoBehaviour));

        GameObject ig = new GameObject("CoverFlowInputRemapper");
        ig.transform.SetParent(go.transform, false);
        ig.layer = layer;
        var it = ResolveType("YARG.Menu.Career.CoverFlow.CoverFlowInputRemapper");
        Component ic = ig.AddComponent(it ?? typeof(MonoBehaviour));
        return (cc, ic);
    }

    private void CreateCanvasAndCards(GameObject root, int layer, out Canvas canvas, out Component[] cards)
    {
        GameObject uiRoot = new GameObject("CoverFlowUI");
        uiRoot.transform.SetParent(root.transform, false);
        uiRoot.layer = layer;
        canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = Camera.main; // Will be overridden at runtime, but set a default
        var crt = uiRoot.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(1920, 1080);
        uiRoot.transform.localScale = Vector3.one * canvasWorldScale;

        var layouts = new (float x, float y, float z, float yRot, float scale)[]
        {
            (-560f, -40f, 250f, -65f, 0.40f),
            (-390f, -20f, 140f, -52f, 0.52f),
            (-220f, -10f,  55f, -38f, 0.68f),
            (   0f,   0f,   0f,   0f, 1.00f),
            ( 220f, -10f,  55f,  38f, 0.68f),
            ( 390f, -20f, 140f,  52f, 0.52f),
            ( 560f, -40f, 250f,  65f, 0.40f),
        };

        var cardType = ResolveType("YARG.Menu.Career.CoverFlow.CoverFlowCard");
        cards = new Component[7];

        for (int i = 0; i < 7; i++)
        {
            GameObject ci;
            if (cardPrefab != null)
                ci = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, uiRoot.transform);
            else
                ci = BuildCardHierarchy(uiRoot.transform);

            ci.name = $"CoverFlowCard_Slot{i}";
            SetLayerRecursive(ci, layer);

            var (x, y, z, yRot, scale) = layouts[i];
            ci.transform.localPosition = new Vector3(x, y, z);
            ci.transform.localRotation = Quaternion.Euler(0, yRot, 0);
            ci.transform.localScale = Vector3.one * scale;

            Component c = ci.GetComponent(cardType);
            if (c == null) c = ci.AddComponent(cardType ?? typeof(MonoBehaviour));
            cards[i] = c;
        }
    }

    private Component CreateBootstrapper(GameObject root, int layer)
    {
        var t = ResolveType("YARG.Menu.Career.CoverFlow.CoverFlowBootstrapper");
        return root.AddComponent(t ?? typeof(MonoBehaviour));
    }

    // ═══════════════════════════════════════════════════════════════
    //  WIRING
    // ═══════════════════════════════════════════════════════════════

    private void WireController(Component ctrl, Component bg, Canvas canvas, Component input,
        TextMeshProUGUI cn, TextMeshProUGUI bn, TextMeshProUGUI pr, Component[] cards)
    {
        var so = new SerializedObject(ctrl);
        SetRef(so, "_background", bg);
        SetRef(so, "_targetCanvas", canvas);
        SetRef(so, "_inputRemapper", input);
        SetRef(so, "_careerNameText", cn);
        SetRef(so, "_bandNameText", bn);
        SetRef(so, "_progressText", pr);
        SetArray(so, "_cardSlots", cards);
        var cp = so.FindProperty("_easingCurve");
        if (cp != null) cp.animationCurveValue = AnimationCurve.EaseInOut(0, 0, 1, 1);
        so.ApplyModifiedProperties();
    }

    private void WireBootstrapper(Component boot, Component ctrl, Component bg, Component[] cards)
    {
        var so = new SerializedObject(boot);
        SetRef(so, "_coverFlowController", ctrl);
        SetRef(so, "_coverFlowBackground", bg);
        SetArray(so, "_cardSlots", cards);
        so.ApplyModifiedProperties();
    }

    // ═══════════════════════════════════════════════════════════════
    //  CARD HIERARCHY (auto-create when no prefab)
    // ═══════════════════════════════════════════════════════════════

    private GameObject BuildCardHierarchy(Transform parent)
    {
        var cardType = ResolveType("YARG.Menu.Career.CoverFlow.CoverFlowCard");
        var v3Type = ResolveType("YARG.Menu.Career.CoverFlow.CoverFlowCardControllerV3");

        GameObject root = new GameObject("CoverFlowCard", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        if (cardType != null) root.AddComponent(cardType);
        if (v3Type != null) root.AddComponent(v3Type);

        var rrt = root.GetComponent<RectTransform>();
        rrt.sizeDelta = new Vector2(460f, 720f);
        var arf = root.AddComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        arf.aspectRatio = 460f / 720f;

        CreateImageChild(root, "WoodenFrame", true);

        GameObject poster = new GameObject("PosterImage", typeof(RectTransform), typeof(RawImage));
        poster.transform.SetParent(root.transform, false);
        StretchInset(poster.GetComponent<RectTransform>(), 15);

        GameObject grayscale = CreateImageChild(root, "GrayscaleOverlay", true);
        grayscale.SetActive(false);
        GameObject sash = CreateImageChild(root, "SoldOutSash", true);
        sash.SetActive(false);
        GameObject accent = CreateImageChild(root, "AccentBorder", true);
        accent.SetActive(false);

        // Lock panel
        GameObject lockPanel = new GameObject("LockPanel", typeof(RectTransform));
        lockPanel.transform.SetParent(root.transform, false);
        StretchInset(lockPanel.GetComponent<RectTransform>(), 20);
        lockPanel.SetActive(false);
        var lvlg = lockPanel.AddComponent<VerticalLayoutGroup>();
        lvlg.childAlignment = TextAnchor.MiddleCenter; lvlg.spacing = 12;
        lvlg.childControlWidth = true; lvlg.childControlHeight = true;
        lvlg.childForceExpandWidth = true; lvlg.childForceExpandHeight = false;

        GameObject lockIcon = CreateImageChild(lockPanel, "LockIcon", false);
        var lle = lockIcon.AddComponent<LayoutElement>();
        lle.minWidth = 64; lle.minHeight = 64; lle.preferredWidth = 64; lle.preferredHeight = 64;

        var unlockTxt = MakeTMPChild(lockPanel, "UnlockRequirementText", "🔒 UNLOCK: CLEAR GIG 00", 22, TextAlignmentOptions.Center);
        unlockTxt.fontStyle = FontStyles.Bold;
        MakeTMPChild(lockPanel, "MysteryTracklistText", "???", 18, TextAlignmentOptions.Center);

        // Active content
        GameObject activePanel = new GameObject("ActiveContentPanel", typeof(RectTransform));
        activePanel.transform.SetParent(root.transform, false);
        StretchInset(activePanel.GetComponent<RectTransform>(), 25, 30);
        var vlg = activePanel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter; vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        var gigTitle = MakeTMPChild(activePanel, "GigTitleText", "GUITAR HERO:\nMETALLICA", 32, TextAlignmentOptions.Center);
        gigTitle.fontStyle = FontStyles.Bold;
        var tle = gigTitle.gameObject.AddComponent<LayoutElement>();
        tle.minHeight = 80; tle.flexibleHeight = 0;

        GameObject bandLogo = CreateImageChild(activePanel, "BandLogoImage", false);
        var ble = bandLogo.AddComponent<LayoutElement>();
        ble.minHeight = 90; ble.flexibleHeight = 0;

        GameObject trackContainer = new GameObject("TracklistContainer", typeof(RectTransform));
        trackContainer.transform.SetParent(activePanel.transform, false);
        var tcle = trackContainer.AddComponent<LayoutElement>();
        tcle.flexibleHeight = 1;
        var tvlg = trackContainer.AddComponent<VerticalLayoutGroup>();
        tvlg.childAlignment = TextAnchor.UpperCenter;
        tvlg.childControlWidth = true; tvlg.childControlHeight = true;

        var trackText = MakeTMPChild(trackContainer, "TracklistText",
            "1. Am I Evil?\n2. Blood and Thunder\n3. Stone Cold Crazy", 22, TextAlignmentOptions.Center);
        trackText.lineSpacing = 10;

        // Footer
        GameObject footer = new GameObject("MetadataFooter", typeof(RectTransform));
        footer.transform.SetParent(activePanel.transform, false);
        var fle = footer.AddComponent<LayoutElement>();
        fle.minHeight = 70; fle.flexibleHeight = 0;
        var fvlg = footer.AddComponent<VerticalLayoutGroup>();
        fvlg.childAlignment = TextAnchor.MiddleCenter; fvlg.spacing = 4;
        fvlg.childControlWidth = true; fvlg.childControlHeight = true;
        fvlg.childForceExpandWidth = true; fvlg.childForceExpandHeight = false;

        CreateStatRow(footer, "AvgTrackRow", "ClockIcon", "AvgTrackText", "AVG. TRACK: ~3 MINS");
        CreateStatRow(footer, "IntensityRow", "SunglassesIcon", "IntensityText", "SETLIST INTENSITY: CASUAL");

        // Wire CoverFlowCard
        var card = root.GetComponent(cardType);
        if (card != null)
        {
            var so = new SerializedObject(card);
            SetRef(so, "_rectTransform", rrt);
            SetRef(so, "_canvasGroup", root.GetComponent<CanvasGroup>());
            SetRef(so, "_posterImage", poster.GetComponent<RawImage>());
            SetRef(so, "_grayscaleOverlay", grayscale.GetComponent<Image>());
            SetRef(so, "_lockIcon", lockIcon.GetComponent<Image>());
            SetRef(so, "_accentBorder", accent.GetComponent<Image>());
            SetRef(so, "_gigTitleText", gigTitle);
            SetRef(so, "_bandLogoImage", bandLogo.GetComponent<Image>());
            SetRef(so, "_activeContentPanel", activePanel);
            SetRef(so, "_woodenFrame", root.transform.Find("WoodenFrame")?.GetComponent<Image>());
            SetRef(so, "_soldOutSash", sash.GetComponent<Image>());
            SetRef(so, "_lockPanel", lockPanel);
            SetRef(so, "_unlockRequirementText", unlockTxt);
            SetRef(so, "_mysteryTracklistText", lockPanel.transform.Find("MysteryTracklistText")?.GetComponent<TextMeshProUGUI>());

            var ar = footer.transform.Find("AvgTrackRow");
            if (ar != null) { var t = ar.Find("AvgTrackText")?.GetComponent<TextMeshProUGUI>(); SetRef(so, "_hypeSlot1Text", t); SetRef(so, "_avgTrackText", t); }
            var ir = footer.transform.Find("IntensityRow");
            if (ir != null) { var t = ir.Find("IntensityText")?.GetComponent<TextMeshProUGUI>(); SetRef(so, "_hypeSlot2Text", t); SetRef(so, "_intensityText", t); }
            so.ApplyModifiedProperties();
        }

        // Wire V3
        var v3 = root.GetComponent(v3Type);
        if (v3 != null)
        {
            var so = new SerializedObject(v3);
            SetRef(so, "_posterImage", poster.GetComponent<RawImage>());
            SetRef(so, "_bandLogoImage", bandLogo.GetComponent<Image>());
            SetRef(so, "_gigTitleText", gigTitle);
            SetRef(so, "_tracklistText", trackText);
            var ar = footer.transform.Find("AvgTrackRow");
            if (ar != null) SetRef(so, "_avgTrackText", ar.Find("AvgTrackText")?.GetComponent<TextMeshProUGUI>());
            var ir = footer.transform.Find("IntensityRow");
            if (ir != null) SetRef(so, "_intensityText", ir.Find("IntensityText")?.GetComponent<TextMeshProUGUI>());
            so.ApplyModifiedProperties();
        }

        return root;
    }

    // ═══════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════

    private static System.Type ResolveType(string fn)
    {
        var t = System.Type.GetType(fn + ", Assembly-CSharp");
        return t ?? System.Type.GetType(fn);
    }

    private static void SetRef(SerializedObject so, string n, Object v) { var p = so.FindProperty(n); if (p != null) p.objectReferenceValue = v; }
    private static void SetArray(SerializedObject so, string n, Component[] el)
    {
        var ap = so.FindProperty(n);
        if (ap == null || !ap.isArray) return;
        ap.ClearArray();
        for (int i = 0; i < el.Length; i++) { ap.InsertArrayElementAtIndex(i); ap.GetArrayElementAtIndex(i).objectReferenceValue = el[i]; }
    }

    private static TextMeshProUGUI MakeTMP(GameObject p, string n, string t, float fs, TextAlignmentOptions a, int layer)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(p.transform, false); go.layer = layer;
        var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.text = t; tmp.fontSize = fs; tmp.alignment = a;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 60);
        return tmp;
    }

    private static TextMeshProUGUI MakeTMPChild(GameObject p, string n, string t, float fs, TextAlignmentOptions a)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(p.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>(); tmp.text = t; tmp.fontSize = fs; tmp.alignment = a;
        tmp.enableWordWrapping = true; tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private static GameObject CreateImageChild(GameObject p, string n, bool stretch)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(p.transform, false);
        if (stretch) { var rt = go.GetComponent<RectTransform>(); rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        return go;
    }

    private static void CreateStatRow(GameObject p, string rn, string icon, string tn, string dt)
    {
        var row = new GameObject(rn, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        row.transform.SetParent(p.transform, false);
        var hlg = row.GetComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 8;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = false;

        var ico = CreateImageChild(row, icon, false);
        var ile = ico.AddComponent<LayoutElement>();
        ile.minWidth = 24; ile.minHeight = 24; ile.preferredWidth = 24; ile.preferredHeight = 24;

        var txt = MakeTMPChild(row, tn, dt, 18, TextAlignmentOptions.Left);
        txt.enableAutoSizing = true; txt.fontSizeMin = 12; txt.fontSizeMax = 18; txt.enableWordWrapping = false;
        var tle = txt.gameObject.AddComponent<LayoutElement>(); tle.flexibleWidth = 1;
    }

    private static void StretchInset(RectTransform rt, int i)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(i, i); rt.offsetMax = new Vector2(-i, -i);
    }

    private static void StretchInset(RectTransform rt, int h, int v)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(h, v); rt.offsetMax = new Vector2(-h, -v);
    }

    private static void EnsureLayerExists(string name)
    {
        var so = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = so.FindProperty("layers");
        for (int i = 8; i < 32; i++)
        {
            var sp = layers.GetArrayElementAtIndex(i);
            if (sp.stringValue == name) return;
            if (string.IsNullOrEmpty(sp.stringValue)) { sp.stringValue = name; so.ApplyModifiedProperties(); return; }
        }
    }

    private static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}