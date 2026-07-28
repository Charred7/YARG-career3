using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Core.Song;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Premium candidate tile — readable title, soft selection glow, no wireframe.
    /// </summary>
    public class MatchCandidateCard : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private Image _glow;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _confidenceText;
        [SerializeField] private TextMeshProUGUI _metaText;
        [SerializeField] private GameObject _starBadge;
        [SerializeField] private LayoutElement _layoutElement;

        public SongEntry Song { get; private set; }
        public float Confidence { get; private set; }
        public bool IsCurrentMatch { get; private set; }

        public void Setup(SongEntry song, float confidence, bool isCurrentMatch)
        {
            Song = song;
            Confidence = confidence;
            IsCurrentMatch = isCurrentMatch;

            if (_titleText != null)
                _titleText.text = song.Name.ToString();

            if (_confidenceText != null)
            {
                var color = SongMatchStyle.ConfidenceColor(confidence);
                _confidenceText.text = $"<color={SongMatchStyle.Hex(color)}>{confidence:P0}</color>";
            }

            if (_metaText != null)
            {
                string artist = song.Artist.ToString();
                string source = song.Source.ToString();
                _metaText.text = $"{artist}  ·  {source}";
            }

            if (_starBadge != null)
                _starBadge.SetActive(isCurrentMatch);

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (_fill != null)
                _fill.color = selected ? SongMatchStyle.PanelGlassSelected : new Color(1f, 1f, 1f, 0.06f);

            if (_glow != null)
            {
                _glow.enabled = selected;
                _glow.color = selected ? SongMatchStyle.GoldSoft : Color.clear;
            }

            if (_layoutElement != null)
            {
                _layoutElement.preferredWidth = selected ? 300f : 280f;
                _layoutElement.preferredHeight = selected ? 156f : 144f;
            }

            transform.localScale = selected ? Vector3.one * 1.02f : Vector3.one;
        }

        public static MatchCandidateCard BuildRuntime(Transform parent)
        {
            var root = new GameObject("CandidateCard", typeof(RectTransform),
                typeof(LayoutElement), typeof(MatchCandidateCard));
            root.transform.SetParent(parent, false);

            var card = root.GetComponent<MatchCandidateCard>();
            var le = root.GetComponent<LayoutElement>();
            le.preferredWidth = 280f;
            le.preferredHeight = 144f;
            le.minWidth = 260f;
            le.minHeight = 130f;
            card._layoutElement = le;

            // Soft glow behind (selected only)
            var glowGo = CreateChild("Glow", root.transform);
            Stretch(glowGo.GetComponent<RectTransform>(), -6f);
            card._glow = glowGo.AddComponent<Image>();
            card._glow.color = Color.clear;
            card._glow.raycastTarget = false;
            card._glow.enabled = false;

            // Card body — instrument panel 9-slice when available
            var fillGo = CreateChild("Fill", root.transform);
            Stretch(fillGo.GetComponent<RectTransform>(), 0f);
            card._fill = fillGo.AddComponent<Image>();
            SongMatchChrome.ApplySlicedPanel(card._fill, SongMatchChrome.InstrumentPanel, 1.4f);
            if (card._fill.sprite == null)
                card._fill.color = new Color(1f, 1f, 1f, 0.06f);
            card._fill.raycastTarget = false;

            var starGo = CreateChild("Star", root.transform);
            var starRt = starGo.GetComponent<RectTransform>();
            starRt.anchorMin = new Vector2(1f, 1f);
            starRt.anchorMax = new Vector2(1f, 1f);
            starRt.pivot = new Vector2(1f, 1f);
            starRt.anchoredPosition = new Vector2(-14f, -12f);
            starRt.sizeDelta = new Vector2(26f, 22f);
            var starText = starGo.AddComponent<TextMeshProUGUI>();
            starText.text = "★";
            SongMatchChrome.ApplyValueFont(starText, 18f, SongMatchStyle.Gold);
            starText.alignment = TextAlignmentOptions.Center;
            card._starBadge = starGo;

            var confGo = CreateChild("Confidence", root.transform);
            var confRt = confGo.GetComponent<RectTransform>();
            confRt.anchorMin = new Vector2(0f, 0.68f);
            confRt.anchorMax = new Vector2(1f, 1f);
            confRt.offsetMin = new Vector2(18f, 0f);
            confRt.offsetMax = new Vector2(-18f, -12f);
            card._confidenceText = confGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyValueFont(card._confidenceText, 30f, SongMatchStyle.TextWhite);
            card._confidenceText.alignment = TextAlignmentOptions.BottomLeft;

            var titleGo = CreateChild("Title", root.transform);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.28f);
            titleRt.anchorMax = new Vector2(1f, 0.70f);
            titleRt.offsetMin = new Vector2(18f, 0f);
            titleRt.offsetMax = new Vector2(-18f, 0f);
            card._titleText = titleGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyTitleFont(card._titleText, 18f);
            card._titleText.alignment = TextAlignmentOptions.TopLeft;
            card._titleText.overflowMode = TextOverflowModes.Ellipsis;
            card._titleText.textWrappingMode = TextWrappingModes.Normal;
            card._titleText.maxVisibleLines = 2;
            card._titleText.lineSpacing = -12f;

            var metaGo = CreateChild("Meta", root.transform);
            var metaRt = metaGo.GetComponent<RectTransform>();
            metaRt.anchorMin = new Vector2(0f, 0f);
            metaRt.anchorMax = new Vector2(1f, 0.28f);
            metaRt.offsetMin = new Vector2(18f, 12f);
            metaRt.offsetMax = new Vector2(-18f, 0f);
            card._metaText = metaGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyLabelFont(card._metaText, 13f, SongMatchStyle.LabelMuted);
            card._metaText.alignment = TextAlignmentOptions.TopLeft;
            card._metaText.overflowMode = TextOverflowModes.Ellipsis;
            card._metaText.textWrappingMode = TextWrappingModes.NoWrap;

            return card;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }
}
