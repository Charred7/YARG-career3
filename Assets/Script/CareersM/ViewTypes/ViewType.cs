using YARG.Menu.ListMenu;

namespace YARG.Menu.Career
{
    public abstract class ViewType : BaseViewType
    {
        public abstract string StableId { get; }

        public override string GetSecondaryText(bool selected) => string.Empty;

        public virtual void PrimaryButtonClick()
        {
        }

        public override void IconClick()
        {
            PrimaryButtonClick();
        }
    }
}