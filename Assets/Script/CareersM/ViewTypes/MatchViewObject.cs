using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YARG.Menu.ListMenu;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Premium setlist row / section header for Song Match left list.
    /// </summary>
    public class MatchViewObject : ViewObject<ViewType>
    {
        [Header("Song Row")]
        [SerializeField] private GameObject _songRoot;
        [SerializeField] private Image _borderOuter;
        [SerializeField] private Image _borderInner;
        [SerializeField] private Image _fill;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _artistText;
        [SerializeField] private Image _badgeBg;
        [SerializeField] private TextMeshProUGUI _badgeText;

        [Header("Section")]
        [SerializeField] private GameObject _sectionRoot;
        [SerializeField] private TextMeshProUGUI _sectionText;

        public override void Show(bool selected, ViewType viewType)
        {
            base.Show(selected, viewType);

            if (viewType is MatchSongViewType song)
            {
                ShowSong(song, selected);
                return;
            }

            if (viewType is MatchSectionViewType section)
            {
                ShowSection(section);
                return;
            }

            // Unknown view — collapse premium chrome
            SetSongChromeVisible(false);
            SetSectionVisible(false);
        }

        private void ShowSong(MatchSongViewType song, bool selected)
        {
            SetSectionVisible(false);
            SetSongChromeVisible(true);

            if (_statusText != null)
            {
                _statusText.text = $"<color={SongMatchStyle.Hex(song.StatusColor)}>{song.StatusGlyph}</color>";
            }

            if (_titleText != null)
            {
                _titleText.text = song.Title;
                SongMatchChrome.ApplyTitleFont(_titleText, 15f);
                _titleText.color = SongMatchStyle.TextWhite;
                _titleText.characterSpacing = 0f;
            }

            if (_artistText != null)
            {
                _artistText.text = song.Artist;
                SongMatchChrome.ApplyLabelFont(_artistText, 12f, SongMatchStyle.LabelMuted);
            }

            bool hasBadge = !string.IsNullOrEmpty(song.MethodBadge);
            if (_badgeBg != null)
            {
                _badgeBg.gameObject.SetActive(hasBadge);
                if (hasBadge)
                    _badgeBg.color = song.MethodBadgeColor;
            }
            if (_badgeText != null)
            {
                _badgeText.gameObject.SetActive(hasBadge);
                if (hasBadge)
                {
                    _badgeText.text = song.MethodBadge;
                    SongMatchChrome.ApplyValueFont(_badgeText, 10f, Color.white);
                    _badgeText.alignment = TextAlignmentOptions.Center;
                }
            }

            if (_fill != null)
            {
                _fill.color = selected
                    ? new Color(0.09f, 0.11f, 0.14f, 1f)
                    : new Color(0.10f, 0.10f, 0.12f, 0.92f);
            }

            SetDoubleBorder(selected);

            // Prefer our cyan frame over stock gold selected wash
            if (NormalBackground != null)
                NormalBackground.SetActive(!selected);
            if (SelectedBackground != null)
                SelectedBackground.SetActive(false);
            if (CategoryBackground != null)
                CategoryBackground.SetActive(false);
        }

        private void ShowSection(MatchSectionViewType section)
        {
            SetSongChromeVisible(false);
            SetSectionVisible(true);
            SetDoubleBorder(false);

            if (_sectionText != null)
            {
                _sectionText.text = section.SectionTitle;
                SongMatchChrome.ApplyTitleFont(_sectionText, 12f);
                _sectionText.color = SongMatchStyle.Gold;
                _sectionText.characterSpacing = 8f;
            }

            if (NormalBackground != null)
                NormalBackground.SetActive(false);
            if (SelectedBackground != null)
                SelectedBackground.SetActive(false);
            if (CategoryBackground != null)
                CategoryBackground.SetActive(true);
        }

        private void SetSongChromeVisible(bool visible)
        {
            if (_songRoot != null)
                _songRoot.SetActive(visible);
            else
            {
                if (_statusText != null) _statusText.gameObject.SetActive(visible);
                if (_titleText != null) _titleText.gameObject.SetActive(visible);
                if (_artistText != null) _artistText.gameObject.SetActive(visible);
                if (_badgeBg != null) _badgeBg.gameObject.SetActive(visible);
                if (_fill != null) _fill.gameObject.SetActive(visible);
            }
        }

        private void SetSectionVisible(bool visible)
        {
            if (_sectionRoot != null)
                _sectionRoot.SetActive(visible);
            else if (_sectionText != null)
                _sectionText.gameObject.SetActive(visible);
        }

        private void SetDoubleBorder(bool selected)
        {
            if (_borderOuter != null)
            {
                _borderOuter.enabled = selected;
                _borderOuter.color = selected
                    ? new Color(SongMatchStyle.AccentSelect.r, SongMatchStyle.AccentSelect.g,
                        SongMatchStyle.AccentSelect.b, 0.95f)
                    : Color.clear;
            }

            if (_borderInner != null)
            {
                _borderInner.enabled = selected;
                _borderInner.color = selected
                    ? new Color(SongMatchStyle.AccentSelect.r, SongMatchStyle.AccentSelect.g,
                        SongMatchStyle.AccentSelect.b, 0.7f)
                    : Color.clear;
            }
        }
    }
}
