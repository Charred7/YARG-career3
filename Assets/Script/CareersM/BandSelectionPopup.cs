using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using YARG.Career;
using YARG.Core.Input;
using YARG.Helpers.Extensions;
using YARG.Menu.Navigation;
using YARG.Menu.MusicLibrary;
using YARG.Menu.Persistent;
using YARG.Menu.Data;

namespace YARG.Menu.Career
{
    public class BandSelectionPopup : MonoBehaviour
    {
        [SerializeField]
        private PopupMenuItem _menuItemPrefab;

        [Space]
        [SerializeField]
        private GameObject _header;
        [SerializeField]
        private TextMeshProUGUI _headerText;
        [SerializeField]
        private Transform _container;
        [SerializeField]
        private NavigationGroup _navGroup;

        public UnityEvent OnBandChanged;

        private void Awake()
        {
            EnsureOverlayCanvas();
        }

        /// <summary>
        /// BandSelectionPopup expects to live under a screen-space canvas (as on CareersView).
        /// CareerFlow has no parent canvas, so ensure this popup can render on its own.
        /// </summary>
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
        }

        private void OnEnable()
        {
            Navigator.Instance.PushScheme(new NavigationScheme(new()
            {
                NavigationScheme.Entry.NavigateUp,
                NavigationScheme.Entry.NavigateDown,
                NavigationScheme.Entry.NavigateSelect,
                new NavigationScheme.Entry(MenuAction.Red, "Menu.Common.Back", () =>
                {
                    gameObject.SetActive(false);
                })
            }, false));

            UpdateBandList();
        }

        private void OnDisable()
        {
            Navigator.Instance.PopScheme();
        }

        private void UpdateBandList()
        {
            // Reset content
            _navGroup.ClearNavigatables();
            _container.DestroyChildren();

            SetHeader("SELECT BAND");

            // Get all bands
            var bands = BandContainer.AllBands;
            var currentBandIndex = BandContainer.CurrentBandIndex;

            // Create menu item for each band
            for (int i = 0; i < bands.Count; i++)
            {
                int bandIndex = i; // Capture for closure
                var band = bands[i];
                
                string displayName = band.BandName.ToUpper();
                if (bandIndex == currentBandIndex)
                {
                    displayName = $"> {displayName} <";
                }

                CreateItem(displayName, () =>
                {
                    if (bandIndex != currentBandIndex)
                    {
                        BandContainer.SwitchToBand(bandIndex);
                        OnBandChanged?.Invoke();
                    }
                    gameObject.SetActive(false);
                });
            }

            // Add separator line (empty disabled item for visual separation)
            var separator = Instantiate(_menuItemPrefab, _container);
            separator.Initialize("────────────────", null);
            separator.Button.enabled = false;
            _navGroup.AddNavigatable(separator.Button);

            // Add "Create New Band..." option
            CreateItem("CREATE NEW BAND...", ShowCreateBandDialog);

            _navGroup.SelectFirst();
        }

        private void ShowCreateBandDialog()
        {
            // CRITICAL: Close the popup FIRST to ensure proper layering
            gameObject.SetActive(false);
            
            // Wait one frame to ensure the popup is fully closed before showing dialog
            UnityEngine.MonoBehaviour runner = FindObjectOfType<DialogManager>();
            if (runner != null)
            {
                runner.StartCoroutine(ShowDialogNextFrame());
            }
            else
            {
                // Fallback if we can't find DialogManager
                ShowDialogImmediate();
            }
        }

        private System.Collections.IEnumerator ShowDialogNextFrame()
        {
            yield return null; // Wait one frame
            ShowDialogImmediate();
        }

        private void ShowDialogImmediate()
        {
            DialogManager.Instance.ShowRenameDialog("CREATE NEW BAND", (newBandName) =>
            {
                if (!string.IsNullOrWhiteSpace(newBandName))
                {
                    BandContainer.CreateBand(newBandName.Trim());
                    
                    // Switch to the newly created band
                    int newBandIndex = BandContainer.AllBands.Count - 1;
                    BandContainer.SwitchToBand(newBandIndex);
                    
                    OnBandChanged?.Invoke();
                }
            });
        }

        private void SetHeader(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                _header.SetActive(false);
            }
            else
            {
                _header.SetActive(true);
                _headerText.text = text;
            }
        }

        private void CreateItem(string body, UnityAction action)
        {
            var item = Instantiate(_menuItemPrefab, _container);
            item.Initialize(body, action);
            _navGroup.AddNavigatable(item.Button);
        }
    }
}