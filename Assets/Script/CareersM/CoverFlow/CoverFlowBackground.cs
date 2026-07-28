using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Full-bleed background layer for the CoverFlow carousel.
    /// Displays a campaign-specific background image or a fallback solid colour.
    /// Can also be toggled into CareerCareerMenu Mode for specialized static menus.
    /// </summary>
    public class CoverFlowBackground : MonoBehaviour
    {
        [Header("Mode Configuration")]
        [SerializeField]
        [Tooltip("If true, this background adapts layout behaviors and precision scaling specific to the CareerCareerMenu prefab setup.")]
        private bool _isCareerCareerMenuMode = false;

        [Header("Required References")]
        [SerializeField]
        private Image _backgroundImage;

        [Header("Fallback")]
        [SerializeField]
        [Tooltip("Global fallback sprite used when a campaign has no background image (imagebg=false or bgimg missing).")]
        private Sprite _fallbackBackgroundSprite;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        private CoverFlowConfig _config;
        private string _bundlePath;
        private bool _hasPendingConfig;
        private bool _isStandaloneCanvas;
        private Transform _attachedCanvasTransform;
        private Coroutine _visibilityGuardRoutine;

        private void Awake()
        {
            EnsureReferences();
            EnsureCanvasComponents();
        }

        private void OnEnable()
        {
            EnsureReferences();
            EnsureCanvasComponents();

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;

            if (_backgroundImage != null)
                _backgroundImage.enabled = true;

            // Re-apply the last config so re-activation always repaints correctly,
            // regardless of the order in which we were activated vs. configured.
            if (_hasPendingConfig && !_isCareerCareerMenuMode)
                RepaintBackground();
            else if (_isCareerCareerMenuMode)
                ApplyCareerMenuPresentation();

            ForceGraphicRebuild();
        }

        private void OnDisable()
        {
            if (_visibilityGuardRoutine != null)
            {
                StopCoroutine(_visibilityGuardRoutine);
                _visibilityGuardRoutine = null;
            }
        }

        /// <summary>
        /// Locates the CoverFlowUI world-space canvas in a menu hierarchy, skipping
        /// any Canvas that lives on the background object itself.
        /// </summary>
        public static Canvas FindCoverFlowUICanvasInHierarchy(Transform root, CoverFlowBackground excludeBackground)
        {
            if (root == null)
                return null;

            var canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (var canvas in canvases)
            {
                if (excludeBackground != null && canvas.gameObject == excludeBackground.gameObject)
                    continue;

                if (canvas.gameObject.name == "CoverFlowUI")
                    return canvas;
            }

            foreach (var canvas in canvases)
            {
                if (excludeBackground != null && canvas.gameObject == excludeBackground.gameObject)
                    continue;

                return canvas;
            }

            return null;
        }

        /// <summary>
        /// Parents this background under the CoverFlowUI world-space canvas and applies
        /// the stretch-fill layout shared by Career and Gig menus.
        /// </summary>
        public void AttachToCoverFlowCanvas(Transform canvasTransform)
        {
            if (canvasTransform == null || canvasTransform == transform)
                return;

            _attachedCanvasTransform = canvasTransform;
            transform.SetParent(canvasTransform, false);
            transform.SetAsFirstSibling();

            // Must be immediate: a deferred Destroy leaves a zombie Canvas for the rest
            // of the frame. Graphics bind to that canvas, then never rebuild against
            // CoverFlowUI after it dies — blank background until the next OnEnable.
            RemoveRedundantLocalCanvas();
            EnsureWorldCamera(canvasTransform);
            ApplyCanvasChildLayout();
            ForceGraphicRebuild();
        }

        /// <summary>
        /// Starts a short post-attach check: if the background still looks wrong after
        /// a frame, re-apply layout/paint and stop once healthy.
        /// </summary>
        public void BeginVisibilityGuard()
        {
            if (!isActiveAndEnabled)
                return;

            if (_visibilityGuardRoutine != null)
                StopCoroutine(_visibilityGuardRoutine);

            _visibilityGuardRoutine = StartCoroutine(VisibilityGuardRoutine());
        }

        private IEnumerator VisibilityGuardRoutine()
        {
            // Let parenting / Canvas rebuild settle one frame.
            yield return null;

            for (int attempt = 0; attempt < 3; attempt++)
            {
                if (IsBackgroundLookingHealthy())
                {
                    Debug.Log($"[CoverFlowBackground] Visibility guard OK on '{name}' (attempt {attempt}).");
                    break;
                }

                Debug.LogWarning(
                    $"[CoverFlowBackground] Visibility guard heal attempt {attempt + 1} on '{name}'. " +
                    DescribeHealthState());

                EnsureReferences();

                if (!IsUnderExternalCanvas() && _attachedCanvasTransform != null)
                    AttachToCoverFlowCanvas(_attachedCanvasTransform);
                else
                    ApplyLayoutForCurrentParent();

                if (_hasPendingConfig && !_isCareerCareerMenuMode)
                    RepaintBackground();
                else if (_isCareerCareerMenuMode)
                    ApplyCareerMenuPresentation();

                ForceGraphicRebuild();
                yield return null;
            }

            _visibilityGuardRoutine = null;
        }

        private bool IsBackgroundLookingHealthy()
        {
            if (!isActiveAndEnabled || _backgroundImage == null || !_backgroundImage.enabled)
                return false;

            if (_canvasGroup != null && _canvasGroup.alpha < 0.01f)
                return false;

            // Must be under CoverFlowUI (or have our own canvas as last resort).
            if (!IsUnderExternalCanvas() && GetComponent<Canvas>() == null)
                return false;

            var rt = GetComponent<RectTransform>();
            if (rt == null)
                return false;

            // Under CoverFlowUI the local scale must be ~1 (stretch-fill). The old
            // root-level world scale (~0.004–0.006) under CoverFlowUI makes the
            // background microscopic / invisible.
            if (IsUnderExternalCanvas() && rt.localScale.x < 0.5f)
                return false;

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float width = Vector3.Distance(corners[0], corners[3]);
            float height = Vector3.Distance(corners[0], corners[1]);
            if (width < 0.05f || height < 0.05f)
                return false;

            // Prefer a sprite; solid colour with alpha is also acceptable.
            if (_backgroundImage.sprite == null && _backgroundImage.color.a < 0.01f)
                return false;

            return true;
        }

        private string DescribeHealthState()
        {
            var rt = GetComponent<RectTransform>();
            return
                $"active={isActiveAndEnabled}, " +
                $"img={(_backgroundImage != null)}, imgEnabled={(_backgroundImage != null && _backgroundImage.enabled)}, " +
                $"sprite={(_backgroundImage != null && _backgroundImage.sprite != null)}, " +
                $"cgAlpha={(_canvasGroup != null ? _canvasGroup.alpha : -1f)}, " +
                $"underCanvas={IsUnderExternalCanvas()}, " +
                $"localScale={(rt != null ? rt.localScale.ToString("F4") : "null")}, " +
                $"parent={(transform.parent != null ? transform.parent.name : "null")}";
        }

        /// <summary>
        /// Re-acquires the required component references from this GameObject if they
        /// are missing. Serialized references are preferred, but this guards against
        /// them being cleared by prefab/scene overrides.
        /// </summary>
        private void EnsureReferences()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();
        }

        private bool IsUnderExternalCanvas()
        {
            var parentCanvas = GetComponentInParent<Canvas>();
            return parentCanvas != null && parentCanvas.gameObject != gameObject;
        }

        /// <summary>
        /// Ensures CanvasRenderer exists. Prefer CoverFlowUI in the menu hierarchy over
        /// spawning a temporary local Canvas — that local Canvas was the first-open blank
        /// bug (added on enable, destroyed deferred on attach, graphics never rebound).
        /// </summary>
        private void EnsureCanvasComponents()
        {
            var canvasRenderer = GetComponent<CanvasRenderer>();
            if (canvasRenderer == null)
            {
                Debug.LogWarning($"[CoverFlowBackground] '{name}' is missing CanvasRenderer — adding one at runtime.");
                gameObject.AddComponent<CanvasRenderer>();
            }

            if (IsUnderExternalCanvas())
            {
                RemoveRedundantLocalCanvas();
                ApplyCanvasChildLayout();
                return;
            }

            // CoverFlowUI is a sibling under the menu root — wait for the bootstrapper
            // to attach us. Do NOT add a local Canvas here.
            Transform searchRoot = transform.parent != null ? transform.parent : transform;
            if (FindCoverFlowUICanvasInHierarchy(searchRoot, this) != null)
            {
                _isStandaloneCanvas = false;
                return;
            }

            if (_isCareerCareerMenuMode)
                return;

            Debug.LogWarning($"[CoverFlowBackground] '{name}' is not under an external Canvas and no CoverFlowUI was found — adding local Canvas at runtime.");
            _isStandaloneCanvas = true;

            var canvas = GetComponent<Canvas>();
            if (canvas == null)
                canvas = gameObject.AddComponent<Canvas>();

            canvas.overrideSorting = true;
            canvas.sortingOrder = -10;
            canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
                | AdditionalCanvasShaderChannels.Normal
                | AdditionalCanvasShaderChannels.Tangent;

            ApplyLayoutForCurrentParent();
        }

        private void RemoveRedundantLocalCanvas()
        {
            var localCanvas = GetComponent<Canvas>();
            if (localCanvas == null)
                return;

            // DestroyImmediate is intentional — see AttachToCoverFlowCanvas.
            DestroyImmediate(localCanvas);
            _isStandaloneCanvas = false;
        }

        private static void EnsureWorldCamera(Transform canvasTransform)
        {
            var canvas = canvasTransform.GetComponent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                return;

            if (canvas.worldCamera != null)
                return;

            Transform menuRoot = canvasTransform.parent != null ? canvasTransform.parent : canvasTransform;
            var cameras = menuRoot.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                if (cam.gameObject.name.IndexOf("RenderCamera", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    canvas.worldCamera = cam;
                    return;
                }
            }
        }

        private void ForceGraphicRebuild()
        {
            if (_backgroundImage != null)
            {
                _backgroundImage.SetAllDirty();
                if (!_backgroundImage.enabled)
                    _backgroundImage.enabled = true;
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Stretch-fills the CoverFlowUI canvas (1920×1080 world-space coords).
        /// Used when parented under CoverFlowUI — localScale must be 1, not 0.004.
        /// </summary>
        public void ApplyCanvasChildLayout()
        {
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = new Vector3(0f, 0f, 4f);
        }

        private void ApplyLayoutForCurrentParent()
        {
            if (IsUnderExternalCanvas())
                ApplyCanvasChildLayout();
            else
                EnforceAbsoluteSizing();
        }

        private void ApplyCareerMenuPresentation()
        {
            ApplyLayoutForCurrentParent();

            if (_backgroundImage == null)
                return;

            _backgroundImage.enabled = true;

            // Prefer the dedicated fallback sprite when the Image lost its baked sprite.
            if (_backgroundImage.sprite == null && _fallbackBackgroundSprite != null)
            {
                _backgroundImage.sprite = _fallbackBackgroundSprite;
                _backgroundImage.color = Color.white;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Forces the RectTransform properties to lock to full HD space
        /// instead of collapsing to 0x0 inside non-UI parents, applying the context-appropriate scale.
        /// Only valid when this object is a direct child of the menu root (not under CoverFlowUI).
        /// </summary>
        public void EnforceAbsoluteSizing()
        {
            if (IsUnderExternalCanvas())
            {
                ApplyCanvasChildLayout();
                return;
            }

            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(1920f, 1080f);
            rectTransform.anchoredPosition = Vector2.zero;

            if (_isCareerCareerMenuMode)
                rectTransform.localScale = new Vector3(0.00400000019f, 0.00400000019f, 0.00400000019f);
            else
                rectTransform.localScale = new Vector3(0.0062f, 0.0062f, 0.0062f);
        }

        /// <summary>
        /// Applies the background configuration for the current campaign.
        /// In CareerCareerMenu Mode this only refreshes layout/visibility (sprite stays baked).
        /// </summary>
        public void ApplyConfig(CoverFlowConfig config, string bundlePath)
        {
            EnsureReferences();

            if (_isCareerCareerMenuMode)
            {
                ApplyCareerMenuPresentation();
                ForceGraphicRebuild();
                return;
            }

            _config = config;
            _bundlePath = bundlePath;
            _hasPendingConfig = true;

            RepaintBackground();
            ForceGraphicRebuild();
        }

        /// <summary>
        /// Applies the currently stored <see cref="_config"/> to the background image.
        /// Safe to call repeatedly (e.g. from <see cref="OnEnable"/>).
        /// </summary>
        private void RepaintBackground()
        {
            if (_backgroundImage == null)
            {
                Debug.LogError("[CoverFlowBackground] _backgroundImage is NULL — cannot repaint.");
                return;
            }

            ApplyLayoutForCurrentParent();
            _backgroundImage.enabled = true;

            if (_config == null)
            {
                ApplyFallbackBackground(null);
                return;
            }

            if (_config.UseImageBackground && !string.IsNullOrEmpty(_config.BackgroundImageName))
            {
                string bgPath = System.IO.Path.Combine(_bundlePath ?? string.Empty, _config.BackgroundImageName);
                var texture = LoadTextureFromFile(bgPath);

                if (texture != null)
                {
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));

                    _backgroundImage.sprite = sprite;
                    _backgroundImage.color = Color.white;
                    _backgroundImage.enabled = true;
                    return;
                }

                Debug.LogWarning($"[CoverFlowBackground] Failed to load bg image at '{bgPath}' — using fallback.");
            }

            ApplyFallbackBackground(_config);
            _backgroundImage.enabled = true;
        }

        private void ApplyFallbackBackground(CoverFlowConfig config)
        {
            if (_fallbackBackgroundSprite != null)
            {
                _backgroundImage.sprite = _fallbackBackgroundSprite;
                _backgroundImage.color = Color.white;
            }
            else
            {
                _backgroundImage.sprite = null;
                _backgroundImage.color = config?.FallbackColor ?? new Color(0.06f, 0.06f, 0.1f);
            }
        }

        public void SetOpacity(float alpha)
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = alpha;
        }

        private static Texture2D LoadTextureFromFile(string path)
        {
            if (!System.IO.File.Exists(path))
                return null;

            try
            {
                byte[] data = System.IO.File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2);
                if (texture.LoadImage(data))
                    return texture;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"CoverFlowBackground: Failed to load background image '{path}': {ex.Message}");
            }

            return null;
        }
    }
}
