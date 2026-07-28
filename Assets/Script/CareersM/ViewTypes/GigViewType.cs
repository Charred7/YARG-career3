using System;
using UnityEngine;
using YARG.Menu.ListMenu;

namespace YARG.Menu.Career
{
    public class GigViewType : ViewType
    {
        public override BackgroundType Background => BackgroundType.Normal;
        public override string StableId => $"Gig:{_careerId}:{_gig.name}";

        private readonly string _careerId;
        private readonly GigInfo _gig;
        private readonly bool _isCompleted;
        private readonly Action<string, GigInfo> _onSelect;

        public GigInfo Gig => _gig;
        public bool IsCompleted => _isCompleted;

        public GigViewType(string careerId, GigInfo gig, bool isCompleted, Action<string, GigInfo> onSelect)
        {
            _careerId = careerId;
            _gig = gig;
            _isCompleted = isCompleted;
            _onSelect = onSelect;
        }

        public override string GetPrimaryText(bool selected)
        {
            string text = _gig.name;

            // Add completion indicator
            if (_isCompleted)
            {
                text = "✓ " + text;
            }

            return FormatAs(text, _isCompleted ? TextType.Bright : TextType.Primary, selected);
        }

        public override string GetSecondaryText(bool selected)
        {
            // Show song count and encore indicator
            int songCount = _gig.songs?.Count ?? 0;
            string info = $"{songCount} SONG{(songCount != 1 ? "S" : "")}";

            if (_gig.HasEncore)
            {
                info += " + ENCORE";
            }

            return FormatAs(info, TextType.Secondary, selected);
        }

        public override Sprite GetIcon()
        {
            // Could return a gig-specific icon or status icon here
            return null;
        }

        public override void PrimaryButtonClick()
        {
            _onSelect?.Invoke(_careerId, _gig);
        }
    }
}