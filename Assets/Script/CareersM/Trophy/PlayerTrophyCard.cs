using System.Collections.Generic;

using TMPro;

using UnityEngine;

using UnityEngine.UI;

using YARG.Menu.Career.Motivation;



namespace YARG.Menu.Career.Trophy

{

    [RequireComponent(typeof(CanvasGroup))]

    public class PlayerTrophyCard : MonoBehaviour

    {

        [Header("Header Elements")]

        [SerializeField] private Image _instrumentIcon;

        [SerializeField] private TextMeshProUGUI _instrumentLabel;

        [SerializeField] private TextMeshProUGUI _accoladeTitle;

        [SerializeField] private Image _neonTopAccent;

        [SerializeField] private Image _accoladeStrip;

        [SerializeField] private Image _instrumentWatermark;

        [SerializeField] private Image _bottomGlow;

        [SerializeField] private Transform _starsContainer;

        [SerializeField] private List<Image> _uiStars;



        [Header("Asymmetric Containers")]

        [SerializeField] private Transform _leftFlexContainer;

        [SerializeField] private Transform _rightConsistencyContainer;

        [Header("High Score Anchor")]
        [SerializeField] private GameObject _highScoreAnchor;
        [SerializeField] private TextMeshProUGUI _highScoreLabel;
        [SerializeField] private TextMeshProUGUI _highScoreValue;
        [SerializeField] private TextMeshProUGUI _highScoreSong;



        [Header("Prefabs & Tuning")]

        [SerializeField] private TrophyStatRow _statRowPrefab;

        [SerializeField] private CanvasGroup _canvasGroup;



        private Image _cardFrame;

        private Sprite _starFilledSprite;

        private Sprite _starEmptySprite;

        private TrophyScreenArt _art;

        private bool _artApplied;

        private GameObject _proceduralIcon;

        private Color _themeColor = Color.white;



        private readonly List<TrophyStatRow> _leftPool = new();

        private readonly List<TrophyStatRow> _rightPool = new();

        private readonly List<Image> _starGlows = new();



        private void Awake()

        {

            _cardFrame ??= GetComponent<Image>();

            EnsureCardShadow();

        }



        public void ApplyArt(TrophyScreenArt art)

        {

            _art = art;

            if (art == null || _artApplied)

                return;



            _artApplied = true;

            _starFilledSprite = art.StarFilled;

            _starEmptySprite = art.StarEmpty;



            EnsureBottomGlow();



            if (_cardFrame != null && art.InstrumentPanel != null)

            {

                _cardFrame.sprite = art.InstrumentPanel;

                _cardFrame.type = Image.Type.Sliced;

                _cardFrame.color = Color.white;

            }



            if (_neonTopAccent != null && art.HeaderBand != null)

            {

                _neonTopAccent.sprite = art.HeaderBand;

                _neonTopAccent.type = Image.Type.Sliced;

            }



            if (_bottomGlow != null && art.CardBottomGlow != null)

            {

                _bottomGlow.sprite = art.CardBottomGlow;

                _bottomGlow.type = Image.Type.Sliced;

                _bottomGlow.gameObject.SetActive(true);

            }



            ApplyWatermarkLayout();

            EnsureStarGlows();

        }



        private void EnsureCardShadow()

        {

            if (_cardFrame == null)

                return;



            if (_cardFrame.GetComponent<Shadow>() == null)

            {

                var shadow = _cardFrame.gameObject.AddComponent<Shadow>();

                shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);

                shadow.effectDistance = new Vector2(0f, -4f);

            }

        }



        private void ApplyWatermarkLayout()

        {

            if (_instrumentWatermark == null)

                return;



            var wmRect = _instrumentWatermark.rectTransform;

            wmRect.anchorMin = new Vector2(1f, 0f);

            wmRect.anchorMax = new Vector2(1f, 0f);

            wmRect.pivot = new Vector2(1f, 0f);

            wmRect.anchoredPosition = new Vector2(-TrophyLayoutSpec.WatermarkMargin, TrophyLayoutSpec.WatermarkMargin);

            wmRect.sizeDelta = TrophyLayoutSpec.WatermarkSize;

            _instrumentWatermark.color = new Color(1f, 1f, 1f, TrophyLayoutSpec.WatermarkAlpha);

            _instrumentWatermark.raycastTarget = false;

            _instrumentWatermark.preserveAspect = true;

        }



