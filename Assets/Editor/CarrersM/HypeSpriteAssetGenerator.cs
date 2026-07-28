using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore; // Fixes GlyphMetrics and GlyphRect namespace error
using UnityEditor;
using TMPro;

public class HypeSpriteAssetGenerator : EditorWindow
{
    // All icon names used in hypeengine.ini, matching sprite filenames in solo/
    private static readonly string[] ALL_ICON_NAMES = {
        "audio", "battery", "broom", "chart_up", "crown", "fire", "flag",
        "guitar", "leaf", "lightning", "megaphone", "numbers", "pin",
        "radio", "rocket", "running", "shield", "skateboard", "sprout",
        "star", "stopwatch", "sunglasses", "ticket", "trophy", "yoga"
    };

    private string sourceFolderPath = "Assets/Art/CareersM/HypeIcons/solo";
    private string savePath = "Assets/Art/CareersM/HypeIcons/HypeIcons_SpriteAsset.asset";
    private string spriteOutputPath = "Assets/Art/CareersM/HypeIcons/sprites";

    [MenuItem("Tools/YARG/Build Hype Engine Sprite Asset")]
    public static void ShowWindow()
    {
        GetWindow<HypeSpriteAssetGenerator>("Hype Sprite Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("HypeEngine TMP Sprite Asset Automator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        sourceFolderPath = EditorGUILayout.TextField("SVG Texture Folder", sourceFolderPath);
        savePath = EditorGUILayout.TextField("Output Asset Path", savePath);
        spriteOutputPath = EditorGUILayout.TextField("Sprite Output Folder", spriteOutputPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate TextMeshPro Sprite Asset", GUILayout.Height(40)))
        {
            BuildSpriteAsset();
        }

        if (GUILayout.Button("Generate Icon Sprites (PNG)", GUILayout.Height(40)))
        {
            GenerateIconSprites();
        }

        if (GUILayout.Button("Pre-populate Icon Names in HypeIconSet", GUILayout.Height(40)))
        {
            PrepopulateIconNames();
        }
    }

    private void BuildSpriteAsset()
    {
        if (!Directory.Exists(sourceFolderPath))
        {
            Debug.LogError($"[HypeEngine] Source folder does not exist: {sourceFolderPath}");
            return;
        }

        TMP_SpriteAsset spriteAsset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();

        uint glyphIndex = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolderPath });
        
        Texture2D backupSheetTex = null;

        foreach (string guid in guids)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            if (tex == null) continue;

            string cleanName = Path.GetFileNameWithoutExtension(filePath).ToLower();
            if (cleanName.Contains("spriteasset")) continue;

            // Grab the first valid texture to use as the inspector backing atlas reference
            if (backupSheetTex == null) {
                backupSheetTex = tex;
            }

            TMP_SpriteGlyph glyph = new TMP_SpriteGlyph
            {
                index = glyphIndex,
                metrics = new GlyphMetrics(tex.width, tex.height, 0, tex.height, tex.width),
                glyphRect = new GlyphRect(0, 0, tex.width, tex.height),
                scale = 1.0f,
                atlasIndex = 0
            };
            spriteAsset.spriteGlyphTable.Add(glyph);

            TMP_SpriteCharacter character = new TMP_SpriteCharacter(glyphIndex, glyph)
            {
                name = cleanName,
                scale = 1.0f
            };
            spriteAsset.spriteCharacterTable.Add(character);

            glyphIndex++;
        }

        // Assign the backing texture reference to satisfy TMPro's inspector drawers
        if (backupSheetTex != null) {
            spriteAsset.spriteSheet = backupSheetTex;
        }

        spriteAsset.UpdateLookupTables();

        AssetDatabase.CreateAsset(spriteAsset, savePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HypeEngine] Successfully compiled {glyphIndex} icons into a native TMPro Sprite Asset at: {savePath}");
    }

    /// <summary>
    /// Converts each SVG in the source folder to a standalone PNG Sprite asset.
    /// The resulting PNGs can be dragged into HypeIconSet Entry.texture fields.
    /// </summary>
    private void GenerateIconSprites()
    {
        if (!Directory.Exists(sourceFolderPath))
        {
            Debug.LogError($"[HypeEngine] Source folder does not exist: {sourceFolderPath}");
            return;
        }

        // Ensure output directory exists
        if (!Directory.Exists(spriteOutputPath))
            Directory.CreateDirectory(spriteOutputPath);

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolderPath });
        int convertedCount = 0;

        foreach (string guid in guids)
        {
            string filePath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
            if (tex == null) continue;

            string cleanName = Path.GetFileNameWithoutExtension(filePath).ToLower();
            if (cleanName.Contains("spriteasset")) continue;

            string pngPath = $"{spriteOutputPath}/{cleanName}.png";

            // Encode texture as PNG
            byte[] pngData = tex.EncodeToPNG();
            if (pngData == null || pngData.Length == 0)
            {
                Debug.LogWarning($"[HypeEngine] Failed to encode {cleanName} to PNG.");
                continue;
            }

            // Write PNG to disk
            File.WriteAllBytes(pngPath, pngData);
            convertedCount++;
        }

        // Refresh assets so Unity detects the new PNGs
        AssetDatabase.Refresh();

        // Set all PNGs in the output folder to Sprite texture type
        string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spriteOutputPath });
        foreach (string guid in pngGuids)
        {
            string pngPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!pngPath.EndsWith(".png")) continue;

            TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"[HypeEngine] Successfully converted {convertedCount} SVGs to PNG Sprites at: {spriteOutputPath}");
    }

    /// <summary>
    /// Finds the first HypeIconSet asset in the project and pre-populates its
    /// entries with all known icon names (sprite fields left empty for the
    /// user to drag PNGs into).
    /// </summary>
    private void PrepopulateIconNames()
    {
        string[] guids = AssetDatabase.FindAssets("t:YARG.Menu.Career.CoverFlow.HypeIconSet");
        if (guids.Length == 0)
        {
            Debug.LogError("[HypeEngine] No HypeIconSet asset found in the project. Create one via Assets → Create → YARG → Hype Icon Set first.");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        var iconSet = AssetDatabase.LoadAssetAtPath<YARG.Menu.Career.CoverFlow.HypeIconSet>(path);

        if (iconSet == null)
        {
            Debug.LogError("[HypeEngine] Failed to load HypeIconSet asset.");
            return;
        }

        // Build the entry list from all known icon names
        var entries = new YARG.Menu.Career.CoverFlow.HypeIconSet.Entry[ALL_ICON_NAMES.Length];
        for (int i = 0; i < ALL_ICON_NAMES.Length; i++)
        {
            entries[i] = new YARG.Menu.Career.CoverFlow.HypeIconSet.Entry
            {
                name = ALL_ICON_NAMES[i],
                sprite = null // User drags PNGs in manually
            };
        }

        // Apply via SerializedObject so changes persist
        SerializedObject so = new SerializedObject(iconSet);
        SerializedProperty iconsProp = so.FindProperty("icons");
        iconsProp.ClearArray();
        iconsProp.arraySize = entries.Length;

        for (int i = 0; i < entries.Length; i++)
        {
            SerializedProperty elem = iconsProp.GetArrayElementAtIndex(i);
            elem.FindPropertyRelative("name").stringValue = entries[i].name;
            elem.FindPropertyRelative("sprite").objectReferenceValue = null;
        }

        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HypeEngine] Pre-populated {entries.Length} icon names in HypeIconSet '{iconSet.name}'. Now drag the PNGs from {spriteOutputPath} into the sprite fields.");
    }
}