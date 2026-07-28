using UnityEngine;
using YARG.Core.Song;
using YARG.Helpers;
using YARG.Menu.Data;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Library song candidate with color-coded confidence (legacy list + shared helpers).
    /// </summary>
    public class CandidateViewType : ViewType
    {
        public override BackgroundType Background => BackgroundType.Normal;
        public override string StableId => $"Candidate:{_song.Hash}";

        private readonly SongEntry _song;
        private readonly float _confidence;
        private readonly bool _isCurrentMatch;

        public SongEntry Song => _song;
        public float Confidence => _confidence;
        public bool IsCurrentMatch => _isCurrentMatch;

        public CandidateViewType(SongEntry song, float confidence, bool isCurrentMatch)
        {
            _song = song;
            _confidence = confidence;
            _isCurrentMatch = isCurrentMatch;
        }

        public override string GetPrimaryText(bool selected)
        {
            string title = _song.Name.ToString();
            string star = _isCurrentMatch ? " ★" : "";
            string pctText = $"{_confidence:P0}";
            var pctColor = SongMatchStyle.ConfidenceColor(_confidence);
            string coloredPct = TextColorer.StyleString(pctText, pctColor, 600);
            string titleFormatted = FormatAs($"{title}{star}", TextType.Primary, selected);
            return $"{titleFormatted}  {coloredPct}";
        }

        public override string GetSecondaryText(bool selected)
        {
            string artist = _song.Artist.ToString();
            string source = _song.Source.ToString();
            string genre = _song.Genre.ToString();

            if (_confidence <= 0.05f && !string.IsNullOrEmpty(genre))
                return FormatAs($"{artist}  [{source}]  •  {genre}", TextType.Secondary, selected);

            return FormatAs($"{artist}  [{source}]", TextType.Secondary, selected);
        }

        public override Sprite GetIcon() => null;

        public override void PrimaryButtonClick() { }
    }

    public class NoCandidatesViewType : ViewType
    {
        public override BackgroundType Background => BackgroundType.Category;
        public override string StableId => "NoMatches";

        public override string GetPrimaryText(bool selected)
            => FormatAs("No matches found", TextType.Secondary, selected);

        public override string GetSecondaryText(bool selected)
            => FormatAs("Try adjusting the gig data or match manually", TextType.Secondary, selected);

        public override Sprite GetIcon() => null;
    }
}
