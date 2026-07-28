using UnityEngine;



namespace YARG.Menu.Career.Trophy

{

    [CreateAssetMenu(fileName = "TrophyScreenArt", menuName = "YARG/Careers/Trophy Screen Art")]

    public class TrophyScreenArt : ScriptableObject

    {

        [Header("Screen")]

        public Sprite Background;



        [Header("Summary Card")]

        public Sprite SummaryPanel;

        public Sprite GoldenVinylEmblem;

        public bool EmblemHasBakedBannerText = true;



        [Header("Instrument Card")]

        public Sprite InstrumentPanel;

        public Sprite HeaderBand;

        public Sprite CardBottomGlow;



        [Header("Header Icons (optional — procedural fallback when null)")]

        public Sprite GuitarHeaderIcon;

        public Sprite BassHeaderIcon;

        public Sprite DrumsHeaderIcon;

        public Sprite VocalsHeaderIcon;



        [Header("Stars")]

        public Sprite StarFilled;

        public Sprite StarEmpty;



        [Header("Watermarks")]

        public Sprite GuitarWatermark;

        public Sprite BassWatermark;

        public Sprite DrumsWatermark;

        public Sprite VocalsWatermark;



        public Sprite GetWatermark(int instrumentIndex) => (instrumentIndex % 4) switch

        {

            0 => GuitarWatermark,

            1 => BassWatermark,

            2 => DrumsWatermark ?? GuitarWatermark,

            3 => VocalsWatermark ?? GuitarWatermark,

            _ => GuitarWatermark

        };



        public Sprite GetHeaderIcon(int instrumentIndex) => (instrumentIndex % 4) switch

        {

            0 => GuitarHeaderIcon,

            1 => BassHeaderIcon,

            2 => DrumsHeaderIcon,

            3 => VocalsHeaderIcon,

            _ => GuitarHeaderIcon

        };

    }

}


