using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.CareerFlow
{
    /// <summary>
    /// Pure renderer MonoBehaviour attached to the CareerGearCrateCard prefab.
    /// Receives CareerCardData via PopulateCard() and drives all child UI components.
    ///
    /// DESIGN PRINCIPLES:
    ///   - Zero decision logic — the card paints what it's told.
    ///   - No thresholds or business logic — those live on CareerFlowMenuController.
    ///   - Defensive null checks on all component references.
    ///   - Clean, unobstructed box art with integrated lower dashboard.
    /// </summary>
    public class CareerFlowController : MonoBehaviour
    {
        [Header("Box Art")]
        [SerializeField]
        private RawImage _boxArtImage;

        [Header("Crate Frame")]
        [SerializeField]
        private Image _crateFrame;

        [Header("Career Name (Optional)")]
        [SerializeField]
        private TextMeshProUGUI _careerNameText;

        [Header("Dashboard — Status Line")]
        [SerializeField]
        private TextMeshProUGUI _statusLineText;

        [SerializeField]
        private Image _bottomGradientOverlay;

        [Header("Dashboard — Progress Bar")]
        [SerializeField]
        private Image _progressBarFill;

        [SerializeField]
        private TextMeshProUGUI _progressLabel;

        // ── Public API ────────────────────────────────────────────

        /// <summary>
        /// Single entry point. Populates all child UI components from the provided data.
        /// Called by CareerFlowMenuController for each card slot.
        /// </summary>
        public void PopulateCard(CareerCardData data)
        {
            Debug.Log($"[CareerFlow] CareerFlowController.PopulateCard() on '{name}' — " +
                $"careerName='{data.careerName}', " +
                $"boxArtSprite={(data.boxArtSprite != null ? $"Sprite({data.boxArtSprite.texture?.width}x{data.boxArtSprite.texture?.height})" : "NULL")}, " +
                $"completionPercent={data.completionPercent:F2}");

            // 1. Box Art — clean, unobstructed, occupies top ~80% of card
            if (_boxArtImage != null)
            {
                if (data.boxArtSprite != null)
                {
                    _boxArtImage.texture = data.boxArtSprite.texture;
                    _boxArtImage.enabled = true;
                    Debug.Log($"[CareerFlow]   BoxArt SET: texture={data.boxArtSprite.texture?.width}x{data.boxArtSprite.texture?.height}");
                }
                else
                {
                    _boxArtImage.texture = null;
                    _boxArtImage.enabled = false;
                    Debug.LogWarning($"[CareerFlow]   BoxArt NULL — disabling _boxArtImage on '{name}'");
                }
            }
            else
            {
                Debug.LogError($"[CareerFlow]   _boxArtImage is NULL on '{name}' — cannot set box art!");
            }

            // 2. Career Name — only shown if displayNameOnCrate flag is true
            if (_careerNameText != null)
            {
                bool showName = data.displayNameOnCrate && !string.IsNullOrEmpty(data.careerName);
                _careerNameText.gameObject.SetActive(showName);
                if (showName)
                {
                    _careerNameText.text = data.careerName.ToUpper();
                    Debug.Log($"[CareerFlow]   CareerName SET: '{data.careerName.ToUpper()}'");
                }
            }

            // 3. Narrative Status Line
            if (_statusLineText != null)
            {
                _statusLineText.text = data.statusText ?? string.Empty;
                Debug.Log($"[CareerFlow]   StatusLine SET: '{data.statusText}'");
            }

            // 4. Bottom Gradient Overlay
            if (_bottomGradientOverlay != null)
            {
                _bottomGradientOverlay.enabled = !string.IsNullOrEmpty(data.statusText);
            }

            // 5. Progress Bar — fill amount 0.0–1.0
            if (_progressBarFill != null)
            {
                // Safety: ensure the Image is configured as a Filled bar
                if (_progressBarFill.type != Image.Type.Filled)
                {
                    Debug.LogWarning($"[CareerFlow]   ProgressBar on '{name}' had type={_progressBarFill.type}, forcing to Filled/Horizontal.");
                    _progressBarFill.type = Image.Type.Filled;
                    _progressBarFill.fillMethod = Image.FillMethod.Horizontal;
                    _progressBarFill.fillOrigin = 0; // left-to-right
                }

                // Safety: ensure the bar is visible
                _progressBarFill.enabled = true;

                // Safety: ensure alpha is fully opaque (material may have been reset)
                var color = _progressBarFill.color;
                if (color.a < 0.99f)
                {
                    Debug.LogWarning($"[CareerFlow]   ProgressBar color alpha was {color.a:F2}, forcing to 1.0.");
                    color.a = 1f;
                    _progressBarFill.color = color;
                }

                // Safety: ensure the GameObject is active
                if (!_progressBarFill.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[CareerFlow]   ProgressBar GameObject '{_progressBarFill.gameObject.name}' is inactive in hierarchy — activating.");
                    _progressBarFill.gameObject.SetActive(true);
                }

                float clamped = Mathf.Clamp01(data.completionPercent);
                _progressBarFill.fillAmount = clamped;
                Debug.Log($"[CareerFlow]   ProgressBar SET: fillAmount={clamped:F3} (raw={data.completionPercent:F3}), " +
                    $"type={_progressBarFill.type}, fillMethod={_progressBarFill.fillMethod}, " +
                    $"color.a={_progressBarFill.color.a:F2}, activeInHierarchy={_progressBarFill.gameObject.activeInHierarchy}");
            }
            else
            {
                Debug.LogError($"[CareerFlow]   _progressBarFill is NULL on '{name}' — cannot set progress bar!");
            }

            // 6. Progress Label
            if (_progressLabel != null)
            {
                int pct = Mathf.RoundToInt(data.completionPercent * 100f);
                _progressLabel.text = $"COMPLETION: {pct}%";
                Debug.Log($"[CareerFlow]   ProgressLabel SET: 'COMPLETION: {pct}%'");
            }

            // 7. Crate Frame
            if (_crateFrame != null)
            {
                _crateFrame.enabled = true;
            }
        }

        // ── Editor Validation ─────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_boxArtImage == null)
                Debug.LogWarning($"CareerFlowController '{name}': _boxArtImage is not assigned.", this);
            if (_crateFrame == null)
                Debug.LogWarning($"CareerFlowController '{name}': _crateFrame is not assigned.", this);
            if (_statusLineText == null)
                Debug.LogWarning($"CareerFlowController '{name}': _statusLineText is not assigned.", this);
            if (_progressBarFill == null)
                Debug.LogWarning($"CareerFlowController '{name}': _progressBarFill is not assigned.", this);
            if (_progressLabel == null)
                Debug.LogWarning($"CareerFlowController '{name}': _progressLabel is not assigned.", this);
        }
#endif
    }
}