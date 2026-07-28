using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using YARG.Menu.Career.CoverFlow;

/// <summary>
/// Custom inspector for CoverFlowCard that adds a collapsible "Sprite Setup"
/// foldout at the top, showing all 7 sprite fields in one place so you never
/// need to drill into the hierarchy to assign artwork.
/// </summary>
[CustomEditor(typeof(CoverFlowCard))]
public class CoverFlowCardEditor : UnityEditor.Editor
{
    private SerializedProperty _rectTransformProp;
    private bool _spriteSetupFoldout = true;

    // Cached sprite references (per-editor session)
    private Sprite _woodenFrameSprite;
    private Sprite _grayscaleOverlaySprite;
    private Sprite _soldOutSashSprite;
    private Sprite _lockIconSprite;
    private Sprite _accentBorderSprite;
    private Sprite _clockIconSprite;
    private Sprite _sunglassesIconSprite;

    private void OnEnable()
    {
        // Read the _rectTransform property just to confirm we have a valid target
        _rectTransformProp = serializedObject.FindProperty("_rectTransform");

        // Load cached sprites from the child Image components on enable
        var card = (CoverFlowCard)target;
        if (card != null)
        {
            var root = card.gameObject;
            _woodenFrameSprite      = GetSprite(root, "WoodenFrame");
            _grayscaleOverlaySprite = GetSprite(root, "GrayscaleOverlay");
            _soldOutSashSprite      = GetSprite(root, "SoldOutSash");
            _lockIconSprite         = GetSprite(root, "LockPanel/LockIcon");
            _accentBorderSprite     = GetSprite(root, "AccentBorder");
            _clockIconSprite        = GetSprite(root, "MetadataFooter/AvgTrackRow/ClockIcon");
            _sunglassesIconSprite   = GetSprite(root, "MetadataFooter/IntensityRow/SunglassesIcon");
        }
    }

    public override void OnInspectorGUI()
    {
        // ── Sprite Setup Foldout ────────────────────────────────────
        _spriteSetupFoldout = EditorGUILayout.Foldout(_spriteSetupFoldout, "Sprite Setup", true);
        if (_spriteSetupFoldout)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.Space();
            _woodenFrameSprite      = (Sprite)EditorGUILayout.ObjectField("Wooden Frame",     _woodenFrameSprite,     typeof(Sprite), false);
            _grayscaleOverlaySprite = (Sprite)EditorGUILayout.ObjectField("Grayscale Overlay", _grayscaleOverlaySprite, typeof(Sprite), false);
            _soldOutSashSprite      = (Sprite)EditorGUILayout.ObjectField("Sold Out Sash",    _soldOutSashSprite,     typeof(Sprite), false);
            _lockIconSprite         = (Sprite)EditorGUILayout.ObjectField("Lock Icon",        _lockIconSprite,        typeof(Sprite), false);
            _accentBorderSprite     = (Sprite)EditorGUILayout.ObjectField("Accent Border",    _accentBorderSprite,    typeof(Sprite), false);
            _clockIconSprite        = (Sprite)EditorGUILayout.ObjectField("Clock Icon",       _clockIconSprite,       typeof(Sprite), false);
            _sunglassesIconSprite   = (Sprite)EditorGUILayout.ObjectField("Sunglasses Icon",  _sunglassesIconSprite,  typeof(Sprite), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Sprites to Children", GUILayout.Height(28)))
            {
                ApplyAllSprites();
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        // ── Default CoverFlowCard Inspector ─────────────────────────
        // Draw all remaining serialized fields (the auto-wired references)
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;
            // Skip the script field (MonoScript reference)
            if (prop.name == "m_Script") continue;

            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ── Sprite Assignment ───────────────────────────────────────────

    private void ApplyAllSprites()
    {
        var card = (CoverFlowCard)target;
        if (card == null) return;

        var root = card.gameObject;
        Undo.RecordObject(root, "Assign CoverFlow Card Sprites");

        SetSprite(root, "WoodenFrame",          _woodenFrameSprite);
        SetSprite(root, "GrayscaleOverlay",     _grayscaleOverlaySprite);
        SetSprite(root, "SoldOutSash",          _soldOutSashSprite);
        SetSprite(root, "LockPanel/LockIcon",    _lockIconSprite);
        SetSprite(root, "AccentBorder",         _accentBorderSprite);
        SetSprite(root, "MetadataFooter/AvgTrackRow/ClockIcon",       _clockIconSprite);
        SetSprite(root, "MetadataFooter/IntensityRow/SunglassesIcon", _sunglassesIconSprite);

        EditorUtility.SetDirty(root);
        Repaint();
    }

    private static Sprite GetSprite(GameObject root, string childPath)
    {
        var child = root.transform.Find(childPath);
        if (child == null) return null;
        var image = child.GetComponent<Image>();
        return image != null ? image.sprite : null;
    }

    private static void SetSprite(GameObject root, string childPath, Sprite sprite)
    {
        if (sprite == null) return;
        var child = root.transform.Find(childPath);
        if (child == null)
        {
            Debug.LogWarning($"[CoverFlowCardEditor] Could not find '{childPath}'.");
            return;
        }
        var image = child.GetComponent<Image>();
        if (image == null) return;
        image.sprite = sprite;
    }
}