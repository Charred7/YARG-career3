#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using YARG.Menu.Career.Trophy;

public static class TrophyArtSetup
{
    private const string ArtFolder = "Assets/Art/CareersM/Trophy/Arts";
    private const string AssetPath = "Assets/Art/CareersM/Trophy/TrophyScreenArt.asset";

    private const string BgPath = ArtFolder + "/01_trophy_screen_bg.png";
    private const string SummaryPanelPath = ArtFolder + "/02_panel_summary_9slice.png";
    private const string SummaryPanelFPath = ArtFolder + "/02_panel_summary_9slicef.png";
    private const string InstrumentPanelPath = ArtFolder + "/03_panel_instrument_9slice.png";
    private const string EmblemPath = ArtFolder + "/04_emblem_golden_vinyl.png";
    private const string StarFilledPath = ArtFolder + "/05_star_backing_filled.png";
    private const string StarEmptyPath = ArtFolder + "/06_ star_backing_empty.png";
    private const string HeaderBandPath = ArtFolder + "/07_header_band_9slicec.png";
    private const string BottomGlowPath = ArtFolder + "/08_card_bottom_glow_9slice.png";
    private const string GuitarWatermarkPath = ArtFolder + "/Guitar shadow.png";
    private const string BassWatermarkPath = ArtFolder + "/Bass shadow.png";

    private const string GuitarHeaderIconPath = "Assets/Art/CareersM/HypeIcons/sprites/guitar.png";
    private const string FallbackStarFilledPath = "Assets/Art/CareersM/Motivation/images/StarProgressGold.png";
    private const string FallbackStarEmptyPath = "Assets/Art/CareersM/Motivation/images/StarProgressEmpty.png";

    [MenuItem("Tools/YARG/Setup Trophy Screen Art", false, 112)]
    public static void SetupAll()
    {
        ConfigureTextureImports();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var art = LoadOrCreateArtAsset();
        PopulateArtAsset(art);

        CampaignTrophyPrefabBuilder.BuildAllPrefabs();

        AssetDatabase.SaveAssets();
        Debug.Log("[TrophyArtSetup] Trophy screen art configured and prefabs rebuilt.");
    }

    private static void ConfigureTextureImports()
    {
        ConfigureSprite(BgPath, borders: Vector4.zero, mipMaps: false);
        ConfigureSprite(SummaryPanelPath, new Vector4(64, 64, 64, 64));
        // f-variant has a larger corner radius (~90px) — borders must clear the full curve
        ConfigureSprite(SummaryPanelFPath, new Vector4(96, 96, 96, 96));
        ConfigureSprite(InstrumentPanelPath, new Vector4(56, 48, 56, 56));
        ConfigureSprite(EmblemPath, Vector4.zero);
        ConfigureSprite(StarFilledPath, Vector4.zero);
        ConfigureSprite(StarEmptyPath, Vector4.zero);
        ConfigureSprite(HeaderBandPath, new Vector4(32, 0, 32, 0));
        ConfigureSprite(BottomGlowPath, new Vector4(48, 8, 48, 8));
        ConfigureSprite(GuitarWatermarkPath, Vector4.zero);
        ConfigureSprite(BassWatermarkPath, Vector4.zero);
        ConfigureSprite(GuitarHeaderIconPath, Vector4.zero);
        ConfigureSprite(FallbackStarFilledPath, Vector4.zero);
        ConfigureSprite(FallbackStarEmptyPath, Vector4.zero);
    }

    private static void ConfigureSprite(string path, Vector4 borders, bool mipMaps = false)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning($"[TrophyArtSetup] Missing texture: {path}");
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = mipMaps;
        importer.filterMode = FilterMode.Bilinear;
        importer.spritePixelsToUnits = 100;
        importer.spriteBorder = borders;
        importer.SaveAndReimport();
    }

    private static TrophyScreenArt LoadOrCreateArtAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TrophyScreenArt>(AssetPath);
        if (existing != null)
            return existing;

        var art = ScriptableObject.CreateInstance<TrophyScreenArt>();
        AssetDatabase.CreateAsset(art, AssetPath);
        return art;
    }

    private static void PopulateArtAsset(TrophyScreenArt art)
    {
        art.Background = LoadSprite(BgPath);
        art.SummaryPanel = LoadSprite(SummaryPanelPath);
        art.InstrumentPanel = LoadSprite(InstrumentPanelPath);
        art.GoldenVinylEmblem = LoadSprite(EmblemPath);
        art.HeaderBand = LoadSprite(HeaderBandPath);
        art.CardBottomGlow = LoadSprite(BottomGlowPath);
        art.GuitarWatermark = LoadSprite(GuitarWatermarkPath);
        art.BassWatermark = LoadSprite(BassWatermarkPath);
        art.GuitarHeaderIcon = LoadSprite(GuitarHeaderIconPath);
        art.EmblemHasBakedBannerText = true;

        art.StarFilled = LoadSprite(StarFilledPath) ?? LoadSprite(FallbackStarFilledPath);
        art.StarEmpty = LoadSprite(StarEmptyPath) ?? LoadSprite(FallbackStarEmptyPath);

        EditorUtility.SetDirty(art);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
#endif
