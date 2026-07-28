#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YARG.Menu.Career.Trophy;

/// <summary>
/// Generates PlayerTrophyCard and TrophyStatRow prefabs from TrophyLayoutSpec.
/// </summary>
public static class TrophyPrefabGenerator
{
    private const string TrophyFolder = "Assets/Prefabs/CareersM/03_Trophy";
    private const string RowPrefabPath = TrophyFolder + "/TrophyStatRow_Template.prefab";
    private const string CardPrefabPath = TrophyFolder + "/PlayerTrophyCard_FinalPrefab.prefab";
    private const string BarlowLightPath = "Assets/Art/Fonts/Barlow/Barlow-Light.asset";
    private const string BarlowBlackPath = "Assets/Art/Fonts/Barlow/Barlow-Black.asset";

    private static TMP_FontAsset _barlowLight;
    private static TMP_FontAsset _barlowBlack;

    private static void EnsureFonts()
    {
        _barlowLight ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarlowLightPath);
        _barlowBlack ??= AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarlowBlackPath);
    }

    private static void ApplyLabelFont(TextMeshProUGUI text)
    {
        EnsureFonts();
        if (_barlowLight != null)
            text.font = _barlowLight;
        text.fontStyle = FontStyles.Normal;
    }

    private static void ApplyValueFont(TextMeshProUGUI text)
    {
        EnsureFonts();
        if (_barlowBlack != null)
            text.font = _barlowBlack;
        text.fontStyle = FontStyles.Normal;
    }

    private static void ApplyTitleFont(TextMeshProUGUI text)
    {
        EnsureFonts();
        if (_barlowBlack != null)
            text.font = _barlowBlack;
        text.fontStyle = FontStyles.Normal;
    }

    [MenuItem("Tools/YARG Career Mod/Generate Trophy Prefabs")]
    public static void GenerateTrophyUI()
    {
        if (!AssetDatabase.IsValidFolder(TrophyFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs/CareersM", "03_Trophy");

        var rowPrefab = BuildStatRowPrefab();
        BuildCardPrefab(rowPrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TrophyPrefabGenerator] Stat row + instrument card prefabs rebuilt.");
    }

    public static GameObject BuildStatRowPrefab()
    {
        EnsureFonts();

        var rowRoot = new GameObject("TrophyStatRow_Template", typeof(RectTransform), typeof(LayoutElement), typeof(TrophyStatRow));
        var rect = rowRoot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400f, TrophyLayoutSpec.SummaryStatRowMinHeight);

        var le = rowRoot.GetComponent<LayoutElement>();
        le.minHeight = TrophyLayoutSpec.SummaryStatRowMinHeight;
        le.preferredHeight = TrophyLayoutSpec.SummaryStatRowMinHeight;
        le.flexibleWidth = 1f;

        var labelGo = CreateUIObject("StatLabel", rowRoot.transform);
        StretchAnchored(labelGo.GetComponent<RectTransform>(), 0f, TrophyLayoutSpec.StatLabelWidthFraction);
        var labelText = labelGo.AddComponent<TextMeshProUGUI>();
        labelText.text = "STAT LABEL";
        labelText.fontSize = TrophyLayoutSpec.SummaryStatLabelSize;
        labelText.color = TrophyLayoutSpec.SummaryLabelWhite;
        ApplyLabelFont(labelText);
        labelText.alignment = TextAlignmentOptions.MidlineLeft;
        labelText.overflowMode = TextOverflowModes.Ellipsis;
        labelText.textWrappingMode = TextWrappingModes.NoWrap;

        var valueGo = CreateUIObject("StatValue", rowRoot.transform);
        StretchAnchored(valueGo.GetComponent<RectTransform>(), TrophyLayoutSpec.StatLabelWidthFraction, 1f);
        var valueText = valueGo.AddComponent<TextMeshProUGUI>();
        valueText.text = "999,999";
        valueText.fontSize = TrophyLayoutSpec.SummaryStatValueSize;
        ApplyValueFont(valueText);
        valueText.alignment = TextAlignmentOptions.MidlineRight;
        valueText.overflowMode = TextOverflowModes.Ellipsis;
        valueText.textWrappingMode = TextWrappingModes.NoWrap;

        var rowScript = rowRoot.GetComponent<TrophyStatRow>();
        var serializedRow = new SerializedObject(rowScript);
        serializedRow.FindProperty("_labelMesh").objectReferenceValue = labelText;
        serializedRow.FindProperty("_valueMesh").objectReferenceValue = valueText;
        serializedRow.ApplyModifiedProperties();
        rowScript.ConfigureStyle(TrophyStatRowStyle.Summary);

        return SavePrefab(rowRoot, RowPrefabPath);
    }

    public static GameObject BuildCardPrefab(GameObject rowPrefab)
    {
        EnsureFonts();
        var art = AssetDatabase.LoadAssetAtPath<TrophyScreenArt>("Assets/Art/CareersM/Trophy/TrophyScreenArt.asset");

        var cardRoot = new GameObject("PlayerTrophyCard_FinalPrefab",
            typeof(RectTransform), typeof(CanvasGroup), typeof(Image), typeof(LayoutElement), typeof(Shadow), typeof(PlayerTrophyCard));
        var rootRect = cardRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = TrophyLayoutSpec.InstrumentCardSize;

        var cardLe = cardRoot.GetComponent<LayoutElement>();
        cardLe.preferredWidth = TrophyLayoutSpec.InstrumentCardSize.x;
        cardLe.preferredHeight = TrophyLayoutSpec.InstrumentCardSize.y;

        var shadow = cardRoot.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -4f);

        var bgImage = cardRoot.GetComponent<Image>();
        bgImage.type = Image.Type.Sliced;
        bgImage.color = Color.white;
        if (art != null && art.InstrumentPanel != null)
            bgImage.sprite = art.InstrumentPanel;

        var bottomGlowGo = CreateUIObject("BottomGlow", cardRoot.transform);
        var bottomGlowRect = bottomGlowGo.GetComponent<RectTransform>();
        bottomGlowRect.anchorMin = new Vector2(0f, 0f);
        bottomGlowRect.anchorMax = new Vector2(1f, 0f);
        bottomGlowRect.pivot = new Vector2(0.5f, 0f);
        bottomGlowRect.anchoredPosition = new Vector2(0f, 4f);
        bottomGlowRect.sizeDelta = new Vector2(-16f, TrophyLayoutSpec.BottomGlowHeight);
        var bottomGlowImg = bottomGlowGo.AddComponent<Image>();
        bottomGlowImg.raycastTarget = false;
        bottomGlowImg.type = Image.Type.Sliced;
        if (art != null && art.CardBottomGlow != null)
            bottomGlowImg.sprite = art.CardBottomGlow;

        var watermarkGo = CreateUIObject("InstrumentWatermark", cardRoot.transform);
        var watermarkRect = watermarkGo.GetComponent<RectTransform>();
        watermarkRect.anchorMin = new Vector2(1f, 0f);
        watermarkRect.anchorMax = new Vector2(1f, 0f);
        watermarkRect.pivot = new Vector2(1f, 0f);
        watermarkRect.anchoredPosition = new Vector2(-TrophyLayoutSpec.WatermarkMargin, TrophyLayoutSpec.WatermarkMargin);
        watermarkRect.sizeDelta = TrophyLayoutSpec.WatermarkSize;
        var watermarkImg = watermarkGo.AddComponent<Image>();
        watermarkImg.color = new Color(1f, 1f, 1f, TrophyLayoutSpec.WatermarkAlpha);
        watermarkImg.preserveAspect = true;
        watermarkImg.raycastTarget = false;

        var contentGo = CreateUIObject("AsymmetricBodySplit", cardRoot.transform);
        var contentRect = contentGo.GetComponent<RectTransform>();
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.offsetMin = new Vector2(TrophyLayoutSpec.BodyPadding, TrophyLayoutSpec.CardBodyBottomOffset);
        contentRect.offsetMax = new Vector2(-TrophyLayoutSpec.BodyPadding, -TrophyLayoutSpec.BodyTopOffset);
        var contentHLG = contentGo.AddComponent<HorizontalLayoutGroup>();
        contentHLG.spacing = TrophyLayoutSpec.StatColumnSpacing;
        contentHLG.childControlWidth = true;
        contentHLG.childControlHeight = true;
        contentHLG.childForceExpandWidth = true;
        contentHLG.childForceExpandHeight = true;

        var leftColGo = CreateUIObject("LeftFlexContainer", contentGo.transform);
        var leftLe = leftColGo.AddComponent<LayoutElement>();
        leftLe.flexibleWidth = 1f;
        var leftVLG = leftColGo.AddComponent<VerticalLayoutGroup>();
        leftVLG.spacing = TrophyLayoutSpec.CardStatRowSpacing;
        leftVLG.childControlHeight = false;
        leftVLG.childForceExpandHeight = false;
        leftVLG.childControlWidth = true;
        leftVLG.childForceExpandWidth = true;

        var rightColGo = CreateUIObject("RightConsistencyContainer", contentGo.transform);
        var rightLe = rightColGo.AddComponent<LayoutElement>();
        rightLe.flexibleWidth = 1f;
        var rightVLG = rightColGo.AddComponent<VerticalLayoutGroup>();
        rightVLG.spacing = TrophyLayoutSpec.CardStatRowSpacing;
        rightVLG.childControlHeight = false;
        rightVLG.childForceExpandHeight = false;
        rightVLG.childControlWidth = true;
        rightVLG.childForceExpandWidth = true;

        var highScoreGo = CreateUIObject("HighScoreAnchor", cardRoot.transform);
        var highScoreRect = highScoreGo.GetComponent<RectTransform>();
        highScoreRect.anchorMin = new Vector2(0f, 0f);
        highScoreRect.anchorMax = new Vector2(1f, 0f);
        highScoreRect.pivot = new Vector2(0.5f, 0f);
        highScoreRect.anchoredPosition = new Vector2(0f, TrophyLayoutSpec.BodyBottomPadding);
        highScoreRect.sizeDelta = new Vector2(-TrophyLayoutSpec.BodyPadding * 2f, TrophyLayoutSpec.HighScoreRowHeight);

        var highScoreBg = highScoreGo.AddComponent<Image>();
        highScoreBg.color = TrophyLayoutSpec.HighScoreBgTint;
        highScoreBg.raycastTarget = false;

        var dividerGo = CreateUIObject("Divider", highScoreGo.transform);
        var dividerRect = dividerGo.GetComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(1f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.anchoredPosition = Vector2.zero;
        dividerRect.sizeDelta = new Vector2(0f, TrophyLayoutSpec.HighScoreDividerHeight);
        var dividerImg = dividerGo.AddComponent<Image>();
        dividerImg.color = TrophyLayoutSpec.HighScoreDividerColor;
        dividerImg.raycastTarget = false;

        float hsValueRowFraction = TrophyLayoutSpec.HighScoreValueRowFraction;

        var hsLabelGo = CreateUIObject("HighScoreLabel", highScoreGo.transform);
        var hsLabelRect = hsLabelGo.GetComponent<RectTransform>();
        hsLabelRect.anchorMin = new Vector2(0f, hsValueRowFraction);
        hsLabelRect.anchorMax = new Vector2(TrophyLayoutSpec.StatLabelWidthFraction, 1f);
        hsLabelRect.offsetMin = Vector2.zero;
        hsLabelRect.offsetMax = Vector2.zero;
        var hsLabelText = hsLabelGo.AddComponent<TextMeshProUGUI>();
        hsLabelText.text = "HIGH SCORE";
        hsLabelText.fontSize = TrophyLayoutSpec.HighScoreLabelSize;
        hsLabelText.color = TrophyLayoutSpec.CardLabelWhite;
        ApplyLabelFont(hsLabelText);
        hsLabelText.alignment = TextAlignmentOptions.MidlineLeft;
        hsLabelText.textWrappingMode = TextWrappingModes.NoWrap;

        var hsValueGo = CreateUIObject("HighScoreValue", highScoreGo.transform);
        var hsValueRect = hsValueGo.GetComponent<RectTransform>();
        hsValueRect.anchorMin = new Vector2(TrophyLayoutSpec.StatLabelWidthFraction, hsValueRowFraction);
        hsValueRect.anchorMax = new Vector2(1f, 1f);
        hsValueRect.offsetMin = Vector2.zero;
        hsValueRect.offsetMax = Vector2.zero;
        var hsValueText = hsValueGo.AddComponent<TextMeshProUGUI>();
        hsValueText.text = "344,138";
        hsValueText.fontSize = TrophyLayoutSpec.HighScoreValueSize;
        hsValueText.color = TrophyLayoutSpec.HighScoreValueColor;
        ApplyValueFont(hsValueText);
        hsValueText.alignment = TextAlignmentOptions.MidlineRight;
        hsValueText.textWrappingMode = TextWrappingModes.NoWrap;
        hsValueText.overflowMode = TextOverflowModes.Overflow;

        var hsSongGo = CreateUIObject("HighScoreSong", highScoreGo.transform);
        var hsSongRect = hsSongGo.GetComponent<RectTransform>();
        hsSongRect.anchorMin = new Vector2(0f, 0f);
        hsSongRect.anchorMax = new Vector2(1f, hsValueRowFraction);
        hsSongRect.offsetMin = Vector2.zero;
        hsSongRect.offsetMax = Vector2.zero;
        var hsSongText = hsSongGo.AddComponent<TextMeshProUGUI>();
        hsSongText.text = "Song Title";
        hsSongText.fontSize = TrophyLayoutSpec.HighScoreSongSize;
        hsSongText.color = TrophyLayoutSpec.HighScoreSongColor;
        ApplyLabelFont(hsSongText);
        hsSongText.alignment = TextAlignmentOptions.MidlineLeft;
        hsSongText.textWrappingMode = TextWrappingModes.NoWrap;
        hsSongText.overflowMode = TextOverflowModes.Ellipsis;

        float starsRowTop = TrophyLayoutSpec.HeaderBandHeight + TrophyLayoutSpec.AccoladeStripHeight
                            + TrophyLayoutSpec.StarsRowTopGap;
        var starsGo = CreateUIObject("StarsRow", cardRoot.transform);
        var starsRect = starsGo.GetComponent<RectTransform>();
        starsRect.anchorMin = new Vector2(0f, 1f);
        starsRect.anchorMax = new Vector2(1f, 1f);
        starsRect.pivot = new Vector2(0.5f, 1f);
        starsRect.anchoredPosition = new Vector2(0f, -starsRowTop);
        starsRect.sizeDelta = new Vector2(-TrophyLayoutSpec.BodyPadding * 2f, TrophyLayoutSpec.StarSize + 4f);
        var starsHLG = starsGo.AddComponent<HorizontalLayoutGroup>();
        starsHLG.childAlignment = TextAnchor.MiddleCenter;
        starsHLG.childControlWidth = false;
        starsHLG.childControlHeight = false;
        starsHLG.spacing = TrophyLayoutSpec.StarSpacing;

        var starFilled = art != null ? art.StarFilled : null;
        var starEmpty = art != null ? art.StarEmpty : null;
        var cardStarsList = new List<Image>();
        for (int i = 1; i <= 5; i++)
        {
            var star = CreateUIObject($"Star_{i}", starsGo.transform);
            star.GetComponent<RectTransform>().sizeDelta = new Vector2(TrophyLayoutSpec.StarSize, TrophyLayoutSpec.StarSize);
            var starImg = star.AddComponent<Image>();
            starImg.sprite = i <= 3 ? starFilled : starEmpty;
            starImg.preserveAspect = true;
            cardStarsList.Add(starImg);
        }

        var accoladeGo = CreateUIObject("AccoladeStrip", cardRoot.transform);
        var accoladeRect = accoladeGo.GetComponent<RectTransform>();
        accoladeRect.anchorMin = new Vector2(0f, 1f);
        accoladeRect.anchorMax = new Vector2(1f, 1f);
        accoladeRect.pivot = new Vector2(0.5f, 1f);
        accoladeRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.HeaderBandHeight);
        accoladeRect.sizeDelta = new Vector2(0f, TrophyLayoutSpec.AccoladeStripHeight);
        var accoladeBg = accoladeGo.AddComponent<Image>();
        accoladeBg.color = TrophyLayoutSpec.AccoladeStripColor;
        accoladeBg.raycastTarget = false;

        var titleGo = CreateUIObject("AccoladeTitle", accoladeGo.transform);
        Stretch(titleGo.GetComponent<RectTransform>());
        var titleText = titleGo.AddComponent<TextMeshProUGUI>();
        titleText.text = "EXPERT VETERAN";
        titleText.fontSize = TrophyLayoutSpec.AccoladeFontSize;
        ApplyTitleFont(titleText);
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;

        var accentGo = CreateUIObject("NeonTopAccent", cardRoot.transform);
        var accentRect = accentGo.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 1f);
        accentRect.anchorMax = new Vector2(1f, 1f);
        accentRect.pivot = new Vector2(0.5f, 1f);
        accentRect.anchoredPosition = Vector2.zero;
        accentRect.sizeDelta = new Vector2(0f, TrophyLayoutSpec.HeaderBandHeight);
        var accentImg = accentGo.AddComponent<Image>();
        accentImg.type = Image.Type.Sliced;
        if (art != null && art.HeaderBand != null)
            accentImg.sprite = art.HeaderBand;

        var bannerRowGo = CreateUIObject("HeaderBannerRow", accentGo.transform);
        Stretch(bannerRowGo.GetComponent<RectTransform>());
        var bannerHlg = bannerRowGo.AddComponent<HorizontalLayoutGroup>();
        bannerHlg.childAlignment = TextAnchor.MiddleLeft;
        bannerHlg.spacing = 10f;
        bannerHlg.padding = new RectOffset((int)TrophyLayoutSpec.HeaderBannerPadding, (int)TrophyLayoutSpec.HeaderBannerPadding, 0, 0);
        bannerHlg.childControlWidth = false;
        bannerHlg.childControlHeight = true;
        bannerHlg.childForceExpandWidth = false;
        bannerHlg.childForceExpandHeight = true;

        var iconGo = CreateUIObject("InstrumentIcon", bannerRowGo.transform);
        var iconLe = iconGo.AddComponent<LayoutElement>();
        iconLe.preferredWidth = TrophyLayoutSpec.HeaderIconSize;
        iconLe.preferredHeight = TrophyLayoutSpec.HeaderIconSize;
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.color = Color.white;

        var instLabelGo = CreateUIObject("InstrumentLabel", bannerRowGo.transform);
        var instLabelLe = instLabelGo.AddComponent<LayoutElement>();
        instLabelLe.flexibleWidth = 1f;
        var instText = instLabelGo.AddComponent<TextMeshProUGUI>();
        instText.text = "GUITAR";
        instText.fontSize = TrophyLayoutSpec.InstrumentLabelFontSize;
        ApplyTitleFont(instText);
        instText.color = Color.white;
        instText.alignment = TextAlignmentOptions.MidlineLeft;
        instText.textWrappingMode = TextWrappingModes.NoWrap;

        var cardScript = cardRoot.GetComponent<PlayerTrophyCard>();
        var serializedCard = new SerializedObject(cardScript);
        serializedCard.FindProperty("_instrumentIcon").objectReferenceValue = iconImg;
        serializedCard.FindProperty("_instrumentLabel").objectReferenceValue = instText;
        serializedCard.FindProperty("_accoladeTitle").objectReferenceValue = titleText;
        serializedCard.FindProperty("_neonTopAccent").objectReferenceValue = accentImg;
        serializedCard.FindProperty("_accoladeStrip").objectReferenceValue = accoladeBg;
        serializedCard.FindProperty("_instrumentWatermark").objectReferenceValue = watermarkImg;
        serializedCard.FindProperty("_bottomGlow").objectReferenceValue = bottomGlowImg;
        serializedCard.FindProperty("_starsContainer").objectReferenceValue = starsGo.transform;
        serializedCard.FindProperty("_leftFlexContainer").objectReferenceValue = leftColGo.transform;
        serializedCard.FindProperty("_rightConsistencyContainer").objectReferenceValue = rightColGo.transform;
        serializedCard.FindProperty("_highScoreAnchor").objectReferenceValue = highScoreGo;
        serializedCard.FindProperty("_highScoreLabel").objectReferenceValue = hsLabelText;
        serializedCard.FindProperty("_highScoreValue").objectReferenceValue = hsValueText;
        serializedCard.FindProperty("_highScoreSong").objectReferenceValue = hsSongText;
        serializedCard.FindProperty("_statRowPrefab").objectReferenceValue = rowPrefab.GetComponent<TrophyStatRow>();
        serializedCard.FindProperty("_canvasGroup").objectReferenceValue = cardRoot.GetComponent<CanvasGroup>();

        var starsProperty = serializedCard.FindProperty("_uiStars");
        starsProperty.ClearArray();
        for (int i = 0; i < cardStarsList.Count; i++)
        {
            starsProperty.InsertArrayElementAtIndex(i);
            starsProperty.GetArrayElementAtIndex(i).objectReferenceValue = cardStarsList[i];
        }

        serializedCard.ApplyModifiedProperties();
        return SavePrefab(cardRoot, CardPrefabPath);
    }

    private static GameObject SavePrefab(GameObject root, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
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

    private static void StretchAnchored(RectTransform rect, float minX, float maxX)
    {
        rect.anchorMin = new Vector2(minX, 0f);
        rect.anchorMax = new Vector2(maxX, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif
