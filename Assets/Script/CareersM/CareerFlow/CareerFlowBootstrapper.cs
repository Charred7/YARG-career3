using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YARG.Career;

namespace YARG.Menu.Career.CareerFlow
{
    /// <summary>
    /// Bootstrapper component that activates the CareerFlow carousel when the
    /// CareerCareerModern menu is opened from the main menu.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class CareerFlowBootstrapper : MonoBehaviour
    {
        [Header("Optional UI")]
        [SerializeField]
        private BandSelectionPopup _bandSelectionPopupPrefab;

        [SerializeField]
        private CampaignManagePopup _campaignManagePopupPrefab;

        [Header("Required References")]
        [SerializeField]
        private CareerFlowMenuController _careerFlowMenuController;

        [SerializeField]
        private CoverFlow.CoverFlowBackground _coverFlowBackground;

        [Header("Card Pool (exactly 7)")]
        [SerializeField]
        private CareerFlowCard[] _cardSlots = new CareerFlowCard[7];

        /// <summary>
        /// Whether the CareerFlow system is fully wired and available.
        /// </summary>
        public bool IsAvailable =>
            _careerFlowMenuController != null &&
            _cardSlots != null && _cardSlots.Length == 7 &&
            _cardSlots[0] != null;

        private bool _hasInitialized;

        private void Awake()
        {
            AutoWireReferences();
            LogDiagnostics();
        }

        private void OnEnable()
        {
            if (IsAvailable && gameObject.activeInHierarchy)
                StartCoroutine(InitializeWhenReady());
        }

        private void OnDisable()
        {
            CloseCareerFlow();
            _hasInitialized = false;
        }

        private IEnumerator InitializeWhenReady()
        {
            yield return null;

            if (!IsAvailable || _hasInitialized)
                yield break;

            CareerManager.Instance.LoadCareerData();
            var allCareers = CareerManager.Instance.GetCareers();
            if (allCareers == null || allCareers.Count == 0)
            {
                Debug.LogWarning("[CareerFlow] InitializeWhenReady — no careers available yet.");
                yield break;
            }

            _hasInitialized = true;
            OpenCareerFlow();
        }

        /// <summary>
        /// Attempts to find missing references from children/self.
        /// Safe to call even if references are already assigned.
        /// </summary>
        private void AutoWireReferences()
        {
            if (_careerFlowMenuController == null)
                _careerFlowMenuController = GetComponentInChildren<CareerFlowMenuController>(true);

            if (_coverFlowBackground == null)
                _coverFlowBackground = GetComponentInChildren<CoverFlow.CoverFlowBackground>(true);

            if (_cardSlots == null || _cardSlots.Length != 7 || _cardSlots[0] == null)
            {
                var found = GetComponentsInChildren<CareerFlowCard>(true);
                if (found.Length >= 7)
                {
                    _cardSlots = new CareerFlowCard[7];
                    for (int i = 0; i < 7 && i < found.Length; i++)
                        _cardSlots[i] = found[i];
                }
            }

            if (_careerFlowMenuController != null && _cardSlots != null && _cardSlots.Length == 7)
                _careerFlowMenuController.AssignCardSlots(_cardSlots);

            EnsureBandSelectionPopup();
            EnsureCampaignManagePopup();
        }

        private void EnsureBandSelectionPopup()
        {
            if (_careerFlowMenuController == null)
                return;

            var popup = GetComponentInChildren<BandSelectionPopup>(true);
            if (popup == null && _bandSelectionPopupPrefab != null)
            {
                popup = Instantiate(_bandSelectionPopupPrefab, transform);
                popup.gameObject.SetActive(false);
            }

            if (popup != null)
                _careerFlowMenuController.AssignBandSelectionPopup(popup);

            Debug.Log($"[CareerFlowBootstrapper] Band popup: {(popup != null ? popup.name : "NULL")}, " +
                $"prefabRef={(_bandSelectionPopupPrefab != null ? _bandSelectionPopupPrefab.name : "NULL")}");
        }

        private void EnsureCampaignManagePopup()
        {
            if (_careerFlowMenuController == null)
                return;

            var popup = GetComponentInChildren<CampaignManagePopup>(true);
            CampaignManagePopup prefab = _campaignManagePopupPrefab;

#if UNITY_EDITOR
            if (popup == null && prefab == null)
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/CareersM/Submenus/CampaignManagePopup.prefab");
                if (asset != null)
                    prefab = asset.GetComponent<CampaignManagePopup>();
            }
#endif

            if (popup == null && prefab != null)
            {
                popup = Instantiate(prefab, transform);
                popup.name = "CampaignManagePopup";
                popup.gameObject.SetActive(false);
            }

            if (popup != null)
                _careerFlowMenuController.AssignCampaignManagePopup(popup);

            Debug.Log($"[CareerFlowBootstrapper] Manage popup: {(popup != null ? popup.name : "NULL")}, " +
                $"prefabRef={(_campaignManagePopupPrefab != null ? _campaignManagePopupPrefab.name : "NULL")}");
        }

