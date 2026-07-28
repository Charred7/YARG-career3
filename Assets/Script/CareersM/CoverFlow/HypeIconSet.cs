using System;
using UnityEngine;

namespace YARG.Menu.Career.CoverFlow
{
    /// <summary>
    /// Maps icon names (from hypeengine.ini icon field) to Sprite assets.
    /// Create one instance via Assets → Create → YARG → Hype Icon Set,
    /// then run "Generate Icon Sprites (PNG)" from Tools → YARG → Build Hype Engine Sprite Asset
    /// and drag the resulting PNGs into the sprite fields.
    ///
    /// Usage:
    ///   _iconSet.GetSprite("crown") → the crown Sprite
    /// </summary>
    [CreateAssetMenu(menuName = "YARG/Hype Icon Set", fileName = "HypeIconSet")]
    public class HypeIconSet : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            /// <summary>Icon name matching hypeengine.ini icon values.</summary>
            public string name;

            /// <summary>
            /// The Sprite asset. Generate PNGs via Tools → YARG → Build Hype Engine Sprite Asset
            /// → "Generate Icon Sprites (PNG)", then drag the PNGs from the sprites/ folder here.
            /// </summary>
            public Sprite sprite;
        }

        [Tooltip("Icon name → Sprite mappings. Names must match icon values in hypeengine.ini.")]
        public Entry[] icons = Array.Empty<Entry>();

        /// <summary>
        /// Looks up a sprite by icon name. Returns null if not found.
        /// Case-insensitive, trims whitespace.
        /// </summary>
        public Sprite GetSprite(string name)
        {
            if (string.IsNullOrEmpty(name) || icons == null)
                return null;

            foreach (var entry in icons)
            {
                if (string.Equals(entry.name?.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase))
                    return entry.sprite;
            }

            return null;
        }
    }
}