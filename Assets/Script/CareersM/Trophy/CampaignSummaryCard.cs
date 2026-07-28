using System.Collections.Generic;

using TMPro;

using UnityEngine;

using UnityEngine.UI;

using YARG.Menu.Career.Motivation;



namespace YARG.Menu.Career.Trophy

{

    public class CampaignSummaryCard : MonoBehaviour

    {

        [Header("Badge")]

        [SerializeField] private Image _vinylBadge;

        [SerializeField] private Image _vinylGlow;

        [SerializeField] private TextMeshProUGUI _completionBannerText;

        [SerializeField] private TextMeshProUGUI _subLabelText;



        [Header("Stats")]

        [SerializeField] private Transform _statRowContainer;

        [SerializeField] private TrophyStatRow _statRowPrefab;



        [Header("Top Songs")]

        [SerializeField] private GameObject _topSongsSection;

        [SerializeField] private TextMeshProUGUI _topSongsHeader;

        [SerializeField] private Transform _topSongsContainer;



        private readonly List<TrophyStatRow> _statPool = new();

        private readonly List<TrophyStatRow> _topSongPool = new();

        private TrophyScreenArt _art;

        private bool _frameStyled;



        public void ConfigureArt(TrophyScreenArt art)

        {

            _art = art;

            EnsureFrameStyle();

        }



        public void Initialize(Image vinylBadge, Image vinylGlow, TextMeshProUGUI completionBannerText,

            TextMeshProUGUI subLabelText, Transform statRowContainer, TrophyStatRow statRowPrefab)

        {

            _vinylBadge = vinylBadge;

            _vinylGlow = vinylGlow;

            _completionBannerText = completionBannerText;

            _subLabelText = subLabelText;

            _statRowContainer = statRowContainer;

            _statRowPrefab = statRowPrefab;

        }



        public void Populate(MilestoneData milestone)

        {

            if (milestone == null)

                return;



            EnsureFrameStyle();



            _completionBannerText.text = milestone.CompletionBannerText;

            _subLabelText.text = string.IsNullOrEmpty(milestone.SubLabel)

                ? milestone.BadgeTitle

                : milestone.SubLabel;



            bool showVinyl = milestone.Tier == MilestoneTier.GoldenVinyl;

            _vinylBadge.gameObject.SetActive(showVinyl);

            if (showVinyl && _art?.GoldenVinylEmblem != null)

                _vinylBadge.color = TrophyLayoutSpec.EmblemWarmTint;



            if (_vinylGlow != null)

            {

                _vinylGlow.gameObject.SetActive(showVinyl);

                if (showVinyl)

                {

                    _vinylGlow.color = TrophyLayoutSpec.EmblemGlowColor;

                    var glowRect = _vinylGlow.rectTransform;

                    glowRect.sizeDelta = TrophyLayoutSpec.SummaryEmblemSize * 1.08f;

                }

            }



            bool hideBannerText = _art != null && _art.EmblemHasBakedBannerText && _art.GoldenVinylEmblem != null;

            if (_completionBannerText != null)

                _completionBannerText.gameObject.SetActive(!hideBannerText);

            if (_subLabelText != null)

                _subLabelText.gameObject.SetActive(!hideBannerText);



            _statPool.ForEach(row => row.gameObject.SetActive(false));



            if (milestone.BandStatRows == null)

                return;



            foreach (var stat in milestone.BandStatRows)

            {

                var row = GetOrCreateStatRow();

                string label = stat.Label?.TrimEnd(':', ' ') ?? string.Empty;

                row.ConfigureStyle(TrophyStatRowStyle.Summary);

                row.Setup(label, stat.Value, TrophyLayoutSpec.ValueGold, TrophyLayoutSpec.SummaryLabelWhite);

                row.gameObject.SetActive(true);

            }



            PopulateTopSongs(milestone.TopSongs);

        }



        private void PopulateTopSongs(List<TopSongData> topSongs)

        {

            _topSongPool.ForEach(row => row.gameObject.SetActive(false));



            bool hasSongs = topSongs != null && topSongs.Count > 0;

            if (_topSongsSection != null)

                _topSongsSection.SetActive(hasSongs);



            if (!hasSongs || _topSongsContainer == null || _statRowPrefab == null)

                return;



            foreach (var song in topSongs)

            {

                var row = GetOrCreateTopSongRow();

                string label = $"{song.Rank}. {song.SongName}";

                row.ConfigureStyle(TrophyStatRowStyle.Summary);

                row.Setup(label, song.ScoreText, TrophyLayoutSpec.ValueGold, TrophyLayoutSpec.TopSongLabelColor);

                row.gameObject.SetActive(true);

            }

        }



        private TrophyStatRow GetOrCreateTopSongRow()

        {

            foreach (var row in _topSongPool)

            {

                if (!row.gameObject.activeSelf)

                    return row;

            }



            var newRow = Instantiate(_statRowPrefab, _topSongsContainer, false);

            _topSongPool.Add(newRow);

            return newRow;

        }



        private void EnsureFrameStyle()

        {

            if (_frameStyled)

                return;



            _frameStyled = true;

            var frame = GetComponent<Image>();

            if (frame == null)

                return;



            if (frame.GetComponent<Outline>() == null)

            {

                var outline = frame.gameObject.AddComponent<Outline>();

                outline.effectColor = TrophyLayoutSpec.ValueGold;

                outline.effectDistance = new Vector2(TrophyLayoutSpec.SummaryOutlineWidth, -TrophyLayoutSpec.SummaryOutlineWidth);

            }



            if (frame.GetComponent<Shadow>() == null)

            {

                var shadow = frame.gameObject.AddComponent<Shadow>();

                shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);

                shadow.effectDistance = new Vector2(0f, -6f);

            }

        }



        private TrophyStatRow GetOrCreateStatRow()

        {

            foreach (var row in _statPool)

            {

                if (!row.gameObject.activeSelf)

                    return row;

            }



            var newRow = Instantiate(_statRowPrefab, _statRowContainer, false);

            _statPool.Add(newRow);

            return newRow;

        }

    }

}


