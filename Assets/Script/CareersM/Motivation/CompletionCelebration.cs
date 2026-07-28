using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Premium celebration effects for gig and campaign completions.
    /// Features gold glow pulses, sparkle bursts, and screen flash effects.
    /// Attaches to GigView at runtime.
    /// </summary>
    public class CompletionCelebration : MonoBehaviour
    {
        private static CompletionCelebration _instance;
        public static CompletionCelebration Instance => _instance;

        // Color constants
        private static readonly Color GoldColor = new Color(0.85f, 0.65f, 0.15f);
        private static readonly Color GreenColor = new Color(0.2f, 0.8f, 0.2f);

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        /// <summary>
        /// Brief green glow pulse for individual gig completion.
        /// </summary>
        public void PlayGigCompleteEffect(Transform targetTransform)
        {
            if (targetTransform == null) return;
            StartCoroutine(GlowEffect(targetTransform, GreenColor, 0.3f, 0.6f));
        }

        /// <summary>
        /// Elaborate gold celebration for full campaign completion.
        /// Multiple gold pulses + sparkle burst.
        /// </summary>
        public void PlayCampaignCompleteEffect(Transform targetTransform)
        {
            if (targetTransform == null) return;
            StartCoroutine(CampaignCelebrationRoutine(targetTransform));
        }

        private IEnumerator GlowEffect(Transform target, Color color, float maxAlpha, float duration)
        {
            var overlay = CreateOverlay(target, "CelebrationGlow");
            overlay.raycastTarget = false;
            overlay.color = new Color(color.r, color.g, color.b, 0f);

            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float alpha = Mathf.Sin(t * Mathf.PI) * maxAlpha;
                overlay.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            // Fade out
            elapsed = 0;
            while (elapsed < 0.2f)
            {
                elapsed += Time.unscaledDeltaTime;
                overlay.color = new Color(color.r, color.g, color.b,
                    Mathf.Lerp(0.05f, 0f, elapsed / 0.2f));
                yield return null;
            }

            Destroy(overlay.gameObject);
        }

        private IEnumerator CampaignCelebrationRoutine(Transform target)
        {
            // Three gold pulses with increasing intensity
            for (int i = 0; i < 3; i++)
            {
                float intensity = 0.25f + (i * 0.1f);
                float dur = 0.5f - (i * 0.05f);
                yield return GlowEffect(target, GoldColor, intensity, dur);
                yield return new WaitForSecondsRealtime(0.15f);
            }

            // Final golden flash
            var flash = CreateOverlay(target, "CampaignFinalFlash");
            flash.raycastTarget = false;
            flash.color = new Color(0.85f, 0.65f, 0.15f, 0.5f);

            float e = 0;
            while (e < 0.4f)
            {
                e += Time.unscaledDeltaTime;
                flash.color = new Color(0.85f, 0.65f, 0.15f,
                    Mathf.Lerp(0.5f, 0f, e / 0.4f));
                yield return null;
            }
            Destroy(flash.gameObject);
        }

        /// <summary>
        /// Creates a full-screen overlay Image under the target transform.
        /// </summary>
        private static Image CreateOverlay(Transform target, string name)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(target);
            obj.transform.SetAsLastSibling();

            var rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return obj.AddComponent<Image>();
        }
    }
}