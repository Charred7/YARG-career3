// ============================================================
//  CampaignStatsController.cs
//  YARG — Campaign Stats Screen  (AAA Milestone / End-of-Campaign)
//
//  Responsibilities:
//    • Builds CampaignStatsData via TryBuildViewData(CareerInfo).
//    • Checks campaign completion via CareerManager.
//    • Calls StatCalculator.ComputeCampaignStats() to get raw engine data.
//    • Maps CampaignStats  →  CampaignStatsData  (view-friendly model).
//    • Optionally pushes data to CampaignStatsView (legacy overlay).
//
//  Integration:
//    • CampaignTrophyController consumes TryBuildViewData for the prefab menu.
//    • CampaignStatsView is optional — attach only for legacy overlay usage.
//
//  Namespace: YARG.Menu.Career.Motivation
// ============================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YARG.Menu.Career.Motivation
{
    /// <summary>
    /// Controller that bridges the engine-side <see cref="CampaignStats"/> model
    /// to view-friendly <see cref="CampaignStatsData"/>.
    ///
    /// Call <see cref="TryBuildViewData"/> to populate the trophy prefab menu,
    /// or <see cref="ShowForCampaign"/> for the legacy overlay view.
    /// </summary>
    [DefaultExecutionOrder(105)]
    public class CampaignStatsController : MonoBehaviour
    {
        private const int SharedStatCount = 3;
        private const int FlexStatCount = 2;
        private const int MaxPlayersSharingFlexType = 2;

        private static readonly StatType[] SharedCoreTypes =
        {
            StatType.AverageAccuracy,
            StatType.FourStarCount,
            StatType.UniqueSongsCleared,
        };

        private static readonly AccoladeTier[] AssignableAccolades =
        {
            AccoladeTier.CampaignMVP,
            AccoladeTier.GoldenFingers,
            AccoladeTier.Perfectionist,
            AccoladeTier.PerfectStrings,
            AccoladeTier.ExpertVeteran,
            AccoladeTier.SharpShooter,
            AccoladeTier.RoadWarrior,
            AccoladeTier.RisingStar,
            AccoladeTier.RockEnthusiast,
            AccoladeTier.ShowOpener,
        };

        private static readonly AccoladeTier[] FallbackAccolades =
        {
            AccoladeTier.RisingStar,
            AccoladeTier.RockEnthusiast,
            AccoladeTier.ShowOpener,
        };

        // ─────────────────────────────────────────────────────────────────────
        //  INSPECTOR
        // ─────────────────────────────────────────────────────────────────────

        [Header("── Celebration ───────────────────────────────")]
        [Tooltip("When enabled, every completed campaign displays the Golden Vinyl milestone " +
                 "tier and badge regardless of actual star performance.")]
        [SerializeField] private bool _celebrateAllCompletionsAsGoldenVinyl = true;

        /// <summary>
        /// When true, completed campaigns always earn the Golden Vinyl presentation.
        /// </summary>
        public bool CelebrateAllCompletionsAsGoldenVinyl
        {
            get => _celebrateAllCompletionsAsGoldenVinyl;
            set => _celebrateAllCompletionsAsGoldenVinyl = value;
        }

        [Header("── Diagnostics ──────────────────────────────")]
        [Tooltip("Log mapping decisions to the Unity Console.")]
        [SerializeField] private bool _enableDiagnostics = true;

        [Header("── Accolade Soft Floors ─────────────────────")]
        [Tooltip("Minimum Full Combos to claim combo-role accolades.")]
        [SerializeField] private int _softFloorFullCombos = 1;
        [Tooltip("Minimum Gold Star songs to claim Golden Fingers.")]
        [SerializeField] private int _softFloorGoldStars = 1;
        [Tooltip("Minimum Perfect (100%) songs to claim Flawless.")]
        [SerializeField] private int _softFloorPerfectSongs = 1;
        [Tooltip("Minimum Expert runs to claim Expert Veteran.")]
        [SerializeField] private int _softFloorExpertSongs = 1;
        [Tooltip("Minimum average accuracy (0-1) to claim Sharp Shooter.")]
        [SerializeField] [Range(0f, 1f)] private float _softFloorAverageAccuracy = 0.70f;
        [Tooltip("Minimum unique songs cleared to claim Road Warrior.")]
        [SerializeField] private int _softFloorUniqueSongs = 1;
        [Tooltip("Minimum 4★+ plays to claim Rising Star.")]
        [SerializeField] private int _softFloorFourStar = 1;
        [Tooltip("Minimum play sessions to claim Rock Enthusiast.")]
        [SerializeField] private int _softFloorSessions = 1;

        // ─────────────────────────────────────────────────────────────────────
        //  PRIVATE — State
        // ─────────────────────────────────────────────────────────────────────

        private CampaignStatsView _view;
        private CareerInfo        _lastShownCareer;

        // ─────────────────────────────────────────────────────────────────────
        //  LIFECYCLE
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _view = GetComponent<CampaignStatsView>();
            if (_view == null)
                _view = GetComponentInChildren<CampaignStatsView>(includeInactive: true);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PUBLIC API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the legacy stats overlay is currently visible.
        /// </summary>
        public bool IsVisible => _view != null && _view.IsVisible;

        /// <summary>
        /// Returns true when every gig in the campaign is completed for the current band.
        /// </summary>
        public static bool IsCampaignCompleted(CareerInfo career)
        {
            CheckCampaignCompleted(career, null, out bool completed);
            return completed;
        }

        /// <summary>
        /// Builds view data for a fully completed campaign. Returns false when the
        /// campaign is null, incomplete, or stats cannot be computed.
        /// </summary>
        public bool TryBuildViewData(CareerInfo career, out CampaignStatsData data)
        {
            data = null;

            if (career == null || !IsCampaignCompleted(career))
                return false;

            DateTime? completedDate = CareerManager.Instance?.GetCampaignCompletedDate(career.id);
            CampaignStats rawStats = StatCalculator.ComputeCampaignStats(career, completedDate);

            if (rawStats == null)
                return false;

            data = MapToViewData(rawStats, career);

            if (_enableDiagnostics)
                LogMappingDiagnostics(data);

            return true;
        }

        /// <summary>
        /// Legacy entry point for the runtime overlay view.
        /// </summary>
        public void ShowForCampaign(CareerInfo career)
        {
            if (_view == null) return;

            if (!TryBuildViewData(career, out var viewData))
            {
                if (_enableDiagnostics)
                    Debug.Log($"[CampaignStatsController] ShowForCampaign('{career?.name}') " +
                              "— not completed or null, hiding panel.");
                HideStats();
                _lastShownCareer = null;
                return;
            }

            if (_lastShownCareer?.id == career.id && _view.IsVisible)
            {
                if (_enableDiagnostics)
                    Debug.Log($"[CampaignStatsController] '{career.name}' already showing — skipping rebuild.");
                return;
            }

            _lastShownCareer = career;
            _view.Populate(viewData);
            _view.Show();
        }

        /// <summary>
        /// Hides the stats panel immediately (with fade-out animation).
        /// </summary>
        public void HideStats()
        {
            _view?.Hide();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  COMPLETION CHECK
        // ─────────────────────────────────────────────────────────────────────

        private static bool CheckCampaignCompleted(CareerInfo career,
            Dictionary<string, bool> cache, out bool result)
        {
            result = false;
            if (career == null)
                return false;

            if (cache != null &&
                cache.TryGetValue(career.id, out bool cached))
            {
                result = cached;
                return true;
            }

            var manager = CareerManager.Instance;
            if (manager?.CurrentBand == null)
            {
                if (cache != null)
                    cache[career.id] = false;
                return false;
            }

            var gigs = manager.GetGigs(career.id);
            if (gigs == null || gigs.Count == 0)
            {
                if (cache != null)
                    cache[career.id] = false;
                return false;
            }

            foreach (var gig in gigs)
            {
                string gigId = $"{career.id}|{gig.name}";
                if (!manager.IsGigCompleted(gigId))
                {
                    if (cache != null)
                        cache[career.id] = false;
                    result = false;
                    return true;
                }
            }

            if (cache != null)
                cache[career.id] = true;
            result = true;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DATA MAPPING  (CampaignStats → CampaignStatsData)
        // ─────────────────────────────────────────────────────────────────────

        private CampaignStatsData MapToViewData(CampaignStats raw, CareerInfo career)
        {
            return new CampaignStatsData
            {
                CareerId         = raw.CareerId,
                ScreenTitle      = BuildScreenTitle(career),
                IsFullyCompleted = raw.IsCompleted,
                ComputedAt       = DateTime.UtcNow,
                Milestone        = BuildMilestoneData(raw),
                Players          = BuildPlayerList(raw),
            };
        }

        private static string BuildScreenTitle(CareerInfo career)
        {
            string name = career?.name?.ToUpper() ?? "CAMPAIGN";

            int pipeIndex = name.LastIndexOf('|');
            if (pipeIndex > 0 && name.Length - pipeIndex - 1 == 10)
                name = name[..pipeIndex].TrimEnd();

            if (!string.IsNullOrWhiteSpace(career?.releaseDate))
                return $"CAMPAIGN CONQUERED: {name} | {career.releaseDate.Trim()}";

            return $"CAMPAIGN CONQUERED: {name}";
        }

        private MilestoneData BuildMilestoneData(CampaignStats raw)
        {
            var band      = raw.BandStats;
            var milestone = new MilestoneData();

            milestone.Tier = _celebrateAllCompletionsAsGoldenVinyl
                ? MilestoneTier.GoldenVinyl
                : DetermineMilestoneTier(band);

            switch (milestone.Tier)
            {
                case MilestoneTier.GoldenVinyl:
                    milestone.BadgeTitle           = "GOLDEN VINYL";
                    milestone.CompletionBannerText = "100% COMPLETE";
                    milestone.SubLabel             = "GOLDEN VINYL";
                    break;
                case MilestoneTier.GoldRecord:
                    milestone.BadgeTitle           = "GOLD RECORD";
                    milestone.CompletionBannerText = "GOLD COMPLETE";
                    milestone.SubLabel             = "GOLD RECORD";
                    break;
                case MilestoneTier.SilverRecord:
                    milestone.BadgeTitle           = "SILVER RECORD";
                    milestone.CompletionBannerText = "SILVER COMPLETE";
                    milestone.SubLabel             = "SILVER RECORD";
                    break;
                default:
                    milestone.BadgeTitle           = "CAMPAIGN COMPLETE";
                    milestone.CompletionBannerText = "COMPLETE";
                    milestone.SubLabel             = string.Empty;
                    break;
            }

            milestone.BandStatRows = BuildBandStatRows(band);
            milestone.TopSongs = BuildTopSongs(band);
            return milestone;
        }

        private static MilestoneTier DetermineMilestoneTier(BandCampaignStats band)
        {
            if (band == null || band.TotalSongs == 0)
                return MilestoneTier.Completed;

            int total    = band.TotalSongs;
            int fiveStar = band.SongsWithFiveStars;

            if (fiveStar >= total)
                return MilestoneTier.GoldenVinyl;

            float ratio = (float)fiveStar / total;
            if (ratio >= 0.80f)
                return MilestoneTier.GoldRecord;
            if (ratio >= 0.50f)
                return MilestoneTier.SilverRecord;

            return MilestoneTier.Completed;
        }

        private static List<StatRowData> BuildBandStatRows(BandCampaignStats band)
        {
            var rows = new List<StatRowData>();
            if (band == null) return rows;

            if (band.TotalBandScore > 0)
                rows.Add(new StatRowData("Total Band Score:", $"{band.TotalBandScore:N0}", StatRowIconType.Trophy));

            if (band.TotalStarsEarned > 0)
                rows.Add(new StatRowData("Total Stars Earned:", $"{band.TotalStarsEarned}", StatRowIconType.Star));

            if (band.SongsWithGoldStars > 0)
                rows.Add(new StatRowData("Gold Star Songs:", $"{band.SongsWithGoldStars}", StatRowIconType.MusicNote));

            if (band.SongsWithFiveStars > 0)
                rows.Add(new StatRowData("5-Star Songs:", $"{band.SongsWithFiveStars}", StatRowIconType.Star));

            if (band.SongsWithFourStars > 0)
                rows.Add(new StatRowData("4-Star Songs:", $"{band.SongsWithFourStars}", StatRowIconType.Star));

            if (band.TotalPlaySessions > 0)
                rows.Add(new StatRowData("Total Sessions:", $"{band.TotalPlaySessions}", StatRowIconType.MusicNote));

            return rows;
        }

        private static List<TopSongData> BuildTopSongs(BandCampaignStats band)
        {
            var songs = new List<TopSongData>();
            if (band?.TopSongs == null)
                return songs;

            int rank = 1;
            foreach (var song in band.TopSongs)
            {
                if (rank > 3)
                    break;
                if (song == null || song.BandScore <= 0)
                    continue;

                songs.Add(new TopSongData(rank, song.SongName ?? "Unknown Song", $"{song.BandScore:N0}"));
                rank++;
            }

            return songs;
        }

        // ── Player List (right panel) ─────────────────────────────────────────

        private List<PlayerStatsData> BuildPlayerList(CampaignStats raw)
        {
            var profiles = raw.ProfileStats ?? new List<ProfileCampaignStats>();
            AccoladeTier[] accolades = AssignAccolades(profiles);
            var flexUsage = new Dictionary<StatType, int>();
            var players = new List<PlayerStatsData>(profiles.Count);

            for (int i = 0; i < profiles.Count; i++)
            {
                AccoladeTier accolade = i < accolades.Length ? accolades[i] : AccoladeTier.ShowOpener;
                players.Add(BuildPlayerData(profiles[i], accolade, flexUsage));
            }

            return players;
        }

        private PlayerStatsData BuildPlayerData(
            ProfileCampaignStats profile,
            AccoladeTier accolade,
            Dictionary<StatType, int> flexUsage)
        {
            DiamondFrameVariant frame = accolade switch
            {
                AccoladeTier.CampaignMVP    => DiamondFrameVariant.OrnateGold_A,
                AccoladeTier.GoldenFingers  => DiamondFrameVariant.OrnateGold_B,
                AccoladeTier.Perfectionist  => DiamondFrameVariant.OrnateGold_C,
                AccoladeTier.PerfectStrings => DiamondFrameVariant.OrnateGold_D,
                AccoladeTier.ExpertVeteran  => DiamondFrameVariant.Standard_A,
                AccoladeTier.SharpShooter   => DiamondFrameVariant.Standard_B,
                AccoladeTier.ImmortalStreak => DiamondFrameVariant.Standard_C,
                AccoladeTier.RoadWarrior    => DiamondFrameVariant.Standard_D,
                AccoladeTier.RisingStar     => DiamondFrameVariant.Plain_A,
                AccoladeTier.RockEnthusiast => DiamondFrameVariant.Plain_B,
                AccoladeTier.ShowOpener     => DiamondFrameVariant.Plain_C,
                _                           => DiamondFrameVariant.Plain_A,
            };

            PlayerAuraType aura = accolade switch
            {
                AccoladeTier.CampaignMVP    => PlayerAuraType.Flames,
                AccoladeTier.GoldenFingers  => PlayerAuraType.Flames,
                AccoladeTier.Perfectionist  => PlayerAuraType.Flames,
                AccoladeTier.PerfectStrings => PlayerAuraType.Flames,
                AccoladeTier.ExpertVeteran  => PlayerAuraType.Lightning,
                AccoladeTier.SharpShooter   => PlayerAuraType.Lightning,
                AccoladeTier.ImmortalStreak => PlayerAuraType.Lightning,
                AccoladeTier.RoadWarrior    => PlayerAuraType.Lightning,
                _                           => PlayerAuraType.None,
            };

            string accoladeTitle = accolade switch
            {
                AccoladeTier.CampaignMVP    => "FULL\nCOMBO\nKING",
                AccoladeTier.GoldenFingers  => "GOLDEN\nFINGERS",
                AccoladeTier.Perfectionist  => "FLAWLESS",
                AccoladeTier.PerfectStrings => "PERFECT\nSTRINGS",
                AccoladeTier.ExpertVeteran  => "EXPERT\nVETERAN",
                AccoladeTier.SharpShooter   => "SHARP\nSHOOTER",
                AccoladeTier.ImmortalStreak => "STREAK\nMASTER",
                AccoladeTier.RoadWarrior    => "ROAD\nWARRIOR",
                AccoladeTier.RisingStar     => "RISING\nSTAR",
                AccoladeTier.RockEnthusiast => "ROCK\nENTHUSIAST",
                AccoladeTier.ShowOpener     => "SHOW\nOPENER",
                _                           => "SHOW\nOPENER",
            };

            int instrumentIndex = profile.InstrumentIndex % 4;
            var (starCount, maxStars) = ComputeStarDisplay(profile, instrumentIndex);

            string instrName  = GetInstrumentName(instrumentIndex);
            string instrLabel = instrName;

            List<StatRowData> statRows = SelectStatsSharedAndFlex(profile, accolade, flexUsage);

            return new PlayerStatsData
            {
                PlayerName      = profile.PlayerName ?? instrName,
                InstrumentLabel = instrLabel,
                InstrumentIndex = instrumentIndex,
                Aura            = aura,
                DiamondFrame    = frame,
                AccoladeTitle   = accoladeTitle,
                StarCount       = starCount,
                MaxStars        = maxStars,
                HighScoreSong   = profile.HighScoreSongName ?? string.Empty,
                Stats           = statRows,
            };
        }

        private static (int starCount, int maxStars) ComputeStarDisplay(ProfileCampaignStats profile, int instrumentIndex)
        {
            int bestStar = 0;
            if (profile.BestStarsByInstrument != null && instrumentIndex < profile.BestStarsByInstrument.Length)
                bestStar = profile.BestStarsByInstrument[instrumentIndex];

            int displayStars = bestStar switch
            {
                >= 6 => 5,
                5    => 5,
                >= 4 => 4,
                _    => 4,
            };

            return (displayStars, 5);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  STAT SELECTION — Shared Core + Accolade Flex (A + little B)
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Builds 3 shared celebration stats + 2 flex stats + High Score anchor.
        /// Flex prefers accolade-driven types, then any remaining positive stats.
        /// </summary>
        private List<StatRowData> SelectStatsSharedAndFlex(
            ProfileCampaignStats profile,
            AccoladeTier accolade,
            Dictionary<StatType, int> flexUsage)
        {
            var rows = new List<StatRowData>();
            if (profile?.AllStats == null)
                return rows;

            var byType = profile.AllStats
                .Where(s => s.Value > 0 && s.Type != StatType.LongestStreak)
                .GroupBy(s => s.Type)
                .ToDictionary(g => g.Key, g => g.First());

            // Shared core — same labels on every card when values exist
            foreach (var type in SharedCoreTypes)
            {
                if (!byType.TryGetValue(type, out var stat))
                    continue;
                rows.Add(ToRow(stat));
            }

            var flexPicked = new HashSet<StatType>();
            int flexAdded = 0;

            // Pass 1: accolade preferences (respect uniqueness cap)
            foreach (var type in GetFlexPreferences(accolade))
            {
                if (flexAdded >= FlexStatCount)
                    break;
                if (SharedCoreTypes.Contains(type) || flexPicked.Contains(type))
                    continue;
                if (type == StatType.HighScore)
                    continue;
                if (!byType.TryGetValue(type, out var stat))
                    continue;
                if (!TryClaimFlexType(type, flexUsage))
                    continue;

                rows.Add(ToRow(stat));
                flexPicked.Add(type);
                flexAdded++;
            }

            // Pass 2: any positive Tier 1–4 not already shown (respect uniqueness)
            if (flexAdded < FlexStatCount)
            {
                var fillers = byType.Values
                    .Where(s =>
                        s.Type != StatType.HighScore &&
                        s.Type != StatType.LongestStreak &&
                        !SharedCoreTypes.Contains(s.Type) &&
                        !flexPicked.Contains(s.Type))
                    .OrderBy(s => (int)s.Tier)
                    .ThenByDescending(s => s.SortPriority);

                foreach (var stat in fillers)
                {
                    if (flexAdded >= FlexStatCount)
                        break;
                    if (!TryClaimFlexType(stat.Type, flexUsage))
                        continue;

                    rows.Add(ToRow(stat));
                    flexPicked.Add(stat.Type);
                    flexAdded++;
                }
            }

            // Pass 3: force-fill any remaining positive (ignore uniqueness) so every card gets 2 flex
            if (flexAdded < FlexStatCount)
            {
                var forceFill = byType.Values
                    .Where(s =>
                        s.Type != StatType.HighScore &&
                        s.Type != StatType.LongestStreak &&
                        !SharedCoreTypes.Contains(s.Type) &&
                        !flexPicked.Contains(s.Type))
                    .OrderBy(s => (int)s.Tier)
                    .ThenByDescending(s => s.SortPriority);

                foreach (var stat in forceFill)
                {
                    if (flexAdded >= FlexStatCount)
                        break;

                    // Still track usage for diagnostics, but do not block
                    flexUsage.TryGetValue(stat.Type, out int used);
                    flexUsage[stat.Type] = used + 1;

                    rows.Add(ToRow(stat));
                    flexPicked.Add(stat.Type);
                    flexAdded++;
                }
            }

            // High Score anchor (extracted by TrophyDataMapper)
            if (byType.TryGetValue(StatType.HighScore, out var highScore))
                rows.Add(ToRow(highScore));

            return rows;
        }

        private static bool TryClaimFlexType(StatType type, Dictionary<StatType, int> flexUsage)
        {
            flexUsage.TryGetValue(type, out int used);
            if (used >= MaxPlayersSharingFlexType)
                return false;

            flexUsage[type] = used + 1;
            return true;
        }

        private static StatType[] GetFlexPreferences(AccoladeTier accolade) => accolade switch
        {
            AccoladeTier.CampaignMVP => new[] { StatType.FullCombos, StatType.ExpertFCs },
            AccoladeTier.GoldenFingers => new[] { StatType.GoldStarSongs, StatType.FiveStarCount },
            AccoladeTier.Perfectionist => new[] { StatType.PerfectAccuracy, StatType.NearPerfectAccuracy },
            AccoladeTier.PerfectStrings => new[] { StatType.ExpertFCs, StatType.FullCombos, StatType.NearPerfectAccuracy },
            AccoladeTier.ExpertVeteran => new[] { StatType.ExpertSongsPlayed, StatType.HardPlusSongsPlayed },
            AccoladeTier.SharpShooter => new[] { StatType.HighAccuracy, StatType.NinetyPlusAccuracy, StatType.NearPerfectAccuracy },
            AccoladeTier.RoadWarrior => new[] { StatType.TotalNotesHit, StatType.TotalScore, StatType.HardPlusSongsPlayed },
            AccoladeTier.RisingStar => new[] { StatType.FiveStarCount, StatType.ThreeStarCount, StatType.HighAccuracy },
            AccoladeTier.RockEnthusiast => new[] { StatType.TotalNotesHit, StatType.TotalScore, StatType.EightyPlusAccuracy },
            AccoladeTier.ShowOpener => new[] { StatType.ThreeStarCount, StatType.TotalNotesHit, StatType.TotalScore },
            _ => new[] { StatType.ThreeStarCount, StatType.TotalNotesHit, StatType.TotalScore },
        };

        private static StatRowData ToRow(PlayerStat stat) =>
            new(GetStatLabel(stat.Type), StatFormatter.FormatStatShort(stat), GetStatIcon(stat.Type));

        // ─────────────────────────────────────────────────────────────────────
        //  ACCOLADE ASSIGNMENT — Band-relative unique best-fit
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Assigns a distinct accolade to each profile by greedy best-fit on
        /// band-normalized axis scores, with soft floors.
        /// </summary>
        private AccoladeTier[] AssignAccolades(List<ProfileCampaignStats> profiles)
        {
            int n = profiles?.Count ?? 0;
            var result = new AccoladeTier[n];
            if (n == 0) return result;

            for (int i = 0; i < n; i++)
                result[i] = AccoladeTier.None;

            // Raw axis values per player
            float[,] raw = new float[n, AssignableAccolades.Length];
            bool[,] eligible = new bool[n, AssignableAccolades.Length];

            for (int p = 0; p < n; p++)
            {
                for (int a = 0; a < AssignableAccolades.Length; a++)
                {
                    float value = GetAxisRawValue(profiles[p], AssignableAccolades[a]);
                    raw[p, a] = value;
                    eligible[p, a] = PassesSoftFloor(AssignableAccolades[a], profiles[p]);
                }
            }

            // Band max per axis for normalization
            float[] bandMax = new float[AssignableAccolades.Length];
            for (int a = 0; a < AssignableAccolades.Length; a++)
            {
                float max = 0f;
                for (int p = 0; p < n; p++)
                {
                    if (raw[p, a] > max)
                        max = raw[p, a];
                }
                bandMax[a] = max;
            }

            var remainingPlayers = new HashSet<int>(Enumerable.Range(0, n));
            var remainingAccolades = new HashSet<int>(Enumerable.Range(0, AssignableAccolades.Length));

            // Greedy: best remaining (player, accolade) by normalized fit
            while (remainingPlayers.Count > 0 && remainingAccolades.Count > 0)
            {
                int bestP = -1;
                int bestA = -1;
                float bestFit = -1f;

                foreach (int p in remainingPlayers)
                {
                    foreach (int a in remainingAccolades)
                    {
                        if (!eligible[p, a])
                            continue;
                        if (bandMax[a] <= 0f && AssignableAccolades[a] != AccoladeTier.ShowOpener)
                            continue;

                        float fit = AssignableAccolades[a] == AccoladeTier.ShowOpener
                            ? 0.01f // always claimable last; tiny fit so real axes win first
                            : raw[p, a] / bandMax[a];

                        // Slight prestige bias so elite axes win ties
                        fit += PrestigeBias(AssignableAccolades[a]);

                        if (fit > bestFit)
                        {
                            bestFit = fit;
                            bestP = p;
                            bestA = a;
                        }
                    }
                }

                if (bestP < 0 || bestA < 0)
                    break;

                result[bestP] = AssignableAccolades[bestA];
                remainingPlayers.Remove(bestP);
                remainingAccolades.Remove(bestA);
            }

            // Fallbacks for anyone left unassigned
            int fallbackIndex = 0;
            foreach (int p in remainingPlayers.OrderBy(i => i))
            {
                AccoladeTier fallback = FallbackAccolades[Mathf.Min(fallbackIndex, FallbackAccolades.Length - 1)];
                // Prefer an unused fallback if possible
                while (fallbackIndex < FallbackAccolades.Length &&
                       result.Contains(FallbackAccolades[fallbackIndex]) &&
                       FallbackAccolades[fallbackIndex] != AccoladeTier.ShowOpener)
                {
                    fallbackIndex++;
                    fallback = FallbackAccolades[Mathf.Min(fallbackIndex, FallbackAccolades.Length - 1)];
                }

                result[p] = fallback;
                fallbackIndex++;
            }

            if (_enableDiagnostics)
            {
                for (int i = 0; i < n; i++)
                    Debug.Log($"[CampaignStatsController] Accolade assign: '{profiles[i].PlayerName}' → {result[i]}");
            }

            return result;
        }

        private static float PrestigeBias(AccoladeTier tier) => tier switch
        {
            AccoladeTier.CampaignMVP => 0.05f,
            AccoladeTier.GoldenFingers => 0.04f,
            AccoladeTier.Perfectionist => 0.03f,
            AccoladeTier.PerfectStrings => 0.02f,
            AccoladeTier.ExpertVeteran => 0.015f,
            AccoladeTier.SharpShooter => 0.01f,
            _ => 0f,
        };

        private float GetAxisRawValue(ProfileCampaignStats profile, AccoladeTier accolade)
        {
            return accolade switch
            {
                AccoladeTier.CampaignMVP => GetStatValue(profile, StatType.FullCombos),
                AccoladeTier.GoldenFingers => GetStatValue(profile, StatType.GoldStarSongs),
                AccoladeTier.Perfectionist => GetStatValue(profile, StatType.PerfectAccuracy),
                AccoladeTier.PerfectStrings => GetPerfectStringsAxis(profile),
                AccoladeTier.ExpertVeteran => GetStatValue(profile, StatType.ExpertSongsPlayed),
                AccoladeTier.SharpShooter => GetStatValue(profile, StatType.AverageAccuracy),
                AccoladeTier.RoadWarrior => GetStatValue(profile, StatType.UniqueSongsCleared),
                AccoladeTier.RisingStar => GetRisingStarAxis(profile),
                AccoladeTier.RockEnthusiast => GetStatValue(profile, StatType.UniqueSongsCleared),
                AccoladeTier.ShowOpener => GetStatValue(profile, StatType.UniqueSongsCleared) > 0 ? 1f : 0f,
                _ => 0f,
            };
        }

        private static float GetPerfectStringsAxis(ProfileCampaignStats profile)
        {
            float expertFc = GetStatValue(profile, StatType.ExpertFCs);
            return expertFc > 0f ? expertFc : GetStatValue(profile, StatType.FullCombos);
        }

        private static float GetRisingStarAxis(ProfileCampaignStats profile)
        {
            float fourStar = GetStatValue(profile, StatType.FourStarCount);
            float unique = Mathf.Max(1f, GetStatValue(profile, StatType.UniqueSongsCleared));
            return fourStar / unique;
        }

        private bool PassesSoftFloor(AccoladeTier accolade, ProfileCampaignStats profile)
        {
            return accolade switch
            {
                AccoladeTier.CampaignMVP => GetStatValue(profile, StatType.FullCombos) >= _softFloorFullCombos,
                AccoladeTier.GoldenFingers => GetStatValue(profile, StatType.GoldStarSongs) >= _softFloorGoldStars,
                AccoladeTier.Perfectionist => GetStatValue(profile, StatType.PerfectAccuracy) >= _softFloorPerfectSongs,
                AccoladeTier.PerfectStrings => GetStatValue(profile, StatType.FullCombos) >= _softFloorFullCombos,
                AccoladeTier.ExpertVeteran => GetStatValue(profile, StatType.ExpertSongsPlayed) >= _softFloorExpertSongs,
                AccoladeTier.SharpShooter => GetStatValue(profile, StatType.AverageAccuracy) >= _softFloorAverageAccuracy,
                AccoladeTier.RoadWarrior => GetStatValue(profile, StatType.UniqueSongsCleared) >= _softFloorUniqueSongs,
                AccoladeTier.RisingStar => GetStatValue(profile, StatType.FourStarCount) >= _softFloorFourStar,
                AccoladeTier.RockEnthusiast => GetStatValue(profile, StatType.UniqueSongsCleared) >= _softFloorSessions,
                AccoladeTier.ShowOpener => true,
                _ => false,
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        //  STATIC HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private static float GetStatValue(ProfileCampaignStats profile, StatType type)
        {
            if (profile?.AllStats == null) return 0f;
            var stat = profile.AllStats.FirstOrDefault(s => s.Type == type);
            return stat?.Value ?? 0f;
        }

        private static string GetInstrumentName(int index) => (index % 4) switch
        {
            0 => "GUITAR",
            1 => "BASS",
            2 => "DRUMS",
            3 => "VOCALS",
            _ => "GUITAR",
        };

        private static string GetStatLabel(StatType type) => type switch
        {
            StatType.GoldStarSongs       => "Gold Star Songs:",
            StatType.ExpertFCs           => "Expert FCs:",
            StatType.PerfectAccuracy     => "Perfect Songs:",
            StatType.NearPerfectAccuracy => "99%+ Songs:",
            StatType.FullCombos          => "Full Combos:",

            StatType.HighScore           => "High Score:",
            StatType.FiveStarCount       => "5-Star Songs:",
            StatType.HighAccuracy        => "95%+ Songs:",
            StatType.ExpertSongsPlayed   => "Expert Songs:",
            StatType.FourStarCount       => "4-Star Songs:",

            StatType.AverageAccuracy     => "Accuracy:",
            StatType.LongestStreak       => "Best Streak:",
            StatType.HardPlusSongsPlayed => "Hard+ Songs:",
            StatType.NinetyPlusAccuracy  => "90%+ Songs:",
            StatType.TotalScore          => "Total Score:",
            StatType.ThreeStarCount      => "3-Star Songs:",
            StatType.UniqueSongsCleared  => "Songs Cleared:",

            StatType.SongsPlayed         => "Sessions:",
            StatType.TotalNotesHit       => "Notes Hit:",
            StatType.EightyPlusAccuracy  => "80%+ Songs:",
            StatType.SeventyPlusAccuracy => "70%+ Songs:",
            StatType.MediumSongsPlayed   => "Medium Songs:",
            StatType.EasySongsPlayed     => "Easy Songs:",
            StatType.AverageScore        => "Avg Score:",
            StatType.NotesMissed         => "Notes Missed:",

            _ => "Stat:",
        };

        private static StatRowIconType GetStatIcon(StatType type) => type switch
        {
            StatType.GoldStarSongs       => StatRowIconType.Star,
            StatType.ExpertFCs           => StatRowIconType.Trophy,
            StatType.PerfectAccuracy     => StatRowIconType.Trophy,
            StatType.NearPerfectAccuracy => StatRowIconType.Trophy,
            StatType.FullCombos          => StatRowIconType.Trophy,

            StatType.HighScore           => StatRowIconType.Trophy,
            StatType.FiveStarCount       => StatRowIconType.Star,
            StatType.HighAccuracy        => StatRowIconType.Trophy,
            StatType.ExpertSongsPlayed   => StatRowIconType.MusicNote,
            StatType.FourStarCount       => StatRowIconType.Star,

            StatType.AverageAccuracy     => StatRowIconType.Trophy,
            StatType.LongestStreak       => StatRowIconType.Trophy,
            StatType.HardPlusSongsPlayed => StatRowIconType.MusicNote,
            StatType.NinetyPlusAccuracy  => StatRowIconType.Trophy,
            StatType.TotalScore          => StatRowIconType.Trophy,
            StatType.ThreeStarCount      => StatRowIconType.Star,
            StatType.UniqueSongsCleared  => StatRowIconType.MusicNote,

            StatType.SongsPlayed         => StatRowIconType.MusicNote,
            StatType.TotalNotesHit       => StatRowIconType.MusicNote,
            StatType.EightyPlusAccuracy  => StatRowIconType.Trophy,
            StatType.SeventyPlusAccuracy => StatRowIconType.Trophy,
            StatType.MediumSongsPlayed   => StatRowIconType.MusicNote,
            StatType.EasySongsPlayed     => StatRowIconType.MusicNote,
            StatType.AverageScore        => StatRowIconType.Trophy,
            StatType.NotesMissed         => StatRowIconType.None,

            _ => StatRowIconType.Trophy,
        };

        private void LogMappingDiagnostics(CampaignStatsData data)
        {
            Debug.Log($"[CampaignStatsController] Mapped data for '{data.ScreenTitle}' — " +
                      $"tier={data.Milestone?.Tier}, " +
                      $"players={data.Players?.Count ?? 0}, " +
                      $"bandRows={data.Milestone?.BandStatRows?.Count ?? 0}");

            if (data.Players == null)
                return;

            foreach (var p in data.Players)
            {
                Debug.Log($"  Player '{p.PlayerName}': " +
                          $"frame={p.DiamondFrame}, aura={p.Aura}, " +
                          $"stars={p.StarCount}, accolade='{p.AccoladeTitle}', " +
                          $"statRows={p.Stats?.Count ?? 0}");
            }
        }
    }
}
