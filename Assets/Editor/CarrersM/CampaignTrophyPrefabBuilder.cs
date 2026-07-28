#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YARG.Menu;
using YARG.Menu.Career.Trophy;
using YARG.Menu.Career.Motivation;

/// <summary>
/// Builds Campaign Trophy menu prefabs and registers the CareerTrophy menu in MenuScene.
/// </summary>
public static class CampaignTrophyPrefabBuilder
{
    private const string TrophyFolder = "Assets/Prefabs/CareersM/03_Trophy";
    private const string RowPrefabPath = TrophyFolder + "/TrophyStatRow_Template.prefab";
    private const string CardPrefabPath = TrophyFolder + "/PlayerTrophyCard_FinalPrefab.prefab";
    private const string SummaryPrefabPath = TrophyFolder + "/CampaignSummaryCard.prefab";
    private const string MenuPrefabPath = TrophyFolder + "/CampaignTrophyMenu.prefab";
    private const string ArtAssetPath = "Assets/Art/CareersM/Trophy/TrophyScreenArt.asset";
    private const string BarlowLightPath = "Assets/Art/Fonts/Barlow/Barlow-Light.asset";
    private const string BarlowBlackPath = "Assets/Art/Fonts/Barlow/Barlow-Black.asset";

    private static TMP_FontAsset LoadBarlowLight() =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarlowLightPath);

    private static TMP_FontAsset LoadBarlowBlack() =>
        AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarlowBlackPath);

    private static void ApplyTitleFont(TextMeshProUGUI text)
    {
        var font = LoadBarlowBlack();
        if (font != null)
            text.font = font;
        text.fontStyle = FontStyles.Normal;
    }

    [MenuItem("Tools/YARG/Build Campaign Trophy Prefabs", false, 110)]
    public static void BuildAllPrefabs()
    {
        EnsureFolder(TrophyFolder);

        var rowPrefab = TrophyPrefabGenerator.BuildStatRowPrefab();
        var cardPrefab = TrophyPrefabGenerator.BuildCardPrefab(rowPrefab);
        var summaryPrefab = BuildSummaryCardPrefab(rowPrefab);
        var art = AssetDatabase.LoadAssetAtPath<TrophyScreenArt>(ArtAssetPath);
        BuildMenuPrefab(cardPrefab, summaryPrefab, art);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CampaignTrophyPrefabBuilder] All trophy prefabs rebuilt.");
    }

    [MenuItem("Tools/YARG/Register CareerTrophy Menu", false, 111)]
    public static void RegisterCareerTrophyMenu()
    {
        var menuManager = Object.FindFirstObjectByType<MenuManager>();
        if (menuManager == null)
        {
            Debug.LogError("[CampaignTrophyPrefabBuilder] Open MenuScene first.");
            return;
        }

        foreach (var mo in menuManager.GetComponentsInChildren<MenuObject>(true))
        {
            if (mo.Menu == MenuManager.Menu.CareerTrophy)
            {
                Debug.Log("[CampaignTrophyPrefabBuilder] CareerTrophy menu already registered.");
                Selection.activeGameObject = mo.gameObject;
                return;
            }
        }

        var menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
        if (menuPrefab == null)
        {
            BuildAllPrefabs();
            menuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MenuPrefabPath);
        }

        if (menuPrefab == null)
        {
            Debug.LogError("[CampaignTrophyPrefabBuilder] Failed to load CampaignTrophyMenu prefab.");
            return;
        }

        var menuRoot = (GameObject)PrefabUtility.InstantiatePrefab(menuPrefab, menuManager.transform);
        menuRoot.name = "CampaignTrophyMenu";
        Undo.RegisterCreatedObjectUndo(menuRoot, "Register CareerTrophy Menu");

        var menuObject = menuRoot.GetComponent<MenuObject>();
        if (menuObject == null)
            menuObject = menuRoot.AddComponent<MenuObject>();

        var so = new SerializedObject(menuObject);
        var menuProp = so.FindProperty("<Menu>k__BackingField") ?? so.FindProperty("_menu");
        if (menuProp != null)
            menuProp.enumValueIndex = (int)MenuManager.Menu.CareerTrophy;

        var hideProp = so.FindProperty("<HideBelow>k__BackingField") ?? so.FindProperty("_hideBelow");
        if (hideProp != null)
            hideProp.boolValue = true;

        so.ApplyModifiedProperties();
        menuRoot.SetActive(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = menuRoot;
        Debug.Log("[CampaignTrophyPrefabBuilder] CareerTrophy menu registered under MenuManager.");
    }

    private static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder("Assets/Prefabs/CareersM", "03_Trophy");
    }

    private static GameObject BuildSummaryCardPrefab(GameObject rowPrefab)
    {
        var art = AssetDatabase.LoadAssetAtPath<TrophyScreenArt>(ArtAssetPath);

        var root = new GameObject("CampaignSummaryCard",
            typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(CampaignSummaryCard));
        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = TrophyLayoutSpec.SummaryCardSize;

        var rootLe = root.GetComponent<LayoutElement>();
        rootLe.preferredWidth = TrophyLayoutSpec.SummaryCardSize.x;
        rootLe.preferredHeight = TrophyLayoutSpec.SummaryCardSize.y;
        rootLe.flexibleWidth = TrophyLayoutSpec.SummaryFlex;
        rootLe.minWidth = TrophyLayoutSpec.SummaryMinWidth;

        var bg = root.GetComponent<Image>();
        bg.type = Image.Type.Sliced;
        bg.color = Color.white;
        bg.raycastTarget = false;
        if (art != null && art.SummaryPanel != null)
            bg.sprite = art.SummaryPanel;

        var glowGo = CreateUIObject("VinylGlow", root.transform);
        var glowRect = glowGo.GetComponent<RectTransform>();
        glowRect.anchorMin = new Vector2(0.5f, 1f);
        glowRect.anchorMax = new Vector2(0.5f, 1f);
        glowRect.pivot = new Vector2(0.5f, 1f);
        glowRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.SummaryEmblemTopInset);
        glowRect.sizeDelta = TrophyLayoutSpec.SummaryEmblemSize;
        var glowImg = glowGo.AddComponent<Image>();
        glowImg.color = new Color(1f, 0.84f, 0.2f, 0.2f);
        glowImg.raycastTarget = false;

        var badgeGo = CreateUIObject("VinylBadge", root.transform);
        var badgeRect = badgeGo.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 1f);
        badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.SummaryEmblemTopInset);
        badgeRect.sizeDelta = TrophyLayoutSpec.SummaryEmblemSize;
        var badgeImg = badgeGo.AddComponent<Image>();
        badgeImg.preserveAspect = true;
        badgeImg.raycastTarget = false;
        if (art != null && art.GoldenVinylEmblem != null)
        {
            badgeImg.sprite = art.GoldenVinylEmblem;
            badgeImg.color = TrophyLayoutSpec.EmblemWarmTint;
        }

        var bannerGo = CreateUIObject("CompletionBanner", root.transform);
        var bannerRect = bannerGo.GetComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 1f);
        bannerRect.anchorMax = new Vector2(0.5f, 1f);
        bannerRect.pivot = new Vector2(0.5f, 1f);
        bannerRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.SummaryStatsTopOffset);
        bannerRect.sizeDelta = new Vector2(300f, 36f);
        var bannerText = bannerGo.AddComponent<TextMeshProUGUI>();
        bannerText.text = "100% COMPLETE";
        bannerText.fontSize = 18;
        ApplyTitleFont(bannerText);
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.gameObject.SetActive(art == null || art.GoldenVinylEmblem == null);

        var subGo = CreateUIObject("SubLabel", root.transform);
        var subRect = subGo.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.SummaryStatsTopOffset - 40f);
        subRect.sizeDelta = new Vector2(300f, 30f);
        var subText = subGo.AddComponent<TextMeshProUGUI>();
        subText.text = "GOLDEN VINYL";
        subText.fontSize = 22;
        ApplyTitleFont(subText);
        subText.alignment = TextAlignmentOptions.Center;
        subText.color = TrophyLayoutSpec.ValueGold;
        subText.gameObject.SetActive(art == null || art.GoldenVinylEmblem == null);

        // Bottom-anchored region reserved for the Top Songs block
        float topSongsReserved = TrophyLayoutSpec.SummaryStatsBottomInset
                                 + TrophyLayoutSpec.TopSongsSectionHeight
                                 + TrophyLayoutSpec.TopSongsSectionTopGap;

        var statsGo = CreateUIObject("BandStatList", root.transform);
        var statsRect = statsGo.GetComponent<RectTransform>();
        statsRect.anchorMin = Vector2.zero;
        statsRect.anchorMax = Vector2.one;
        statsRect.offsetMin = new Vector2(TrophyLayoutSpec.SummaryStatsHorizontalInset, topSongsReserved);
        statsRect.offsetMax = new Vector2(-TrophyLayoutSpec.SummaryStatsHorizontalInset, -TrophyLayoutSpec.SummaryStatsTopOffset);
        statsGo.AddComponent<RectMask2D>();
        var statsVlg = statsGo.AddComponent<VerticalLayoutGroup>();
        statsVlg.spacing = TrophyLayoutSpec.SummaryStatRowSpacing;
        statsVlg.childControlHeight = false;
        statsVlg.childForceExpandHeight = false;
        statsVlg.childControlWidth = true;
        statsVlg.childForceExpandWidth = true;
        statsVlg.childAlignment = TextAnchor.UpperCenter;
        statsVlg.padding = new RectOffset(4, 4, 0, 0);

        // ── Top Songs block (bottom of band panel) ────────────────────────────
        var topSongsGo = CreateUIObject("TopSongsSection", root.transform);
        var topSongsRect = topSongsGo.GetComponent<RectTransform>();
        topSongsRect.anchorMin = new Vector2(0f, 0f);
        topSongsRect.anchorMax = new Vector2(1f, 0f);
        topSongsRect.pivot = new Vector2(0.5f, 0f);
        topSongsRect.anchoredPosition = new Vector2(0f, TrophyLayoutSpec.SummaryStatsBottomInset);
        topSongsRect.sizeDelta = new Vector2(-TrophyLayoutSpec.SummaryStatsHorizontalInset * 2f, TrophyLayoutSpec.TopSongsSectionHeight);

        var topSongsHeaderGo = CreateUIObject("TopSongsHeader", topSongsGo.transform);
        var topSongsHeaderRect = topSongsHeaderGo.GetComponent<RectTransform>();
        topSongsHeaderRect.anchorMin = new Vector2(0f, 1f);
        topSongsHeaderRect.anchorMax = new Vector2(1f, 1f);
        topSongsHeaderRect.pivot = new Vector2(0.5f, 1f);
        topSongsHeaderRect.anchoredPosition = Vector2.zero;
        topSongsHeaderRect.sizeDelta = new Vector2(0f, TrophyLayoutSpec.TopSongsHeaderHeight);
        var topSongsHeaderText = topSongsHeaderGo.AddComponent<TextMeshProUGUI>();
        topSongsHeaderText.text = "TOP SONGS";
        topSongsHeaderText.fontSize = TrophyLayoutSpec.TopSongsHeaderSize;
        topSongsHeaderText.color = TrophyLayoutSpec.TopSongsHeaderColor;
        ApplyTitleFont(topSongsHeaderText);
        topSongsHeaderText.alignment = TextAlignmentOptions.MidlineLeft;
        topSongsHeaderText.textWrappingMode = TextWrappingModes.NoWrap;

        var topSongsListGo = CreateUIObject("TopSongsList", topSongsGo.transform);
        var topSongsListRect = topSongsListGo.GetComponent<RectTransform>();
        topSongsListRect.anchorMin = Vector2.zero;
        topSongsListRect.anchorMax = Vector2.one;
        topSongsListRect.offsetMin = Vector2.zero;
        topSongsListRect.offsetMax = new Vector2(0f, -(TrophyLayoutSpec.TopSongsHeaderHeight + 4f));
        var topSongsVlg = topSongsListGo.AddComponent<VerticalLayoutGroup>();
        topSongsVlg.spacing = TrophyLayoutSpec.TopSongRowSpacing;
        topSongsVlg.childControlHeight = false;
        topSongsVlg.childForceExpandHeight = false;
        topSongsVlg.childControlWidth = true;
        topSongsVlg.childForceExpandWidth = true;
        topSongsVlg.childAlignment = TextAnchor.UpperCenter;

        var summary = root.GetComponent<CampaignSummaryCard>();
        var summarySo = new SerializedObject(summary);
        summarySo.FindProperty("_vinylBadge").objectReferenceValue = badgeImg;
        summarySo.FindProperty("_vinylGlow").objectReferenceValue = glowImg;
        summarySo.FindProperty("_completionBannerText").objectReferenceValue = bannerText;
        summarySo.FindProperty("_subLabelText").objectReferenceValue = subText;
        summarySo.FindProperty("_statRowContainer").objectReferenceValue = statsGo.transform;
        summarySo.FindProperty("_statRowPrefab").objectReferenceValue = rowPrefab.GetComponent<TrophyStatRow>();
        summarySo.FindProperty("_topSongsSection").objectReferenceValue = topSongsGo;
        summarySo.FindProperty("_topSongsHeader").objectReferenceValue = topSongsHeaderText;
        summarySo.FindProperty("_topSongsContainer").objectReferenceValue = topSongsListGo.transform;
        summarySo.ApplyModifiedProperties();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, SummaryPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildMenuPrefab(GameObject cardPrefab, GameObject summaryPrefab, TrophyScreenArt art)
    {
        var root = new GameObject("CampaignTrophyMenu",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
            typeof(MenuObject), typeof(CampaignTrophyController), typeof(CampaignStatsController), typeof(CampaignTrophyView));
        Stretch(root.GetComponent<RectTransform>());

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(TrophyLayoutSpec.ReferenceWidth, TrophyLayoutSpec.ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        var menuSo = new SerializedObject(root.GetComponent<MenuObject>());
        var menuProp = menuSo.FindProperty("<Menu>k__BackingField") ?? menuSo.FindProperty("_menu");
        if (menuProp != null)
            menuProp.enumValueIndex = (int)MenuManager.Menu.CareerTrophy;
        menuSo.ApplyModifiedProperties();

        var bgGo = CreateUIObject("Background", root.transform);
        Stretch(bgGo.GetComponent<RectTransform>());
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.raycastTarget = false;
        if (art != null && art.Background != null)
        {
            bgImg.sprite = art.Background;
            bgImg.color = Color.white;
        }
        else
        {
            bgImg.color = new Color(0.02f, 0.02f, 0.04f, 1f);
        }

        var dimGo = CreateUIObject("BackgroundDim", root.transform);
        Stretch(dimGo.GetComponent<RectTransform>());
        var dimImg = dimGo.AddComponent<Image>();
        dimImg.color = TrophyLayoutSpec.BackgroundDim;
        dimImg.raycastTarget = false;

        var titleGo = CreateUIObject("TitleBar", root.transform);
        var titleRect = titleGo.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.TitleTopInset);
        titleRect.sizeDelta = new Vector2(-TrophyLayoutSpec.HorizontalPadding * 2f, TrophyLayoutSpec.TitleHeight);
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "CAMPAIGN CONQUERED";
        titleText.fontSize = TrophyLayoutSpec.TitleFontSize;
        ApplyTitleFont(titleText);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = TrophyLayoutSpec.ValueGold;
        titleGo.AddComponent<GoldShineTextEffect>();

        var contentGo = CreateUIObject("ContentRow", root.transform);
        var contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(TrophyLayoutSpec.HorizontalPadding, TrophyLayoutSpec.ContentBottomInset);
        contentRect.offsetMax = new Vector2(-TrophyLayoutSpec.HorizontalPadding, -TrophyLayoutSpec.ContentTopOffset);
        var contentHlg = contentGo.AddComponent<HorizontalLayoutGroup>();
        contentHlg.spacing = TrophyLayoutSpec.ContentRowSpacing;
        contentHlg.childControlWidth = true;
        contentHlg.childControlHeight = true;
        contentHlg.childForceExpandWidth = true;
        contentHlg.childForceExpandHeight = true;
        contentHlg.childAlignment = TextAnchor.MiddleLeft;

        var summaryInstance = (GameObject)PrefabUtility.InstantiatePrefab(summaryPrefab, contentGo.transform);
        summaryInstance.name = "CampaignSummaryCard";

        var carouselGo = CreateUIObject("TrophyCardCarousel", contentGo.transform);
        var carouselLe = carouselGo.AddComponent<LayoutElement>();
        carouselLe.flexibleWidth = TrophyLayoutSpec.CarouselFlex;
        carouselLe.minWidth = TrophyLayoutSpec.CarouselMinWidth;
        Stretch(carouselGo.GetComponent<RectTransform>());

        var viewportGo = CreateUIObject("Viewport", carouselGo.transform);
        var viewportRect = viewportGo.GetComponent<RectTransform>();
        Stretch(viewportRect);
        viewportGo.AddComponent<RectMask2D>();

        var cardContainerGo = CreateUIObject("CardContainer", viewportGo.transform);
        var containerRect = cardContainerGo.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0f, 0.5f);
        containerRect.anchorMax = new Vector2(0f, 0.5f);
        containerRect.pivot = new Vector2(0f, 0.5f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(2000f, TrophyLayoutSpec.InstrumentCardSize.y);
        var containerHlg = cardContainerGo.AddComponent<HorizontalLayoutGroup>();
        containerHlg.spacing = TrophyLayoutSpec.CardSpacing;
        containerHlg.childAlignment = TextAnchor.MiddleLeft;
        containerHlg.childControlWidth = false;
        containerHlg.childControlHeight = false;
        containerHlg.childForceExpandWidth = false;
        containerHlg.childForceExpandHeight = false;
        containerHlg.padding = new RectOffset(0, (int)TrophyLayoutSpec.CarouselPeekWidth, 0, 0);

        var carousel = carouselGo.AddComponent<TrophyCardCarousel>();
        var carouselSo = new SerializedObject(carousel);
        carouselSo.FindProperty("_cardContainer").objectReferenceValue = cardContainerGo.transform;
        carouselSo.FindProperty("_viewport").objectReferenceValue = viewportRect;
        carouselSo.FindProperty("_cardPrefab").objectReferenceValue = cardPrefab.GetComponent<PlayerTrophyCard>();
        carouselSo.FindProperty("_art").objectReferenceValue = art;
        carouselSo.ApplyModifiedProperties();

        var view = root.GetComponent<CampaignTrophyView>();
        var viewSo = new SerializedObject(view);
        viewSo.FindProperty("_titleText").objectReferenceValue = titleText;
        viewSo.FindProperty("_summaryCard").objectReferenceValue = summaryInstance.GetComponent<CampaignSummaryCard>();
        viewSo.FindProperty("_carousel").objectReferenceValue = carousel;
        viewSo.FindProperty("_art").objectReferenceValue = art;
        viewSo.ApplyModifiedProperties();

        var controller = root.GetComponent<CampaignTrophyController>();
        var ctrlSo = new SerializedObject(controller);
        ctrlSo.FindProperty("_view").objectReferenceValue = view;
        ctrlSo.FindProperty("_statsController").objectReferenceValue = root.GetComponent<CampaignStatsController>();
        ctrlSo.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(root, MenuPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