        private void LogDiagnostics()
        {
            Debug.Log($"[CareerFlowBootstrapper] DIAGNOSTICS for '{name}':\n" +
                $"  _careerFlowMenuController = {(_careerFlowMenuController != null ? _careerFlowMenuController.name : "NULL")}\n" +
                $"  _coverFlowBackground = {(_coverFlowBackground != null ? _coverFlowBackground.name : "NULL")}\n" +
                $"  _cardSlots array = {(_cardSlots != null ? $"length={_cardSlots.Length}" : "NULL")}\n" +
                $"  _cardSlots[0] = {(_cardSlots != null && _cardSlots.Length > 0 && _cardSlots[0] != null ? _cardSlots[0].name : "NULL")}\n" +
                $"  IsAvailable = {IsAvailable}");
        }

        /// <summary>
        /// Activates CareerFlow UI and loads the career carousel.
        /// </summary>
        public void OpenCareerFlow()
        {
            if (!ActivateCareerFlowUI())
                return;

            _careerFlowMenuController.LoadCareers();
        }

        /// <summary>
        /// Legacy entry point for callers that pass a pre-built career list.
        /// </summary>
        public void OpenCareerFlow(List<CareerInfo> careers)
        {
            if (!ActivateCareerFlowUI())
                return;

            _careerFlowMenuController.SetCareers(careers);
        }

        private bool ActivateCareerFlowUI()
        {
            if (!IsAvailable)
            {
                Debug.LogWarning("CareerFlowBootstrapper: Not fully wired — cannot open CareerFlow.");
                return false;
            }

            if (_coverFlowBackground != null)
            {
                var cfCanvas = CoverFlow.CoverFlowBackground.FindCoverFlowUICanvasInHierarchy(
                    transform, _coverFlowBackground);
                if (cfCanvas != null)
                    _coverFlowBackground.AttachToCoverFlowCanvas(cfCanvas.transform);

                _coverFlowBackground.gameObject.SetActive(true);
            }

            foreach (var card in _cardSlots)
            {
                if (card != null)
                    card.gameObject.SetActive(true);
            }

            _careerFlowMenuController.gameObject.SetActive(true);

            Transform cfParent = _careerFlowMenuController.transform.parent ?? transform;
            cfParent.SetAsLastSibling();

            _careerFlowMenuController.SetStaticBackground(new Color(0.06f, 0.06f, 0.1f));

            if (_coverFlowBackground != null)
                _coverFlowBackground.BeginVisibilityGuard();

            return true;
        }

        /// <summary>
        /// Cleanup when leaving the menu.
        /// </summary>
        public void CloseCareerFlow()
        {
            if (_careerFlowMenuController != null)
                _careerFlowMenuController.gameObject.SetActive(false);

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
