using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.CareerFlow
{
    /// <summary>
    /// Simplified two-state model for CareerFlow cards.
    /// No Locked state — all career cards are always accessible.
    /// </summary>
    public enum CareerFlowCardState
    {
        /// <summary>Career is available to play. Full colour box art, active dashboard.</summary>
        Active,

        /// <summary>All gigs in this career have been completed. "TOUR COMPLETE" stamp.</summary>
        Completed
    }

    /// <summary>
    /// A single card in the CareerFlow carousel. Subclass of CoverFlowCard that
    /// overrides state visuals for the Gear Crate / Road Case motif.
    ///
    /// Inherits: RectTransform, CanvasGroup, SnapTo(), SetPoster(), SetAccentColor(),
    ///           ForceSashVisibility(), PlayRevealEffect().
    ///
    /// Overrides: ApplyStateVisuals() — swaps wooden frame for metallic crate frame,
    ///            removes lock panel/sold-out sash, uses two-state Active/Completed model.
    /// </summary>
    public class CareerFlowCard : CoverFlow.CoverFlowCard
    {
        [Header("Crate Frame")]
        [SerializeField]
        private Image _crateFrame;

        [Header("Completed State")]
        [SerializeField]
        private Image _completionStamp;

        [Header("Dashboard Panel")]
        [SerializeField]
        private GameObject _dashboardPanel;

        // ── Runtime State ─────────────────────────────────────────

        private CareerFlowCardState _careerFlowState = CareerFlowCardState.Active;
        private CareerInfo _career;

        public CareerInfo Career => _career;

        // ── Public API ────────────────────────────────────────────

        /// <summary>
        /// Initialises the card for CareerFlow use. No config needed
        /// (accent colours come from per-career data).
        /// </summary>
        public void Initialize()
        {
            Debug.Log($"[CareerFlow] CareerFlowCard.Initialize() on '{name}'");
            // Base class Initialize expects a CoverFlowConfig, but we don't need one.
            // We override state visuals directly.
        }

        /// <summary>
        /// Populates the card from a career and sets its visual state.
        /// </summary>
        public void SetCareer(CareerInfo career, CareerFlowCardState state)
        {
            Debug.Log($"[CareerFlow] CareerFlowCard.SetCareer() on '{name}' — " +
                $"career={(career != null ? $"'{career.id}'/'{career.name}'" : "NULL")}, state={state}");
            _career = career;
            _careerFlowState = state;

            ApplyStateVisuals();
        }

        /// <summary>
        /// Recalculates opacity of all text sub-elements.
        /// When the card is NOT in the centre, dashboard fades to 0% instantly.
        /// </summary>
        public new void ApplyTextOpacityForSlot(bool isCenterSlot)
        {
            if (_dashboardPanel != null)
                _dashboardPanel.SetActive(isCenterSlot);
        }

        // ── Visual State Application ─────────────────────────────

        private void ApplyStateVisuals()
        {
            switch (_careerFlowState)
            {
                case CareerFlowCardState.Completed:
                    ApplyCompletedVisuals();
                    break;
                case CareerFlowCardState.Active:
                default:
                    ApplyActiveVisuals();
                    break;
            }
        }

        private void ApplyActiveVisuals()
        {
            // Crate frame — always visible, metallic
            if (_crateFrame != null)
                _crateFrame.enabled = true;

            // No completion stamp
            if (_completionStamp != null)
                _completionStamp.enabled = false;

            // Dashboard visible (controlled by ApplyTextOpacityForSlot)
            if (_dashboardPanel != null)
                _dashboardPanel.SetActive(true);
        }

        private void ApplyCompletedVisuals()
        {
            // Crate frame — still visible
            if (_crateFrame != null)
                _crateFrame.enabled = true;

            // Show "TOUR COMPLETE" stamp overlay
            if (_completionStamp != null)
            {
                _completionStamp.enabled = true;
                _completionStamp.canvasRenderer.SetAlpha(1f);
            }

            // Dashboard visible (shows 100% completion)
            if (_dashboardPanel != null)
                _dashboardPanel.SetActive(true);
        }

        // ── Editor Validation ─────────────────────────────────────

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_crateFrame == null)
                Debug.LogWarning($"CareerFlowCard '{name}': _crateFrame is not assigned.", this);
        }
#endif
    }
}