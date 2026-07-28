using System.Collections;
using TMPro;
using UnityEngine;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Animated gold shine effect for TextMeshProUGUI components.
    /// Creates a sweeping highlight that moves across the text periodically,
    /// making completed campaigns feel AMAZING and accomplished.
    ///
    /// Features:
    /// - Per-character vertex color manipulation for smooth gradient
    /// - Configurable shine speed, width, and interval
    /// - Vertex-color-based pulse/breathing effect (no transform.localScale — layout-safe)
    /// - Immediate trigger capability for event-driven shines
    /// </summary>
    public class GoldShineTextEffect : MonoBehaviour
    {
        private TextMeshProUGUI _textComponent;

        [Header("Shine Settings")]
        [SerializeField]
        private Color _baseGoldColor = new Color(1.0f, 0.84f, 0.0f);      // #FFD700
        [SerializeField]
        private Color _shineGoldColor = new Color(1.0f, 0.98f, 0.7f);     // Bright shine
        [SerializeField]
        private float _shineDuration = 1.2f;                               // Sweep duration
        [SerializeField]
        private float _shineInterval = 3.5f;                               // Time between shines
        [SerializeField]
        [Range(0.05f, 0.8f)]
        private float _shineWidth = 0.3f;                                  // Width of shine band
        [SerializeField]
        private AnimationCurve _shineIntensityCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Pulse Settings")]
        [SerializeField]
        [Range(0.01f, 0.25f)]
        private float _pulseAmount = 0.06f;                                // Brightness oscillation (±)
        [SerializeField]
        [Range(0.5f, 4f)]
        private float _pulseSpeed = 1.5f;                                  // Oscillation frequency

        // Public properties for runtime configuration
        public float ShineInterval { get => _shineInterval; set => _shineInterval = value; }
        public float ShineDuration { get => _shineDuration; set => _shineDuration = value; }

        private Coroutine _shineCoroutine;
        private Coroutine _pulseCoroutine;
        private bool _isShining = false;

        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
            if (_textComponent == null)
            {
                Debug.LogError("GoldShineTextEffect requires TextMeshProUGUI component!");
                enabled = false;
                return;
            }

            _textComponent.color = _baseGoldColor;
        }

        private void OnEnable()
        {
            if (_shineCoroutine != null)
                StopCoroutine(_shineCoroutine);
            _shineCoroutine = StartCoroutine(ShineLoop());

            if (_pulseCoroutine != null)
                StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(PulseLoop());
        }

        private void OnDisable()
        {
            if (_shineCoroutine != null)
            {
                StopCoroutine(_shineCoroutine);
                _shineCoroutine = null;
            }

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }
        }

        /// <summary>
        /// Subtle breathing/pulse effect using vertex color modulation.
        /// Operates on _textComponent.color (face tint) — NOT transform.localScale.
        /// This ensures ZERO impact on layout groups or RectTransform bounds.
        /// During an active shine sweep, the pulse naturally pauses because
        /// ShineOnce() takes over _textComponent.color, and the vertex colors
        /// written by ApplyShineGradient override the face tint per-character.
        /// </summary>
        private IEnumerator PulseLoop()
        {
            while (true)
            {
                if (_textComponent != null && !_isShining)
                {
                    // Modulate face color brightness — multiplies against vertex colors
                    float brightness = 1f + Mathf.Sin(Time.unscaledTime * _pulseSpeed) * _pulseAmount;
                    Color pulsed = _baseGoldColor * brightness;
                    pulsed.a = _baseGoldColor.a;
                    _textComponent.color = pulsed;
                }
                yield return null;
            }
        }

        /// <summary>
        /// Main shine animation loop - repeats forever with intervals.
        /// </summary>
        private IEnumerator ShineLoop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(_shineInterval);
                yield return ShineOnce();
            }
        }

        /// <summary>
        /// Performs a single shine sweep across the text.
        /// </summary>
        private IEnumerator ShineOnce()
        {
            _isShining = true;
            float elapsed = 0f;

            while (elapsed < _shineDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / _shineDuration;

                // Shine position moves from left to right
                float shinePosition = Mathf.Lerp(-_shineWidth, 1f + _shineWidth, t);

                // Update vertex colors with gradient effect
                ApplyShineGradient(shinePosition);

                yield return null;
            }

            // Reset to base color
            if (_textComponent != null)
                _textComponent.color = _baseGoldColor;
            _isShining = false;
        }

        /// <summary>
        /// Applies gradient shine effect across text using vertex color manipulation.
        /// Each character's color is interpolated based on distance from the shine center.
        /// </summary>
        private void ApplyShineGradient(float shineCenter)
        {
            if (_textComponent == null) return;

            // Force text mesh update to get current character info
            _textComponent.ForceMeshUpdate();

            var textInfo = _textComponent.textInfo;
            if (textInfo == null || textInfo.characterCount == 0)
                return;

            // Get text bounds for normalization
            Bounds bounds = _textComponent.bounds;
            float textWidth = bounds.size.x;
            if (textWidth <= 0) return;
            float textLeft = bounds.min.x;

            // Update vertex colors for each visible character
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                    continue;

                var charInfo = textInfo.characterInfo[i];
                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                // Get normalized character center position (0-1)
                float charCenter = (charInfo.origin - textLeft) / textWidth;

                // Calculate distance from shine center
                float distanceFromShine = Mathf.Abs(charCenter - shineCenter);

                // Calculate shine influence (0 = no shine, 1 = full shine)
                float shineInfluence = 1f - Mathf.Clamp01(distanceFromShine / _shineWidth);
                shineInfluence = _shineIntensityCurve.Evaluate(shineInfluence);

                // Interpolate between base and shine color
                Color charColor = Color.Lerp(_baseGoldColor, _shineGoldColor, shineInfluence);

                // Apply to all 4 vertices of the character quad
                Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
                colors[vertexIndex + 0] = charColor;
                colors[vertexIndex + 1] = charColor;
                colors[vertexIndex + 2] = charColor;
                colors[vertexIndex + 3] = charColor;
            }

            // Update the mesh with new vertex colors
            _textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        /// <summary>
        /// Manually trigger a shine sweep animation.
        /// Useful for event-driven shines (e.g., on campaign selection).
        /// </summary>
        public void TriggerShine()
        {
            if (!_isShining && gameObject.activeInHierarchy && enabled)
            {
                StartCoroutine(ShineOnce());
            }
        }

        /// <summary>
        /// Override the base gold color at runtime.
        /// </summary>
        public void SetBaseColor(Color color)
        {
            _baseGoldColor = color;
            if (_textComponent != null && !_isShining)
                _textComponent.color = _baseGoldColor;
        }

        /// <summary>
        /// Override the shine color at runtime.
        /// </summary>
        public void SetShineColor(Color color)
        {
            _shineGoldColor = color;
        }
    }
}
