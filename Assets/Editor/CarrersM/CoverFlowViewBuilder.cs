/// <summary>
/// DEPRECATED — This builder uses ScreenSpaceOverlay with 5 slots, which does NOT match
/// the render goal. Use CoverFlowBuilderWindow (WorldSpace, 7 slots, parabolic arc) instead.
/// Kept for reference only — will be removed in a future cleanup pass.
/// </summary>
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

public class CoverFlowViewBuilder : EditorWindow
{
    private GameObject cardPrefab;

    [MenuItem("Tools/YARG/Generate Complete GigView v4 Menu")]
    public static void ShowWindow()
    {
        GetWindow<CoverFlowViewBuilder>("GigView v4 Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("YARG CoverFlow v3/v4 Hierarchy Automator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        cardPrefab = (GameObject)EditorGUILayout.ObjectField("CoverFlowCard Prefab", cardPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Complete Menu Hierarchy", GUILayout.Height(40)))
        {
            if (cardPrefab == null)
            {
                EditorUtility.DisplayDialog("Missing Prefab", "Please assign the CoverFlowCard prefab created by CoverFlowPrefabBuilder before generating.", "OK");
                return;
            }
            BuildCompleteMenu();
        }
    }

    private void BuildCompleteMenu()
    {
        // 1. Create Canvas Root if missing, or use selected
        GameObject rootMenu = new GameObject("CareerGigMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(rootMenu, "Generate Career Menu");

        Canvas canvas = rootMenu.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        CanvasScaler scaler = rootMenu.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // Add Bootstrapper to root
        System.Type bootstrapperType = System.Type.GetType("YARG.Menu.Career.CoverFlow.CoverFlowBootstrapper") ?? typeof(MonoBehaviour);
        Component bootstrapper = rootMenu.AddComponent(bootstrapperType);

        // 2. CoverFlowBackground Child
        GameObject bgGo = new GameObject("CoverFlowBackground", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(rootMenu.transform, false);
        StretchRect(bgGo.GetComponent<RectTransform>());
        System.Type bgType = System.Type.GetType("YARG.Menu.Career.CoverFlow.CoverFlowBackground") ?? typeof(MonoBehaviour);
        Component backgroundComp = bgGo.AddComponent(bgType);

        // 3. Header Text Info Panels (Career Name, Band Name, Progress)
        GameObject headerPanel = new GameObject("HeaderPanel", typeof(RectTransform));
        headerPanel.transform.SetParent(rootMenu.transform, false);
        RectTransform headerRt = headerPanel.GetComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0f, 0.85f);
        headerRt.anchorMax = new Vector2(1f, 1f);
        headerRt.sizeDelta = Vector2.zero;

        GameObject careerTextGo = CreateTextObject(headerPanel, "CareerNameText", "FIRSTBAND", 28, TextAlignmentOptions.Left);
        careerTextGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, -30);

        GameObject progressTextGo = CreateTextObject(headerPanel, "ProgressText", "0/18 COMPLETE", 28, TextAlignmentOptions.Right);
        progressTextGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(-50, -30);

        GameObject bandTextGo = CreateTextObject(headerPanel, "BandNameText", "YARG CHAMPION II", 42, TextAlignmentOptions.Center);
        bandTextGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);

        // 4. CoverFlowController Child
        GameObject controllerGo = new GameObject("CoverFlowController");
        controllerGo.transform.SetParent(rootMenu.transform, false);
        System.Type controllerType = System.Type.GetType("YARG.Menu.Career.CoverFlow.CoverFlowController") ?? typeof(MonoBehaviour);
        Component controllerComp = controllerGo.AddComponent(controllerType);

        // Input Remapper Child
        GameObject inputGo = new GameObject("CoverFlowInputRemapper");
        inputGo.transform.SetParent(controllerGo.transform, false);
        System.Type inputType = System.Type.GetType("YARG.Menu.Career.CoverFlow.CoverFlowInputRemapper") ?? typeof(MonoBehaviour);
        Component inputComp = inputGo.AddComponent(inputType);

        // 5. CoverFlowUI Layer Container
        GameObject uiContainer = new GameObject("CoverFlowUI", typeof(RectTransform));
        uiContainer.transform.SetParent(rootMenu.transform, false);
        StretchRect(uiContainer.GetComponent<RectTransform>());

        // 6. Instantiate exactly 5 slots from the prefab
        Component[] spawnedCards = new Component[5];
        System.Type cardType = System.Type.GetType("YARG.Menu.Career.CoverFlow.CoverFlowCard") ?? typeof(MonoBehaviour);

        for (int i = 0; i < 5; i++)
        {
            GameObject cardInstance = (GameObject)PrefabUtility.InstantiatePrefab(cardPrefab, uiContainer.transform);
            cardInstance.name = $"CoverFlowCard_Slot{i}";
            
            Component c = cardInstance.GetComponent(cardType);
            if (c == null) c = cardInstance.AddComponent(cardType);
            spawnedCards[i] = c;
        }

        // ==========================================
        // PROMOTOR SERIALIZATION HOOKS (THE WIRING)
        // ==========================================
        
        // Wire Bootstrapper
        SerializedObject bootSerialized = new SerializedObject(bootstrapper);
        SetFieldValue(bootSerialized, "_coverFlowController", controllerComp);
        SetFieldValue(bootSerialized, "_coverFlowBackground", backgroundComp);
        SetArrayValues(bootSerialized, "_cardSlots", spawnedCards);
        bootSerialized.ApplyModifiedProperties();

        // Wire Controller
        SerializedObject controllerSerialized = new SerializedObject(controllerComp);
        SetFieldValue(controllerSerialized, "_background", backgroundComp);
        SetFieldValue(controllerSerialized, "_careerNameText", careerTextGo.GetComponent<TextMeshProUGUI>());
        SetFieldValue(controllerSerialized, "_bandNameText", bandTextGo.GetComponent<TextMeshProUGUI>());
        SetFieldValue(controllerSerialized, "_progressText", progressTextGo.GetComponent<TextMeshProUGUI>());
        SetFieldValue(controllerSerialized, "_targetCanvas", canvas);
        SetFieldValue(controllerSerialized, "_inputRemapper", inputComp);
        SetArrayValues(controllerSerialized, "_cardSlots", spawnedCards);

        // Inject Config Default Parameters safely into Controller serialized fields
        SetNumericField(controllerSerialized, "_transitionDuration", 0.18f);
        SetNumericField(controllerSerialized, "_overshootDuration", 0.05f);
        SetNumericField(controllerSerialized, "_overshootAmount", 0.015f);
        SetNumericField(controllerSerialized, "_blitzTotalMinutesMax", 9.0f);
        SetNumericField(controllerSerialized, "_blitzAvgTrackMinutesMax", 3.5f);
        SetNumericField(controllerSerialized, "_biteSizeAvgTrackMinutesMax", 3.0f);
        SetNumericField(controllerSerialized, "_tier2BlacklistTotalMinutes", 10.0f);
        SetNumericField(controllerSerialized, "_tier2BlacklistAvgTrackMinutes", 3.5f);
        SetStringField(controllerSerialized, "_defaultFallbackText", "SETLIST VIBE: PURE ROCK & ROLL");
        
        // Setup a snappier default animation evaluation curve if available
        SerializedProperty curveProp = controllerSerialized.FindProperty("_easingCurve");
        if (curveProp != null)
        {
            curveProp.animationCurveValue = AnimationCurve.EaseInOut(0, 0, 1, 1);
        }
        controllerSerialized.ApplyModifiedProperties();

        Selection.activeGameObject = rootMenu;
        EditorUtility.DisplayDialog("Success!", "GigView v4 Layout generated and completely wired. Convert root 'CareerGigMenu' into your layout variant prefab.", "Awesome");
    }

    private static void SetFieldValue(SerializedObject so, string fieldName, Object value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null) prop.objectReferenceValue = value;
    }

    private static void SetNumericField(SerializedObject so, string fieldName, float value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null) prop.floatValue = value;
    }

    private static void SetStringField(SerializedObject so, string fieldName, string value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null) prop.stringValue = value;
    }

    private static void SetArrayValues(SerializedObject so, string fieldName, Component[] elements)
    {
        SerializedProperty arrayProp = so.FindProperty(fieldName);
        if (arrayProp == null || !arrayProp.isArray) return;

        arrayProp.ClearArray();
        for (int i = 0; i < elements.Length; i++)
        {
            arrayProp.InsertArrayElementAtIndex(i);
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = elements[i];
        }
    }

    private static GameObject CreateTextObject(GameObject parent, string name, string defaultText, float size, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = size;
        tmp.alignment = align;
        
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 60);
        return go;
    }

    private static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}