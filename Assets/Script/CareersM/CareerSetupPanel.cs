using System.Collections.Generic;
using UnityEngine;
using TMPro;
using YARG.Core.Input;
using YARG.Menu.Navigation;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Popup panel for career song analysis results and access to the match workflow.
    /// </summary>
    public class CareerSetupPanel : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _careerNameText;
        [SerializeField] private TextMeshProUGUI _totalSongsText;
        [SerializeField] private TextMeshProUGUI _hashMatchText;
        [SerializeField] private TextMeshProUGUI _exactMatchText;
        [SerializeField] private TextMeshProUGUI _fuzzyMatchText;
        [SerializeField] private TextMeshProUGUI _manualMatchText;
        [SerializeField] private TextMeshProUGUI _unmatchedText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private RectTransform _progressBarFill;

        [Space]
        [SerializeField] private ManualMatchPanel _manualMatchPanel;

        [Header("Button Hints")]
        [SerializeField] private TextMeshProUGUI _enterMatchPanelHint;

        private string _careerId;
        private CareerMatchCache _cache;
        private bool _manualMatchOpen;

        public void Show(string careerId, string careerName)
        {
            _careerId = careerId;
            _manualMatchOpen = false;
            gameObject.SetActive(true);

            SetText(_careerNameText, careerName.ToUpper());

            _cache = SongMatchCache.GetOrLoadCache(careerId);
            if (_cache == null || _cache.totalSongs == 0)
                _cache = CareerManager.Instance.AnalyzeCareer(careerId);

            UpdateDisplay(_cache);
            UpdateHint();
            PushNavScheme();
        }

        private void OnDisable()
        {
            if (!_manualMatchOpen)
                Navigator.Instance?.PopScheme();
        }

        private void PushNavScheme()
        {
            Navigator.Instance.PushScheme(new NavigationScheme(new()
            {
                new NavigationScheme.Entry(MenuAction.Green, "Match Panel", OpenManualMatch),
                new NavigationScheme.Entry(MenuAction.Orange, "Re-Analyze (Clear)", RunFreshAnalysis),
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", Close),
            }, false));
        }

        private void UpdateDisplay(CareerMatchCache cache)
        {
            if (cache == null)
            {
                ShowNotAnalyzed();
                return;
            }

            int matched = cache.hashMatches + cache.exactMatches +
                cache.fuzzyMatches + cache.manualMatches;

            SetText(_totalSongsText, $"{cache.totalSongs} UNIQUE SONGS");
            SetText(_hashMatchText, $"{cache.hashMatches} PERFECT (HASH)");
            SetText(_exactMatchText, $"{cache.exactMatches} EXACT (TITLE+ARTIST)");
            SetText(_fuzzyMatchText, $"{cache.fuzzyMatches} FUZZY");
            SetText(_manualMatchText, $"{cache.manualMatches} MANUAL");
            SetText(_unmatchedText, $"{cache.unmatched} UNMATCHED");

            if (cache.totalSongs > 0)
            {
                float pct = (float) matched / cache.totalSongs;
                SetText(_statusText, $"{matched}/{cache.totalSongs} MATCHED ({pct:P0})");
                SetProgressBar(pct);
            }
            else
            {
                SetText(_statusText, "NO SONGS");
                SetProgressBar(0f);
            }
        }

        private void ShowNotAnalyzed()
        {
            int uniqueSongs = CareerManager.Instance.GetUniqueSongCount(_careerId);
            SetText(_totalSongsText, $"{uniqueSongs} UNIQUE SONGS");
            SetText(_hashMatchText, "—");
            SetText(_exactMatchText, "—");
            SetText(_fuzzyMatchText, "—");
            SetText(_manualMatchText, "—");
            SetText(_unmatchedText, "—");
            SetText(_statusText, "NOT YET ANALYZED");
            SetProgressBar(0f);
        }

        private void RunFreshAnalysis()
        {
            SongMatchCache.ClearCache(_careerId);
            SetText(_statusText, "ANALYZING...");
            _cache = CareerManager.Instance.AnalyzeCareer(_careerId);
            UpdateDisplay(_cache);
            UpdateHint();
        }

        private void OpenManualMatch()
        {
            if (_manualMatchPanel == null) return;

            _manualMatchOpen = true;
            Navigator.Instance.PopScheme();
            _manualMatchPanel.Show(_careerId, OnManualMatchClosed);
        }

        private void OnManualMatchClosed()
        {
            _manualMatchOpen = false;
            _cache = SongMatchCache.GetOrLoadCache(_careerId);
            UpdateDisplay(_cache);
            UpdateHint();
            PushNavScheme();
        }

        private void Close()
        {
            gameObject.SetActive(false);
        }

        private void UpdateHint()
        {
            if (_enterMatchPanelHint == null) return;

            int unmatched = _cache?.unmatched ?? 0;
            _enterMatchPanelHint.text = unmatched > 0
                ? $"<color=#00CC88>▶ PRESS GREEN</color>  to enter match panel  <color=#FF6666>({unmatched} unmatched)</color>"
                : "<color=#00CC88>▶ PRESS GREEN</color>  to enter match panel";
        }

        private void SetProgressBar(float fill)
        {
            if (_progressBarFill == null) return;
            _progressBarFill.anchorMin = new Vector2(0f, 0f);
            _progressBarFill.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            _progressBarFill.offsetMin = Vector2.zero;
            _progressBarFill.offsetMax = Vector2.zero;
        }

        private static void SetText(TextMeshProUGUI text, string value)
        {
            if (text != null) text.text = value;
        }
    }
}