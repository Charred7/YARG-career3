using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace YARG.Menu.Career
{
    /// <summary>
    /// Loads Trophy-screen chrome (9-slice panels, Barlow fonts) for Song Match.
    /// </summary>
    public static class SongMatchChrome
    {
        private const string SummaryPanelPath =
            "Assets/Art/CareersM/Trophy/Arts/02_panel_summary_9slice.png";
        private const string InstrumentPanelPath =
            "Assets/Art/CareersM/Trophy/Arts/03_panel_instrument_9slice.png";
        private const string HeaderBandPath =
            "Assets/Art/CareersM/Trophy/Arts/07_header_band_9slicec.png";
        private const string BgPath =
            "Assets/Art/CareersM/Trophy/Arts/01_trophy_screen_bg.png";
        private const string BarlowBlackPath =
            "Assets/Art/Fonts/Barlow/Barlow-Black.asset";
        private const string BarlowLightPath =
            "Assets/Art/Fonts/Barlow/Barlow-Light.asset";

        private static Sprite _summaryPanel;
        private static Sprite _instrumentPanel;
        private static Sprite _headerBand;
        private static Sprite _background;
        private static TMP_FontAsset _barlowBlack;
        private static TMP_FontAsset _barlowLight;
        private static bool _loaded;

        public static Sprite SummaryPanel
        {
            get { EnsureLoaded(); return _summaryPanel; }
        }

        public static Sprite InstrumentPanel
        {
            get { EnsureLoaded(); return _instrumentPanel; }
        }

        public static Sprite HeaderBand
        {
            get { EnsureLoaded(); return _headerBand; }
        }

        public static Sprite Background
        {
            get { EnsureLoaded(); return _background; }
        }

        public static TMP_FontAsset BarlowBlack
        {
            get { EnsureLoaded(); return _barlowBlack; }
        }

        public static TMP_FontAsset BarlowLight
        {
            get { EnsureLoaded(); return _barlowLight; }
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

#if UNITY_EDITOR
            _summaryPanel = AssetDatabase.LoadAssetAtPath<Sprite>(SummaryPanelPath);
            _instrumentPanel = AssetDatabase.LoadAssetAtPath<Sprite>(InstrumentPanelPath);
            _headerBand = AssetDatabase.LoadAssetAtPath<Sprite>(HeaderBandPath);
            _background = AssetDatabase.LoadAssetAtPath<Sprite>(BgPath);
            _barlowBlack = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarlowBlackPath);
            _barlowLight = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BarlowLightPath);
#endif
            _summaryPanel ??= Resources.Load<Sprite>("CareersM/panel_summary");
            _instrumentPanel ??= Resources.Load<Sprite>("CareersM/panel_instrument");
            _headerBand ??= Resources.Load<Sprite>("CareersM/header_band");
            _background ??= Resources.Load<Sprite>("CareersM/song_match_bg");
            _barlowBlack ??= Resources.Load<TMP_FontAsset>("CareersM/Barlow-Black");
            _barlowLight ??= Resources.Load<TMP_FontAsset>("CareersM/Barlow-Light");
        }

        public static void ApplySlicedPanel(Image image, Sprite sprite, float ppuMultiplier = 1.15f)
        {
            if (image == null) return;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = ppuMultiplier;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
                image.color = SongMatchStyle.PanelGlass;
            }
        }

        public static void ApplyTitleFont(TextMeshProUGUI tmp, float size)
        {
            if (tmp == null) return;
            if (BarlowBlack != null) tmp.font = BarlowBlack;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = SongMatchStyle.TextWhite;
            tmp.characterSpacing = 8f;
            tmp.raycastTarget = false;
        }

        public static void ApplyLabelFont(TextMeshProUGUI tmp, float size, Color color)
        {
            if (tmp == null) return;
            if (BarlowLight != null) tmp.font = BarlowLight;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = color;
            tmp.raycastTarget = false;
        }

        public static void ApplyValueFont(TextMeshProUGUI tmp, float size, Color color)
        {
            if (tmp == null) return;
            if (BarlowBlack != null) tmp.font = BarlowBlack;
            tmp.fontSize = size;
            tmp.fontStyle = FontStyles.Normal;
            tmp.color = color;
            tmp.raycastTarget = false;
        }
    }
}
