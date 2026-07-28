using UnityEngine;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Bootstrapper component that activates the CoverFlow carousel when present
    /// on the CareerGig menu GameObject in the scene.
    ///
    /// Drag this component onto the CareerGig menu GameObject and assign its
    /// serialized references in the Inspector.
    ///
    /// When this component is present and configured:
    ///   - CareerView opening a campaign will show the CoverFlow carousel
    ///
    /// When this component is absent or has missing references:
    ///   - Classic vertical GigView list is used as before (no code changes needed)
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CoverFlowBootstrapper : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField]
        private CoverFlowController _coverFlowController;

        [SerializeField]
        private CoverFlowBackground _coverFlowBackground;

        [Header("Card Pool (exactly 7)")]
        [SerializeField]
        private CoverFlowCard[] _cardSlots = new CoverFlowCard[7];

        /// <summary>
        /// Whether the CoverFlow system is fully wired and available.
        /// </summary>
        public bool IsAvailable =>
            _coverFlowController != null &&
            _cardSlots != null && _cardSlots.Length == 7 &&
            _cardSlots[0] != null;

        private void Awake()
        {
            AutoWireReferences();
            LogDiagnostics();
        }

        /// <summary>
        /// Attempts to find missing references from children/self.
        /// Safe to call even if references are already assigned.
        /// </summary>
        private void AutoWireReferences()
        {
            // Only auto-find if not already assigned
            if (_coverFlowController == null)
                _coverFlowController = GetComponentInChildren<CoverFlowController>(true);

            if (_coverFlowBackground == null)
                _coverFlowBackground = GetComponentInChildren<CoverFlowBackground>(true);

            // Rebuild if array size doesn't match exactly 7, or first slot is null.
            // This catches both too-few (Length<7) AND too-many (Length>7) cases
            // that can arise from Inspector edits.
            if (_cardSlots == null || _cardSlots.Length != 7 || _cardSlots[0] == null)
            {
                var found = GetComponentsInChildren<CoverFlowCard>(true);
                Debug.Log($"[CoverFlowBootstrapper] AutoWire: found {found.Length} CoverFlowCard(s) in children of '{name}'");
                if (found.Length >= 7)
                {
                    _cardSlots = new CoverFlowCard[7];
                    for (int i = 0; i < 7 && i < found.Length; i++)
                        _cardSlots[i] = found[i];
                }
            }

            // Forward card slots to the controller if it also needs them
            if (_coverFlowController != null && _cardSlots != null && _cardSlots.Length == 7)
            {
                _coverFlowController.AssignCardSlots(_cardSlots);
            }
        }

        private void LogDiagnostics()
        {
            Debug.Log($"[CoverFlowBootstrapper] DIAGNOSTICS for '{name}':\n" +
                $"  _coverFlowController = {(_coverFlowController != null ? _coverFlowController.name : "NULL")}\n" +
                $"  _coverFlowBackground = {(_coverFlowBackground != null ? _coverFlowBackground.name : "NULL")}\n" +
                $"  _cardSlots array = {(_cardSlots != null ? $"length={_cardSlots.Length}" : "NULL")}\n" +
                $"  _cardSlots[0] = {(_cardSlots != null && _cardSlots.Length > 0 && _cardSlots[0] != null ? _cardSlots[0].name : "NULL")}\n" +
                $"  _cardSlots[1] = {(_cardSlots != null && _cardSlots.Length > 1 && _cardSlots[1] != null ? _cardSlots[1].name : "NULL")}\n" +
                $"  _cardSlots[2] = {(_cardSlots != null && _cardSlots.Length > 2 && _cardSlots[2] != null ? _cardSlots[2].name : "NULL")}\n" +
                $"  _cardSlots[3] = {(_cardSlots != null && _cardSlots.Length > 3 && _cardSlots[3] != null ? _cardSlots[3].name : "NULL")}\n" +
                $"  _cardSlots[4] = {(_cardSlots != null && _cardSlots.Length > 4 && _cardSlots[4] != null ? _cardSlots[4].name : "NULL")}\n" +
                $"  _cardSlots[5] = {(_cardSlots != null && _cardSlots.Length > 5 && _cardSlots[5] != null ? _cardSlots[5].name : "NULL")}\n" +
                $"  _cardSlots[6] = {(_cardSlots != null && _cardSlots.Length > 6 && _cardSlots[6] != null ? _cardSlots[6].name : "NULL")}\n" +
                $"  IsAvailable = {IsAvailable}\n" +
                $"  Children of '{name}': {string.Join(", ", System.Linq.Enumerable.Select(GetComponentsInChildren<Transform>(true), t => t.name))}");
        }

        /// <summary>
        /// Called by CareerView to activate the CoverFlow UI for a campaign.
        /// Does NOT disable GigView — instead the CoverFlow UI renders on top
        /// via sibling index ordering. This avoids GigView.OnDisable() nav scheme issues.
        /// </summary>
        public void OpenCampaign(CareerInfo career)
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("CoverFlowBootstrapper: Not fully wired — cannot open CoverFlow. " +
                    "Assign CoverFlowController and 7 card slots in the Inspector.");
                return;
            }

            // Disable the legacy GigSongListPanel — CoverFlow cards are self-contained
            var legacyPanel = GetComponentInChildren<GigSongListPanel>(includeInactive: true);
            if (legacyPanel != null)
                legacyPanel.gameObject.SetActive(false);

            // Activate the CoverFlow elements (they render on top of GigView)
            if (_coverFlowBackground != null)
            {
                var cfCanvas = CoverFlowBackground.FindCoverFlowUICanvasInHierarchy(
                    transform, _coverFlowBackground);
                if (cfCanvas != null)
                    _coverFlowBackground.AttachToCoverFlowCanvas(cfCanvas.transform);
                else
                    Debug.LogError("[CoverFlowBootstrapper] No CoverFlowUI Canvas found — CoverFlowBackground won't render!");

                _coverFlowBackground.gameObject.SetActive(true);
            }

            foreach (var card in _cardSlots)
            {
                if (card != null)
                    card.gameObject.SetActive(true);
            }

            _coverFlowController.gameObject.SetActive(true);

            // Move CoverFlow elements to top of sibling order so they render over GigView
            Transform cfParent = _coverFlowController.transform.parent ?? transform;
            cfParent.SetAsLastSibling();

            // Initialize the controller (loads campaign bg via ApplyConfig)
            _coverFlowController.SetCareer(career);

            // Re-arm the guard after ApplyConfig so a failed first paint can self-heal.
            if (_coverFlowBackground != null)
                _coverFlowBackground.BeginVisibilityGuard();
        }

        /// <summary>
        /// Cleanup when leaving the menu.
        /// </summary>
        public void CloseCampaign()
        {
            if (_coverFlowController != null)
                _coverFlowController.gameObject.SetActive(false);

            if (_coverFlowBackground != null)
                _coverFlowBackground.gameObject.SetActive(false);

            if (_cardSlots != null)
            {
                foreach (var card in _cardSlots)
                {
                    if (card != null)
                        card.gameObject.SetActive(false);
                }
            }
        }
    }
}