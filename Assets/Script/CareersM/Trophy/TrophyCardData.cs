using System.Collections.Generic;
using UnityEngine;

namespace YARG.Menu.Career.Trophy
{
    public enum StatColumn
    {
        LeftFlex,
        RightConsistency
    }

    [System.Serializable]
    public struct TrophyStatEntry
    {
        public string Label;
        public string Value;
        public StatColumn TargetColumn;

        public TrophyStatEntry(string label, string value, StatColumn column)
        {
            Label = label;
            Value = value;
            TargetColumn = column;
        }
    }

    public class TrophyCardData
    {
        public string InstrumentLabel;
        public string AccoladeTitle;
        public Color ThemeColor;
        public Sprite WatermarkSprite;
        public int StarCount;
        public int InstrumentIndex;
        /// <summary>Formatted high score shown in the bottom anchor row. Empty hides the anchor.</summary>
        public string HighScoreValue;
        /// <summary>Song the high score was earned on, shown as a subtitle under the value.</summary>
        public string HighScoreSong;
        public List<TrophyStatEntry> Stats = new();
    }
}
