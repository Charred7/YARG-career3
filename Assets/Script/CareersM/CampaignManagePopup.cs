using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using YARG.Career;
using YARG.Core.Input;
using YARG.Helpers;
using YARG.Helpers.Extensions;
using YARG.Menu.MusicLibrary;
using YARG.Menu.Navigation;
using YARG.Menu.Persistent;

namespace YARG.Menu.Career
{
    /// <summary>
    /// CareerFlow overlay for toggling campaign visibility (per-band hide list).
    /// List is campaigns-only; Install / Open Folder / Show All are fret actions
    /// so they stay reachable with long career lists.
    /// </summary>
    public class CampaignManagePopup : MonoBehaviour
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

        public UnityEvent OnVisibilityChanged;

        private int _restoreIndex;

        private void Awake()
        {
            EnsureOverlayCanvas();
        }

        /// <summary>
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
                }),
                new NavigationScheme.Entry(MenuAction.Orange, "Install Bundle", InstallCareerBundle),
                new NavigationScheme.Entry(MenuAction.Blue, "Open Folder", OpenBundlesFolder),
                new NavigationScheme.Entry(MenuAction.Yellow, "Show All", ShowAllCampaigns),
            }, false));

            _restoreIndex = 0;
            UpdateCampaignList();
        }

        private void OnDisable()
        {
            Navigator.Instance.PopScheme();
        }

        private void UpdateCampaignList()
        {
            _navGroup.ClearNavigatables();
            _container.DestroyChildren();

            SetHeader("MANAGE CAMPAIGNS");

            var careers = CareerManager.Instance?.GetCareers();
            if (careers == null || careers.Count == 0)
            {
                CreateItem("(NO CAMPAIGNS — INSTALL OR OPEN FOLDER)", () => { });
                _navGroup.SelectFirst();
                return;
            }

            var sorted = new List<CareerInfo>(careers);
            sorted.Sort((a, b) =>
                string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            for (int i = 0; i < sorted.Count; i++)
            {
                var career = sorted[i];
                string careerId = career.id;
                bool hidden = BandContainer.IsCareerHidden(careerId);
                string label = FormatRowLabel(career.name, hidden);

                CreateItem(label, () =>
                {
                    _restoreIndex = _navGroup.SelectedIndex ?? 0;
                    BandContainer.ToggleCampaignHidden(careerId);
                    OnVisibilityChanged?.Invoke();
                    UpdateCampaignList();
                });
            }

            int maxIndex = _navGroup.Count > 0 ? _navGroup.Count - 1 : 0;
            int selectIndex = Mathf.Clamp(_restoreIndex, 0, maxIndex);
            _navGroup.SelectAt(selectIndex);
        }

        private void InstallCareerBundle()
        {
            string startingDir = PathHelper.PersistentDataPath;
            if (string.IsNullOrEmpty(startingDir) || !Directory.Exists(startingDir))
                startingDir = null;

            FileExplorerHelper.OpenChooseFolder(startingDir, path =>
            {
                string runtimeId = CareerManager.Instance.InstallCareerBundle(path);
                if (string.IsNullOrEmpty(runtimeId))
                {
                    DialogManager.Instance.ShowMessage(
                        "Install Failed",
                        "Could not install that folder as a career bundle.\n" +
                        "Make sure it contains a careerdef.ini file.");
                    return;
                }

                _restoreIndex = 0;
                UpdateCampaignList();
                OnVisibilityChanged?.Invoke();

                DialogManager.Instance.ShowMessage(
                    "Bundle Installed",
                    $"Installed career bundle:\n{runtimeId}");
            });
        }

        private void OpenBundlesFolder()
        {
            string bundlesDir = CareerBundleManager.BundlesDirectory;
            if (string.IsNullOrEmpty(bundlesDir))
            {
                bundlesDir = Path.Combine(PathHelper.PersistentDataPath, "career", "bundles");
            }

            Directory.CreateDirectory(bundlesDir);
            FileExplorerHelper.OpenFolder(bundlesDir);
        }

        private void ShowAllCampaigns()
        {
            _restoreIndex = _navGroup.SelectedIndex ?? 0;
            BandContainer.UnhideAllCampaigns();
            OnVisibilityChanged?.Invoke();
            UpdateCampaignList();
        }

        private static string FormatRowLabel(string careerName, bool hidden)
        {
            string state = hidden ? "OFF" : "ON";
            string name = string.IsNullOrEmpty(careerName)
                ? "(UNNAMED)"
                : careerName.ToUpperInvariant();
            return $"[{state}]  {name}";
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
