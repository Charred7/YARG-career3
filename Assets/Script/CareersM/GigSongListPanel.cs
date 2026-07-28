using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YARG.Core.Song;
using Cysharp.Threading.Tasks;

namespace YARG.Menu.Career
{
    public class GigSongListPanel : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _songListText;
        [SerializeField]
        private RectTransform _scrollContainer;
        [SerializeField]
        private float _scrollSpeed = 20f;
        [SerializeField]
        private float _pauseDuration = 2f;
        [SerializeField]
        private bool _enableScrolling = true;
        [SerializeField]
        private int _maxRefreshAttempts = 10;
        [SerializeField]
        private float _refreshDelayMs = 120f;

        private string _fullSongListText;
        private bool _isScrolling;
        private float _scrollPosition;
        private bool _needsScrolling;

        private void OnDisable()
        {
            _isScrolling = false;
        }

        public async void SetGig(GigInfo gig)
        {
            // Stop any existing scrolling
            _isScrolling = false;

            if (gig == null)
            {
                Clear();
                return;
            }

            Debug.Log($"GigSongListPanel: SetGig called for '{gig.name}' with {gig.songs?.Count ?? 0} songs");

            if (gig.songs == null || gig.songs.Count == 0)
            {
                if (_songListText != null)
                {
                    _songListText.text = "<color=#888888>No songs in gig</color>";
                }
                _needsScrolling = false;
                ResetPosition();
                return;
            }

            // Resolve songs from the user's library
            var resolvedSongs = CareerManager.Instance.ResolveGig(gig);

            // Generate the song list text
            _fullSongListText = GenerateSongListText(resolvedSongs, gig.encoreIndex);
            Debug.Log($"GigSongListPanel: Generated text with {_fullSongListText.Length} characters");

            if (_songListText == null)
            {
                Debug.LogError("GigSongListPanel: _songListText is null!");
                return;
            }

            // Set the text immediately
            _songListText.text = _fullSongListText;

            // Reset position to top before checking dimensions
            ResetPosition();

            // Wait for layout to refresh with retry mechanism
            await RefreshLayoutWithRetry();
        }

        private async UniTask RefreshLayoutWithRetry()
        {
            if (_songListText == null || _scrollContainer == null)
            {
                Debug.LogError($"GigSongListPanel: Cannot refresh layout - text is null: {_songListText == null}, container is null: {_scrollContainer == null}");
                return;
            }

            int attempts = 0;
            bool layoutReady = false;

            Debug.Log($"GigSongListPanel: Starting layout refresh with max {_maxRefreshAttempts} attempts");

            while (attempts < _maxRefreshAttempts && !layoutReady)
            {
                attempts++;

                // Force canvas update
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_songListText.rectTransform);

                // Wait a frame for Unity to process
                await UniTask.Delay((int) _refreshDelayMs);

                // Check if dimensions are valid
                float textHeight = _songListText.preferredHeight;
                float containerHeight = _scrollContainer.rect.height;

                Debug.Log($"GigSongListPanel: Attempt {attempts}/{_maxRefreshAttempts} - text height: {textHeight}, container height: {containerHeight}, text length: {_songListText.text.Length}");

                // Consider layout ready if we have valid dimensions
                if (textHeight > 0 && containerHeight > 0)
                {
                    layoutReady = true;
                    _needsScrolling = textHeight > containerHeight && _enableScrolling;

                    Debug.Log($"GigSongListPanel: Layout ready after {attempts} attempts (text height: {textHeight}, container height: {containerHeight}, needs scrolling: {_needsScrolling})");

                    // Start scrolling if needed
                    if (_needsScrolling)
                    {
                        _scrollPosition = 0f;
                        StartScrolling().Forget();
                    }
                }
                else if (attempts == _maxRefreshAttempts)
                {
                    // Last attempt - set text anyway
                    Debug.LogWarning($"GigSongListPanel: Max attempts reached. Forcing text display. Text empty: {string.IsNullOrEmpty(_songListText.text)}");
                    _needsScrolling = false;
                }
            }

