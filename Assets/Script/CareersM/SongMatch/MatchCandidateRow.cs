using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Core;
using YARG.Core.Song;
using YARG.Helpers.Extensions;
using YARG.Menu.MusicLibrary;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Accordion candidate row — compact collapsed strip / rich expanded card.
    /// </summary>
    public class MatchCandidateRow : MonoBehaviour
    {
        public const int LayoutVersion = 2;
        public const float CollapsedHeight = 44f;
        public const float ExpandedHeight = 198f;

        [SerializeField] private int _rowLayoutVersion = LayoutVersion;

        [SerializeField] private Image _fill;
        [SerializeField] private Image _borderOuter;
        [SerializeField] private Image _borderInner;
        [SerializeField] private LayoutElement _layoutElement;
        [SerializeField] private GameObject _collapsedRoot;
        [SerializeField] private GameObject _expandedRoot;
        [SerializeField] private GameObject _starBadge;

        [Header("Collapsed")]
        [SerializeField] private RawImage _collapsedThumb;
        [SerializeField] private TextMeshProUGUI _collapsedTitle;
        [SerializeField] private Image _collapsedBadgeBg;
        [SerializeField] private TextMeshProUGUI _collapsedBadgeText;
        [SerializeField] private TextMeshProUGUI _collapsedConfidence;

        [Header("Expanded")]
        [SerializeField] private RawImage _expandedThumb;
        [SerializeField] private TextMeshProUGUI _expandedTitle;
        [SerializeField] private TextMeshProUGUI _expandedArtist;
        [SerializeField] private Image _expandedBadgeBg;
        [SerializeField] private TextMeshProUGUI _expandedBadgeText;
        [SerializeField] private TextMeshProUGUI _expandedConfidence;
        [SerializeField] private TextMeshProUGUI _expandedSectionLabel;
        [SerializeField] private TextMeshProUGUI _expandedMeta;
        [SerializeField] private Transform _expandedRingsRow;
        [SerializeField] private DifficultyRing _difficultyRingPrefab;

        // Legacy serialized fields (ignored if new badge fields wired)
        [SerializeField] private TextMeshProUGUI _collapsedSource;
        [SerializeField] private TextMeshProUGUI _expandedSource;
        [SerializeField] private TextMeshProUGUI _expandedTiers;
        [SerializeField] private Image _outline;

        public SongEntry Song { get; private set; }
        public float Confidence { get; private set; }
        public bool IsCurrentMatch { get; private set; }
        public int RowLayoutVersion => _rowLayoutVersion;

        private readonly List<DifficultyRing> _rings = new();
        private CancellationTokenSource _albumCts;
        private bool _selected;

        public void Initialize(DifficultyRing ringPrefab)
        {
            if (ringPrefab != null)
                _difficultyRingPrefab = ringPrefab;
            EnsureExpandedRings();
        }

        public void Setup(SongEntry song, float confidence, bool isCurrentMatch)
        {
            Song = song;
            Confidence = confidence;
            IsCurrentMatch = isCurrentMatch;

            string title = song.Name.ToString();
            string artist = song.Artist.ToString();
            string rawSource = song.Source.ToString();
            string badge = SongMatchStyle.FormatSourceBadge(rawSource);
            var badgeColor = SongMatchStyle.SourceBadgeColor(rawSource);
            var confColor = SongMatchStyle.ConfidenceColor(confidence);
            string confPct = $"{confidence:P0}";
            string confRich =
                $"<color={SongMatchStyle.Hex(confColor)}>{confPct}</color>" +
                $" <size=55%><color={SongMatchStyle.Hex(confColor)}>MATCH</color></size>";

            if (_collapsedTitle != null)
                _collapsedTitle.text = title;
            ApplyBadge(_collapsedBadgeBg, _collapsedBadgeText, badge, badgeColor);
            if (_collapsedConfidence != null)
                _collapsedConfidence.text = confRich;
            // Legacy fallback
            if (_collapsedSource != null && _collapsedBadgeText == null)
                _collapsedSource.text = $"[{rawSource}]";

            if (_expandedTitle != null)
                _expandedTitle.text = title;
            if (_expandedArtist != null)
                _expandedArtist.text = artist;
            ApplyBadge(_expandedBadgeBg, _expandedBadgeText, badge, badgeColor);
            if (_expandedConfidence != null)
                _expandedConfidence.text = confRich;
            if (_expandedSectionLabel != null)
                _expandedSectionLabel.text = "Candidate Instrument Availability & Tiers";
            if (_expandedMeta != null)
                _expandedMeta.text = BuildMetaLine(song);
            if (_expandedSource != null && _expandedBadgeText == null)
                _expandedSource.text = badge;
            if (_expandedTiers != null)
                _expandedTiers.text = "";

            if (_starBadge != null)
                _starBadge.SetActive(isCurrentMatch);

            EnsureExpandedRings();
            UpdateExpandedRings(song);

            SetSelected(_selected);
            LoadAlbumCover(song).Forget();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;

            if (_collapsedRoot != null)
                _collapsedRoot.SetActive(!selected);
            if (_expandedRoot != null)
                _expandedRoot.SetActive(selected);

            float h = selected ? ExpandedHeight : CollapsedHeight;
            var le = _layoutElement != null
                ? _layoutElement
                : (_layoutElement = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>());
            le.ignoreLayout = false;
            le.minHeight = h;
            le.preferredHeight = h;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;

            // Keep rect in sync for first layout pass / non-controlled parents
            if (transform is RectTransform rt)
            {
                var size = rt.sizeDelta;
                size.y = h;
                rt.sizeDelta = size;
            }

            if (_fill != null)
            {
                _fill.color = selected
                    ? new Color(0.09f, 0.11f, 0.14f, 1f)
                    : new Color(0.10f, 0.10f, 0.12f, 0.95f);
            }

            SetDoubleBorder(selected);

            if (_outline != null)
            {
                bool useLegacy = _borderOuter == null;
                _outline.enabled = useLegacy && selected;
                _outline.color = selected ? SongMatchStyle.AccentSelect : Color.clear;
            }
        }

        public float CurrentHeight => _selected ? ExpandedHeight : CollapsedHeight;

        private void SetDoubleBorder(bool selected)
        {
            if (_borderOuter != null)
            {
                _borderOuter.enabled = selected;
                _borderOuter.color = selected
                    ? new Color(SongMatchStyle.AccentSelect.r, SongMatchStyle.AccentSelect.g,
                        SongMatchStyle.AccentSelect.b, 0.95f)
                    : Color.clear;
            }

            if (_borderInner != null)
            {
                _borderInner.enabled = selected;
                _borderInner.color = selected
                    ? new Color(SongMatchStyle.AccentSelect.r, SongMatchStyle.AccentSelect.g,
                        SongMatchStyle.AccentSelect.b, 0.75f)
                    : Color.clear;
            }
        }

        private void EnsureExpandedRings()
        {
            if (_expandedRingsRow == null || _difficultyRingPrefab == null)
                return;

            if (_rings.Count > 0)
                return;

            foreach (var existing in _expandedRingsRow.GetComponentsInChildren<DifficultyRing>(true))
                _rings.Add(existing);

            if (_rings.Count >= 6)
                return;

            _rings.Clear();
            for (int i = 0; i < 6; i++)
            {
                var ring = UnityEngine.Object.Instantiate(_difficultyRingPrefab, _expandedRingsRow);
                ring.gameObject.name = $"Ring_{i}";
                var le = ring.GetComponent<LayoutElement>() ?? ring.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 40f;
                le.preferredHeight = 40f;
                le.minWidth = 40f;
                le.minHeight = 40f;
                ring.transform.localScale = Vector3.one * 0.62f;
                _rings.Add(ring);
            }
        }

        private void UpdateExpandedRings(SongEntry entry)
        {
            if (_rings.Count < 6)
                return;

            foreach (var ring in _rings)
                ring.gameObject.SetActive(true);

            _rings[0].SetInfo("guitar", Instrument.FiveFretGuitar, entry[Instrument.FiveFretGuitar]);
            _rings[1].SetInfo("bass", Instrument.FiveFretBass, entry[Instrument.FiveFretBass]);

            if (entry.HasInstrument(Instrument.FiveLaneDrums))
                _rings[2].SetInfo("ghDrums", Instrument.FiveLaneDrums, entry[Instrument.FiveLaneDrums]);
            else if (entry.HasInstrument(Instrument.ProDrums))
                _rings[2].SetInfo("realDrums", Instrument.ProDrums, entry[Instrument.ProDrums]);
            else
                _rings[2].SetInfo("drums", Instrument.FourLaneDrums, entry[Instrument.FourLaneDrums]);

            _rings[3].SetInfo("keys", Instrument.Keys, entry[Instrument.Keys]);

            var vocals = GetVocals(entry);
            _rings[4].SetInfo(vocals.icon, Instrument.Vocals, vocals.values);
            _rings[5].SetInfo("band", Instrument.Band, entry[Instrument.Band]);
        }

        private static (string icon, PartValues values) GetVocals(SongEntry songEntry)
        {
            PartValues vocalsPart;
            if (!songEntry.HasInstrument(Instrument.Vocals) && songEntry.HasInstrument(Instrument.Harmony))
                vocalsPart = songEntry[Instrument.Harmony];
            else
                vocalsPart = songEntry[Instrument.Vocals];

            var partIcon = songEntry.VocalsCount switch
            {
                >= 3 => "harmVocals",
                2 => "twoVocals",
                _ => "vocals",
            };
            return (partIcon, vocalsPart);
        }

        private static string BuildMetaLine(SongEntry song)
        {
            var parts = new List<string>();
            string year = song.ParsedYear;
            if (!string.IsNullOrWhiteSpace(year) && year != "XXXX")
                parts.Add($"Year {year}");

            string genre = song.Genre.ToString();
            if (!string.IsNullOrWhiteSpace(genre))
                parts.Add(genre);

            string hash = song.Hash.ToString();
            if (!string.IsNullOrEmpty(hash))
            {
                if (hash.Length > 8)
                    hash = hash[..8];
                parts.Add($"HASH [{hash}…]");
            }

            return string.Join("  |  ", parts);
        }

        private static void ApplyBadge(Image bg, TextMeshProUGUI label, string text, Color color)
        {
            if (label != null)
                label.text = text;
            if (bg != null)
            {
                bg.color = color;
                bg.gameObject.SetActive(true);
            }
        }

        private async UniTaskVoid LoadAlbumCover(SongEntry songEntry)
        {
            CancelAlbumLoad();
            _albumCts = new CancellationTokenSource();
            var token = _albumCts.Token;

            Texture2D texture = null;
            using var image = await UniTask.RunOnThreadPool(songEntry.LoadAlbumData);
            if (image != null)
                texture = image.LoadTexture(false);

            if (token.IsCancellationRequested || Song != songEntry)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
                return;
            }

            ClearThumbs();
            BindThumb(_collapsedThumb, texture);
            BindThumb(_expandedThumb, texture);
        }

        private static void BindThumb(RawImage target, Texture2D texture)
        {
            if (target == null) return;
            target.texture = texture;
            target.uvRect = new Rect(0f, 0f, 1f, -1f);
            target.color = texture != null
                ? Color.white
                : new Color(0.12f, 0.12f, 0.14f, 1f);
        }

        private void CancelAlbumLoad()
        {
            if (_albumCts == null) return;
            _albumCts.Cancel();
            _albumCts.Dispose();
            _albumCts = null;
        }

        private void ClearThumbs()
        {
            Texture shared = null;
            if (_collapsedThumb != null && _collapsedThumb.texture != null)
                shared = _collapsedThumb.texture;
            else if (_expandedThumb != null && _expandedThumb.texture != null)
                shared = _expandedThumb.texture;

            if (_collapsedThumb != null)
            {
                _collapsedThumb.texture = null;
                _collapsedThumb.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            }
            if (_expandedThumb != null)
            {
                _expandedThumb.texture = null;
                _expandedThumb.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            }

            if (shared != null)
                UnityEngine.Object.Destroy(shared);
        }

        private void OnDisable()
        {
            CancelAlbumLoad();
            ClearThumbs();
        }

        public static MatchCandidateRow BuildRuntime(Transform parent, DifficultyRing ringPrefab = null)
        {
            var root = new GameObject("CandidateRow", typeof(RectTransform),
                typeof(LayoutElement), typeof(MatchCandidateRow));
            root.transform.SetParent(parent, false);

            var row = root.GetComponent<MatchCandidateRow>();
            var le = root.GetComponent<LayoutElement>();
            le.preferredHeight = CollapsedHeight;
            le.minHeight = CollapsedHeight;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;
            row._layoutElement = le;

            // Double-border frame (outer → inner → fill)
            var borderOuter = CreateChild("BorderOuter", root.transform);
            Stretch(borderOuter.GetComponent<RectTransform>(), 0f);
            row._borderOuter = borderOuter.AddComponent<Image>();
            row._borderOuter.color = Color.clear;
            row._borderOuter.raycastTarget = false;
            row._borderOuter.enabled = false;

            var gap = CreateChild("BorderGap", root.transform);
            Stretch(gap.GetComponent<RectTransform>(), 2f);
            var gapImg = gap.AddComponent<Image>();
            gapImg.color = new Color(0.06f, 0.07f, 0.09f, 1f);
            gapImg.raycastTarget = false;

            var borderInner = CreateChild("BorderInner", root.transform);
            Stretch(borderInner.GetComponent<RectTransform>(), 3f);
            row._borderInner = borderInner.AddComponent<Image>();
            row._borderInner.color = Color.clear;
            row._borderInner.raycastTarget = false;
            row._borderInner.enabled = false;

            var fillGo = CreateChild("Fill", root.transform);
            Stretch(fillGo.GetComponent<RectTransform>(), 5f);
            row._fill = fillGo.AddComponent<Image>();
            row._fill.color = new Color(0.10f, 0.10f, 0.12f, 0.95f);
            row._fill.raycastTarget = false;

            // ── Collapsed (single tight row) ──
            var collapsed = CreateChild("Collapsed", root.transform);
            Stretch(collapsed.GetComponent<RectTransform>(), 5f);
            row._collapsedRoot = collapsed;

            var cThumb = CreateChild("Thumb", collapsed.transform);
            var cThumbRt = cThumb.GetComponent<RectTransform>();
            cThumbRt.anchorMin = new Vector2(0f, 0.5f);
            cThumbRt.anchorMax = new Vector2(0f, 0.5f);
            cThumbRt.pivot = new Vector2(0f, 0.5f);
            cThumbRt.anchoredPosition = new Vector2(8f, 0f);
            cThumbRt.sizeDelta = new Vector2(30f, 30f);
            row._collapsedThumb = cThumb.AddComponent<RawImage>();
            row._collapsedThumb.color = new Color(0.12f, 0.12f, 0.14f, 1f);

            var cTitle = CreateChild("Title", collapsed.transform);
            var cTitleRt = cTitle.GetComponent<RectTransform>();
            cTitleRt.anchorMin = new Vector2(0f, 0f);
            cTitleRt.anchorMax = new Vector2(1f, 1f);
            cTitleRt.offsetMin = new Vector2(46f, 4f);
            cTitleRt.offsetMax = new Vector2(-210f, -4f);
            row._collapsedTitle = cTitle.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyTitleFont(row._collapsedTitle, 15f);
            row._collapsedTitle.alignment = TextAlignmentOptions.MidlineLeft;
            row._collapsedTitle.overflowMode = TextOverflowModes.Ellipsis;
            row._collapsedTitle.textWrappingMode = TextWrappingModes.NoWrap;
            row._collapsedTitle.characterSpacing = 0f;

            var cBadge = CreateBadge("Badge", collapsed.transform, out row._collapsedBadgeBg,
                out row._collapsedBadgeText);
            var cBadgeRt = cBadge.GetComponent<RectTransform>();
            cBadgeRt.anchorMin = new Vector2(1f, 0.5f);
            cBadgeRt.anchorMax = new Vector2(1f, 0.5f);
            cBadgeRt.pivot = new Vector2(1f, 0.5f);
            cBadgeRt.anchoredPosition = new Vector2(-118f, 0f);
            cBadgeRt.sizeDelta = new Vector2(78f, 20f);

            var cConf = CreateChild("Confidence", collapsed.transform);
            var cConfRt = cConf.GetComponent<RectTransform>();
            cConfRt.anchorMin = new Vector2(1f, 0.5f);
            cConfRt.anchorMax = new Vector2(1f, 0.5f);
            cConfRt.pivot = new Vector2(1f, 0.5f);
            cConfRt.anchoredPosition = new Vector2(-10f, 0f);
            cConfRt.sizeDelta = new Vector2(100f, 28f);
            row._collapsedConfidence = cConf.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyValueFont(row._collapsedConfidence, 14f, SongMatchStyle.TextWhite);
            row._collapsedConfidence.alignment = TextAlignmentOptions.MidlineRight;

            // ── Expanded ──
            var expanded = CreateChild("Expanded", root.transform);
            Stretch(expanded.GetComponent<RectTransform>(), 5f);
            expanded.SetActive(false);
            row._expandedRoot = expanded;

            var eTitle = CreateChild("Title", expanded.transform);
            Place(eTitle, 12f, -8f, 380f, 26f);
            row._expandedTitle = eTitle.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyTitleFont(row._expandedTitle, 20f);
            row._expandedTitle.alignment = TextAlignmentOptions.TopLeft;
            row._expandedTitle.overflowMode = TextOverflowModes.Ellipsis;
            row._expandedTitle.characterSpacing = 1f;

            var eArtist = CreateChild("Artist", expanded.transform);
            Place(eArtist, 12f, -32f, 360f, 20f);
            row._expandedArtist = eArtist.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyLabelFont(row._expandedArtist, 13f, SongMatchStyle.LabelMuted);
            row._expandedArtist.overflowMode = TextOverflowModes.Ellipsis;

            var eBadge = CreateBadge("Badge", expanded.transform, out row._expandedBadgeBg,
                out row._expandedBadgeText);
            var eBadgeRt = eBadge.GetComponent<RectTransform>();
            eBadgeRt.anchorMin = new Vector2(1f, 1f);
            eBadgeRt.anchorMax = new Vector2(1f, 1f);
            eBadgeRt.pivot = new Vector2(1f, 1f);
            eBadgeRt.anchoredPosition = new Vector2(-128f, -10f);
            eBadgeRt.sizeDelta = new Vector2(86f, 22f);

            var eConf = CreateChild("Confidence", expanded.transform);
            var eConfRt = eConf.GetComponent<RectTransform>();
            eConfRt.anchorMin = new Vector2(1f, 1f);
            eConfRt.anchorMax = new Vector2(1f, 1f);
            eConfRt.pivot = new Vector2(1f, 1f);
            eConfRt.anchoredPosition = new Vector2(-12f, -8f);
            eConfRt.sizeDelta = new Vector2(110f, 28f);
            row._expandedConfidence = eConf.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyValueFont(row._expandedConfidence, 18f, SongMatchStyle.TextWhite);
            row._expandedConfidence.alignment = TextAlignmentOptions.TopRight;

            var eThumb = CreateChild("Thumb", expanded.transform);
            var eThumbRt = eThumb.GetComponent<RectTransform>();
            eThumbRt.anchorMin = new Vector2(0f, 1f);
            eThumbRt.anchorMax = new Vector2(0f, 1f);
            eThumbRt.pivot = new Vector2(0f, 1f);
            eThumbRt.anchoredPosition = new Vector2(12f, -58f);
            eThumbRt.sizeDelta = new Vector2(88f, 88f);
            row._expandedThumb = eThumb.AddComponent<RawImage>();
            row._expandedThumb.color = new Color(0.12f, 0.12f, 0.14f, 1f);

            var eSection = CreateChild("SectionLabel", expanded.transform);
            Place(eSection, 114f, -58f, 420f, 18f);
            row._expandedSectionLabel = eSection.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyLabelFont(row._expandedSectionLabel, 12f, SongMatchStyle.LabelMuted);

            var eMeta = CreateChild("Meta", expanded.transform);
            Place(eMeta, 114f, -78f, 480f, 18f);
            row._expandedMeta = eMeta.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyLabelFont(row._expandedMeta, 11f, SongMatchStyle.LabelMuted);
            row._expandedMeta.overflowMode = TextOverflowModes.Ellipsis;

            var rings = CreateChild("Rings", expanded.transform);
            var ringsRt = rings.GetComponent<RectTransform>();
            ringsRt.anchorMin = new Vector2(0f, 1f);
            ringsRt.anchorMax = new Vector2(1f, 1f);
            ringsRt.pivot = new Vector2(0f, 1f);
            ringsRt.anchoredPosition = new Vector2(114f, -100f);
            ringsRt.sizeDelta = new Vector2(-130f, 48f);
            var hlg = rings.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 6f;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            row._expandedRingsRow = rings.transform;

            var star = CreateChild("Star", root.transform);
            var starRt = star.GetComponent<RectTransform>();
            starRt.anchorMin = new Vector2(0f, 1f);
            starRt.anchorMax = new Vector2(0f, 1f);
            starRt.pivot = new Vector2(0f, 1f);
            starRt.anchoredPosition = new Vector2(8f, -4f);
            starRt.sizeDelta = new Vector2(18f, 18f);
            var starTmp = star.AddComponent<TextMeshProUGUI>();
            starTmp.text = "★";
            SongMatchChrome.ApplyValueFont(starTmp, 14f, SongMatchStyle.Gold);
            starTmp.alignment = TextAlignmentOptions.Center;
            row._starBadge = star;
            star.SetActive(false);

            row._difficultyRingPrefab = ringPrefab;
            row._rowLayoutVersion = LayoutVersion;
            row.Initialize(ringPrefab);
            return row;
        }

        private static GameObject CreateBadge(string name, Transform parent,
            out Image bg, out TextMeshProUGUI label)
        {
            var go = CreateChild(name, parent);
            bg = go.AddComponent<Image>();
            bg.color = SongMatchStyle.BadgeDefault;

            var textGo = CreateChild("Label", go.transform);
            Stretch(textGo.GetComponent<RectTransform>(), 2f);
            label = textGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyValueFont(label, 10f, Color.white);
            label.alignment = TextAlignmentOptions.Center;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            return go;
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

        private static void Place(GameObject go, float x, float y, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }
    }
}
