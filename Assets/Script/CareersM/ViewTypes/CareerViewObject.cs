using TMPro;
using UnityEngine;
using YARG.Menu.Career.Motivation;
using YARG.Menu.ListMenu;

namespace YARG.Menu.Career
{
    public class CareerViewObject : ViewObject<CareerViewType>
    {
        /// <summary>
        /// Called when the view object is shown/updated.
        /// View objects are POOLED and REUSED for different campaigns.
        /// Always removes old gold shine first, then only re-adds
        /// if THIS SPECIFIC campaign is completed.
        /// </summary>
        public override void Show(bool selected, CareerViewType viewType)
        {
            // CRITICAL: Remove any existing gold shine effects from previous assignments
            // Since view objects are pooled, a component might persist from a completed campaign
            // that was previously shown in this same view object slot.
            RemoveGoldShineEffects();

            base.Show(selected, viewType);

            // Only add gold shine if THIS specific campaign item is completed
            AddGoldShineIfCompleted();
        }

        /// <summary>
        /// Removes ALL GoldShineTextEffect components from children.
        /// Called before every Show() to clean up from previous pool assignments.
        /// </summary>
        private void RemoveGoldShineEffects()
        {
            var existingShines = GetComponentsInChildren<GoldShineTextEffect>(true);
            foreach (var shine in existingShines)
            {
                Destroy(shine);
            }
        }

        /// <summary>
        /// Checks if the current view item represents a completed campaign
        /// and adds the gold shine text effect to its primary text.
        /// Only applies to the specific completed campaign, not all visible items.
        /// </summary>
        private void AddGoldShineIfCompleted()
        {
            if (ViewType?.Career == null) return;

            // Check if THIS SPECIFIC campaign is fully completed (per-view-item, not global)
            if (!ViewType.IsFullyCompleted()) return;

            // Find text components in this view object
            var textComponents = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in textComponents)
            {
                // Match the text that contains the campaign name
                if (tmp.text.Contains(ViewType.Career.name))
                {
                    // Strip rich text color tags — the base Show() wrapped the name in
                    // <color=#...><font-weight=...> tags via FormatAs(). These tags override
                    // _textComponent.color and conflict with GoldShineTextEffect's color management.
                    // Since GoldShineTextEffect handles all coloring, we use the plain name.
                    tmp.text = ViewType.Career.name;

                    // Add gold shine effect for AAA-quality polish
                    var shine = tmp.gameObject.AddComponent<GoldShineTextEffect>();
                    shine.ShineInterval = 2.5f;
                    shine.ShineDuration = 1.0f;
                    shine.SetBaseColor(new Color(1.0f, 0.84f, 0.0f)); // Gold
                    shine.SetShineColor(new Color(1.0f, 0.98f, 0.7f)); // Bright shine
                    break;
                }
            }
        }
    }
}
