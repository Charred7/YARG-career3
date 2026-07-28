using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace YARG.Menu.ListMenu
{
    public class ViewObject<TViewType> : MonoBehaviour
        where TViewType : BaseViewType
    {
        [SerializeField]
        private CanvasGroup _canvasGroup;

        [Space]
        [SerializeField]
        protected GameObject NormalBackground;
        [SerializeField]
        protected GameObject SelectedBackground;
        [SerializeField]
        protected GameObject CategoryBackground;

        [Space]
        [SerializeField]
        private Image _icon;
        [SerializeField]
        private List<TextMeshProUGUI> _primaryText;
        [SerializeField]
        private List<TextMeshProUGUI> _secondaryText;

        protected bool Showing { get; private set; }

        protected TViewType ViewType;

        public virtual void Show(bool selected, TViewType viewType)
        {
            Showing = true;
            ViewType = viewType;

            // Set background
            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
            SetBackground(selected, viewType.Background);

            // Set text
            if (_primaryText != null)
            {
                foreach (var i in _primaryText)
                {
                    if (i != null)
                        i.text = viewType.GetPrimaryText(selected);
                }
            }
            if (_secondaryText != null)
            {
                foreach (var i in _secondaryText)
                {
                    if (i != null)
                        i.text = viewType.GetSecondaryText(selected);
                }
            }

            if (_icon != null)
            {
                _icon.sprite = viewType.GetIcon();
                _icon.gameObject.SetActive(_icon.sprite != null);
            }
        }

        public virtual void Hide()
        {
            Showing = false;
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;
        }

        protected virtual void SetBackground(bool selected, BaseViewType.BackgroundType type)
        {
            if (NormalBackground != null) NormalBackground.SetActive(false);
            if (SelectedBackground != null) SelectedBackground.SetActive(false);
            if (CategoryBackground != null) CategoryBackground.SetActive(false);

            switch (type)
            {
                case BaseViewType.BackgroundType.Normal:
                    if (selected)
                    {
                        if (SelectedBackground != null)
                            SelectedBackground.SetActive(true);
                    }
                    else
                    {
                        if (NormalBackground != null)
                            NormalBackground.SetActive(true);
                    }

                    break;
                case BaseViewType.BackgroundType.Category:
                    if (selected)
                    {
                        if (SelectedBackground != null)
                            SelectedBackground.SetActive(true);
                    }
                    else
                    {
                        if (CategoryBackground != null)
                            CategoryBackground.SetActive(true);
                    }

                    break;
            }
        }

        public void IconClick()
        {
            ViewType.IconClick();
        }
    }
}