        public void PopulateCard(TrophyCardData data)

        {

            _themeColor = data.ThemeColor;

            _instrumentLabel.text = data.InstrumentLabel.ToUpper();

            _instrumentLabel.color = Color.white;

            _accoladeTitle.text = data.AccoladeTitle.ToUpper();

            _accoladeTitle.color = Color.white;



            if (data.WatermarkSprite != null)

            {

                _instrumentWatermark.sprite = data.WatermarkSprite;

                _instrumentWatermark.gameObject.SetActive(true);

            }

            else

            {

                _instrumentWatermark.gameObject.SetActive(false);

            }



            if (_neonTopAccent != null)

                _neonTopAccent.color = data.ThemeColor;



            if (_accoladeStrip != null)

                _accoladeStrip.color = TrophyLayoutSpec.AccoladeStripColor;



            RefreshInstrumentIcon(data.InstrumentIndex, data.ThemeColor);

            RefreshStars(data.StarCount, data.ThemeColor);

            ApplyBottomGlowAlpha(isFocused: true);



            _leftPool.ForEach(row => row.gameObject.SetActive(false));

            _rightPool.ForEach(row => row.gameObject.SetActive(false));

            bool hasHighScore = !string.IsNullOrEmpty(data.HighScoreValue);
            if (_highScoreAnchor != null)
                _highScoreAnchor.SetActive(hasHighScore);

            if (hasHighScore && _highScoreValue != null)
            {
                if (_highScoreLabel != null)
                    _highScoreLabel.text = "HIGH SCORE";

                _highScoreValue.text = data.HighScoreValue;
                _highScoreValue.color = TrophyLayoutSpec.HighScoreValueColor;

                if (_highScoreSong != null)
                {
                    bool hasSong = !string.IsNullOrEmpty(data.HighScoreSong);
                    _highScoreSong.gameObject.SetActive(hasSong);
                    if (hasSong)
                        _highScoreSong.text = data.HighScoreSong;
                }
            }

            foreach (var stat in data.Stats)

            {

                if (stat.TargetColumn == StatColumn.LeftFlex)

                {

                    TrophyStatRow row = GetOrCreateRow(_leftPool, _leftFlexContainer);

                    row.ConfigureStyle(TrophyStatRowStyle.Card);

                    row.Setup(stat.Label, stat.Value, Color.white, TrophyLayoutSpec.CardLabelWhite);

                    row.gameObject.SetActive(true);

                }

                else

                {

                    TrophyStatRow row = GetOrCreateRow(_rightPool, _rightConsistencyContainer);

                    row.ConfigureStyle(TrophyStatRowStyle.Card);

                    row.Setup(stat.Label, stat.Value, Color.white, TrophyLayoutSpec.CardLabelWhite);

                    row.gameObject.SetActive(true);

                }

            }

        }



        private void RefreshInstrumentIcon(int instrumentIndex, Color themeColor)

        {

            if (_proceduralIcon != null)

            {

                Destroy(_proceduralIcon);

                _proceduralIcon = null;

            }



            Transform iconParent = _instrumentIcon != null ? _instrumentIcon.transform.parent : transform;

            Sprite headerSprite = _art?.GetHeaderIcon(instrumentIndex);



            if (_instrumentIcon != null && headerSprite != null)

            {

                _instrumentIcon.sprite = headerSprite;

                _instrumentIcon.color = Color.white;

                _instrumentIcon.gameObject.SetActive(true);

                return;

            }



            if (_instrumentIcon != null)

                _instrumentIcon.gameObject.SetActive(false);



            _proceduralIcon = instrumentIndex switch

            {

                0 => IconLibrary.CreateGuitar(iconParent, TrophyLayoutSpec.HeaderIconSize, Color.white),

                1 => IconLibrary.CreateMusicNote(iconParent, TrophyLayoutSpec.HeaderIconSize, Color.white),

                2 => IconLibrary.CreateTarget(iconParent, TrophyLayoutSpec.HeaderIconSize, Color.white),

                _ => IconLibrary.CreateMusicNote(iconParent, TrophyLayoutSpec.HeaderIconSize, Color.white),

            };

        }



        private void EnsureStarGlows()

