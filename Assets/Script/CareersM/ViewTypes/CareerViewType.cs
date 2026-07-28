using System;
using UnityEngine;
using YARG.Menu.ListMenu;
using YARG.Song;

namespace YARG.Menu.Career
{
    public class CareerViewType : ViewType
    {
        public override BackgroundType Background => BackgroundType.Normal;
        public override string StableId => $"Career:{_career.id}";

        private readonly CareerInfo _career;
        private readonly Action<CareerInfo> _onSelect;
        private Sprite _artwork;
        private Sprite _sourceIcon;
        private bool _artworkLoaded;
        private bool _sourceIconLoaded;

        public CareerInfo Career => _career;

        public CareerViewType(CareerInfo career, Action<CareerInfo> onSelect)
        {
            _career = career;
            _onSelect = onSelect;
            _artworkLoaded = false;
            _sourceIconLoaded = false;
        }

        public override string GetPrimaryText(bool selected)
        {
            // NO text prefix for completed campaigns - the gold shine effect
            // on the view object visually distinguishes completed campaigns.
            // Adding text here caused layout overlaps with the source icon.
            return FormatAs(_career.name, TextType.Primary, selected);
        }

        /// <summary>
        /// Returns true if all gigs in this campaign are marked completed.
        /// Public so CareerViewObject can check per-view-item completion.
        /// </summary>
        public bool IsFullyCompleted()
        {
            var band = CareerManager.Instance.CurrentBand;
            if (band == null) return false;

            var gigs = CareerManager.Instance.GetGigs(_career.id);
            if (gigs == null || gigs.Count == 0) return false;

            foreach (var gig in gigs)
            {
                string gigId = $"{_career.id}|{gig.name}";
                if (!CareerManager.Instance.IsGigCompleted(gigId))
                    return false;
            }
            return true;
        }

        public override string GetSecondaryText(bool selected)
        {
            // Get progress from CareerManager
            var band = CareerManager.Instance.CurrentBand;
            if (band == null)
            {
                return string.Empty;
            }

            var gigs = CareerManager.Instance.GetGigs(_career.id);
            if (gigs == null || gigs.Count == 0)
            {
                return FormatAs("No gigs", TextType.Bright, selected);
            }

            int completedCount = 0;
            foreach (var gig in gigs)
            {
                string gigId = $"{_career.id}|{gig.name}";
                if (CareerManager.Instance.IsGigCompleted(gigId))
                {
                    completedCount++;
                }
            }

            // Calculate percentage
            float percentage = (float)completedCount / gigs.Count * 100f;
            
            // Format: "X/Y GIGS (Z%)" with completion badge (text only, icons rendered separately)
            string progress = $"{completedCount}/{gigs.Count} GIGS ({percentage:F0}%)";
            if (completedCount >= gigs.Count && gigs.Count > 0)
                progress = $"{progress}  CONQUERED!";
            return FormatAs(progress, TextType.Bright, selected);
        }

        public override Sprite GetIcon()
        {
            // Load source icon if not already loaded
            if (!_sourceIconLoaded && !string.IsNullOrEmpty(_career.sourceIcon))
            {
                _sourceIconLoaded = true;
                _sourceIcon = SongSources.SourceToIcon(_career.sourceIcon);
            }
            
            return _sourceIcon;
        }

        public Sprite GetArtwork()
        {
            return _artwork;
        }

        public bool IsArtworkLoaded()
        {
            if (_artworkLoaded)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_career.artworkPath))
            {
                _artworkLoaded = true;
                return false;
            }

            // NEW: Load from AppData instead of StreamingAssets
            string careerDirectory = System.IO.Path.Combine(YARG.Helpers.PathHelper.PersistentDataPath, "career");
            string fullPath = System.IO.Path.Combine(careerDirectory, _career.artworkPath);
            
            if (System.IO.File.Exists(fullPath))
            {
                byte[] fileData = System.IO.File.ReadAllBytes(fullPath);
                Texture2D texture = new Texture2D(2, 2);
                if (texture.LoadImage(fileData))
                {
                    _artwork = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    _artworkLoaded = true;
                    return true;
                }
            }

            _artworkLoaded = true;
            return false;
        }

        public override void PrimaryButtonClick()
        {
            _onSelect?.Invoke(_career);
        }

    }
}