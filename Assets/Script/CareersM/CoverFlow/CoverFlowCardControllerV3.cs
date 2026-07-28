using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Pure renderer MonoBehaviour attached to the CoverFlowCard prefab[cite: 5].
    /// Receives GigCardData via PopulateCard() and drives all child UI components[cite: 5].
    ///
    /// DESIGN PRINCIPLES:
    ///   - Zero decision logic — the card paints what it's told[cite: 5].
    ///   - No thresholds — those live on CoverFlowController[cite: 5].
    ///   - Cached StringBuilder for tracklist to minimize GC allocation[cite: 5].
    ///   - Defensive null checks on all component references[cite: 5].
    /// </summary>
    public class CoverFlowCardControllerV3 : MonoBehaviour
    {
        [Header("Poster & Branding")]
        [SerializeField]
        private RawImage _posterImage;

        [SerializeField]
        private Image _bandLogoImage;

        [Header("Text Regions")]
        [SerializeField]
        private TextMeshProUGUI _gigTitleText;

        [SerializeField]
        private TextMeshProUGUI _tracklistText;

        [Header("Hype Engine Slots (Footer)")]
        [SerializeField]
        private TextMeshProUGUI _avgTrackText;

        [SerializeField]
        private TextMeshProUGUI _intensityText;

        [Header("Hype Icon")]
        [SerializeField]
        private Image _hypeIcon;

        [SerializeField]
        private HypeIconSet _iconSet;

        // ── Cached StringBuilder (reused to avoid GC) ─────────────

        private readonly StringBuilder _tracklistBuilder = new StringBuilder(512);

        // ── Headliner Dual-Pulse State ────────────────────────────

        /// <summary>Uppercased headliner song titles for vertex-color matching during pulse.</summary>
        private readonly List<string> _headlinerTitles = new(8);

        /// <summary>Uppercased headliner artist names for vertex-color matching during pulse.</summary>
        private readonly List<string> _headlinerArtists = new(8);

        /// <summary>Campaign accent colour, cached for the pulse coroutine.</summary>
        private Color _accentColor = Color.white;

        /// <summary>True while the pulse coroutine is actively running.</summary>
        private bool _pulseActive;

        // ── Public API ────────────────────────────────────────────

        /// <summary>
        /// Single entry point. Populates all child UI components from the provided data[cite: 5].
        /// Called by CoverFlowController for each card slot[cite: 5].
        /// </summary>
        public void PopulateCard(GigCardData data)
        {
            // ── Stop any existing pulse before rebuilding ─────────
            StopPulse();

            // ── Cache accent colour for the coroutine ─────────────
            _accentColor = data.accentColor;

            // 1. Poster Image
            if (_posterImage != null)
            {
                if (data.posterSprite != null)
                {
                    _posterImage.texture = data.posterSprite.texture;
                    _posterImage.enabled = true;
                }
                else
                {
                    _posterImage.texture = null;
                    _posterImage.enabled = false;
                }
            }

            // 2. Band Logo Image — disable entire GameObject if null so
            //    the VerticalLayoutGroup collapses the space seamlessly[cite: 5].
            if (_bandLogoImage != null)
            {
                if (data.bandLogoSprite != null)
                {
                    _bandLogoImage.sprite = data.bandLogoSprite;
                    _bandLogoImage.gameObject.SetActive(true);
                }
                else
                {
                    _bandLogoImage.sprite = null;
                    _bandLogoImage.gameObject.SetActive(false);
                }
            }

            // 3. Gig Title — uppercase as per design spec[cite: 5].
            if (_gigTitleText != null)
            {
                _gigTitleText.text = !string.IsNullOrEmpty(data.gigTitle)
                    ? data.gigTitle.ToUpper()
                    : string.Empty;
            }

            // 4. Numbered Tracklist — built via cached StringBuilder[cite: 5].
            if (_tracklistText != null)
            {
                BuildTracklistString(data.tracklist, data.hasEncore);
                _tracklistText.text = _tracklistBuilder.ToString();

                // Start dual-pulse if this gig has headliner tracks
                StartPulse();
            }

            // 5. Hype Engine Slots — pre-computed by controller[cite: 5].
            if (_avgTrackText != null)
            {
                _avgTrackText.text = data.hypeSlot1 ?? string.Empty;
            }

            if (_intensityText != null)
            {
                _intensityText.text = data.hypeSlot2 ?? string.Empty;
            }

            // 6. Hype Icon — resolved from HypeIconSet by icon name.
            if (_iconSet != null && _hypeIcon != null)
            {
                var sprite = _iconSet.GetSprite(data.hypeIcon);
                _hypeIcon.sprite = sprite;
                _hypeIcon.enabled = sprite != null;
            }
        }

        // ── Tracklist Builder ─────────────────────────────────────

        /// <summary>Fallback when TMP autosize min is unset.</summary>
        private const float DEFAULT_FONT_SIZE_MIN = 15f;

        /// <summary>Artist subtitle size relative to the title (matches &lt;size=65%&gt;).</summary>
        private const float ARTIST_SIZE_RATIO = 0.65f;

        /// <summary>Warm gold for the +ENCORE! teaser (matches legacy GigSongListPanel).</summary>
        private const string ENCORE_COLOR_HEX = "#FFAA00";

        /// <summary>
        /// Builds a numbered, tightly packed tracklist string into the cached StringBuilder[cite: 5].
        /// Employs precise inline line-height adjustments to bring the artist text right below
        /// the song title while maintaining distinct spacing blocks between separate tracks.
        /// Also tracks headliner song titles and artist names for the dual-pulse vertex animation.
        /// Long titles/artists are truncated at the autosize minimum font size so later songs
        /// are never eaten by whole-string TMP ellipsis.
        /// </summary>
        private void BuildTracklistString(System.Collections.Generic.List<SongChartData> tracklist, bool hasEncore)
        {
            _tracklistBuilder.Clear();
            _headlinerTitles.Clear();
            _headlinerArtists.Clear();

            bool hasSongs = tracklist != null && tracklist.Count > 0;
            if (!hasSongs)
            {
                if (hasEncore)
                    AppendEncoreTeaser(hadPriorContent: false);
                return;
            }

            float maxWidth = GetTracklistMaxWidth();
            float measureSize = GetTracklistMeasureFontSize();

            for (int i = 0; i < tracklist.Count; i++)
            {
                var song = tracklist[i];
                string title = !string.IsNullOrEmpty(song.title) ? song.title.ToUpper() : "???";
                string artist = !string.IsNullOrEmpty(song.artist) ? song.artist.ToUpper() : null;

                // Truncate plain strings before rich-text wrapping (measure at autosize min).
                string numberPrefix = $"{i + 1}. ";
                if (maxWidth > 0f)
                {
                    // Titles render inside <b>; include that in width math.
                    title = TruncateToWidth(title, maxWidth, measureSize, numberPrefix, bold: true);
                    if (artist != null)
                        artist = TruncateToWidth(artist, maxWidth, measureSize * ARTIST_SIZE_RATIO, "   ", bold: false);
                }

                // Track headliner strings for vertex-color pulse animation (displayed text).
                if (song.isHeadliner)
                {
                    _headlinerTitles.Add(title);
                    if (artist != null)
                        _headlinerArtists.Add(artist);
                }

                // 1. Open a tight line-height block for Title + Artist cohesion
                _tracklistBuilder.Append("<line-height=40%>");
                _tracklistBuilder.Append(numberPrefix);
                _tracklistBuilder.Append("<b>");
                _tracklistBuilder.Append(title);
                _tracklistBuilder.Append("</b>\n");

                if (artist != null)
                {
                    // 2. Format artist subtitle — slightly smaller (65%) and softer grey (#A0A0A0)
                    _tracklistBuilder.Append("   <size=65%><color=#A0A0A0>");
                    _tracklistBuilder.Append(artist);
                    _tracklistBuilder.Append("</color></size>");
                }

                // 3. Close the tight line-height tracking region
                _tracklistBuilder.Append("</line-height>");

                // 4. Inter-song spacing when another track (or encore teaser) follows
                if (i < tracklist.Count - 1 || hasEncore)
                {
                    _tracklistBuilder.Append("<line-height=150%>\n</line-height>");
                }
            }

            if (hasEncore)
                AppendEncoreTeaser(hadPriorContent: true);
        }

        private void AppendEncoreTeaser(bool hadPriorContent)
        {
            // Spacing already appended after the last song when hadPriorContent is true.
            if (!hadPriorContent)
                _tracklistBuilder.Append("<line-height=40%>");

            _tracklistBuilder.Append("<color=");
            _tracklistBuilder.Append(ENCORE_COLOR_HEX);
            _tracklistBuilder.Append("><b>+ENCORE!</b></color>");

            if (!hadPriorContent)
                _tracklistBuilder.Append("</line-height>");
        }

        /// <summary>
        /// Available horizontal space for a single tracklist line (rect width minus TMP margins).
        /// Returns 0 when layout has not resolved yet — callers skip truncation in that case.
        /// </summary>
        private float GetTracklistMaxWidth()
        {
            if (_tracklistText == null) return 0f;

            float width = _tracklistText.rectTransform.rect.width;
            if (width <= 0f) return 0f;

            var margin = _tracklistText.margin;
            return Mathf.Max(0f, width - margin.x - margin.z);
        }

        /// <summary>
        /// Font size used for truncation math: TMP autosize minimum (fallback 15).
        /// Titles that fit at this size will not be truncated; autosize may still grow them.
        /// </summary>
        private float GetTracklistMeasureFontSize()
        {
            if (_tracklistText == null) return DEFAULT_FONT_SIZE_MIN;

            float min = _tracklistText.fontSizeMin;
            return min > 0f ? min : DEFAULT_FONT_SIZE_MIN;
        }

        /// <summary>
        /// Truncates <paramref name="text"/> with "..." so that <paramref name="prefix"/> + result
        /// fits within <paramref name="maxWidth"/> when measured at <paramref name="fontSize"/>.
        /// Temporarily disables autosize on the tracklist TMP for accurate measurement.
        /// When <paramref name="bold"/> is true, measures with &lt;b&gt; tags to match rendered titles.
        /// </summary>
        private string TruncateToWidth(string text, float maxWidth, float fontSize, string prefix, bool bold)
        {
            if (_tracklistText == null || string.IsNullOrEmpty(text) || maxWidth <= 0f)
                return text;

            bool wasAuto = _tracklistText.enableAutoSizing;
            float prevSize = _tracklistText.fontSize;

            _tracklistText.enableAutoSizing = false;
            _tracklistText.fontSize = fontSize;

            string Measure(string body) =>
                bold ? prefix + "<b>" + body + "</b>" : prefix + body;

            try
            {
                if (_tracklistText.GetPreferredValues(Measure(text)).x <= maxWidth)
                    return text;

                const string ellipsis = "...";
                int lo = 0;
                int hi = text.Length;
                int best = 0;

                while (lo <= hi)
                {
                    int mid = (lo + hi) / 2;
                    string candidate = text.Substring(0, mid).TrimEnd() + ellipsis;
                    if (_tracklistText.GetPreferredValues(Measure(candidate)).x <= maxWidth)
                    {
                        best = mid;
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid - 1;
                    }
                }

                if (best <= 0)
                    return ellipsis;

                return text.Substring(0, best).TrimEnd() + ellipsis;
            }
            finally
            {
                _tracklistText.fontSize = prevSize;
                _tracklistText.enableAutoSizing = wasAuto;
            }
        }

        // ── Headliner Dual-Pulse Animation ────────────────────────

        /// <summary>Pulse sine-wave frequency in Hz. 2.5 = gentle, organic breathing.</summary>
        private const float PULSE_FREQUENCY = 2.5f;

        /// <summary>Title pulse amplitude: how far toward accent colour (0=white, 1=full accent).</summary>
        private const float PULSE_TITLE_AMPLITUDE = 0.35f;

        /// <summary>Artist pulse amplitude: subtle warm tint (keeps artist visually subordinate to title).</summary>
        private const float PULSE_ARTIST_AMPLITUDE = 0.12f;

        /// <summary>Baseline artist colour #A0A0A0 (normalised RGB).</summary>
        private static readonly Color ARTIST_GREY = new Color(0.627f, 0.627f, 0.627f);

        /// <summary>
        /// Coroutine that runs each frame, scanning the parsed TMP mesh for headliner
        /// words and modulating their vertex colours with a sine wave.
        /// Title words oscillate between white and accent at <see cref="PULSE_TITLE_AMPLITUDE"/>.
        /// Artist words oscillate between grey and accent at <see cref="PULSE_ARTIST_AMPLITUDE"/>.
        /// Both pulse in sync on the same sine phase.
        /// </summary>
        private IEnumerator PulseHeadlinersCoroutine()
        {
            while (_pulseActive)
            {
                if (_tracklistText == null ||
                    (_headlinerTitles.Count == 0 && _headlinerArtists.Count == 0))
                {
                    yield return null;
                    continue;
                }

                // Force a mesh rebuild so textInfo reflects any layout changes
                _tracklistText.ForceMeshUpdate();
                var textInfo = _tracklistText.textInfo;

                // Sine wave: [0, 1] at PULSE_FREQUENCY Hz
                float phase = Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f * PULSE_FREQUENCY) * 0.5f + 0.5f;

                // Pre-compute target colours for this frame
                Color titleTarget  = Color.Lerp(Color.white,   _accentColor, phase * PULSE_TITLE_AMPLITUDE);
                Color artistTarget = Color.Lerp(ARTIST_GREY,   _accentColor, phase * PULSE_ARTIST_AMPLITUDE);

                // Scan each word in the parsed textInfo
                for (int w = 0; w < textInfo.wordCount; w++)
                {
                    var wordInfo = textInfo.wordInfo[w];
                    string word = wordInfo.GetWord();

                    Color? wordColor = null;

                    // Check title matches first (higher visual priority)
                    if (_headlinerTitles.Count > 0)
                    {
                        foreach (var title in _headlinerTitles)
                        {
                            if (word.Contains(title) || title.Contains(word))
                            {
                                wordColor = titleTarget;
                                break;
                            }
                        }
                    }

                    // Check artist matches (only if not already matched as title)
                    if (wordColor == null && _headlinerArtists.Count > 0)
                    {
                        foreach (var artist in _headlinerArtists)
                        {
                            if (word.Contains(artist) || artist.Contains(word))
                            {
                                wordColor = artistTarget;
                                break;
                            }
                        }
                    }

                    // Apply vertex colour for every character in this word
                    if (wordColor.HasValue)
                    {
                        Color32 c32 = wordColor.Value;
                        for (int ch = wordInfo.firstCharacterIndex; ch <= wordInfo.lastCharacterIndex; ch++)
                        {
                            var charInfo = textInfo.characterInfo[ch];
                            if (!charInfo.isVisible) continue;

                            int matIndex = charInfo.materialReferenceIndex;
                            if (matIndex < 0 || matIndex >= textInfo.meshInfo.Length) continue;

                            var colors = textInfo.meshInfo[matIndex].colors32;
                            int vi = charInfo.vertexIndex;
                            if (vi < 0 || vi + 3 >= colors.Length) continue;

                            colors[vi + 0] = c32;
                            colors[vi + 1] = c32;
                            colors[vi + 2] = c32;
                            colors[vi + 3] = c32;
                        }
                    }
                }

                // Push modified vertex colours back to the mesh
                _tracklistText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

                yield return null;
            }
        }

        /// <summary>
        /// Starts the headliner dual-pulse coroutine.
        /// No-op if there are no headliner tracks or if already running.
        /// </summary>
        public void StartPulse()
        {
            bool hasHeadliners = (_headlinerTitles != null && _headlinerTitles.Count > 0)
                               || (_headlinerArtists != null && _headlinerArtists.Count > 0);
            if (!hasHeadliners || _pulseActive) return;

            _pulseActive = true;
            StartCoroutine(PulseHeadlinersCoroutine());
        }

        /// <summary>
        /// Stops the pulse coroutine and resets all headliner vertex colours back to
        /// their defaults (white for titles, #A0A0A0 for artists).
        /// </summary>
        public void StopPulse()
        {
            _pulseActive = false;
            StopAllCoroutines();

            // Reset vertex colours if we have a tracklist reference
            if (_tracklistText == null || (_headlinerTitles.Count == 0 && _headlinerArtists.Count == 0))
                return;

            _tracklistText.ForceMeshUpdate();
            var textInfo = _tracklistText.textInfo;

            for (int w = 0; w < textInfo.wordCount; w++)
            {
                var wordInfo = textInfo.wordInfo[w];
                string word = wordInfo.GetWord();

                // Determine reset colour: artist words reset to grey, titles to white
                Color32 resetColor = Color.white;

                bool isHeadlinerWord = false;

                // Check if this word matches a headliner title
                foreach (var title in _headlinerTitles)
                {
                    if (word.Contains(title) || title.Contains(word))
                    {
                        isHeadlinerWord = true;
                        break;
                    }
                }

                // Check if this word matches a headliner artist (reset to grey)
                if (!isHeadlinerWord)
                {
                    foreach (var artist in _headlinerArtists)
                    {
                        if (word.Contains(artist) || artist.Contains(word))
                        {
                            isHeadlinerWord = true;
                            resetColor = ARTIST_GREY;
                            break;
                        }
                    }
                }

                if (!isHeadlinerWord) continue;

                for (int ch = wordInfo.firstCharacterIndex; ch <= wordInfo.lastCharacterIndex; ch++)
                {
                    var charInfo = textInfo.characterInfo[ch];
                    if (!charInfo.isVisible) continue;

                    int matIndex = charInfo.materialReferenceIndex;
                    if (matIndex < 0 || matIndex >= textInfo.meshInfo.Length) continue;

                    var colors = textInfo.meshInfo[matIndex].colors32;
                    int vi = charInfo.vertexIndex;
                    if (vi < 0 || vi + 3 >= colors.Length) continue;

                    colors[vi + 0] = resetColor;
                    colors[vi + 1] = resetColor;
                    colors[vi + 2] = resetColor;
                    colors[vi + 3] = resetColor;
                }
            }

            _tracklistText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        // ── Lifecycle ─────────────────────────────────────────────

        private void OnDisable()
        {
            // Ensure cleanup when the card GameObject is deactivated (e.g. scrolling away)
            StopPulse();
        }

        // ── Editor Validation ─────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Warn if critical references are missing in the prefab[cite: 5].
            if (_gigTitleText == null)
                Debug.LogWarning($"CoverFlowCardControllerV3 '{name}': _gigTitleText is not assigned.", this);
            if (_tracklistText == null)
                Debug.LogWarning($"CoverFlowCardControllerV3 '{name}': _tracklistText is not assigned.", this);
            if (_avgTrackText == null)
                Debug.LogWarning($"CoverFlowCardControllerV3 '{name}': _avgTrackText is not assigned.", this);
            if (_intensityText == null)
                Debug.LogWarning($"CoverFlowCardControllerV3 '{name}': _intensityText is not assigned.", this);
        }
#endif
    }
}