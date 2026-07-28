using UnityEngine;
using YARG.Menu.ListMenu;

namespace YARG.Menu.Career
{
    public enum MatchStatusKind
    {
        Open,
        Fuzzy,
        Confirmed
    }

    /// <summary>
    /// Left-column setlist song row — structured fields for rich MatchViewObject layout.
    /// </summary>
    public class MatchSongViewType : ViewType
    {
        public override BackgroundType Background => BackgroundType.Normal;
        public override string StableId => $"MatchSong:{_entry.Key}";

        private readonly MatchEntry _entry;
        public MatchEntry Entry => _entry;

        public string Title { get; }
        public string Artist { get; }
        public MatchStatusKind StatusKind { get; }
        public Color StatusColor { get; }
        public string StatusGlyph { get; }
        public string MethodBadge { get; }
        public Color MethodBadgeColor { get; }

        public MatchSongViewType(MatchEntry entry)
        {
            _entry = entry;
            Title = entry.GigSong.title ?? "(unknown)";
            Artist = entry.GigSong.artist ?? "";

            var match = entry.Match;
            if (match == null || !match.IsResolved)
            {
                StatusKind = MatchStatusKind.Open;
                StatusColor = SongMatchStyle.StatusOpen;
                StatusGlyph = "○";
                MethodBadge = null;
                MethodBadgeColor = SongMatchStyle.BadgeDefault;
                return;
            }

            switch (match.method)
            {
                case MatchMethod.Fuzzy:
                    StatusKind = MatchStatusKind.Fuzzy;
                    StatusColor = SongMatchStyle.StatusFuzzy;
                    StatusGlyph = "◑";
                    MethodBadge = "FUZZY";
                    MethodBadgeColor = SongMatchStyle.StatusFuzzy;
                    break;
                case MatchMethod.Manual:
                    StatusKind = MatchStatusKind.Confirmed;
                    StatusColor = SongMatchStyle.AccentSelect;
                    StatusGlyph = "●";
                    MethodBadge = "MANUAL";
                    MethodBadgeColor = SongMatchStyle.BadgeYarg;
                    break;
                case MatchMethod.Hash:
                    StatusKind = MatchStatusKind.Confirmed;
                    StatusColor = SongMatchStyle.AccentSelect;
                    StatusGlyph = "●";
                    MethodBadge = "HASH";
                    MethodBadgeColor = SongMatchStyle.BadgeYarg;
                    break;
                case MatchMethod.ExactTitleArtist:
                case MatchMethod.ExactTitleArtistSource:
                    StatusKind = MatchStatusKind.Confirmed;
                    StatusColor = SongMatchStyle.AccentSelect;
                    StatusGlyph = "●";
                    MethodBadge = "EXACT";
                    MethodBadgeColor = SongMatchStyle.BadgeRb;
                    break;
                default:
                    StatusKind = MatchStatusKind.Open;
                    StatusColor = SongMatchStyle.StatusOpen;
                    StatusGlyph = "○";
                    MethodBadge = null;
                    MethodBadgeColor = SongMatchStyle.BadgeDefault;
                    break;
            }
        }

        public override string GetPrimaryText(bool selected)
        {
            // Fallback for unmigrated prefabs
            return $"<color={SongMatchStyle.Hex(StatusColor)}>{StatusGlyph}</color>  {Title}";
        }

        public override string GetSecondaryText(bool selected) => Artist;

        public override Sprite GetIcon() => null;

        public override void PrimaryButtonClick() { }
    }

    /// <summary>
    /// Soft section header in the left setlist (NEEDS MATCH / MATCHED).
    /// </summary>
    public class MatchSectionViewType : ViewType
    {
        public override BackgroundType Background => BackgroundType.Category;
        public override string StableId => $"MatchSection:{_title}";

        private readonly string _title;
        public string SectionTitle => _title;

        public MatchSectionViewType(string title)
        {
            _title = title ?? "";
        }

        public override string GetPrimaryText(bool selected) => _title;

        public override string GetSecondaryText(bool selected) => string.Empty;

        public override Sprite GetIcon() => null;

        public override void PrimaryButtonClick() { }
    }

    public class MatchEntry
    {
        public GigSong GigSong;
        public SongMatch Match;
        public string Key;
    }
}
