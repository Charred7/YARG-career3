using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Menu.Career.Motivation;

namespace YARG.Menu.Career.Trophy
{
    /// <summary>
    /// Legacy runtime UI builder. Use editor-built CampaignTrophyMenu.prefab instead.
    /// </summary>
    [System.Obsolete("Use editor-built CampaignTrophyMenu.prefab from Tools/YARG/Build Campaign Trophy Prefabs.")]
    [DefaultExecutionOrder(-10)]
    public class CampaignTrophyMenuInitializer : MonoBehaviour
    {
        [Header("Child Prefabs")]
        [SerializeField] private PlayerTrophyCard _cardPrefab;
        [SerializeField] private TrophyStatRow _statRowPrefab;

        [Header("Art")]
        [SerializeField] private TrophyScreenArt _art;

        private void Awake()
        {
            if (GetComponentInChildren<CampaignTrophyView>(true) != null)
                return;

            Debug.LogWarning(
                "[CampaignTrophyMenuInitializer] Building UI at runtime (legacy). " +
                "Run Tools → YARG → Build Campaign Trophy Prefabs to use the editor-built layout.");

            TryLoadArtFromResources();
            BuildMenu();
        }

        private void TryLoadArtFromResources()
        {
            if (_art != null)
                return;

            _art = Resources.Load<TrophyScreenArt>("CareersM/Trophy/TrophyScreenArt");
        }

        private void BuildMenu()
        {
            EnsureCanvas();
            BuildBackground();
            BuildTitle(out var titleText);
            BuildContentRow(out var summaryCard, out var carousel);

            var view = gameObject.AddComponent<CampaignTrophyView>();
            view.Initialize(titleText, summaryCard, carousel, _art);

            var statsController = GetComponent<CampaignStatsController>();
            if (statsController == null)
                statsController = gameObject.AddComponent<CampaignStatsController>();

            var controller = GetComponent<CampaignTrophyController>();
            if (controller != null)
                controller.Initialize(view, statsController);
        }

        private void BuildBackground()
        {
            if (_art != null && _art.Background != null)
            {
                var bgGo = CreateUIObject("Background", transform);
                Stretch(bgGo.GetComponent<RectTransform>());
                var bgImg = bgGo.AddComponent<Image>();
                bgImg.sprite = _art.Background;
                bgImg.type = Image.Type.Simple;
                bgImg.preserveAspect = false;
                bgImg.color = Color.white;
                bgImg.raycastTarget = false;
            }
            else
            {
                CreateStretchImage("Background", transform, new Color(0.02f, 0.02f, 0.04f, 1f));
            }

            var dimGo = CreateUIObject("BackgroundDim", transform);
            Stretch(dimGo.GetComponent<RectTransform>());
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = TrophyLayoutSpec.BackgroundDim;
            dimImg.raycastTarget = false;
        }

        private void BuildTitle(out TextMeshProUGUI titleText)
        {
            var titleGo = CreateUIObject("TitleBar", transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.TitleTopInset);
            titleRect.sizeDelta = new Vector2(0f, TrophyLayoutSpec.TitleHeight);
            titleText = titleGo.AddComponent<TextMeshProUGUI>();
            titleText.text = "CAMPAIGN CONQUERED";
            titleText.fontSize = TrophyLayoutSpec.TitleFontSize;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.84f, 0.2f);
            titleGo.AddComponent<GoldShineTextEffect>();
        }

        private void BuildContentRow(out CampaignSummaryCard summaryCard, out TrophyCardCarousel carousel)
        {
            var contentGo = CreateUIObject("ContentRow", transform);
            var contentRect = contentGo.GetComponent<RectTransform>();
            Stretch(contentRect);
            contentRect.offsetMin = new Vector2(TrophyLayoutSpec.HorizontalPadding, TrophyLayoutSpec.ContentBottomInset);
            contentRect.offsetMax = new Vector2(-TrophyLayoutSpec.HorizontalPadding, -TrophyLayoutSpec.ContentTopOffset);
            var contentHlg = contentGo.AddComponent<HorizontalLayoutGroup>();
            contentHlg.spacing = TrophyLayoutSpec.ContentRowSpacing;
            contentHlg.childControlWidth = true;
            contentHlg.childControlHeight = true;
            contentHlg.childForceExpandWidth = true;
            contentHlg.childForceExpandHeight = true;

            summaryCard = BuildSummaryCard(contentGo.transform);
            var summaryLe = summaryCard.gameObject.AddComponent<LayoutElement>();
            summaryLe.flexibleWidth = TrophyLayoutSpec.SummaryFlex;
            summaryLe.minWidth = TrophyLayoutSpec.SummaryMinWidth;
            summaryLe.preferredWidth = TrophyLayoutSpec.SummaryPreferredWidth;

            carousel = BuildCarousel(contentGo.transform);
            var carouselLe = carousel.gameObject.AddComponent<LayoutElement>();
            carouselLe.flexibleWidth = TrophyLayoutSpec.CarouselFlex;
            carouselLe.minWidth = TrophyLayoutSpec.CarouselMinWidth;
        }

        private CampaignSummaryCard BuildSummaryCard(Transform parent)
        {
            var root = CreateUIObject("CampaignSummaryCard", parent);
            root.GetComponent<RectTransform>().sizeDelta = TrophyLayoutSpec.SummaryCardSize;

            var bg = root.AddComponent<Image>();
            if (_art != null && _art.SummaryPanel != null)
            {
                bg.sprite = _art.SummaryPanel;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = new Color(0.04f, 0.04f, 0.05f, 0.55f);
                var outline = root.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.84f, 0.2f, 0.8f);
                outline.effectDistance = new Vector2(2f, -2f);
            }

            bg.raycastTarget = false;