            if (!layoutReady)
            {
                Debug.LogWarning($"GigSongListPanel: Failed to get valid layout after {_maxRefreshAttempts} attempts - text may not display correctly");
                _needsScrolling = false;
            }
        }

        private void ResetPosition()
        {
            _scrollPosition = 0f;
            if (_songListText != null && _songListText.rectTransform != null)
            {
                var pos = _songListText.rectTransform.anchoredPosition;
                pos.y = 0f;
                _songListText.rectTransform.anchoredPosition = pos;
            }
        }

        private string GenerateSongListText(List<(GigSong GigSong, SongEntry SongEntry)> resolvedSongs, int encoreIndex)
        {
            var text = new System.Text.StringBuilder();

            for (int i = 0; i < resolvedSongs.Count; i++)
            {
                var (gigSong, songEntry) = resolvedSongs[i];
                bool isEncore = (i == encoreIndex);

                // If this is the encore song, hide its identity
                if (isEncore)
                {
                    text.AppendLine("<color=#666666>▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒</color>");
                    text.AppendLine("<b><color=#FFAA00>+ ENCORE!</color></b>");
                    text.AppendLine("<color=#666666>▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒</color>");
                }
                else if (songEntry != null)
                {
                    // Resolved song — show real metadata
                    text.AppendLine($"<b>{songEntry.Artist}</b>");
                    text.AppendLine($"  {songEntry.Name}");

                    // Add spacing between songs (but not before encore block)
                    if (i < resolvedSongs.Count - 1 && i + 1 != encoreIndex)
                    {
                        text.AppendLine();
                    }
                }
                else
                {
                    // Unresolved song — show fallback info from gig metadata
                    string displayArtist = !string.IsNullOrEmpty(gigSong.artist) ? gigSong.artist : "Unknown Artist";
                    string displayTitle = !string.IsNullOrEmpty(gigSong.title) ? gigSong.title : "Unknown Song";
                    text.AppendLine($"<color=#FF4444><b>{displayArtist}</b></color>");
                    text.AppendLine($"  <color=#FF4444>{displayTitle} (NOT FOUND)</color>");

                    if (i < resolvedSongs.Count - 1 && i + 1 != encoreIndex)
                    {
                        text.AppendLine();
                    }
                }
            }

            return text.ToString();
        }

        private async UniTaskVoid StartScrolling()
        {
            _isScrolling = true;

            while (_isScrolling && this != null && gameObject.activeInHierarchy)
            {
                // Calculate max scroll distance
                float maxScroll = _songListText.preferredHeight - _scrollContainer.rect.height;

                if (maxScroll <= 0)
                {
                    _isScrolling = false;
                    break;
                }

                // Scroll down
                while (_scrollPosition < maxScroll && _isScrolling)
                {
                    _scrollPosition += _scrollSpeed * Time.deltaTime;
                    _scrollPosition = Mathf.Min(_scrollPosition, maxScroll);
                    UpdateScrollPosition();
                    await UniTask.Yield();
                }

                // Pause at bottom
                await UniTask.Delay((int) (_pauseDuration * 1000));

                if (!_isScrolling) break;

                // Scroll back to top
                while (_scrollPosition > 0 && _isScrolling)
                {
                    _scrollPosition -= _scrollSpeed * Time.deltaTime;
                    _scrollPosition = Mathf.Max(_scrollPosition, 0);
                    UpdateScrollPosition();
                    await UniTask.Yield();
                }

                // Pause at top
                await UniTask.Delay((int) (_pauseDuration * 1000));
            }
        }

        private void UpdateScrollPosition()
        {
            if (_songListText != null && _songListText.rectTransform != null)
            {
                var pos = _songListText.rectTransform.anchoredPosition;
                pos.y = _scrollPosition;
                _songListText.rectTransform.anchoredPosition = pos;
            }
        }

        public void Clear()
        {
            _isScrolling = false;
            _fullSongListText = string.Empty;
            if (_songListText != null)
            {
                _songListText.text = string.Empty;
            }
            ResetPosition();
        }
    }
}