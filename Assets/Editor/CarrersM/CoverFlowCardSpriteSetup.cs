using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using YARG.Menu.Career.CoverFlow;

/// <summary>
/// Editor window for quickly assigning sprites to all Image components
/// of a CoverFlowCard prefab after creation. Opens automatically from
/// CoverFlowPrefabBuilder.CreateHierarchy().
/// </summary>
public class CoverFlowCardSpriteSetup : EditorWindow
{
    private GameObject _targetRoot;

    // 7 sprite fields — one per Image component that needs artwork
    private Sprite _woodenFrameSprite;
    private Sprite _grayscaleOverlaySprite;
    private Sprite _soldOutSashSprite;
    private Sprite _lockIconSprite;
    private Sprite _accentBorderSprite;
    private Sprite _clockIconSprite;
    private Sprite _sunglassesIconSprite;

    public static void ShowWindow(GameObject root)
    {
        var window = GetWindow<CoverFlowCardSpriteSetup>("CoverFlow Card Sprites");
        window._targetRoot = root;
        window.minSize = new Vector2(360, 400);
        window.Show();
    }

    private void OnGUI()
    {
        if (_targetRoot == null)
        {
            EditorGUILayout.HelpBox("No CoverFlowCard selected. Select one in the Hierarchy.", MessageType.Warning);
            if (Selection.activeGameObject != null)
                _targetRoot = Selection.activeGameObject;
            else
                return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Assign Sprites for: " + _targetRoot.name, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // ── Sprite Fields ───────────────────────────────────────────
        _woodenFrameSprite     = (Sprite)EditorGUILayout.ObjectField("Wooden Frame",     _woodenFrameSprite,     typeof(Sprite), false);
        _grayscaleOverlaySprite = (Sprite)EditorGUILayout.ObjectField("Grayscale Overlay", _grayscaleOverlaySprite, typeof(Sprite), false);
        _soldOutSashSprite     = (Sprite)EditorGUILayout.ObjectField("Sold Out Sash",    _soldOutSashSprite,     typeof(Sprite), false);
        _lockIconSprite        = (Sprite)EditorGUILayout.ObjectField("Lock Icon",        _lockIconSprite,        typeof(Sprite), false);
        _accentBorderSprite    = (Sprite)EditorGUILayout.ObjectField("Accent Border",    _accentBorderSprite,    typeof(Sprite), false);
        _clockIconSprite       = (Sprite)EditorGUILayout.ObjectField("Clock Icon",       _clockIconSprite,       typeof(Sprite), false);
        _sunglassesIconSprite  = (Sprite)EditorGUILayout.ObjectField("Sunglasses Icon",  _sunglassesIconSprite,  typeof(Sprite), false);

        EditorGUILayout.Space();

        GUI.enabled = _targetRoot != null;
        if (GUILayout.Button("Apply All Sprites", GUILayout.Height(36)))
        {
            ApplyAllSprites();
        }
        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Sprites are assigned directly to the child Image components. " +
            "You can reopen this window anytime via the CoverFlowCard inspector.",
            MessageType.Info);
    }

    private void ApplyAllSprites()
    {
        if (_targetRoot == null) return;

        Undo.RecordObject(_targetRoot, "Assign CoverFlow Card Sprites");

        SetImageSprite("WoodenFrame",          _woodenFrameSprite);
        SetImageSprite("GrayscaleOverlay",     _grayscaleOverlaySprite);
        SetImageSprite("SoldOutSash",          _soldOutSashSprite);
        SetImageSprite("LockPanel/LockIcon",    _lockIconSprite);
        SetImageSprite("AccentBorder",         _accentBorderSprite);
        SetImageSprite("MetadataFooter/AvgTrackRow/ClockIcon",       _clockIconSprite);
        SetImageSprite("MetadataFooter/IntensityRow/SunglassesIcon", _sunglassesIconSprite);

        EditorUtility.SetDirty(_targetRoot);
        Debug.Log($"[CoverFlowCardSpriteSetup] All sprites applied to '{_targetRoot.name}'.");
    }

    private void SetImageSprite(string childPath, Sprite sprite)
    {
        if (sprite == null) return;

        var child = _targetRoot.transform.Find(childPath);
        if (child == null)
        {
            Debug.LogWarning($"[CoverFlowCardSpriteSetup] Could not find '{childPath}' under '{_targetRoot.name}'.");
            return;
        }

        var image = child.GetComponent<Image>();
        if (image == null)
        {
            Debug.LogWarning($"[CoverFlowCardSpriteSetup] '{childPath}' has no Image component.");
            return;
        }

        image.sprite = sprite;
    }

    /// <summary>
    /// Menu item to manually reopen the window for the currently selected CoverFlowCard.
    /// </summary>
    [MenuItem("CONTEXT/CoverFlowCard/Open Sprite Setup...", false, 200)]
    private static void OpenFromInspector(MenuCommand command)
    {
        var card = command.context as CoverFlowCard;
        if (card != null)
            ShowWindow(card.gameObject);
    }
}