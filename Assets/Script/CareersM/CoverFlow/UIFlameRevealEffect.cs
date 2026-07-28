using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// AAA UI Flame Burst — pure DOTween/UI implementation.
    /// Spawns procedural Image-based flame tongues directly inside the card's
    /// RectTransform hierarchy, guaranteeing render order without any camera,
    /// sorting layer, or particle system dependencies.
    /// </summary>
    public class UIFlameRevealEffect : MonoBehaviour
    {
        public enum RevealMode
        {
            ModeA_RadialBurst,
            ModeB_CombustionBloom
        }

        [Header("Animation Mode")]
        [SerializeField] 
        private RevealMode _revealMode = RevealMode.ModeB_CombustionBloom;

        [Header("Mode A: Legacy Flame Burst Settings")]
        [SerializeField] private Sprite _flameSprite;
        [SerializeField] [Range(6, 24)] private int _flameCount = 14;
        [SerializeField] [Range(50f, 400f)] private float _burstRadius = 220f;
        [SerializeField] [Range(0.1f, 0.6f)] private float _burstDuration = 0.28f;
        [SerializeField] [Range(0.05f, 0.4f)] private float _fadeDuration = 0.22f;
        [SerializeField] [Range(20f, 200f)] private float _flameWidth = 60f;
        [SerializeField] [Range(40f, 300f)] private float _flameHeight = 140f;
        [SerializeField] private Sprite _centralFlashSprite;
        [SerializeField] [Range(100f, 600f)] private float _centralFlashSize = 300f;

        [Header("Mode B: Combustion Bloom Settings")]
        [SerializeField] [Tooltip("Assign 'flame - Copy.png' here. Backdrop fireball volume.")]
        private Sprite _bloomSprite;

        [SerializeField] [Tooltip("Assign '02.png' here. Sharp vertical exhaust core.")]
        private Sprite _exhaustSprite;

        [SerializeField] [Range(0.05f, 0.5f)] [Tooltip("How quickly the flame reaches peak expansion.")]
        private float _combustionImpactDuration = 0.14f;

        [SerializeField] [Range(0.1f, 1.0f)] [Tooltip("How long individual embers linger while dissipating.")]
        private float _combustionFadeDuration = 0.45f;

        [SerializeField] [Range(1.0f, 5.0f)] [Tooltip("Scale multiplier for the wide fireball backdrop.")]
        private float _bloomScale = 3.2f;

        [SerializeField] [Range(1.0f, 6.0f)] [Tooltip("Vertical stretch multiplier for the exhaust plume spike.")]
        private float _exhaustStretchY = 4.5f;

        [Header("Mode B: Fine-Tuning Particle Chaos")]
        [SerializeField] [Range(10, 60)] [Tooltip("Total number of sub-sprites spawned per trigger.")]
        private int _particleCount = 40;

        [SerializeField] [Range(10f, 200f)] [Tooltip("Horizontal spawn footprint width at the base of the card.")]
        private float _spawnWidthOffset = 80f;

        [SerializeField] [Tooltip("Minimum upward vertical launch distance.")]
        private float _minVelocityY = 350f;

        [SerializeField] [Tooltip("Maximum upward vertical launch distance.")]
        private float _maxVelocityY = 600f;

        [Header("Colour Gradient (Shared)")]
        [SerializeField] private Color _coreColor = new Color(1.0f, 0.98f, 0.78f, 1.0f);
        [SerializeField] private Color _tipColor = new Color(1.0f, 0.23f, 0.0f, 0.0f);

        [Header("Debug")]
        [SerializeField] private bool _debugMode = false;

        private RectTransform _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_rectTransform == null)
                _rectTransform = gameObject.AddComponent<RectTransform>();
        }

        public void PlayBurst()
        {
            if (_debugMode)
            {
                SpawnDebugFlash();
                return;
            }

            if (_revealMode == RevealMode.ModeA_RadialBurst)
                StartCoroutine(SpawnBurst());
            else
                StartCoroutine(SpawnCombustionBloom());
        }

        private void SpawnDebugFlash()
        {
            var cardImage = GetComponentInParent<Image>();
            if (cardImage == null)
            {
                RectTransform parent = _rectTransform.parent as RectTransform ?? _rectTransform;
                var go = new GameObject("DebugFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(parent, false);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                var img = go.GetComponent<Image>();
                img.color = new Color(1f, 0.5f, 0f, 0.8f);
                img.raycastTarget = false;

                var seq = DOTween.Sequence().SetLink(go);
                seq.AppendInterval(0.1f);                                   
                seq.Append(img.DOFade(0f, 0.3f).SetEase(Ease.InQuad));     
                seq.OnComplete(() => { if (go != null) Destroy(go); });
            }
        }

        private IEnumerator SpawnBurst()
        {
            RectTransform parent = _rectTransform.parent as RectTransform ?? _rectTransform;
            float angleStep = 360f / _flameCount;

            for (int i = 0; i < _flameCount; i++)
            {
                float angle = i * angleStep + Random.Range(-8f, 8f);
                float radiusVariance = Random.Range(0.75f, 1.25f);
                float sizeVariance = Random.Range(0.7f, 1.3f);
                float durationVariance = Random.Range(0.85f, 1.15f);

                SpawnFlameTongue(parent, angle, _burstRadius * radiusVariance, sizeVariance, durationVariance);
            }

            SpawnCentralFlash(parent);
            yield return null;
        }

        private void SpawnFlameTongue(RectTransform parent, float angleDeg, float radius, float sizeScale, float durationScale)
        {
            var go = new GameObject("FlameTongue", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0f);   
            rt.sizeDelta = new Vector2(_flameWidth * sizeScale, _flameHeight * sizeScale);
            rt.anchoredPosition = Vector2.zero;
            rt.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            rt.localScale = Vector3.one * 0.1f;

            var img = go.GetComponent<Image>();
            img.sprite = _flameSprite;
            img.color = _coreColor;
            img.raycastTarget = false;

            float burstTime = _burstDuration * durationScale;
            float fadeTime = _fadeDuration * durationScale;

            float rad = angleDeg * Mathf.Deg2Rad;
            Vector2 targetPos = new Vector2(Mathf.Sin(rad) * radius, Mathf.Cos(rad) * radius);

            var seq = DOTween.Sequence().SetLink(go);
            seq.Append(rt.DOAnchorPos(targetPos, burstTime).SetEase(Ease.OutCubic));
            seq.Join(rt.DOScale(Vector3.one, burstTime).SetEase(Ease.OutBack));
            seq.Join(img.DOColor(_tipColor, burstTime).SetEase(Ease.InQuad));
            seq.Append(img.DOFade(0f, fadeTime).SetEase(Ease.InQuad));
            seq.OnComplete(() => { if (go != null) Destroy(go); });
        }

        private void SpawnCentralFlash(RectTransform parent)
        {
            var go = new GameObject("FlameFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(_centralFlashSize, _centralFlashSize);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.zero;

            var img = go.GetComponent<Image>();
            img.sprite = _centralFlashSprite;
            img.color = new Color(1f, 1f, 0.8f, 0.9f);
            img.raycastTarget = false;

            var seq = DOTween.Sequence().SetLink(go);
            seq.Append(rt.DOScale(Vector3.one * 1.4f, _burstDuration * 0.6f).SetEase(Ease.OutExpo));
            seq.Join(img.DOFade(0f, _burstDuration * 0.8f).SetEase(Ease.InQuad));
            seq.OnComplete(() => { if (go != null) Destroy(go); });
        }

        private IEnumerator SpawnCombustionBloom()
        {
            RectTransform parent = _rectTransform.parent as RectTransform ?? _rectTransform;

            // 1. THE DETONATION SHOCKWAVE (Instant Bright Center Anchor)
            if (_bloomSprite != null)
            {
                var flashGo = new GameObject("Explosion_Core_Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                flashGo.transform.SetParent(parent, false);

                var rt = flashGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(280f, 280f);
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one * 0.2f;

                var img = flashGo.GetComponent<Image>();
                img.sprite = _bloomSprite;
                img.color = Color.white; 
                img.raycastTarget = false;

                var flashSeq = DOTween.Sequence().SetLink(flashGo);
                flashSeq.Append(rt.DOScale(Vector3.one * _bloomScale, 0.08f).SetEase(Ease.OutExpo));
                flashSeq.Join(img.DOFade(0f, 0.12f).SetEase(Ease.InQuad));
                flashSeq.OnComplete(() => { if (flashGo != null) Destroy(flashGo); });
            }

            // 2. TUNED HIGH-DENSITY CHAOS EMITTER
            for (int i = 0; i < _particleCount; i++)
            {
                Sprite selectedSprite = (i % 2 == 0) ? _exhaustSprite : _bloomSprite;
                if (selectedSprite == null) continue;

                var pGo = new GameObject($"FakeFlameParticle_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                pGo.transform.SetParent(parent, false);

                var rt = pGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.1f); 

                float pWidth = Random.Range(40f, 95f);
                float pHeight = Random.Range(80f, 170f);
                rt.sizeDelta = new Vector2(pWidth, pHeight);

                float spawnXOffset = Random.Range(-_spawnWidthOffset, _spawnWidthOffset);
                float spawnYOffset = Random.Range(-160f, -100f); // Fastened relative to center layout coordinates
                rt.anchoredPosition = new Vector2(spawnXOffset, spawnYOffset);
                
                rt.localScale = Vector3.zero;
                rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-30f, 30f));

                var img = pGo.GetComponent<Image>();
                img.sprite = selectedSprite;
                img.color = _coreColor;
                img.raycastTarget = false;

                float delay = Random.Range(0f, 0.14f); 
                float lifetime = Random.Range(0.3f, 0.55f);
                
                float targetX = spawnXOffset + Random.Range(-100f, 100f);
                float targetY = spawnYOffset + Random.Range(_minVelocityY, _maxVelocityY);
                
                float randomStretchX = Random.Range(0.7f, 1.3f) * _bloomScale;
                float randomStretchY = Random.Range(1.3f, 2.3f) * _exhaustStretchY;

                var pSeq = DOTween.Sequence().SetLink(pGo).SetDelay(delay);

                pSeq.Append(rt.DOAnchorPos(new Vector2(targetX, targetY), lifetime).SetEase(Ease.OutQuad));
                pSeq.Join(rt.DOScaleX(randomStretchX, lifetime * 0.35f).SetEase(Ease.OutCubic));
                pSeq.Join(rt.DOScaleY(randomStretchY, lifetime * 0.35f).SetEase(Ease.OutCubic));
                pSeq.Join(rt.DORotate(new Vector3(0f, 0f, rt.localEulerAngles.z + Random.Range(-45f, 45f)), lifetime, RotateMode.FastBeyond360).SetEase(Ease.Linear));

                pSeq.Join(img.DOColor(_tipColor, lifetime * 0.65f).SetEase(Ease.InCubic));
                pSeq.Insert(delay + (lifetime * 0.35f), img.DOFade(0f, lifetime * 0.65f).SetEase(Ease.InQuad));
                
                pSeq.OnComplete(() => { if (pGo != null) Destroy(pGo); });
            }

            yield return null;
        }
    }
}