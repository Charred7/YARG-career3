using System.Collections.Generic;
using YARG.Menu.ListMenu;

namespace YARG.Menu.Career
{
    /// <summary>
    /// Left-column list for career songs with match status.
    /// </summary>
    public class MatchSongListMenu : ListMenu.ListMenu<ViewType, MatchViewObject>
    {
        protected override int ExtraListViewPadding => 10;

        private List<ViewType> _externalViewList = new();

        public void SetViewList(List<ViewType> viewList)
        {
            _externalViewList = viewList;
            RequestViewListUpdate();
        }

        protected override List<ViewType> CreateViewList() => _externalViewList;
    }
}