        {

            if (_uiStars == null || _uiStars.Count == 0)

                return;



            _starGlows.Clear();

            foreach (var star in _uiStars)

            {

                if (star == null)

                    continue;



                var glowGo = star.transform.Find("StarGlow");

                if (glowGo == null)

                {

                    glowGo = new GameObject("StarGlow", typeof(RectTransform), typeof(Image)).transform;

                    glowGo.SetParent(star.transform, false);

                    glowGo.SetAsFirstSibling();

                    var glowRect = glowGo.GetComponent<RectTransform>();

                    glowRect.anchorMin = Vector2.zero;

                    glowRect.anchorMax = Vector2.one;

                    glowRect.offsetMin = Vector2.zero;

                    glowRect.offsetMax = Vector2.zero;

                    glowRect.localScale = Vector3.one * TrophyLayoutSpec.StarGlowScale;

                }



                var glowImg = glowGo.GetComponent<Image>();

                glowImg.raycastTarget = false;

                glowImg.preserveAspect = true;

                _starGlows.Add(glowImg);

            }

        }



        private void RefreshStars(int starCount, Color themeColor)

        {

            EnsureStarGlows();



            for (int i = 0; i < _uiStars.Count; i++)

            {

                var star = _uiStars[i];

                bool earned = i < starCount;

                star.gameObject.SetActive(true);



                Image glow = i < _starGlows.Count ? _starGlows[i] : null;

                if (glow != null)

                    glow.gameObject.SetActive(earned);



                if (earned && _starFilledSprite != null)

                {

                    star.sprite = _starFilledSprite;

                    star.color = themeColor;

                    if (glow != null)

                    {

                        glow.sprite = _starFilledSprite;

                        glow.color = new Color(themeColor.r, themeColor.g, themeColor.b, TrophyLayoutSpec.StarGlowAlpha);

                    }

                }

                else if (!earned && _starEmptySprite != null)

                {

                    star.sprite = _starEmptySprite;

                    star.color = new Color(1f, 1f, 1f, 0.35f);

                }

                else

                {

                    star.gameObject.SetActive(earned);

                    if (earned)

                        star.color = themeColor;

                }

            }

        }



        private void ApplyBottomGlowAlpha(bool isFocused)

        {

            if (_bottomGlow == null)

                return;



            float alpha = isFocused ? TrophyLayoutSpec.FocusedGlowAlpha : TrophyLayoutSpec.UnfocusedGlowAlpha;

            _bottomGlow.color = new Color(_themeColor.r, _themeColor.g, _themeColor.b, alpha);

        }



        private void EnsureBottomGlow()

        {

            if (_bottomGlow != null)

                return;



            var existing = transform.Find("BottomGlow");

            if (existing != null)

            {

                _bottomGlow = existing.GetComponent<Image>();

                return;

            }



            var glowGo = new GameObject("BottomGlow", typeof(RectTransform));

            glowGo.transform.SetParent(transform, false);

            glowGo.transform.SetAsFirstSibling();



            var rect = glowGo.GetComponent<RectTransform>();

            rect.anchorMin = new Vector2(0f, 0f);

            rect.anchorMax = new Vector2(1f, 0f);

            rect.pivot = new Vector2(0.5f, 0f);

            rect.anchoredPosition = new Vector2(0f, 4f);

            rect.sizeDelta = new Vector2(-16f, TrophyLayoutSpec.BottomGlowHeight);



            _bottomGlow = glowGo.AddComponent<Image>();

            _bottomGlow.raycastTarget = false;

        }



        private TrophyStatRow GetOrCreateRow(List<TrophyStatRow> pool, Transform container)

        {

            foreach (var row in pool)

            {

                if (!row.gameObject.activeSelf)

                    return row;

            }



            TrophyStatRow newRow = Instantiate(_statRowPrefab, container, false);

            pool.Add(newRow);

            return newRow;

        }



        public void SetFocusState(bool isFocused)

        {

            if (_canvasGroup == null)

                _canvasGroup = GetComponent<CanvasGroup>();



            transform.localScale = Vector3.one * (isFocused ? TrophyLayoutSpec.FocusedScale : TrophyLayoutSpec.UnfocusedScale);

            _canvasGroup.alpha = isFocused ? 1f : TrophyLayoutSpec.UnfocusedAlpha;

            ApplyBottomGlowAlpha(isFocused);

        }

    }

}


