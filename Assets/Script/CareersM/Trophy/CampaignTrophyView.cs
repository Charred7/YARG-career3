using TMPro;
using UnityEngine;
using YARG.Menu.Career.Motivation;

namespace YARG.Menu.Career.Trophy
{
    public class CampaignTrophyView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private CampaignSummaryCard _summaryCard;
        [SerializeField] private TrophyCardCarousel _carousel;
        [SerializeField] private TrophyScreenArt _art;

        public TrophyCardCarousel Carousel => _carousel;

        public void Initialize(TextMeshProUGUI titleText, CampaignSummaryCard summaryCard,
            TrophyCardCarousel carousel, TrophyScreenArt art = null)
        {
            _titleText = titleText;
            _summaryCard = summaryCard;
            _carousel = carousel;
            _art = art;
        }

        public void Populate(CampaignStatsData data)
        {
            if (data == null)
                return;

            if (_titleText != null)
                _titleText.text = data.ScreenTitle;

            _summaryCard?.ConfigureArt(_art);
            _summaryCard?.Populate(data.Milestone);

            var cards = TrophyDataMapper.MapPlayerCards(data, _art);
            if (cards.Count == 0)
                Debug.LogWarning($"[CampaignTrophyView] No instrument cards for career '{data.CareerId}'.");

            _carousel?.Populate(cards);
        }
    }
}
