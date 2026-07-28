using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Core.Song;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// A single card in the CoverFlow carousel. Each card is a pooled MonoBehaviour
    /// with RectTransform, CanvasGroup, and UI Image/Text components for its
    /// three visual states (Completed / Active / Locked).
    ///
    /// V3 REFACTOR: Text-population methods have moved to CoverFlowCardControllerV3.
    /// This class now handles ONLY state visuals (overlays, frames, lock icons).
    /// </summary>
    public class CoverFlowCard : MonoBehaviour
    {
        [Header("Card Rect")]
        [SerializeField]
        private RectTransform _rectTransform;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        [Header("Poster Layer")]
        [SerializeField]
        private RawImage _posterImage;

        [SerializeField]
        private Image _grayscaleOverlay;

        [SerializeField]
        private Image _lockIcon;

        [Header("Active Card Text Regions")]
        [SerializeField]
        private TextMeshProUGUI _hypeSlot1Text;

        [SerializeField]
        private TextMeshProUGUI _hypeSlot2Text;

        [Header("Accent Border")]
        [SerializeField]
        private Image _accentBorder;

        [Header("Active Card — Content Regions")]
        [SerializeField] private TextMeshProUGUI _gigTitleText;
        [SerializeField] private Image           _bandLogoImage;
        [SerializeField] private TextMeshProUGUI _avgTrackText;
        [SerializeField] private TextMeshProUGUI _intensityText;
        [SerializeField] private GameObject      _activeContentPanel;

        [Header("Card Frame")]
        [SerializeField] private Image _woodenFrame;

        [Header("Completed State")]
        [SerializeField] private Image _soldOutSash;

        [Header("Locked State")]
        [SerializeField] private GameObject      _lockPanel;
        [SerializeField] private TextMeshProUGUI _unlockRequirementText;
        [SerializeField] private TextMeshProUGUI _mysteryTracklistText;

        [Header("Reveal Effect")]
        [SerializeField] private UIFlameRevealEffect _uiFlameEffect;

        // ── Runtime State ─────────────────────────────────────────

        private CoverFlowCardState _currentState = CoverFlowCardState.Locked;
        private GigInfo _gig;
        private CoverFlowConfig _config;

        public RectTransform RectTransform => _rectTransform;
        public CanvasGroup CanvasGroup => _canvasGroup;
        public GigInfo Gig => _gig;
        public CoverFlowCardState State => _currentState;

        // ── Public API ────────────────────────────────────────────

        /// <summary>
        /// Initialises the card with gig data and the campaign's visual config.
        /// </summary>
        public void Initialize(CoverFlowConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Populates the card from a gig and sets its visual state.
        /// </summary>
        public void SetGig(GigInfo gig, CoverFlowCardState state)
        {
            // PRE-EMPTIVE STRIKE: Instantly turn off the sash before doing ANYTHING.
            // This guarantees the pooled object drops its old visual state instantly.
            ForceSashVisibility(false);

            _gig = gig;
            _currentState = state;

            ApplyStateVisuals();

            // Apply sash visibility strictly from the state computed by GetCardState().
            ForceSashVisibility(state == CoverFlowCardState.Completed);
        }

        /// <summary>
        /// Sets the poster texture. Can be null to show empty/dark.
        /// </summary>
        public void SetPoster(Texture2D texture)
        {
            if (_posterImage == null) return;

            if (texture != null)
            {
                _posterImage.texture = texture;
                _posterImage.enabled = true;
            }
            else
            {
                _posterImage.texture = null;
                _posterImage.enabled = false;
            }
        }

        /// <summary>
        /// Sets accent colour for the border/glow (driven by config).
        /// </summary>
        public void SetAccentColor(Color color)
        {
            if (_accentBorder != null)
                _accentBorder.color = color;
        }

        /// <summary>
        /// Sets the unlock requirement text on a locked card.
        /// gigIndex is 1-based for display (e.g. gigIndex=8 → "CLEAR GIG 08").
        /// </summary>
        public void SetUnlockRequirement(int gigIndex)
        {
            if (_unlockRequirementText != null)
                _unlockRequirementText.text = $"UNLOCK: CLEAR GIG {gigIndex:D2}";
            if (_mysteryTracklistText != null)
                _mysteryTracklistText.text = "???";
        }

        /// <summary>
        /// Sets a tier-aware unlock requirement message on a locked card.
        /// Displays the tier name and how many more gigs must be completed
        /// before the tier unlocks.
        /// </summary>
        /// <param name="tierName">Display name of the locked tier.</param>
        /// <param name="remainingGigsNeeded">How many more gigs the player must complete.</param>
        public void SetUnlockRequirement(string tierName, int remainingGigsNeeded)
        {
            if (_unlockRequirementText != null)
            {
                if (remainingGigsNeeded > 0)
                    _unlockRequirementText.text = $"TIER LOCKED: COMPLETE {remainingGigsNeeded} MORE GIG{(remainingGigsNeeded == 1 ? "" : "S")} TO UNLOCK {tierName}";
                else
                    _unlockRequirementText.text = $"TIER LOCKED: {tierName}";
            }
            if (_mysteryTracklistText != null)
                _mysteryTracklistText.text = "???";
        }

        /// <summary>
        /// Shows or hides the active content panel.
        /// Call with isCenterSlot=true only for slot index 2.
        /// </summary>
        public void SetActiveContentVisible(bool visible)
        {
            if (_activeContentPanel != null)
                _activeContentPanel.SetActive(visible);
        }

        private void Start()
        {
            if (_soldOutSash == null)
            {
                // CareerFlowCard subclass doesn't use SoldOutSash — skip warning
                Debug.LogWarning($"{gameObject.name}: _soldOutSash is not assigned. This is expected for CareerFlow cards.");
            }
        }

        private void Awake()
        {
            if (_lockIcon != null && !_lockIcon.transform.IsChildOf(transform))
                Debug.LogWarning($"CoverFlowCard '{name}': _lockIcon is not a child of the card — it will not move with the card.", this);
            if (_grayscaleOverlay != null && !_grayscaleOverlay.transform.IsChildOf(transform))
                Debug.LogWarning($"CoverFlowCard '{name}': _grayscaleOverlay is not a child of the card — it will not move with the card.", this);
            if (_soldOutSash != null && !_soldOutSash.transform.IsChildOf(transform))
                Debug.LogWarning($"CoverFlowCard '{name}': _soldOutSash is not a child of the card — it will not move with the card.", this);
            if (_lockPanel != null && !_lockPanel.transform.IsChildOf(transform))
                Debug.LogWarning($"CoverFlowCard '{name}': _lockPanel is not a child of the card — it will not move with the card.", this);
        }

        /// <summary>
        /// Recalculates opacity of all text sub-elements.
        /// When the card is NOT in the centre, text fades to 0% instantly.
        /// The active content panel is never shown for locked cards,
        /// regardless of slot position.
        /// </summary>
        public void ApplyTextOpacityForSlot(bool isCenterSlot)
        {
            // Never show active content for locked cards
            SetActiveContentVisible(isCenterSlot && _currentState != CoverFlowCardState.Locked);

            float alpha = isCenterSlot ? 1f : 0f;
            SetTextAlpha(_hypeSlot1Text, alpha);
            SetTextAlpha(_hypeSlot2Text, alpha);
        }

        /// <summary>
        /// Instantly snaps the card to a TRUE 3D transform state.
        /// </summary>
        public void SnapTo(Vector3 localPosition, float yRotation, float scale, float alpha)
        {
            transform.localPosition = localPosition;
            transform.localRotation = Quaternion.Euler(0f, yRotation, 0f);
            transform.localScale = new Vector3(scale, scale, 1f);

            if (_canvasGroup != null)
                _canvasGroup.alpha = alpha;
        }

        // ── Visual State Application ─────────────────────────────

        public void ForceSashVisibility(bool visible)
        {
            if (_soldOutSash == null) return;
            
            if (visible)
            {
                _soldOutSash.enabled = true;
                _soldOutSash.color = Color.white;
                // Force the renderer to show it immediately
                _soldOutSash.canvasRenderer.SetAlpha(1f); 
            }
            else
            {
                // Hard disable the component so it drops out of the Canvas build queue
                _soldOutSash.enabled = false;
                // Render-thread override to ensure zero visibility instantly
                _soldOutSash.canvasRenderer.SetAlpha(0f);
            }
        }

        private void ApplyStateVisuals()
        {
            switch (_currentState)
            {
                case CoverFlowCardState.Completed:
                    ApplyCompletedVisuals();
                    break;
                case CoverFlowCardState.Active:
                    ApplyActiveVisuals();
                    break;
                case CoverFlowCardState.Locked:
                    ApplyLockedVisuals();
                    break;
            }
        }

        private void ApplyCompletedVisuals()
        {
            if (_posterImage != null) _posterImage.enabled = true;
            if (_grayscaleOverlay != null) _grayscaleOverlay.enabled = false;

            if (_lockPanel != null) _lockPanel.SetActive(false);
            if (_lockIcon != null) _lockIcon.enabled = false;
            if (_woodenFrame != null) _woodenFrame.enabled = true;
            if (_accentBorder != null) _accentBorder.enabled = false;
        }

        private void ApplyActiveVisuals()
        {
            if (_posterImage != null) _posterImage.enabled = true;
            if (_grayscaleOverlay != null) _grayscaleOverlay.enabled = false;
            
            if (_lockPanel != null) _lockPanel.SetActive(false);
            if (_lockIcon != null) _lockIcon.enabled = false;
            if (_woodenFrame != null) _woodenFrame.enabled = true;
            
            if (_accentBorder != null)
            {
                _accentBorder.enabled = true;
                if (_config != null) _accentBorder.color = _config.AccentColor;
            }
        }

        private void ApplyLockedVisuals()
        {
            if (_posterImage != null) _posterImage.enabled = false;
            if (_accentBorder != null) _accentBorder.enabled = false;

            if (_lockPanel != null) _lockPanel.SetActive(true);
            if (_lockIcon != null) _lockIcon.enabled = true;
            if (_grayscaleOverlay != null) _grayscaleOverlay.enabled = true;
            if (_woodenFrame != null) _woodenFrame.enabled = true;
        }
        
        // ── Helpers ───────────────────────────────────────────────

        private static void SetTextAlpha(TextMeshProUGUI text, float alpha)
        {
            if (text == null) return;
            var color = text.color;
            color.a = alpha;
            text.color = color;
        }

        // ── Reveal Effect ───────────────────────────────────────

        /// <summary>
        /// Triggers the flame-reveal visual effect on this card:
        /// transitions from Locked → Active, plays a flame burst,
        /// and performs a brief scale-pop animation.
        /// </summary>
        /// <param name="posterTexture">
        /// Optional poster texture to load BEFORE the state transition.
        /// If provided, the poster is set while the card is still in its
        /// Locked silhouette state, and becomes visible the instant the
        /// card transitions to Active. If null, the existing poster (or
        /// none) is preserved.
        /// </param>
        public void PlayRevealEffect(Texture2D posterTexture = null)
        {
            // 1. Pre-load poster texture while still in Locked state
            if (posterTexture != null)
                SetPoster(posterTexture);

            // 2. Transition from Locked → Active (instant — same frame)
            _currentState = CoverFlowCardState.Active;
            ApplyStateVisuals();  // Poster appears, grayscale clears, lock panel hides

            // 3. Deferred flame burst (next frame — allows canvas layout to settle)
            StartCoroutine(DeferredFlameBurst());

            // 4. Short scale pop animation (bounce)
            StartCoroutine(RevealScalePop());
        }

        /// <summary>
        /// Waits one frame for canvas layout to settle, then triggers the UI flame burst.
        /// </summary>
        private System.Collections.IEnumerator DeferredFlameBurst()
        {
            yield return null;

            if (_uiFlameEffect != null)
                _uiFlameEffect.PlayBurst();
        }

        /// <summary>
        /// Quick 1.1→1.0 scale bounce over 0.12s using linear interpolation.
        /// </summary>
        private IEnumerator RevealScalePop()
        {
            var startScale = Vector3.one * 1.15f;
            var endScale = Vector3.one;
            float duration = 0.15f;
            float elapsed = 0f;

            transform.localScale = startScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease-out cubic for snappy pop
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(startScale, endScale, easedT);
                yield return null;
            }

            transform.localScale = endScale;
        }
    }
}