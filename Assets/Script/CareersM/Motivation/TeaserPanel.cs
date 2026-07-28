using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Shows a subtle horizontal scrolling ticker at the bottom of the GigView
    /// that teases upcoming songs from the next gig. Encore song names are hidden
    /// and replaced with "🎸 Encore Surprise!" to preserve the surprise.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class TeaserPanel : MonoBehaviour
    {
        private static TeaserPanel _instance;
        public static TeaserPanel Instance => _instance;

        private GigView _gigView;
        private GameObject _tickerBar;
        private TextMeshProUGUI _tickerText;
        private CanvasGroup _tickerCanvasGroup;

        private Coroutine _scrollCoroutine;
        private CareerInfo _currentCareer;

        // Ticker state
        private string _fullTickerText = "";
        private float _scrollPosition;
        private const float ScrollSpeed = 30f; // pixels per second
        private const float PaddingPixels = 100f; // gap between end and restart

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }
            _instance = this;

            _gigView = GetComponent<GigView>();
            if (_gigView == null)
                _gigView = FindObjectOfType<GigView>();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        /// <summary>
        /// Initializes the ticker for the given career.
        /// Called from GigView.SetCareer() hook.
        /// </summary>
        public void Initialize(CareerInfo career)
        {
            _currentCareer = career;
            CreateTickerBar();
            UpdateTicker();
        }

        /// <summary>
        /// Updates the ticker to show the next upcoming gig.
        /// </summary>
        public void UpdateTicker()
        {
            if (_tickerBar == null || _currentCareer == null) return;

            var progress = GigProgressEnhancer.Instance;
            int nextIndex = progress?.GetNextGigIndex() ?? -1;

            if (nextIndex < 0)
            {
                // All gigs completed
                ShowCampaignCompleteTicker();
                return;
            }

            var gigs = CareerManager.Instance.GetGigs(_currentCareer.id);
            if (nextIndex >= gigs.Count)
            {
                HideTicker();
                return;
            }

            var nextGig = gigs[nextIndex];
            string tickerText = $"▶ NEXT GIG: {nextGig.name.ToUpper()}  •  ";

            // Build song list, hiding encore song name
            int songCount = nextGig.songs.Count;
            int songsToList = Mathf.Min(songCount, 4);

            for (int i = 0; i < songsToList; i++)
            {
                var song = nextGig.songs[i];

                // Check if this is the encore song — hide its name
                if (nextGig.HasEncore && i == nextGig.encoreIndex)
                {
                    tickerText += "🎸 Encore Surprise!";
                }
                else if (!string.IsNullOrEmpty(song.title))
                {
                    tickerText += song.title;
                    if (!string.IsNullOrEmpty(song.artist))
                        tickerText += $" - {song.artist}";
                }

                if (i < songsToList - 1)
                    tickerText += "  •  ";
            }

            if (songCount > 4)
            {
                int remaining = songCount - 4;
                tickerText += $"  •  +{remaining} more";
            }

            // If encore exists but wasn't in the first 4 songs, append it
            if (nextGig.HasEncore && nextGig.encoreIndex >= 4)
            {
                tickerText += "  •  🎸 Encore Surprise!";
            }

            _fullTickerText = tickerText;
            _scrollPosition = 0;
            ShowTicker();
        }

        private void ShowCampaignCompleteTicker()
        {
            _fullTickerText = "👑 CAMPAIGN COMPLETE!  You've conquered every gig!  Select a new campaign to continue the journey.";
            _scrollPosition = 0;
            ShowTicker();
        }

        #region UI Creation

        private void CreateTickerBar()
        {
            if (_tickerBar != null) return;
            if (_gigView == null) return;

            _tickerBar = new GameObject("SongTeaserTicker", typeof(RectTransform), typeof(CanvasGroup));
            _tickerBar.transform.SetParent(_gigView.transform);

            // Position at bottom of screen, full width, thin
            // Moved up slightly (anchorMin.y from 0.0 to 0.01) to avoid overlapping navigation hints
            var rect = _tickerBar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.01f);
            rect.anchorMax = new Vector2(1f, 0.055f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Background bar
            var image = _tickerBar.AddComponent<Image>();
            image.color = new Color(0.06f, 0.06f, 0.10f, 0.92f);
            image.raycastTarget = false;

            // Top accent line
            var accentObj = new GameObject("AccentLine", typeof(RectTransform));
            accentObj.transform.SetParent(_tickerBar.transform);
            var accentRect = accentObj.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0.85f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = Vector2.zero;
            var accentImage = accentObj.AddComponent<Image>();
            accentImage.color = new Color(0.3f, 0.6f, 1f, 0.3f);
            accentImage.raycastTarget = false;

            // Scrolling text
            var textObj = new GameObject("TickerText", typeof(RectTransform));
            textObj.transform.SetParent(_tickerBar.transform);
            _tickerText = textObj.AddComponent<TextMeshProUGUI>();
            _tickerText.fontSize = 14;
            _tickerText.color = new Color(0.7f, 0.8f, 0.9f);
            _tickerText.alignment = TextAlignmentOptions.MidlineLeft;
            _tickerText.overflowMode = TextOverflowModes.Overflow;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 2);
            textRect.offsetMax = new Vector2(0, -2);

            // Mask to clip text at edges
            var maskObj = new GameObject("Mask", typeof(RectTransform));
            maskObj.transform.SetParent(_tickerBar.transform);
            var maskRect = maskObj.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            var mask = maskObj.AddComponent<RectMask2D>();

            // Reparent text under mask
            textObj.transform.SetParent(maskObj.transform);

            _tickerCanvasGroup = _tickerBar.GetComponent<CanvasGroup>();
            _tickerCanvasGroup.alpha = 0;

            _tickerBar.SetActive(false);
        }

        private void ShowTicker()
        {
            if (_tickerBar == null) return;
            _tickerBar.SetActive(true);

            if (_scrollCoroutine != null)
                StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = StartCoroutine(ScrollTicker());
        }

        private void HideTicker()
        {
            if (_tickerBar != null)
                _tickerBar.SetActive(false);
        }

        private IEnumerator ScrollTicker()
        {
            // Fade in
            _tickerCanvasGroup.alpha = 0;
            float elapsed = 0;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                _tickerCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsed / 0.3f);
                yield return null;
            }
            _tickerCanvasGroup.alpha = 1;

            // Scroll the text
            while (_tickerBar != null && _tickerBar.activeInHierarchy)
            {
                if (_tickerText != null && !string.IsNullOrEmpty(_fullTickerText))
                {
                    _tickerText.text = _fullTickerText;

                    // Get text width (approximate)
                    float textWidth = _fullTickerText.Length * 10f; // rough pixel estimate
                    float viewWidth = Screen.width * 0.95f;

                    // Scroll position
                    _scrollPosition += ScrollSpeed * Time.unscaledDeltaTime;

                    // Reset when fully scrolled past
                    if (_scrollPosition > textWidth + PaddingPixels)
                        _scrollPosition = -viewWidth;

                    // Apply position offset
                    var rect = _tickerText.GetComponent<RectTransform>();
                    rect.anchoredPosition = new Vector2(-_scrollPosition, 0);
                }
                yield return null;
            }
        }

        #endregion
    }
}