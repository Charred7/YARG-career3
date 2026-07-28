using TMPro;

using UnityEngine;

using UnityEngine.UI;



namespace YARG.Menu.Career.Trophy

{

    public enum TrophyStatRowStyle

    {

        Summary,

        Card

    }



    public class TrophyStatRow : MonoBehaviour

    {

        [SerializeField] private TextMeshProUGUI _labelMesh;

        [SerializeField] private TextMeshProUGUI _valueMesh;



        private TrophyStatRowStyle _style = TrophyStatRowStyle.Summary;



        public void ConfigureStyle(TrophyStatRowStyle style)

        {

            _style = style;

            ApplyTypography();

        }



        public void Setup(string label, string value, Color valueColor, Color? labelColor = null)

        {

            _labelMesh.text = label.ToUpper();

            _valueMesh.text = value;

            _valueMesh.color = valueColor;

            _labelMesh.color = labelColor ?? (_style == TrophyStatRowStyle.Summary

                ? TrophyLayoutSpec.SummaryLabelWhite

                : TrophyLayoutSpec.CardLabelWhite);

        }



        private void ApplyTypography()

        {

            if (_labelMesh == null || _valueMesh == null)

                return;



            float rowHeight;

            switch (_style)

            {

                case TrophyStatRowStyle.Summary:

                    _labelMesh.fontSize = TrophyLayoutSpec.SummaryStatLabelSize;

                    _valueMesh.fontSize = TrophyLayoutSpec.SummaryStatValueSize;

                    rowHeight = TrophyLayoutSpec.SummaryStatRowMinHeight;

                    break;

                default:

                    _labelMesh.fontSize = TrophyLayoutSpec.CardStatLabelSize;

                    _valueMesh.fontSize = TrophyLayoutSpec.CardStatValueSize;

                    rowHeight = TrophyLayoutSpec.CardStatRowMinHeight;

                    break;

            }



            _labelMesh.fontStyle = FontStyles.Normal;

            _valueMesh.fontStyle = FontStyles.Normal;

            _labelMesh.fontStyle &= ~FontStyles.Italic;

            _valueMesh.fontStyle &= ~FontStyles.Italic;



            _labelMesh.alignment = TextAlignmentOptions.MidlineLeft;

            _valueMesh.alignment = TextAlignmentOptions.MidlineRight;

            _labelMesh.textWrappingMode = TextWrappingModes.NoWrap;

            _valueMesh.textWrappingMode = TextWrappingModes.NoWrap;



            var le = GetComponent<LayoutElement>();

            if (le == null)

                le = gameObject.AddComponent<LayoutElement>();

            le.minHeight = rowHeight;

            le.preferredHeight = rowHeight;

            le.flexibleWidth = 1f;

        }



        private void Reset()

        {

            ConfigureStyle(TrophyStatRowStyle.Summary);

        }

    }

}


