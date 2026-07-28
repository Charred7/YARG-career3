using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Core.Input;
using YARG.Core.Song;
using YARG.Menu.MusicLibrary;
using YARG.Menu.Navigation;
using YARG.Song;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YARG.Menu.Career
{
    /// <summary>
    /// Premium Setlist Desk song matching screen.
    /// Left = career setlist. Right = vertical candidates + detail inspector.
    /// </summary>
    public class ManualMatchPanel : MonoBehaviour
    {
        private enum ActivePanel { Left, Right }

        private const int MaxCandidates = 10;
        private const string DifficultyRingPrefabPath =
            "Assets/Prefabs/Menu/MusicLibrary/DifficultyRing.prefab";

        [Header("List Menus")]
        [SerializeField] private MatchSongListMenu _leftList;
        [SerializeField] private CandidateListMenu _rightList;

        [Header("Headers")]
        [SerializeField] private TextMeshProUGUI _leftHeader;
        [SerializeField] private TextMeshProUGUI _rightHeader;

        [Header("Legacy")]
        [SerializeField] private TextMeshProUGUI _detailText;

        [Header("Setlist Desk")]
        [SerializeField] private Transform _candidateStripContent;
        [SerializeField] private MatchDetailInspector _detailInspector;
        [SerializeField] private DifficultyRing _difficultyRingPrefab;
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _leftPanelImage;
        [SerializeField] private Image _rightPanelImage;
        [SerializeField] private MatchCandidateRow _candidateRowPrefab;
        [SerializeField] private MatchCandidateCard _candidateCardPrefab;
        [SerializeField] private bool _authoredLayout;

        private string _careerId;
        private readonly List<MatchEntry> _careerSongs = new();
        private readonly List<MatchCandidateRow> _candidateRows = new();
        private ActivePanel _activePanel = ActivePanel.Left;
        private Action _onClose;
        private bool _hasRealCandidates;
        private int _candidateIndex;
        private bool _deskBuilt;
        private int _builtChromeVersion;
        private NavigationScheme _navScheme;
        private Image _bgArtImage;
        private ScrollRect _candidateScrollRect;

        public void Show(string careerId, Action onClose = null)
        {
            _careerId = careerId;
            _onClose = onClose;
            _activePanel = ActivePanel.Left;
            _candidateIndex = 0;

            if (transform.parent != null)
                transform.SetParent(null, false);

            EnsureOverlayCanvas();
            if (_authoredLayout
                && _detailInspector != null
                && _candidateStripContent != null
                && _candidateRowPrefab != null
                && _candidateRowPrefab.RowLayoutVersion >= MatchCandidateRow.LayoutVersion)
            {
                // Prefab-authored layout — do not rebuild at runtime
                EnsureVerticalCandidateContent();
                ApplyLeftPanelChrome();
                SoftenListRowChrome();
                if (_detailInspector != null)
                    _detailInspector.Initialize(_difficultyRingPrefab);
            }
            else
            {
                // Stale/missing accordion prefab — rebuild desk at runtime
                _authoredLayout = false;
                EnsureDeskBuilt();
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (_leftList == null)
            {
                Debug.LogError("[ManualMatchPanel] Missing MatchSongListMenu reference.");
                gameObject.SetActive(false);
                _onClose?.Invoke();
                return;
            }

            BuildCareerSongList();
            RefreshLeftList();
            ApplyLeftPanelChrome();
            SoftenListRowChrome();
            RefreshCandidates();
            UpdateInspector();
            SoftenListRowChrome();
            EnsureLeftListStacksRows();
            PushLeftNavScheme();
        }

        private void OnDisable()
        {
            if (_navScheme != null && Navigator.Instance != null)
            {
                Navigator.Instance.PopScheme(_navScheme);
                _navScheme = null;
            }
        }

        private void PushLeftNavScheme()
        {
            ReplaceNavScheme(new NavigationScheme(new()
            {
                new NavigationScheme.Entry(MenuAction.Up, "Menu.Common.Up", () =>
                {
                    StepLeft(-1);
                }),
                new NavigationScheme.Entry(MenuAction.Down, "Menu.Common.Down", () =>
                {
                    StepLeft(1);
                }),
                new NavigationScheme.Entry(MenuAction.Green, "Select Song", EnterRightPanel),
                new NavigationScheme.Entry(MenuAction.Orange, "Clear Match", ClearMatch),
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", Close),
            }, false));
        }

        private void PushRightNavScheme()
        {
            ReplaceNavScheme(new NavigationScheme(new()
            {
                new NavigationScheme.Entry(MenuAction.Up, "Menu.Common.Up", () => StepCandidate(-1)),
                new NavigationScheme.Entry(MenuAction.Down, "Menu.Common.Down", () => StepCandidate(1)),
                new NavigationScheme.Entry(MenuAction.Left, "Prev Match", () => StepCandidate(-1)),
                new NavigationScheme.Entry(MenuAction.Right, "Next Match", () => StepCandidate(1)),
                new NavigationScheme.Entry(MenuAction.Green, "Confirm Match", ConfirmMatch),
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", ReturnToLeft),
            }, false));
        }

        private void ReplaceNavScheme(NavigationScheme scheme)
        {
            var nav = Navigator.Instance;
            if (nav == null) return;
            if (_navScheme != null)
                nav.PopScheme(_navScheme);
            _navScheme = scheme;
            nav.PushScheme(_navScheme);
        }

        private void StepCandidate(int delta)
        {
            if (_candidateRows.Count == 0) return;
            _candidateIndex = Mathf.Clamp(_candidateIndex + delta, 0, _candidateRows.Count - 1);
            RefreshCandidateSelectionVisuals();
            UpdateInspector();
        }

        private void BuildCareerSongList()
        {
            _careerSongs.Clear();
            var gigs = CareerManager.Instance.GetGigs(_careerId);
            var seen = new HashSet<string>();

            foreach (var gig in gigs)
            {
                if (gig.songs == null) continue;
                foreach (var song in gig.songs)
                {
                    string key = SongMatch.CreateKey(song);
                    if (!seen.Add(key)) continue;
                    _careerSongs.Add(new MatchEntry
                    {
                        GigSong = song,
                        Match = SongMatchCache.GetMatch(_careerId, song),
                        Key = key
                    });
                }
            }

            _careerSongs.Sort((a, b) =>
            {
                int ap = GetSortPriority(a.Match);
                int bp = GetSortPriority(b.Match);
                if (ap != bp) return ap.CompareTo(bp);
                return string.Compare(a.GigSong.title, b.GigSong.title, StringComparison.OrdinalIgnoreCase);
            });
        }

        private void StepLeft(int delta)
        {
            if (_leftList == null || _leftList.ViewList == null || _leftList.ViewList.Count == 0)
                return;

            int count = _leftList.ViewList.Count;
            int idx = _leftList.SelectedIndex;
            for (int step = 0; step < count; step++)
            {
                idx = Mathf.Clamp(idx + delta, 0, count - 1);
                _leftList.SelectedIndex = idx;
                if (_leftList.CurrentSelection is MatchSongViewType)
                    break;

                if ((delta > 0 && idx >= count - 1) || (delta < 0 && idx <= 0))
                {
                    EnsureLeftSongSelection();
                    break;
                }
            }

            OnLeftSelectionChanged();
        }

        private void EnsureLeftSongSelection()
        {
            if (_leftList == null || _leftList.ViewList == null || _leftList.ViewList.Count == 0)
                return;

            if (_leftList.CurrentSelection is MatchSongViewType)
                return;

            for (int i = 0; i < _leftList.ViewList.Count; i++)
            {
                if (_leftList.ViewList[i] is MatchSongViewType)
                {
                    _leftList.SelectedIndex = i;
                    return;
                }
            }
        }

        private void RefreshLeftList()
        {
            var open = new List<MatchEntry>();
            var matched = new List<MatchEntry>();
            foreach (var entry in _careerSongs)
            {
                if (entry.Match == null || !entry.Match.IsResolved)
                    open.Add(entry);
                else
                    matched.Add(entry);
            }

            var views = new List<ViewType>();
            if (open.Count > 0)
            {
                views.Add(new MatchSectionViewType("NEEDS MATCH"));
                foreach (var entry in open)
                    views.Add(new MatchSongViewType(entry));
            }

            if (matched.Count > 0)
            {
                views.Add(new MatchSectionViewType("MATCHED"));
                foreach (var entry in matched)
                    views.Add(new MatchSongViewType(entry));
            }

            int previousKeyIndex = -1;
            string previousKey = null;
            if (_leftList.CurrentSelection is MatchSongViewType prevSong)
                previousKey = prevSong.Entry.Key;

            _leftList.SetViewList(views);

            if (!string.IsNullOrEmpty(previousKey))
            {
                for (int i = 0; i < views.Count; i++)
                {
                    if (views[i] is MatchSongViewType song && song.Entry.Key == previousKey)
                    {
                        previousKeyIndex = i;
                        break;
                    }
                }
            }

            if (previousKeyIndex >= 0)
                _leftList.SelectedIndex = previousKeyIndex;
            else
                EnsureLeftSongSelection();

            if (_leftHeader != null)
            {
                _leftHeader.text =
                    $"SETLIST\n<size=65%><color={SongMatchStyle.Hex(SongMatchStyle.Gold)}>" +
                    $"{_careerSongs.Count} SONGS  ·  {open.Count} OPEN</color></size>";
                SongMatchChrome.ApplyTitleFont(_leftHeader, 18f);
                _leftHeader.color = SongMatchStyle.Gold;
                _leftHeader.characterSpacing = 6f;
            }
        }

        private void RefreshCandidates()
        {
            ClearCandidateRows();
            _hasRealCandidates = false;
            _candidateIndex = 0;

            var leftSelection = _leftList.CurrentSelection as MatchSongViewType;
            if (leftSelection == null)
            {
                UpdateRightHeader(null, 0);
                return;
            }

            var entry = leftSelection.Entry;
            var items = new List<(SongEntry song, float confidence, bool current)>();

            SongEntry resolvedSong = null;
            if (entry.Match != null && entry.Match.IsResolved)
            {
                var hash = HashWrapper.FromString(entry.Match.resolvedHash);
                if (SongContainer.SongsByHash.TryGetValue(hash, out var resolved))
                {
                    resolvedSong = resolved[0];
                    items.Add((resolvedSong, entry.Match.confidence, true));
                }
            }

            foreach (var (song, confidence) in SongMatchAnalyzer.FindFuzzyMatches(entry.GigSong, MaxCandidates))
            {
                if (resolvedSong != null && song.Hash.Equals(resolvedSong.Hash)) continue;
                items.Add((song, confidence, false));
            }

            _hasRealCandidates = items.Count > 0;
            foreach (var (song, confidence, current) in items)
            {
                MatchCandidateRow row;
                bool usePrefab = _candidateRowPrefab != null
                    && _candidateRowPrefab.RowLayoutVersion >= MatchCandidateRow.LayoutVersion;
                if (usePrefab)
                {
                    row = UnityEngine.Object.Instantiate(_candidateRowPrefab, _candidateStripContent);
                    row.gameObject.SetActive(true);
                }
                else
                {
                    row = MatchCandidateRow.BuildRuntime(_candidateStripContent, _difficultyRingPrefab);
                }
                row.Initialize(_difficultyRingPrefab);
                row.Setup(song, confidence, current);
                _candidateRows.Add(row);
            }

            UpdateRightHeader(entry, items.Count);
            RefreshCandidateSelectionVisuals();
        }

        private void ClearCandidateRows()
        {
            foreach (var row in _candidateRows)
            {
                if (row != null)
                    Destroy(row.gameObject);
            }
            _candidateRows.Clear();
        }

        private void RefreshCandidateSelectionVisuals()
        {
            for (int i = 0; i < _candidateRows.Count; i++)
                _candidateRows[i].SetSelected(i == _candidateIndex);

            RebuildCandidateListLayout();
            EnsureSelectedCandidateVisible();
        }

        private void RebuildCandidateListLayout()
        {
            if (_candidateStripContent is not RectTransform contentRt)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
            Canvas.ForceUpdateCanvases();
        }

        private void EnsureSelectedCandidateVisible()
        {
            if (_candidateRows.Count == 0
                || _candidateIndex < 0
                || _candidateIndex >= _candidateRows.Count)
                return;

            if (_candidateScrollRect == null && _candidateStripContent != null)
                _candidateScrollRect = _candidateStripContent.GetComponentInParent<ScrollRect>();
            if (_candidateScrollRect == null || _candidateScrollRect.content == null)
                return;

            var scroll = _candidateScrollRect;
            var viewport = scroll.viewport != null ? scroll.viewport : (RectTransform)scroll.transform;
            RebuildCandidateListLayout();

            float contentHeight = scroll.content.rect.height;
            float viewportHeight = viewport.rect.height;
            float scrollable = Mathf.Max(0f, contentHeight - viewportHeight);
            if (scrollable <= 0.01f)
            {
                scroll.verticalNormalizedPosition = 1f;
                return;
            }

            // Distance from top of content to top of selected row (VLG, top-anchored)
            float yFromTop = 0f;
            var vlg = _candidateStripContent.GetComponent<VerticalLayoutGroup>();
            float spacing = vlg != null ? vlg.spacing : 0f;
            float padTop = vlg != null ? vlg.padding.top : 0f;
            yFromTop += padTop;
            for (int i = 0; i < _candidateIndex; i++)
                yFromTop += _candidateRows[i].CurrentHeight + spacing;

            float selectedHeight = _candidateRows[_candidateIndex].CurrentHeight;
            float currentOffset = (1f - scroll.verticalNormalizedPosition) * scrollable;
            float newOffset = currentOffset;

            // Keep selected row fully inside the viewport
            if (yFromTop < currentOffset)
                newOffset = yFromTop;
            else if (yFromTop + selectedHeight > currentOffset + viewportHeight)
                newOffset = yFromTop + selectedHeight - viewportHeight;

            newOffset = Mathf.Clamp(newOffset, 0f, scrollable);
            scroll.verticalNormalizedPosition = 1f - (newOffset / scrollable);
        }

        private void UpdateRightHeader(MatchEntry entry, int count)
        {
            if (_rightHeader == null) return;

            if (entry == null)
            {
                _rightHeader.text = "MATCH CANDIDATES";
                return;
            }

            string title = entry.GigSong.title ?? "";
            if (count > 0)
            {
                _rightHeader.text =
                    $"MATCH CANDIDATES FOR\n<size=70%><color={SongMatchStyle.Hex(SongMatchStyle.TextWhite)}>" +
                    $"{title.ToUpperInvariant()}</color></size>";
            }
            else
            {
                _rightHeader.text =
                    $"NO MATCHES\n<size=65%><color={SongMatchStyle.Hex(SongMatchStyle.StatusUnmatched)}>" +
                    $"{title}</color></size>";
            }
        }

        private void UpdateInspector()
        {
            if (_detailInspector == null) return;
            if (_candidateRows.Count == 0 || _candidateIndex < 0 || _candidateIndex >= _candidateRows.Count)
            {
                _detailInspector.ShowEmpty();
                return;
            }
            var row = _candidateRows[_candidateIndex];
            _detailInspector.ShowCandidate(row.Song, row.Confidence, row.IsCurrentMatch);
        }

        private void OnLeftSelectionChanged()
        {
            RefreshCandidates();
            UpdateInspector();
        }

        private void EnterRightPanel()
        {
            if (_leftList.CurrentSelection is not MatchSongViewType)
                EnsureLeftSongSelection();
            if (!_hasRealCandidates) return;
            if (_leftList.CurrentSelection is not MatchSongViewType) return;
            _activePanel = ActivePanel.Right;
            PushRightNavScheme();
            RefreshCandidateSelectionVisuals();
            UpdateInspector();
        }

        private void ConfirmMatch()
        {
            if (_candidateIndex < 0 || _candidateIndex >= _candidateRows.Count) return;
            var songView = _leftList.CurrentSelection as MatchSongViewType;
            if (songView == null) return;

            var row = _candidateRows[_candidateIndex];
            var entry = songView.Entry;
            SongMatchAnalyzer.SetManualMatch(_careerId, entry.GigSong, row.Song, row.Confidence);
            entry.Match = SongMatchCache.GetMatch(_careerId, entry.GigSong);

            string confirmedKey = entry.Key;
            ReturnToLeft();
            RefreshLeftList();

            // Advance to the next song row after the one we just matched
            int start = 0;
            for (int i = 0; i < _leftList.ViewList.Count; i++)
            {
                if (_leftList.ViewList[i] is MatchSongViewType s && s.Entry.Key == confirmedKey)
                {
                    start = i + 1;
                    break;
                }
            }

            bool found = false;
            for (int i = start; i < _leftList.ViewList.Count; i++)
            {
                if (_leftList.ViewList[i] is MatchSongViewType)
                {
                    _leftList.SelectedIndex = i;
                    found = true;
                    break;
                }
            }

            if (!found)
                EnsureLeftSongSelection();

            OnLeftSelectionChanged();
        }

        private void ReturnToLeft()
        {
            _activePanel = ActivePanel.Left;
            PushLeftNavScheme();
            _leftList.RefreshViewsObjects();
            SoftenListRowChrome();
            RefreshCandidateSelectionVisuals();
            UpdateInspector();
        }

        private void ClearMatch()
        {
            var songView = _leftList.CurrentSelection as MatchSongViewType;
            if (songView == null) return;
            var entry = songView.Entry;
            SongMatchAnalyzer.ClearManualMatch(_careerId, entry.GigSong);
            entry.Match = SongMatchCache.GetMatch(_careerId, entry.GigSong);
            RefreshLeftList();
            RefreshCandidates();
            UpdateInspector();
        }

        private void Close()
        {
            gameObject.SetActive(false);
            _onClose?.Invoke();
        }

        private static int GetSortPriority(SongMatch match)
        {
            if (match == null || !match.IsResolved) return 0;
            return match.method switch
            {
                MatchMethod.Fuzzy => 1,
                MatchMethod.ExactTitleArtist => 2,
                MatchMethod.ExactTitleArtistSource => 3,
                MatchMethod.Manual => 4,
                MatchMethod.Hash => 5,
                _ => 0
            };
        }

        // ── Premium chrome ───────────────────────────────────────────────

        private void EnsureVerticalCandidateContent()
        {
            if (_candidateStripContent == null) return;

            var hlg = _candidateStripContent.GetComponent<HorizontalLayoutGroup>();
            if (hlg != null)
                UnityEngine.Object.DestroyImmediate(hlg);

            var vlg = _candidateStripContent.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = _candidateStripContent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            // Must be true or LayoutElement preferredHeight is ignored (rows stay one size)
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(2, 2, 4, 4);

            var fitter = _candidateStripContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = _candidateStripContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var contentRt = _candidateStripContent as RectTransform;
            if (contentRt != null)
            {
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
            }

            var scroll = _candidateStripContent.GetComponentInParent<ScrollRect>();
            if (scroll != null)
            {
                scroll.horizontal = false;
                scroll.vertical = true;
            }
        }

        private void EnsureOverlayCanvas()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
        }

        private void EnsureDeskBuilt()
        {
            SongMatchChrome.EnsureLoaded();

            if (_deskBuilt && _builtChromeVersion == SongMatchStyle.ChromeVersion
                && _candidateStripContent != null && _detailInspector != null)
            {
                ApplyOpaqueChrome();
                SoftenListRowChrome();
                return;
            }

            DestroyNamed("RightDesk");
            DestroyNamed("BgArt");
            DestroyNamed("Vignette");
            DestroyNamed("ScreenTitle");
            _candidateStripContent = null;
            _candidateScrollRect = null;
            _detailInspector = null;
            _rightHeader = null;
            _bgArtImage = null;

            if (_leftList != null)
            {
                DestroyChild(_leftList.transform, "LeftFrame");
                DestroyChild(_leftList.transform, "LeftGoldHairline");
            }

            ResolveDifficultyRingPrefab();

            var rootRt = GetComponent<RectTransform>();
            if (rootRt != null)
            {
                rootRt.anchorMin = Vector2.zero;
                rootRt.anchorMax = Vector2.one;
                rootRt.offsetMin = new Vector2(0f, 56f);
                rootRt.offsetMax = Vector2.zero;
            }

            _backgroundImage = GetComponent<Image>();
            if (_backgroundImage == null)
                _backgroundImage = gameObject.AddComponent<Image>();
            _backgroundImage.sprite = null;
            _backgroundImage.color = SongMatchStyle.BgSolid;
            _backgroundImage.raycastTarget = true;

            EnsureBgArt();
            EnsureVignette();
            EnsureScreenTitle();

            if (_detailText != null)
                _detailText.gameObject.SetActive(false);
            if (_rightList != null)
                _rightList.gameObject.SetActive(false);
            if (_rightHeader != null && !_rightHeader.gameObject.activeInHierarchy)
                _rightHeader = null;

            RestyleExistingColumns();
            BuildRightDesk();
            ApplyOpaqueChrome();
            SoftenListRowChrome();

            _deskBuilt = true;
            _builtChromeVersion = SongMatchStyle.ChromeVersion;
        }

        private void DestroyNamed(string name)
        {
            var t = transform.Find(name);
            if (t != null) Destroy(t.gameObject);
        }

        private static void DestroyChild(Transform parent, string name)
        {
            if (parent == null) return;
            var t = parent.Find(name);
            if (t != null) UnityEngine.Object.Destroy(t.gameObject);
        }

        private void EnsureBgArt()
        {
            var artGo = new GameObject("BgArt", typeof(RectTransform), typeof(Image));
            artGo.transform.SetParent(transform, false);
            artGo.transform.SetAsFirstSibling();
            StretchFull(artGo.GetComponent<RectTransform>());
            _bgArtImage = artGo.GetComponent<Image>();
            _bgArtImage.raycastTarget = false;
            ApplyTrophyBackground(_bgArtImage);
        }

        private void EnsureVignette()
        {
            var go = new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetSiblingIndex(1);
            StretchFull(go.GetComponent<RectTransform>());
            var img = go.GetComponent<Image>();
            img.color = SongMatchStyle.Vignette;
            img.raycastTarget = false;
        }

        private void EnsureScreenTitle()
        {
            var go = new GameObject("ScreenTitle", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -16f);
            rt.sizeDelta = new Vector2(-160f, 44f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "SONG MATCH";
            SongMatchChrome.ApplyTitleFont(tmp, 30f);
            tmp.alignment = TextAlignmentOptions.Top;
            tmp.color = SongMatchStyle.Gold;
            tmp.characterSpacing = 22f;
        }

        private void ApplyOpaqueChrome()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.sprite = null;
                _backgroundImage.color = SongMatchStyle.BgSolid;
            }
            if (_leftPanelImage != null)
            {
                _leftPanelImage.sprite = null;
                _leftPanelImage.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);
            }
            if (_rightPanelImage != null)
            {
                _rightPanelImage.sprite = null;
                _rightPanelImage.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);
            }
            if (_bgArtImage != null)
                ApplyTrophyBackground(_bgArtImage);
        }

        private void SoftenListRowChrome()
        {
            if (_leftList == null) return;
            foreach (var img in _leftList.GetComponentsInChildren<Image>(true))
            {
                string n = img.gameObject.name;
                if (n == "Selected Background")
                    img.color = new Color(SongMatchStyle.AccentSelect.r, SongMatchStyle.AccentSelect.g,
                        SongMatchStyle.AccentSelect.b, 0.12f);
                else if (n == "Normal Background")
                    img.color = new Color(1f, 1f, 1f, 0.02f);
                else if (n == "Category Background")
                    img.color = new Color(1f, 1f, 1f, 0.03f);
            }
        }

        private void ApplyLeftPanelChrome()
        {
            if (_leftList != null)
            {
                _leftPanelImage = _leftList.GetComponent<Image>() ?? _leftPanelImage;
                DestroyChild(_leftList.transform, "LeftFrame");
                DestroyChild(_leftList.transform, "LeftGoldHairline");
                EnsureLeftListStacksRows();
            }

            if (_leftPanelImage != null)
            {
                _leftPanelImage.sprite = null;
                _leftPanelImage.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);
            }
        }

        /// <summary>
        /// ListMenu pools many row instances as siblings. Without a VerticalLayoutGroup
        /// they all sit on top of each other — only one song appears visible.
        /// </summary>
        private void EnsureLeftListStacksRows()
        {
            if (_leftList == null) return;

            Transform parent = _leftList.transform.Find("LeftScrollContent");
            if (parent == null)
            {
                // Fall back: first child that already hosts MatchViewObject instances
                foreach (Transform child in _leftList.transform)
                {
                    if (child.GetComponentInChildren<MatchViewObject>(true) != null)
                    {
                        parent = child;
                        break;
                    }
                }
            }

            if (parent == null) return;

            var vlg = parent.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
                vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();

            vlg.spacing = 4f;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Rows need a driven height; ensure LayoutElements exist on pooled rows
            foreach (var row in parent.GetComponentsInChildren<MatchViewObject>(true))
            {
                var le = row.GetComponent<LayoutElement>() ?? row.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 46f;
                le.preferredHeight = 46f;
                le.flexibleHeight = 0f;
                le.flexibleWidth = 1f;

                if (row.transform is RectTransform rt)
                {
                    rt.anchorMin = new Vector2(0f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    var size = rt.sizeDelta;
                    size.y = 46f;
                    rt.sizeDelta = size;
                }
            }

            if (parent is RectTransform parentRt)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
        }

        private void RestyleExistingColumns()
        {
            if (_leftList != null)
            {
                var leftCol = _leftList.transform as RectTransform;
                if (leftCol != null)
                {
                    leftCol.anchorMin = new Vector2(0.035f, 0.04f);
                    leftCol.anchorMax = new Vector2(0.355f, 0.90f);
                    leftCol.offsetMin = Vector2.zero;
                    leftCol.offsetMax = Vector2.zero;

                    _leftPanelImage = leftCol.GetComponent<Image>()
                                      ?? leftCol.gameObject.AddComponent<Image>();
                    _leftPanelImage.sprite = null;
                    _leftPanelImage.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);
                    _leftPanelImage.raycastTarget = true;
                }

                DestroyChild(_leftList.transform, "LeftFrame");
                DestroyChild(_leftList.transform, "LeftGoldHairline");
            }

            if (_leftHeader != null)
            {
                SongMatchChrome.ApplyTitleFont(_leftHeader, 18f);
                _leftHeader.alignment = TextAlignmentOptions.TopLeft;
                _leftHeader.characterSpacing = 6f;
                _leftHeader.color = SongMatchStyle.Gold;
                var headerRt = _leftHeader.rectTransform;
                headerRt.anchorMin = new Vector2(0f, 1f);
                headerRt.anchorMax = new Vector2(1f, 1f);
                headerRt.pivot = new Vector2(0.5f, 1f);
                headerRt.anchoredPosition = new Vector2(0f, -22f);
                headerRt.offsetMin = new Vector2(28f, headerRt.offsetMin.y);
                headerRt.offsetMax = new Vector2(-28f, headerRt.offsetMax.y);
                headerRt.sizeDelta = new Vector2(headerRt.sizeDelta.x, 56f);
            }
        }

        private void BuildRightDesk()
        {
            var rightGo = new GameObject("RightDesk", typeof(RectTransform), typeof(Image));
            rightGo.transform.SetParent(transform, false);
            var rightRoot = rightGo.transform;

            var rt = rightGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.375f, 0.04f);
            rt.anchorMax = new Vector2(0.965f, 0.90f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _rightPanelImage = rightGo.GetComponent<Image>();
            _rightPanelImage.sprite = null;
            _rightPanelImage.color = new Color(0.07f, 0.08f, 0.10f, 0.98f);

            var headerGo = new GameObject("RightHeader", typeof(RectTransform));
            headerGo.transform.SetParent(rightRoot, false);
            var hrt = headerGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -22f);
            hrt.sizeDelta = new Vector2(-56f, 56f);
            _rightHeader = headerGo.AddComponent<TextMeshProUGUI>();
            SongMatchChrome.ApplyTitleFont(_rightHeader, 18f);
            _rightHeader.alignment = TextAlignmentOptions.TopLeft;
            _rightHeader.characterSpacing = 6f;
            _rightHeader.color = SongMatchStyle.Gold;

            // Vertical candidate list (upper ~55%)
            var stripGo = new GameObject("CandidateList", typeof(RectTransform));
            stripGo.transform.SetParent(rightRoot, false);
            var stripRt = stripGo.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0f, 0.38f);
            stripRt.anchorMax = new Vector2(1f, 0.90f);
            stripRt.offsetMin = new Vector2(24f, 4f);
            stripRt.offsetMax = new Vector2(-24f, -4f);

            var scroll = stripGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
            viewport.transform.SetParent(stripGo.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            StretchFull(vpRt);
            var vpImg = viewport.GetComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.001f);
            vpImg.raycastTarget = true;

            var content = new GameObject("Content", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(2, 2, 4, 4);

            var fitter = content.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRt;
            scroll.content = contentRt;
            _candidateStripContent = content.transform;
            _candidateScrollRect = scroll;

            var inspectorHost = new GameObject("InspectorHost", typeof(RectTransform));
            inspectorHost.transform.SetParent(rightRoot, false);
            var hostRt = inspectorHost.GetComponent<RectTransform>();
            hostRt.anchorMin = new Vector2(0f, 0.03f);
            hostRt.anchorMax = new Vector2(1f, 0.36f);
            hostRt.offsetMin = new Vector2(24f, 16f);
            hostRt.offsetMax = new Vector2(-24f, -4f);

            _detailInspector = MatchDetailInspector.BuildRuntime(inspectorHost.transform, _difficultyRingPrefab);
        }

        private void ResolveDifficultyRingPrefab()
        {
            if (_difficultyRingPrefab != null) return;
#if UNITY_EDITOR
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(DifficultyRingPrefabPath);
            if (go != null)
                _difficultyRingPrefab = go.GetComponent<DifficultyRing>();
#endif
            if (_difficultyRingPrefab == null)
            {
                var ringGo = Resources.Load<GameObject>("CareersM/DifficultyRing");
                if (ringGo != null)
                    _difficultyRingPrefab = ringGo.GetComponent<DifficultyRing>();
            }
        }

        private static void ApplyTrophyBackground(Image image)
        {
            if (image == null) return;
            var sprite = SongMatchChrome.Background;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
                image.color = SongMatchStyle.BgArtTint;
                return;
            }

            var tex = Resources.Load<Texture2D>("CareersM/song_match_bg");
            if (tex != null)
            {
                image.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
                image.color = SongMatchStyle.BgArtTint;
            }
            else
            {
                image.sprite = null;
                image.color = Color.clear;
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