            Image glowImg = null;
            var glowGo = CreateUIObject("VinylGlow", root.transform);
            var glowRect = glowGo.GetComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 1f);
            glowRect.anchorMax = new Vector2(0.5f, 1f);
            glowRect.pivot = new Vector2(0.5f, 1f);
            glowRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.SummaryEmblemTopInset);
            glowRect.sizeDelta = TrophyLayoutSpec.SummaryEmblemSize;
            glowImg = glowGo.AddComponent<Image>();
            glowImg.color = new Color(1f, 0.84f, 0.2f, 0.2f);
            glowImg.raycastTarget = false;
            glowGo.SetActive(_art == null || _art.GoldenVinylEmblem == null);

            var badgeGo = CreateUIObject("VinylBadge", root.transform);
            var badgeRect = badgeGo.GetComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 1f);
            badgeRect.anchoredPosition = new Vector2(0f, -TrophyLayoutSpec.SummaryEmblemTopInset);
            badgeRect.sizeDelta = TrophyLayoutSpec.SummaryEmblemSize;
            var badgeImg = badgeGo.AddComponent<Image>();
            if (_art != null && _art.GoldenVinylEmblem != null)
            {
                badgeImg.sprite = _art.GoldenVinylEmblem;
                badgeImg.preserveAspect = true;
                badgeImg.color = Color.white;
            }

            badgeImg.raycastTarget = false;

            var bannerGo = CreateUIObject("CompletionBanner", root.transform);
            var bannerRect = bannerGo.GetComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0.5f, 1f);
            bannerRect.anchorMax = new Vector2(0.5f, 1f);
            bannerRect.pivot = new Vector2(0.5f, 1f);
            bannerRect.anchoredPosition = new Vector2(0f, -330f);
            bannerRect.sizeDelta = new Vector2(300f, 36f);
            var bannerText = bannerGo.AddComponent<TextMeshProUGUI>();
            bannerText.text = "100% COMPLETE";
            bannerText.fontSize = 18;
            bannerText.fontStyle = FontStyles.Bold;
            bannerText.alignment = TextAlignmentOptions.Center;

            var subGo = CreateUIObject("SubLabel", root.transform);
            var subRect = subGo.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 1f);
            subRect.anchorMax = new Vector2(0.5f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.anchoredPosition = new Vector2(0f, -370f);
            subRect.sizeDelta = new Vector2(300f, 30f);
            var subText = subGo.AddComponent<TextMeshProUGUI>();
            subText.text = "GOLDEN VINYL";
            subText.fontSize = 22;
            subText.fontStyle = FontStyles.Bold;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(1f, 0.84f, 0.2f);

            var statsGo = CreateUIObject("BandStatList", root.transform);
            var statsRect = statsGo.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0f);
            statsRect.anchorMax = new Vector2(1f, 1f);
            statsRect.offsetMin = new Vector2(TrophyLayoutSpec.SummaryStatsHorizontalInset,
                TrophyLayoutSpec.SummaryStatsBottomInset);
            statsRect.offsetMax = new Vector2(-TrophyLayoutSpec.SummaryStatsHorizontalInset,
                -TrophyLayoutSpec.SummaryStatsTopOffset);
            var statsVlg = statsGo.AddComponent<VerticalLayoutGroup>();
            statsVlg.spacing = TrophyLayoutSpec.SummaryStatRowSpacing;
            statsVlg.childControlHeight = false;
            statsVlg.childForceExpandHeight = false;
            statsVlg.childControlWidth = true;
            statsVlg.childForceExpandWidth = true;
            statsVlg.padding = new RectOffset(8, 8, 0, 0);

            var summary = root.AddComponent<CampaignSummaryCard>();
            summary.Initialize(badgeImg, glowImg, bannerText, subText, statsGo.transform, _statRowPrefab);
            summary.ConfigureArt(_art);
            return summary;
        }

        private TrophyCardCarousel BuildCarousel(Transform parent)
        {
            var carouselGo = CreateUIObject("TrophyCardCarousel", parent);
            Stretch(carouselGo.GetComponent<RectTransform>());

            var viewportGo = CreateUIObject("Viewport", carouselGo.transform);
            Stretch(viewportGo.GetComponent<RectTransform>());
            viewportGo.AddComponent<RectMask2D>();

            var containerGo = CreateUIObject("CardContainer", viewportGo.transform);
            var containerRect = containerGo.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0f, 0.5f);
            containerRect.anchorMax = new Vector2(0f, 0.5f);
            containerRect.pivot = new Vector2(0f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(2000f, TrophyLayoutSpec.InstrumentCardSize.y);
            var containerHlg = containerGo.AddComponent<HorizontalLayoutGroup>();
            containerHlg.spacing = TrophyLayoutSpec.CardSpacing;
            containerHlg.childAlignment = TextAnchor.MiddleLeft;
            containerHlg.childControlWidth = false;
            containerHlg.childControlHeight = false;
            containerHlg.padding = new RectOffset(0, (int)TrophyLayoutSpec.CarouselPeekWidth, 0, 0);

            var carousel = carouselGo.AddComponent<TrophyCardCarousel>();
            carousel.Initialize(containerGo.transform, _cardPrefab, _art, viewportGo.GetComponent<RectTransform>());
            return carousel;
        }

        private void EnsureCanvas()
        {
            if (GetComponent<Canvas>() == null)
            {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 150;
                gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                gameObject.AddComponent<GraphicRaycaster>();
            }

            var rect = GetComponent<RectTransform>();
            if (rect == null)
                rect = gameObject.AddComponent<RectTransform>();

            Stretch(rect);
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void CreateStretchImage(string name, Transform parent, Color color)
        {
            var go = CreateUIObject(name, parent);
            Stretch(go.GetComponent<RectTransform>());
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
