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
    /// Selected-candidate details: art, meta, source/hash, stock DifficultyRings.
    /// </summary>
    public class MatchDetailInspector : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _headerText;
        [SerializeField] private RawImage _albumCover;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _artistText;
        [SerializeField] private TextMeshProUGUI _metaText;
        [SerializeField] private TextMeshProUGUI _sourceText;
        [SerializeField] private TextMeshProUGUI _hashText;
        [SerializeField] private TextMeshProUGUI _confidenceText;
        [SerializeField] private Transform _ringsTopRow;
        [SerializeField] private Transform _ringsBottomRow;
        [SerializeField] private DifficultyRing _difficultyRingPrefab;
        [SerializeField] private GameObject _emptyState;

        private readonly List<DifficultyRing> _rings = new();
        private CancellationTokenSource _albumCts;
        private SongEntry _currentSong;

        public void Initialize(DifficultyRing ringPrefab)
        {
            if (ringPrefab != null)
                _difficultyRingPrefab = ringPrefab;

            EnsureHeader();
            EnsureRings();
        }

        public void ShowEmpty(string message = "Select a career song to browse matches")
        {
            _currentSong = null;
            CancelAlbumLoad();
            ClearAlbum();

            if (_emptyState != null)
                _emptyState.SetActive(true);

            EnsureHeader();
            SetText(_titleText, "");
            SetText(_artistText, "");
            SetText(_metaText, message);
            SetText(_sourceText, "");
            SetText(_hashText, "");
            SetText(_confidenceText, "");

            foreach (var ring in _rings)
                ring.gameObject.SetActive(false);
        }

        public void ShowCandidate(SongEntry song, float confidence, bool isCurrentMatch)
        {
            if (song == null)
            {
                ShowEmpty();
                return;
            }

            _currentSong = song;
            if (_emptyState != null)
                _emptyState.SetActive(false);

            EnsureHeader();

            SetText(_titleText, song.Name.ToString().ToUpperInvariant());
            SetText(_artistText, song.Artist.ToString());

            string year = song.ParsedYear;
            string genre = song.Genre.ToString();
            var metaParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(genre))
                metaParts.Add($"Genre: {genre}");
            if (!string.IsNullOrWhiteSpace(year) && year != "XXXX")
                metaParts.Add($"Year: {year}");
            SetText(_metaText, string.Join("\n", metaParts));

            string source = song.Source.ToString();
            SetText(_sourceText, string.IsNullOrWhiteSpace(source)
                ? ""
                : $"Source: [{source.Trim().Trim('[', ']').ToLowerInvariant()}]");

            string hash = song.Hash.ToString();
            SetText(_hashText, string.IsNullOrEmpty(hash) ? "" : $"Full hash: {hash}");

            var confColor = SongMatchStyle.ConfidenceColor(confidence);
            string star = isCurrentMatch ? "  ★" : "";
            SetText(_confidenceText,
                $"<color={SongMatchStyle.Hex(confColor)}>{confidence:P0}</color>" +
                $"<size=55%> MATCH{star}</size>");

            EnsureRings();
            UpdateDifficulties(song);
            LoadAlbumCover(song).Forget();
        }

        private void EnsureHeader()
        {
            if (_headerText != null)
                _headerText.text = "SELECTED CANDIDATE DETAILS";
        }

        private void EnsureRings()
        {
            if (_rings.Count > 0)
            {
                CondenseRingLayout();
                return;
            }

            if (_ringsTopRow != null)
            {
                foreach (var ring in _ringsTopRow.GetComponentsInChildren<DifficultyRing>(true))
                    _rings.Add(ring);
            }
            if (_ringsBottomRow != null)
            {
                foreach (var ring in _ringsBottomRow.GetComponentsInChildren<DifficultyRing>(true))
                    _rings.Add(ring);
            }

            if (_rings.Count >= 10)
            {
                CondenseRingLayout();
                return;
            }

            if (_difficultyRingPrefab == null || _ringsTopRow == null)
                return;

            _rings.Clear();
            // Prefer single dense row (render); fall back to top+bottom if only top exists
            var primary = _ringsTopRow;
            for (int i = 0; i < 10; i++)
                _rings.Add(SpawnRing(primary, $"DifficultyRing_{i}"));

            if (_ringsBottomRow != null)
                _ringsBottomRow.gameObject.SetActive(false);

            CondenseRingLayout();
        }

        private DifficultyRing SpawnRing(Transform parent, string name)
        {
            var ring = UnityEngine.Object.Instantiate(_difficultyRingPrefab, parent);
            ring.gameObject.name = name;
            var le = ring.gameObject.GetComponent<LayoutElement>()
                     ?? ring.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = 42f;
            le.preferredHeight = 42f;
            le.minWidth = 42f;
            le.minHeight = 42f;
            ring.transform.localScale = Vector3.one * 0.58f;
            return ring;
        }

        /// <summary>
        /// Collapse authored two-row rings into one dense bottom strip (render layout).
        /// </summary>
        private void CondenseRingLayout()
        {
            if (_ringsTopRow == null)
                return;

            // Prefer a single bottom-anchored row; pull rings from bottom into top if needed
            if (_ringsBottomRow != null && _ringsBottomRow != _ringsTopRow)
            {
                for (int i = _ringsBottomRow.childCount - 1; i >= 0; i--)
                    _ringsBottomRow.GetChild(i).SetParent(_ringsTopRow, false);
                _ringsBottomRow.gameObject.SetActive(false);
            }

            var rt = _ringsTopRow as RectTransform;
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 10f);
                rt.sizeDelta = new Vector2(-40f, 46f);
            }

            var hlg = _ringsTopRow.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
            {
                hlg.spacing = 4f;
                hlg.padding = new RectOffset(20, 20, 0, 0);
                hlg.childAlignment = TextAnchor.MiddleLeft;
            }

            foreach (var ring in _ringsTopRow.GetComponentsInChildren<DifficultyRing>(true))
            {
                var le = ring.GetComponent<LayoutElement>() ?? ring.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = 42f;
                le.preferredHeight = 42f;
                le.minWidth = 42f;
                le.minHeight = 42f;
                ring.transform.localScale = Vector3.one * 0.58f;
            }
        }

        private void UpdateDifficulties(SongEntry entry)
        {
            foreach (var ring in _rings)
                ring.gameObject.SetActive(true);

            if (_rings.Count < 10)
                return;

            _rings[0].SetInfo("guitar", Instrument.FiveFretGuitar, entry[Instrument.FiveFretGuitar]);
            _rings[1].SetInfo("bass", Instrument.FiveFretBass, entry[Instrument.FiveFretBass]);

            if (entry.HasInstrument(Instrument.FiveLaneDrums))
                _rings[2].SetInfo("ghDrums", Instrument.FiveLaneDrums, entry[Instrument.FiveLaneDrums]);
            else if (entry.HasInstrument(Instrument.ProDrums))
                _rings[2].SetInfo("realDrums", Instrument.ProDrums, entry[Instrument.ProDrums]);
            else
                _rings[2].SetInfo("drums", Instrument.FourLaneDrums, entry[Instrument.FourLaneDrums]);

            _rings[3].SetInfo("keys", Instrument.Keys, entry[Instrument.Keys]);

            var vocalsPart = GetVocalsPartValues(entry);
            _rings[4].SetInfo(vocalsPart.PartIcon, Instrument.Vocals, vocalsPart.PartValues);

            if (entry.HasInstrument(Instrument.ProGuitar_17Fret) || entry.HasInstrument(Instrument.ProGuitar_22Fret))
            {
                var values = entry[Instrument.ProGuitar_17Fret];
                var instrument = Instrument.ProGuitar_17Fret;
                if (values.Intensity == -1 && entry.HasInstrument(Instrument.ProGuitar_22Fret))
                {
                    values = entry[Instrument.ProGuitar_22Fret];
                    instrument = Instrument.ProGuitar_22Fret;
                }
                _rings[5].SetInfo("realGuitar", instrument, values);
            }
            else
            {
                _rings[5].SetInfo("guitarCoop", Instrument.FiveFretCoopGuitar, entry[Instrument.FiveFretCoopGuitar]);
            }

            if (entry.HasInstrument(Instrument.ProBass_17Fret) || entry.HasInstrument(Instrument.ProBass_22Fret))
            {
                var values = entry[Instrument.ProBass_17Fret];
                var instrument = Instrument.ProBass_17Fret;
                if (values.Intensity == -1 && entry.HasInstrument(Instrument.ProBass_22Fret))
                {
                    values = entry[Instrument.ProBass_22Fret];
                    instrument = Instrument.ProBass_22Fret;
                }
                _rings[6].SetInfo("realBass", instrument, values);
            }
            else
            {
                _rings[6].SetInfo("rhythm", Instrument.FiveFretRhythm, entry[Instrument.FiveFretRhythm]);
            }

            _rings[7].SetInfo("eliteDrums", Instrument.EliteDrums, entry[Instrument.EliteDrums]);
            _rings[8].SetInfo("realKeys", Instrument.ProKeys, entry[Instrument.ProKeys]);
            _rings[9].SetInfo("band", Instrument.Band, entry[Instrument.Band]);
        }

        private static (string PartIcon, PartValues PartValues) GetVocalsPartValues(SongEntry songEntry)
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

        private async UniTaskVoid LoadAlbumCover(SongEntry songEntry)
        {
            CancelAlbumLoad();
            _albumCts = new CancellationTokenSource();
            var token = _albumCts.Token;

            Texture2D texture = null;
            using var image = await UniTask.RunOnThreadPool(songEntry.LoadAlbumData);
            if (image != null)
                texture = image.LoadTexture(false);

            if (token.IsCancellationRequested || _currentSong != songEntry)
            {
                if (texture != null)
                    UnityEngine.Object.Destroy(texture);
                return;
            }

            ClearAlbum();
            if (_albumCover == null)
                return;

            _albumCover.texture = texture;
            _albumCover.uvRect = new Rect(0f, 0f, 1f, -1f);
            _albumCover.color = texture != null ? Color.white : new Color(0.12f, 0.12f, 0.14f, 1f);
        }

        private void CancelAlbumLoad()
        {
            if (_albumCts == null) return;
            _albumCts.Cancel();
            _albumCts.Dispose();
            _albumCts = null;
        }

        private void ClearAlbum()
        {
            if (_albumCover == null) return;
            if (_albumCover.texture != null)
                UnityEngine.Object.Destroy(_albumCover.texture);
            _albumCover.texture = null;
            _albumCover.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        }

        private void OnDisable()
        {
            CancelAlbumLoad();
            ClearAlbum();
        }

        private static void SetText(TextMeshProUGUI tmp, string value)
        {
            if (tmp != null)
                tmp.text = value ?? "";
        }

        public static MatchDetailInspector BuildRuntime(Transform parent, DifficultyRing ringPrefab)
        {
            var root = new GameObject("DetailInspector", typeof(RectTransform), typeof(Image),
                typeof(MatchDetailInspector));
            root.transform.SetParent(parent, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = root.GetComponent<Image>();
            bg.sprite = null;
            bg.color = new Color(0.07f, 0.08f, 0.10f, 1f);

            var inspector = root.GetComponent<MatchDetailInspector>();

            var headerGo = CreateUi("Header", root.transform);
            PlaceText(headerGo, 28f, -18f, 640f, 28f);
            inspector._headerText = headerGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyTitleFont(inspector._headerText, 16f);
            inspector._headerText.color = SongMatchStyle.Gold;
            inspector._headerText.characterSpacing = 10f;
            inspector._headerText.text = "SELECTED CANDIDATE DETAILS";

            var artGo = CreateUi("AlbumCover", root.transform);
            var artRt = artGo.GetComponent<RectTransform>();
            artRt.anchorMin = new Vector2(0f, 1f);
            artRt.anchorMax = new Vector2(0f, 1f);
            artRt.pivot = new Vector2(0f, 1f);
            artRt.anchoredPosition = new Vector2(24f, -48f);
            artRt.sizeDelta = new Vector2(140f, 140f);
            inspector._albumCover = artGo.AddComponent<RawImage>();
            inspector._albumCover.color = new Color(0.12f, 0.12f, 0.14f, 1f);

            var titleGo = CreateUi("Title", root.transform);
            PlaceText(titleGo, 184f, -48f, 420f, 32f);
            inspector._titleText = titleGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyTitleFont(inspector._titleText, 24f);
            inspector._titleText.alignment = TextAlignmentOptions.TopLeft;
            inspector._titleText.overflowMode = TextOverflowModes.Ellipsis;
            inspector._titleText.textWrappingMode = TextWrappingModes.NoWrap;
            inspector._titleText.characterSpacing = 2f;

            var artistGo = CreateUi("Artist", root.transform);
            PlaceText(artistGo, 184f, -80f, 360f, 22f);
            inspector._artistText = artistGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyLabelFont(inspector._artistText, 15f, SongMatchStyle.LabelMuted);
            inspector._artistText.alignment = TextAlignmentOptions.TopLeft;

            var metaGo = CreateUi("Meta", root.transform);
            PlaceText(metaGo, 184f, -104f, 360f, 20f);
            inspector._metaText = metaGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyLabelFont(inspector._metaText, 13f, SongMatchStyle.LabelMuted);
            inspector._metaText.alignment = TextAlignmentOptions.TopLeft;

            var sourceGo = CreateUi("Source", root.transform);
            PlaceText(sourceGo, 520f, -80f, 240f, 20f);
            inspector._sourceText = sourceGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyLabelFont(inspector._sourceText, 12f, SongMatchStyle.AccentSelect);
            inspector._sourceText.alignment = TextAlignmentOptions.TopLeft;

            var hashGo = CreateUi("Hash", root.transform);
            PlaceText(hashGo, 520f, -104f, 280f, 36f);
            inspector._hashText = hashGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyLabelFont(inspector._hashText, 10f, new Color(0.38f, 0.38f, 0.42f));
            inspector._hashText.alignment = TextAlignmentOptions.TopLeft;
            inspector._hashText.overflowMode = TextOverflowModes.Ellipsis;
            inspector._hashText.textWrappingMode = TextWrappingModes.Normal;
            inspector._hashText.maxVisibleLines = 2;

            var confGo = CreateUi("Confidence", root.transform);
            var confRt = confGo.GetComponent<RectTransform>();
            confRt.anchorMin = new Vector2(1f, 1f);
            confRt.anchorMax = new Vector2(1f, 1f);
            confRt.pivot = new Vector2(1f, 1f);
            confRt.anchoredPosition = new Vector2(-24f, -48f);
            confRt.sizeDelta = new Vector2(180f, 36f);
            inspector._confidenceText = confGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyValueFont(inspector._confidenceText, 22f, SongMatchStyle.TextWhite);
            inspector._confidenceText.alignment = TextAlignmentOptions.TopRight;

            // Single dense ring row (render)
            var ringsTop = CreateUi("RingsTop", root.transform);
            SetupRingRow(ringsTop, 8f, 46f);
            inspector._ringsTopRow = ringsTop.transform;
            inspector._ringsBottomRow = null;

            inspector._difficultyRingPrefab = ringPrefab;
            inspector.Initialize(ringPrefab);
            return inspector;
        }

        private static void SetupRingRow(GameObject row, float bottom, float height)
        {
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, bottom);
            rt.sizeDelta = new Vector2(-40f, height);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.spacing = 4f;
            hlg.padding = new RectOffset(20, 20, 0, 0);
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        private static GameObject CreateUi(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void PlaceText(GameObject go, float x, float y, float w, float h)
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
