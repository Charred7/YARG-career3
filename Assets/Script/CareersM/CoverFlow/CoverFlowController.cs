using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using YARG.Career;
using YARG.Core.Audio;
using YARG.Core.Input;
using YARG.Core.Song;
using YARG.Menu.Career.Motivation;
using YARG.Menu.Navigation;
using YARG.Menu.Persistent;
using YARG.Player;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// TRUE 3D CoverFlow controller — 7-slot parabolic arc in WorldSpace.
    /// Uses pure Transform manipulation (localPosition, localRotation, localScale).
    /// Decoupled sliding deck implementation providing premium physics-like transitions
    /// and continuous depth-sorting for high-end soft shadow layouts.
    /// </summary>
    public class CoverFlowController : MonoBehaviour
    {
        [Header("Card Pool")]
        [SerializeField]
        private CoverFlowCard[] _cardSlots = new CoverFlowCard[7];

        [Header("Background")]
        [SerializeField]
        private CoverFlowBackground _background;

        [Header("Header UI")]
        [SerializeField]
        private TextMeshProUGUI _careerNameText;

        [SerializeField]
        private TextMeshProUGUI _bandNameText;

        [SerializeField]
        private TextMeshProUGUI _progressText;

        [Header("Canvas")]
        [SerializeField]
        private Canvas _targetCanvas;

        [Header("Input")]
        [SerializeField]
        private CoverFlowInputRemapper _inputRemapper;

        [Header("Animation")]
        [SerializeField]
        private AnimationCurve _easingCurve;

        [SerializeField]
        [Range(0.10f, 0.35f)]
        private float _transitionDuration = 0.18f;

        [SerializeField]
        [Range(0.0f, 0.15f)]
        private float _overshootDuration = 0.08f;

        [SerializeField]
        [Range(0.0f, 0.10f)]
        private float _overshootAmount = 0.025f;

        [Header("Hype Engine — Tier 2 (Micro-Time Efficiency)")]
        [SerializeField]
        private float _blitzTotalMinutesMax = 9.0f;

        [SerializeField]
        private float _blitzAvgTrackMinutesMax = 3.5f;

        [SerializeField]
        private float _biteSizeAvgTrackMinutesMax = 3.0f;

        [SerializeField]
        private float _tier2BlacklistTotalMinutes = 10.0f;

        [SerializeField]
        private float _tier2BlacklistAvgTrackMinutes = 3.5f;

        [Header("Hype Engine — Tier 3 (Aesthetic Vibe)")]
        [SerializeField]
        private string[] _headlinerArtistNames = { "METALLICA", "QUEEN", "ROCK BAND" };

        [Header("Hype Engine — Fallback")]
        [SerializeField]
        private string _defaultFallbackText = "SETLIST VIBE: PURE ROCK & ROLL";

        [Header("SFX")]
        [SerializeField]
        [Range(0.05f, 0.3f)]
        private float _scrollSfxCooldown = 0.1f;

        [SerializeField]
        [Range(0.1f, 1.0f)]
        private float _cashSfxCooldown = 0.3f;

        [Header("Tier-Unlock Sequence")]
        [SerializeField]
        [Range(0.04f, 0.15f)]
        private float _sweepRapidDuration = 0.08f;

        [SerializeField]
        [Range(0.1f, 1.0f)]
        private float _dramaticPauseDuration = 0.4f;

        [SerializeField]
        [Range(0.1f, 0.8f)]
        private float _revealStepDelay = 0.45f;

        // ── TRUE 3D Parabolic Arc Layout (7 slots, center=3) ──────

        private struct CardLayout
        {
            public Vector3 LocalPosition;   
            public float YRotation;          
            public float Scale;              
            public float Opacity;            
        }

        private static readonly CardLayout[] LAYOUTS = new CardLayout[]
        {
            new() { LocalPosition = new Vector3(-560f, -40f, 250f), YRotation = -65f, Scale = 0.40f, Opacity = 0.20f }, 
            new() { LocalPosition = new Vector3(-390f, -20f, 140f), YRotation = -52f, Scale = 0.52f, Opacity = 0.32f }, 
            new() { LocalPosition = new Vector3(-220f, -10f,  55f), YRotation = -38f, Scale = 0.68f, Opacity = 0.58f }, 
            new() { LocalPosition = new Vector3(   0f,   0f,   0f), YRotation =   0f, Scale = 1.00f, Opacity = 1.00f }, 
            new() { LocalPosition = new Vector3( 220f, -10f,  55f), YRotation =  38f, Scale = 0.68f, Opacity = 0.58f }, 
            new() { LocalPosition = new Vector3( 390f, -20f, 140f), YRotation =  52f, Scale = 0.52f, Opacity = 0.32f }, 
            new() { LocalPosition = new Vector3( 560f, -40f, 250f), YRotation =  65f, Scale = 0.40f, Opacity = 0.20f }, 
        };

        private static readonly CardLayout VIRTUAL_LEFT  = new() { LocalPosition = new Vector3(-730f, -60f, 360f), YRotation = -75f, Scale = 0.28f, Opacity = 0.0f };
        private static readonly CardLayout VIRTUAL_RIGHT = new() { LocalPosition = new Vector3( 730f, -60f, 360f), YRotation =  75f, Scale = 0.28f, Opacity = 0.0f };

        [Header("Song Matching")]
        [SerializeField]
        private ManualMatchPanel _manualMatchPanelPrefab;

        [SerializeField]
        private ManualMatchPanel _manualMatchPanelInstance;

        // ── Runtime State ─────────────────────────────────────────

        private CareerInfo _currentCareer;
        private List<GigInfo> _gigs;
        private CoverFlowConfig _config;

        private int _centerIndex;
        private int _visibleGigCount;
        private bool _isAnimating;

        /// <summary>
        /// The navigation scheme this menu pushed, tracked so we only ever pop/refresh
        /// our own scheme and never clobber another menu's scheme.
        /// </summary>
        private NavigationScheme _navScheme;

        // Cinematic Override Mask: State-Diff tracker for precise one-by-one reveals
        private HashSet<int> _maskedLockedGigs = new HashSet<int>();

        private readonly Dictionary<string, Texture2D> _posterCache = new();
        private HypeEngine _hypeEngine;
        private HypeThresholds _hypeThresholds;

        // ── SFX Runtime ────────────────────────────────────────────

        private float _lastScrollSfxTime = -1f;
        private float _lastCashSfxTime = -1f;

        // ── Initialisation ─────────────────────────────────────────

        private void Awake()
        {
            if (_easingCurve == null || _easingCurve.keys.Length == 0)
            {
                _easingCurve = new AnimationCurve(
                    new Keyframe(0f, 0f, 0f, 3f),
                    new Keyframe(1f, 1f, 0f, 0f));
            }
            _hypeEngine = new HypeEngine();
            BuildHypeThresholds();
        }

        private void OnEnable()
        {
            if (_gigs != null && _currentCareer != null)
            {
                RebuildNavScheme();
            }
        }

        private void OnDisable()
        {
            // Own our scheme's lifecycle: pop it when we leave instead of relying on the
            // next menu to clean it up. Guarded so we never pop a scheme that isn't ours.
            if (_navScheme != null)
            {
                Navigator.Instance?.PopScheme(_navScheme);
                _navScheme = null;
            }
        }

        private void OnValidate() => BuildHypeThresholds();

        private void BuildHypeThresholds()
        {
            _hypeThresholds = new HypeThresholds
            {
                BlitzTotalMinutesMax          = _blitzTotalMinutesMax,
                BlitzAvgTrackMinutesMax       = _blitzAvgTrackMinutesMax,
                BiteSizeAvgTrackMinutesMax    = _biteSizeAvgTrackMinutesMax,
                Tier2BlacklistTotalMinutes    = _tier2BlacklistTotalMinutes,
                Tier2BlacklistAvgTrackMinutes = _tier2BlacklistAvgTrackMinutes,
                HeadlinerArtistNames          = _headlinerArtistNames,
                DefaultFallbackText           = _defaultFallbackText
            };
        }

        public void AssignCardSlots(CoverFlowCard[] slots)
        {
            if (slots == null || slots.Length < 7) return;
            if (_cardSlots == null || _cardSlots.Length < 7 || _cardSlots[0] == null)
                _cardSlots = slots;
        }

        public void SetCareer(CareerInfo career)
        {
            if (_cardSlots == null || _cardSlots.Length < 7 || _cardSlots[0] == null) return;

            _currentCareer = career;
            HypeEngineCache.LoadIfNeeded();
            _config = career.CoverFlowConfig ?? new CoverFlowConfig();
            _gigs = CareerManager.Instance.GetGigs(career.id);

            CampaignProgressionManager.Instance.Initialize(career, _gigs?.Count ?? 0);
            RecomputeVisibleGigCount();

            _centerIndex = FindFirstIncompleteGigIndex();
            if (_centerIndex >= _visibleGigCount)
                _centerIndex = Math.Max(0, _visibleGigCount - 1);
            if (_centerIndex < 0)
                _centerIndex = 0;

            _posterCache.Clear();
            PopulateCampaignMetrics();

            if (_careerNameText != null) _careerNameText.text = career.name.ToUpper();
            if (_bandNameText != null) _bandNameText.text = CareerManager.Instance.CurrentBand?.BandName.ToUpper() ?? string.Empty;

            if (_background != null) _background.ApplyConfig(_config, career.bundlePath);

            foreach (var card in _cardSlots)
            {
                if (card != null)
                {
                    card.Initialize(_config);
                    card.SetAccentColor(_config.AccentColor);
                }
            }

            SnapCarouselToIndex(_centerIndex);
            UpdateProgressDisplay();
            RebuildNavScheme();
        }

        private void RecomputeVisibleGigCount()
        {
            if (_gigs == null || _gigs.Count == 0 || _currentCareer == null)
            {
                _visibleGigCount = 0;
                return;
            }

            int completedCount = _gigs.Count(g => CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{g.name}"));
            int horizonIdx = CampaignProgressionManager.Instance.GetHorizonGigIndex(_gigs.Count, completedCount);
            _visibleGigCount = horizonIdx < 0 ? 0 : horizonIdx + 1;
        }

        private void PopulateCampaignMetrics()
        {
            if (_gigs == null || _gigs.Count == 0) return;
            var cache = SongMatchCache.GetOrLoadCache(_currentCareer.id);
            float totalDifficulty = 0f, totalDensity = 0f;
            int gigCount = 0;
            foreach (var gig in _gigs)
            {
                if (cache?.gigDurations != null && cache.gigDurations.TryGetValue(gig.name, out var d) && d.resolvedSongCount > 0)
                {
                    totalDifficulty += (float)(d.avgTrackSeconds / 60.0);
                    totalDensity += d.resolvedSongCount;
                    gigCount++;
                }
            }
            CampaignMetrics.GlobalAverageDifficulty = gigCount > 0 ? totalDifficulty / gigCount : 3.5f;
            CampaignMetrics.GlobalAverageNoteDensity = gigCount > 0 ? totalDensity / gigCount : 500f;
        }

        private void SnapCarouselToIndex(int centerIndex)
        {
            _centerIndex = centerIndex;
            LayoutCards();
            UpdateActiveCardContent();
        }

        public void StepCarousel(int direction)
        {
            if (_isAnimating || _gigs == null || _gigs.Count == 0) return;
            int newIndex = _centerIndex + direction;
            if (newIndex < 0 || newIndex >= _visibleGigCount)
            {
                PlayScrollSound(SfxSample.ScrollCantScroll);
                return;
            }
            _centerIndex = newIndex;
            PlayScrollSound(SfxSample.ScrollMain);
            StartCoroutine(AnimateTransition(direction));
        }

        private IEnumerator AnimateTransition(int direction, bool resetAnimatingFlag = true)
        {
            _isAnimating = true;

            var startPos = new Vector3[7];
            var startRot = new Quaternion[7];
            var startScl = new Vector3[7];
            var startAlpha = new float[7];

            var targetPos = new Vector3[7];
            var targetRot = new Quaternion[7];
            var targetScl = new Vector3[7];
            var targetAlpha = new float[7];

            var movingCards = new CoverFlowCard[7];
            Array.Copy(_cardSlots, movingCards, 7);

            if (direction == 1)
            {
                for (int i = 1; i < 7; i++)
                {
                    if (movingCards[i] == null) continue;
                    startPos[i] = LAYOUTS[i].LocalPosition;
                    startRot[i] = Quaternion.Euler(0, LAYOUTS[i].YRotation, 0);
                    startScl[i] = Vector3.one * LAYOUTS[i].Scale;
                    startAlpha[i] = LAYOUTS[i].Opacity;

                    int targetSlot = i - 1;
                    targetPos[i] = LAYOUTS[targetSlot].LocalPosition;
                    targetRot[i] = Quaternion.Euler(0, LAYOUTS[targetSlot].YRotation, 0);
                    targetScl[i] = Vector3.one * LAYOUTS[targetSlot].Scale;
                    targetAlpha[i] = LAYOUTS[targetSlot].Opacity;
                }

                if (movingCards[0] != null)
                {
                    startPos[0] = LAYOUTS[0].LocalPosition;
                    startRot[0] = Quaternion.Euler(0, LAYOUTS[0].YRotation, 0);
                    startScl[0] = Vector3.one * LAYOUTS[0].Scale;
                    startAlpha[0] = LAYOUTS[0].Opacity;

                    targetPos[0] = VIRTUAL_LEFT.LocalPosition;
                    targetRot[0] = Quaternion.Euler(0, VIRTUAL_LEFT.YRotation, 0);
                    targetScl[0] = Vector3.one * VIRTUAL_LEFT.Scale;
                    targetAlpha[0] = VIRTUAL_LEFT.Opacity;
                }

                int incomingRightGig = _centerIndex + 3;
                var recycledRight = movingCards[0];
                if (incomingRightGig >= 0 && incomingRightGig < _visibleGigCount && recycledRight != null)
                {
                    PopulateCard(recycledRight, incomingRightGig);
                    recycledRight.gameObject.SetActive(true);

                    recycledRight.transform.localPosition = VIRTUAL_RIGHT.LocalPosition;
                    recycledRight.transform.localRotation = Quaternion.Euler(0, VIRTUAL_RIGHT.YRotation, 0);
                    recycledRight.transform.localScale = Vector3.one * VIRTUAL_RIGHT.Scale;
                    recycledRight.CanvasGroup.alpha = VIRTUAL_RIGHT.Opacity;

                    startPos[0] = VIRTUAL_RIGHT.LocalPosition;
                    startRot[0] = Quaternion.Euler(0, VIRTUAL_RIGHT.YRotation, 0);
                    startScl[0] = Vector3.one * VIRTUAL_RIGHT.Scale;
                    startAlpha[0] = VIRTUAL_RIGHT.Opacity;

                    targetPos[0] = LAYOUTS[6].LocalPosition;
                    targetRot[0] = Quaternion.Euler(0, LAYOUTS[6].YRotation, 0);
                    targetScl[0] = Vector3.one * LAYOUTS[6].Scale;
                    targetAlpha[0] = LAYOUTS[6].Opacity;
                }
            }
            else
            {
                for (int i = 0; i < 6; i++)
                {
                    if (movingCards[i] == null) continue;
                    startPos[i] = LAYOUTS[i].LocalPosition;
                    startRot[i] = Quaternion.Euler(0, LAYOUTS[i].YRotation, 0);
                    startScl[i] = Vector3.one * LAYOUTS[i].Scale;
                    startAlpha[i] = LAYOUTS[i].Opacity;

                    int targetSlot = i + 1;
                    targetPos[i] = LAYOUTS[targetSlot].LocalPosition;
                    targetRot[i] = Quaternion.Euler(0, LAYOUTS[targetSlot].YRotation, 0);
                    targetScl[i] = Vector3.one * LAYOUTS[targetSlot].Scale;
                    targetAlpha[i] = LAYOUTS[targetSlot].Opacity;
                }

                if (movingCards[6] != null)
                {
                    startPos[6] = LAYOUTS[6].LocalPosition;
                    startRot[6] = Quaternion.Euler(0, LAYOUTS[6].YRotation, 0);
                    startScl[6] = Vector3.one * LAYOUTS[6].Scale;
                    startAlpha[6] = LAYOUTS[6].Opacity;

                    targetPos[6] = VIRTUAL_RIGHT.LocalPosition;
                    targetRot[6] = Quaternion.Euler(0, VIRTUAL_RIGHT.YRotation, 0);
                    targetScl[6] = Vector3.one * VIRTUAL_RIGHT.Scale;
                    targetAlpha[6] = VIRTUAL_RIGHT.Opacity;
                }

                int incomingLeftGig = _centerIndex - 3;
                var recycledLeft = movingCards[6];
                if (incomingLeftGig >= 0 && incomingLeftGig < _visibleGigCount && recycledLeft != null)
                {
                    PopulateCard(recycledLeft, incomingLeftGig);
                    recycledLeft.gameObject.SetActive(true);

                    recycledLeft.transform.localPosition = VIRTUAL_LEFT.LocalPosition;
                    recycledLeft.transform.localRotation = Quaternion.Euler(0, VIRTUAL_LEFT.YRotation, 0);
                    recycledLeft.transform.localScale = Vector3.one * VIRTUAL_LEFT.Scale;
                    recycledLeft.CanvasGroup.alpha = VIRTUAL_LEFT.Opacity;

                    startPos[6] = VIRTUAL_LEFT.LocalPosition;
                    startRot[6] = Quaternion.Euler(0, VIRTUAL_LEFT.YRotation, 0);
                    startScl[6] = Vector3.one * VIRTUAL_LEFT.Scale;
                    startAlpha[6] = VIRTUAL_LEFT.Opacity;

                    targetPos[6] = LAYOUTS[0].LocalPosition;
                    targetRot[6] = Quaternion.Euler(0, LAYOUTS[0].YRotation, 0);
                    targetScl[6] = Vector3.one * LAYOUTS[0].Scale;
                    targetAlpha[6] = LAYOUTS[0].Opacity;
                }
            }

            float totalAnimationTime = _transitionDuration + _overshootDuration;
            float elapsed = 0f;

            while (elapsed < totalAnimationTime)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / totalAnimationTime);

                float t;
                if (normalizedTime < 0.72f)
                {
                    float travelTime = normalizedTime / 0.72f;
                    t = _easingCurve.Evaluate(travelTime) * (1f + _overshootAmount);
                }
                else
                {
                    float settleTime = (normalizedTime - 0.72f) / 0.28f;
                    float settleEase = 1f - Mathf.Pow(1f - settleTime, 3f);
                    t = Mathf.Lerp(1f + _overshootAmount, 1f, settleEase);
                }

                for (int i = 0; i < 7; i++)
                {
                    var c = movingCards[i];
                    if (c == null || !c.gameObject.activeSelf) continue;

                    c.transform.localPosition = Vector3.LerpUnclamped(startPos[i], targetPos[i], t);
                    c.transform.localRotation = Quaternion.SlerpUnclamped(startRot[i], targetRot[i], t);
                    c.transform.localScale = Vector3.LerpUnclamped(startScl[i], targetScl[i], t);
                    c.CanvasGroup.alpha = Mathf.LerpUnclamped(startAlpha[i], targetAlpha[i], Mathf.Clamp01(t));
                }

                UpdateLiveDepthSorting(movingCards);
                yield return null;
            }

            if (direction == 1)
            {
                if (!((_centerIndex + 3) >= 0 && (_centerIndex + 3) < _visibleGigCount) && movingCards[0] != null)
                    movingCards[0].gameObject.SetActive(false);

                _cardSlots[0] = movingCards[1]; _cardSlots[1] = movingCards[2]; _cardSlots[2] = movingCards[3];
                _cardSlots[3] = movingCards[4]; _cardSlots[4] = movingCards[5]; _cardSlots[5] = movingCards[6]; _cardSlots[6] = movingCards[0];
            }
            else
            {
                if (!((_centerIndex - 3) >= 0 && (_centerIndex - 3) < _visibleGigCount) && movingCards[6] != null)
                    movingCards[6].gameObject.SetActive(false);

                _cardSlots[6] = movingCards[5]; _cardSlots[5] = movingCards[4]; _cardSlots[4] = movingCards[3];
                _cardSlots[3] = movingCards[2]; _cardSlots[2] = movingCards[1]; _cardSlots[1] = movingCards[0]; _cardSlots[0] = movingCards[6];
            }

            int[] postTransitionIndices = GetSlotGigIndices(_centerIndex);
            for (int i = 0; i < 7; i++)
            {
                var c = _cardSlots[i];
                if (c == null || !c.gameObject.activeSelf) continue;

                int gi = postTransitionIndices[i];
                if (gi >= 0 && gi < _visibleGigCount)
                    c.SetGig(_gigs[gi], GetCardState(gi));

                var finalLayout = LAYOUTS[i];
                c.transform.localPosition = finalLayout.LocalPosition;
                c.transform.localRotation = Quaternion.Euler(0, finalLayout.YRotation, 0);
                c.transform.localScale = Vector3.one * finalLayout.Scale;
                c.CanvasGroup.alpha = finalLayout.Opacity;
                c.ApplyTextOpacityForSlot(i == 3);
            }

            UpdateCardSortingOrder();
            UpdateActiveCardContent();
            UpdateProgressDisplay();
            if (resetAnimatingFlag)
                _isAnimating = false;
        }

        private void LayoutCards()
        {
            int[] indices = GetSlotGigIndices(_centerIndex);
            for (int i = 0; i < 7; i++)
            {
                var card = _cardSlots[i];
                if (card == null) continue;
                int gi = indices[i];
                if (gi < 0 || gi >= _visibleGigCount) { card.gameObject.SetActive(false); continue; }
                
                card.gameObject.SetActive(true);
                PopulateCard(card, gi);

                var layout = LAYOUTS[i];
                card.transform.localPosition = layout.LocalPosition;
                card.transform.localRotation = Quaternion.Euler(0, layout.YRotation, 0);
                card.transform.localScale = Vector3.one * layout.Scale;
                card.CanvasGroup.alpha = layout.Opacity;
                card.ApplyTextOpacityForSlot(i == 3);
            }
            UpdateCardSortingOrder();
        }

        private void PopulateCard(CoverFlowCard card, int gigIndex)
        {
            var gig = _gigs[gigIndex];
            var state = GetCardState(gigIndex);

            card.SetGig(gig, state);

            if (state == CoverFlowCardState.Locked)
            {
                int tierIdx = CampaignProgressionManager.Instance.GetTierForGig(gigIndex);
                var tierData = CampaignProgressionManager.Instance.GetTierData(tierIdx);
                if (tierData != null)
                {
                    int completedCount = _gigs.Count(g => CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{g.name}"));
                    int remaining = Math.Max(0, tierData.GigsRequiredToUnlock - completedCount);
                    card.SetUnlockRequirement(tierData.Name, remaining);
                }
                else
                {
                    card.SetUnlockRequirement(gigIndex);
                }
                card.SetPoster(null);
            }
            else
            {
                card.SetPoster(LoadPosterForGig(gig, gigIndex));
            }

            var v3 = card.GetComponent<CoverFlowCardControllerV3>();
            if (v3 != null)
                v3.PopulateCard(BuildGigCardData(gig, gigIndex, state));
        }

        private GigCardData BuildGigCardData(GigInfo gig, int gigIndex, CoverFlowCardState state)
        {
            int totalGigs = _gigs.Count;
            int completedCount = _gigs.Count(g => CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{g.name}"));

            int currentTier = CampaignProgressionManager.Instance.GetTierForGig(gigIndex);
            int gigsInTier = 0, completedInTier = 0;
            if (currentTier >= 0)
            {
                for (int i = 0; i < totalGigs; i++)
                {
                    if (CampaignProgressionManager.Instance.GetTierForGig(i) == currentTier)
                    {
                        gigsInTier++;
                        if (CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{_gigs[i].name}"))
                            completedInTier++;
                    }
                }
            }
            else
            {
                gigsInTier = totalGigs;
                completedInTier = completedCount;
            }

            var tracklist = new List<SongChartData>();
            var resolved = CareerManager.Instance.ResolveGig(gig, _currentCareer.id);
            for (int i = 0; i < resolved.Count; i++)
            {
                var (gs, se) = resolved[i];
                if (gig.HasEncore && i == gig.encoreIndex) continue;

                string artistName = se?.Artist.ToString() ?? gs.artist ?? "";
                tracklist.Add(new SongChartData
                {
                    title = se?.Name ?? gs.title ?? "???",
                    artist = artistName,
                    durationSeconds = (float)(se?.SongLengthSeconds ?? 0.0),
                    isHeadliner = IsHeadlinerArtist(artistName),
                });
            }

            _hypeEngine.Evaluate(gig, _currentCareer, _config, _hypeThresholds,
                out string h1, out string h2, out string icon1, out string icon2);
            Sprite posterSprite = null;
            if (state != CoverFlowCardState.Locked)
            {
                var tex = LoadPosterForGig(gig, gigIndex);
                if (tex != null)
                    posterSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            return new GigCardData
            {
                gigID = $"{_currentCareer.id}|{gig.name}",
                gigTitle = gig.name,
                posterSprite = posterSprite,
                tracklist = tracklist,
                remainingGigsInTier = gigsInTier - completedInTier,
                totalRemainingGigsInFile = totalGigs - completedCount,
                isLocked = state == CoverFlowCardState.Locked,
                isCompleted = state == CoverFlowCardState.Completed,
                hasEncore = gig.HasEncore,
                hypeSlot1 = h1,
                hypeSlot2 = h2,
                hypeIcon = icon1,
                accentColor = _config.AccentColor
            };
        }

        private bool IsHeadlinerArtist(string artistName)
        {
            if (string.IsNullOrEmpty(artistName)) return false;
            if (_config.IsIconicArtist(artistName)) return true;
            if (_hypeThresholds.HeadlinerArtistNames != null)
            {
                foreach (var name in _hypeThresholds.HeadlinerArtistNames)
                {
                    if (!string.IsNullOrEmpty(name) && string.Equals(name.Trim(), artistName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        private void UpdateActiveCardContent()
        {
            var cc = _cardSlots[3];
            if (cc == null || !cc.gameObject.activeSelf || cc.Gig == null) return;
            var state = GetCardState(_centerIndex);
            cc.SetGig(cc.Gig, state);
            cc.ApplyTextOpacityForSlot(true);
            var v3 = cc.GetComponent<CoverFlowCardControllerV3>();
            if (v3 != null) v3.PopulateCard(BuildGigCardData(cc.Gig, _centerIndex, state));
        }

        private CoverFlowCardState GetCardState(int gigIndex)
        {
            // ── VISUAL OVERRIDE MASK ──
            if (_maskedLockedGigs.Contains(gigIndex))
                return CoverFlowCardState.Locked;

            if (CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{_gigs[gigIndex].name}"))
                return CoverFlowCardState.Completed;

            int completedCount = _gigs.Count(g => CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{g.name}"));
            if (CampaignProgressionManager.Instance.IsGigLocked(gigIndex, completedCount))
                return CoverFlowCardState.Locked;

            return CoverFlowCardState.Active;
        }

        private int[] GetSlotGigIndices(int c) => new[] { c - 3, c - 2, c - 1, c, c + 1, c + 2, c + 3 };

        private void UpdateCardSortingOrder()
        {
            for (int i = 0; i < 7; i++)
            {
                int idx = i < 3 ? i : 6 - (i - 3);
                var c = _cardSlots[idx];
                if (c != null && c.gameObject.activeSelf)
                    c.transform.SetAsLastSibling();
            }
        }

        private void UpdateLiveDepthSorting(CoverFlowCard[] cards)
        {
            var sortedActive = cards.Where(c => c != null && c.gameObject.activeSelf).OrderByDescending(c => c.transform.localPosition.z).ToList();
            foreach (var card in sortedActive) card.transform.SetAsLastSibling();
        }

        private void PlayScrollSound(SfxSample sample)
        {
            if (Time.unscaledTime - _lastScrollSfxTime < _scrollSfxCooldown) return;
            _lastScrollSfxTime = Time.unscaledTime;
            GlobalAudioHandler.PlaySoundEffect(sample);
        }

        private Texture2D LoadPosterForGig(GigInfo gig, int gigIndex)
        {
            string key = $"{_currentCareer.id}|{gig.name}";
            if (_posterCache.TryGetValue(key, out var c)) return c;
            Texture2D t = null;
            if (!string.IsNullOrEmpty(_currentCareer.gigPosterConfigPath))
            {
                string f = ResolveGigPosterFilename(gig);
                if (!string.IsNullOrEmpty(f)) t = LoadTextureFromFile(f);
            }
            if (t == null && _currentCareer.useArtAsGigPoster && !string.IsNullOrEmpty(_currentCareer.artworkPath))
                t = LoadTextureFromFile(_currentCareer.artworkPath);
            _posterCache[key] = t;
            return t;
        }

        private string ResolveGigPosterFilename(GigInfo gig)
        {
            string configDir = System.IO.Path.GetDirectoryName(_currentCareer.gigPosterConfigPath);
            if (string.IsNullOrEmpty(configDir)) return null;

            var posterConfig = CoverFlow.GigPosterConfig.Load(_currentCareer.gigPosterConfigPath);
            if (posterConfig?.posters != null && posterConfig.posters.Count > 0)
            {
                if (posterConfig.posters.TryGetValue(gig.name, out string filename))
                {
                    string fullPath = System.IO.Path.Combine(configDir, filename);
                    if (System.IO.File.Exists(fullPath)) return fullPath;
                }
                var match = posterConfig.posters.FirstOrDefault(kvp => string.Equals(kvp.Key, gig.name, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Value))
                {
                    string fullPath = System.IO.Path.Combine(configDir, match.Value);
                    if (System.IO.File.Exists(fullPath)) return fullPath;
                }
            }

            if (posterConfig != null && !string.IsNullOrEmpty(posterConfig.fallback))
            {
                string fallbackPath = System.IO.Path.Combine(configDir, posterConfig.fallback);
                if (System.IO.File.Exists(fallbackPath)) return fallbackPath;
            }

            string sanitized = SanitizeGigName(gig.name);
            foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
            {
                string path = System.IO.Path.Combine(configDir, sanitized + ext);
                if (System.IO.File.Exists(path)) return path;
            }

            string postersDir = System.IO.Path.Combine(configDir, "posters");
            if (System.IO.Directory.Exists(postersDir))
            {
                foreach (var ext in new[] { ".png", ".jpg", ".jpeg" })
                {
                    string path = System.IO.Path.Combine(postersDir, sanitized + ext);
                    if (System.IO.File.Exists(path)) return path;
                }
            }
            return null;
        }

        private static string SanitizeGigName(string name) => name.Replace("'", "").Replace(" ", "_").Replace("/", "_").Replace("\\", "_").Replace("?", "").Replace(":", "").ToLowerInvariant();

        private static Texture2D LoadTextureFromFile(string path)
        {
            if (!System.IO.File.Exists(path)) return null;
            try { var t = new Texture2D(2, 2); if (t.LoadImage(System.IO.File.ReadAllBytes(path))) return t; }
            catch (Exception e) { Debug.LogError($"LoadTexture failed: {path}: {e.Message}"); }
            return null;
        }

        private void UpdateProgressDisplay()
        {
            if (_progressText == null || _currentCareer == null) return;
            int total = _visibleGigCount;
            int completed = _gigs?.Count(g => CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{g.name}")) ?? 0;
            _progressText.text = $"{completed}/{total} COMPLETE";
        }

        private int FindFirstIncompleteGigIndex()
        {
            if (_gigs == null) return 0;
            int completedCount = _gigs.Count(g => CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{g.name}"));
            for (int i = 0; i < _gigs.Count; i++)
            {
                if (!CareerManager.Instance.IsGigCompleted($"{_currentCareer.id}|{_gigs[i].name}"))
                    if (!CampaignProgressionManager.Instance.IsGigLocked(i, completedCount)) return i;
            }
            return _gigs.Count - 1;
        }

        private void RebuildNavScheme()
        {
            var nav = Navigator.Instance;
            if (nav == null) return;

            // Don't touch the stack if our scheme is buried under a temporary scheme
            // (e.g. the tier-unlock sequence's input lock) — we'd clobber it otherwise.
            if (_navScheme != null && !ReferenceEquals(nav.PeekScheme(), _navScheme))
                return;

            // Remove only our own scheme (guarded no-op if it isn't on top).
            if (_navScheme != null)
            {
                nav.PopScheme(_navScheme);
                _navScheme = null;
            }

            string matchLabel = HasExistingMatchCache(_currentCareer?.id) ? "Rematch" : "Song Matching";

            _navScheme = new NavigationScheme(new()
            {
                new(MenuAction.Up,     "Prev Gig",            () => StepCarousel(-1)),
                new(MenuAction.Down,   "Next Gig",            () => StepCarousel(1)),
                new(MenuAction.Green,  "Menu.Common.Confirm", () => OnGigSelected()),
                new(MenuAction.Red,    "Menu.Common.Back",    () => OnBackToCareers()),
                new(MenuAction.Orange, "Toggle Complete",     () => ToggleGigCompletion()),
                new(MenuAction.Select, matchLabel,            OpenSongMatching),
            }, true);
            nav.PushScheme(_navScheme);
        }

        private static bool HasExistingMatchCache(string careerId)
        {
            if (string.IsNullOrEmpty(careerId))
                return false;

            var cache = SongMatchCache.GetOrLoadCache(careerId);
            return cache?.matches != null && cache.matches.Count > 0;
        }

        private void OnGigSelected()
        {
            var cc = _cardSlots[3];
            if (cc?.Gig == null) return;
            if (PlayerContainer.Players.Count <= 0) return;
            var resolvedSongs = CareerManager.Instance.ResolveGig(cc.Gig, _currentCareer.id);
            var songEntries = resolvedSongs.Where(r => r.SongEntry != null).Select(r => r.SongEntry).ToList();
            if (songEntries.Count == 0) return;
            MusicLibraryWrapper.OpenGigInMusicLibrary(cc.Gig.name, songEntries);
        }

        private void OpenSongMatching()
        {
            if (_currentCareer == null) return;

            var panel = EnsureManualMatchPanel();
            if (panel == null)
            {
                Debug.LogWarning("[CoverFlow] OpenSongMatching — ManualMatchPanel unavailable.");
                return;
            }

            panel.Show(_currentCareer.id, onClose: null);
        }

        private ManualMatchPanel EnsureManualMatchPanel()
        {
            if (_manualMatchPanelInstance != null)
                return _manualMatchPanelInstance;

            _manualMatchPanelInstance = GetComponentInChildren<ManualMatchPanel>(true);
            if (_manualMatchPanelInstance != null)
                return _manualMatchPanelInstance;

            // Prefab reference (Inspector) or editor/resources fallback
            ManualMatchPanel prefab = _manualMatchPanelPrefab;
#if UNITY_EDITOR
            if (prefab == null)
            {
                var asset = UnityEditor.AssetDatabase
                    .LoadAssetAtPath<GameObject>(
                        "Assets/Prefabs/CareersM/Submenus/ManualMatchPanel.prefab");
                if (asset != null)
                    prefab = asset.GetComponent<ManualMatchPanel>();
            }
#endif
            if (prefab == null)
                prefab = Resources.Load<ManualMatchPanel>("CareersM/ManualMatchPanel");

            if (prefab != null)
            {
                _manualMatchPanelInstance = Instantiate(prefab);
                _manualMatchPanelInstance.name = "ManualMatchPanel";
                _manualMatchPanelInstance.transform.SetParent(null, false);
                _manualMatchPanelInstance.gameObject.SetActive(false);
                return _manualMatchPanelInstance;
            }

            Debug.LogError("[CoverFlow] ManualMatchPanel prefab missing. " +
                           "Assign it on CoverFlowController or run Tools/YARG Career Mod/Build Song Match Prefabs.");
            return null;
        }

        private void ToggleGigCompletion()
        {
            var cc = _cardSlots[3];
            if (cc?.Gig == null || _currentCareer == null) return;
            string gigId = $"{_currentCareer.id}|{cc.Gig.name}";

            // 1. CLEAR MASK & CAPTURE PURE OLD STATES
            _maskedLockedGigs.Clear(); 
            var oldStates = new CoverFlowCardState[_gigs.Count];
            for (int i = 0; i < _gigs.Count; i++) oldStates[i] = GetCardState(i);

            // 2. APPLY THE GLOBAL MODIFICATION
            if (CareerManager.Instance.IsGigCompleted(gigId)) BandContainer.RemoveCompletedGig(gigId);
            else CareerManager.Instance.CompleteGig(gigId);

            if (CareerManager.Instance.IsGigCompleted(gigId))
            {
                if (Time.unscaledTime - _lastCashSfxTime >= _cashSfxCooldown)
                {
                    _lastCashSfxTime = Time.unscaledTime;
                    GlobalAudioHandler.PlaySoundEffect(SfxSample.Cash);
                }
                MomentumTracker.Instance?.RecordGigCompletion(gigId, _currentCareer.id);
                CompletionCelebration.Instance?.PlayGigCompleteEffect(transform);
                if (GigProgressEnhancer.Instance?.IsCampaignComplete() == true) CompletionCelebration.Instance?.PlayCampaignCompleteEffect(transform);
            }

            // 3. CAPTURE NEW HORIZON 
            RecomputeVisibleGigCount();

            // 4. DETECT UNLOCKS VIA EXACT STATE DIFF & MASK THEM
            List<int> newlyUnlocked = new List<int>();
            for (int i = 0; i < _visibleGigCount; i++)
            {
                var newState = GetCardState(i); 
                if (oldStates[i] == CoverFlowCardState.Locked && newState == CoverFlowCardState.Active)
                {
                    newlyUnlocked.Add(i);
                    _maskedLockedGigs.Add(i); // Mask them so UI refresh keeps them visually locked
                }
            }

            if (newlyUnlocked.Count > 0)
            {
                StartCoroutine(PlayTierUnlockSequence(newlyUnlocked));
                return; 
            }

            UpdateProgressDisplay();
            UpdateActiveCardContent();
            GigProgressEnhancer.Instance?.OnGigListChanged();
            TeaserPanel.Instance?.UpdateTicker();
        }

        private IEnumerator PlayTierUnlockSequence(List<int> newlyUnlocked)
        {
            // PHASE 1: Lock all input
            _isAnimating = true;
            Navigator.Instance.PushScheme(NavigationScheme.Empty);

            // PHASE 2: Wait for cash SFX to finish
            yield return new WaitForSecondsRealtime(_cashSfxCooldown);

            // PHASE 3: Scroll-to-reveal each card ONE BY ONE
            for (int i = 0; i < newlyUnlocked.Count; i++)
            {
                int gigIdx = newlyUnlocked[i];

                // 3a. Sweep carousel to bring this gig to center (slot 3)
                yield return StartCoroutine(SweepToIndex(gigIdx));

                // 3b. Dramatic pause before reveal
                yield return new WaitForSecondsRealtime(_dramaticPauseDuration);

                // 3c. FLAME REVEAL on the center card
                // Remove mask so GetCardState returns Active instead of Locked
                _maskedLockedGigs.Remove(gigIdx);

                var centerCard = _cardSlots[3];
                if (centerCard != null && centerCard.gameObject.activeSelf)
                {
                    var gig = _gigs[gigIdx];

                    // Pre-load poster while card transitions
                    Texture2D poster = LoadPosterForGig(gig, gigIdx);

                    // Update V3 card data to Active state BEFORE layout settle
                    var v3 = centerCard.GetComponent<CoverFlowCardControllerV3>();
                    if (v3 != null)
                        v3.PopulateCard(BuildGigCardData(gig, gigIdx, CoverFlowCardState.Active));

                    // Wait one frame for Sweep transform overrides to settle
                    yield return null;

                    // Play the reveal SFX
                    GlobalAudioHandler.PlaySoundEffect(SfxSample.UIReveal);

                    // Trigger visual reveal (flame burst + state transition + scale pop)
                    centerCard.PlayRevealEffect(poster);
                }

                // 3d. Post-reveal pause for dramatic effect
                yield return new WaitForSecondsRealtime(_revealStepDelay);
            }

            // PHASE 4: Clear all masks (safety net)
            _maskedLockedGigs.Clear();

            // PHASE 5: Sweep back to first newly unlocked gig
            yield return StartCoroutine(SweepToIndex(newlyUnlocked[0]));

            // PHASE 6: Release input, refresh UI
            _isAnimating = false;
            Navigator.Instance.PopScheme();

            UpdateProgressDisplay();
            UpdateActiveCardContent();
            GigProgressEnhancer.Instance?.OnGigListChanged();
            TeaserPanel.Instance?.UpdateTicker();
        }

        private IEnumerator SweepToIndex(int targetIndex)
        {
            float originalTransition = _transitionDuration;
            float originalOvershoot = _overshootDuration;
            _transitionDuration = _sweepRapidDuration;
            _overshootDuration = 0.0f;

            while (_centerIndex < targetIndex)
            {
                _centerIndex++;
                yield return StartCoroutine(AnimateTransition(1, resetAnimatingFlag: false));
            }

            _transitionDuration = originalTransition;
            _overshootDuration = originalOvershoot;
        }

        private void OnBackToCareers() => MenuManager.Instance.PopMenu();
    }
}