using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YARG.Menu.Career.Trophy
{
    public class TrophyCardCarousel : MonoBehaviour
    {
        [SerializeField] private Transform _cardContainer;
        [SerializeField] private RectTransform _viewport;
        [SerializeField] private PlayerTrophyCard _cardPrefab;
        [SerializeField] private TrophyScreenArt _art;

        private readonly List<PlayerTrophyCard> _cards = new();
        private int _focusedIndex;
        private Coroutine _scrollCoroutine;

        public void Initialize(Transform cardContainer, PlayerTrophyCard cardPrefab, TrophyScreenArt art = null,
            RectTransform viewport = null)
        {
            _cardContainer = cardContainer;
            _cardPrefab = cardPrefab;
            _art = art;
            if (viewport != null)
                _viewport = viewport;
            else if (_viewport == null)
                _viewport = transform.Find("Viewport") as RectTransform;
        }

        public int CardCount => _cards.Count;
        public int FocusedIndex => _focusedIndex;

        public void Populate(IReadOnlyList<TrophyCardData> cardData)
        {
            ClearCards();

            if (_cardPrefab == null)
            {
                Debug.LogError("[TrophyCardCarousel] Card prefab is not assigned.");
                return;
            }

            if (cardData == null || cardData.Count == 0)
            {
                Debug.LogWarning("[TrophyCardCarousel] No player card data to display.");
                return;
            }

            EnsureLayoutReady();

            foreach (var data in cardData)
            {
                var card = Instantiate(_cardPrefab, _cardContainer, false);
                var le = card.GetComponent<LayoutElement>();
                if (le == null)
                    le = card.gameObject.AddComponent<LayoutElement>();
                le.preferredWidth = TrophyLayoutSpec.InstrumentCardSize.x;
                le.preferredHeight = TrophyLayoutSpec.InstrumentCardSize.y;
                le.flexibleWidth = 0f;
                le.flexibleHeight = 0f;
                le.minWidth = TrophyLayoutSpec.InstrumentCardSize.x;
                le.minHeight = TrophyLayoutSpec.InstrumentCardSize.y;

                card.ApplyArt(_art);
                card.PopulateCard(data);
                _cards.Add(card);
            }

            if (_cardContainer is RectTransform containerRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

            _focusedIndex = 0;
            RefreshFocus(immediate: true);
        }

        private void EnsureLayoutReady()
        {
            Canvas.ForceUpdateCanvases();

            if (_viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_viewport);

            var carouselRect = transform as RectTransform;
            if (carouselRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(carouselRect);
        }

        public void Step(int direction)
        {
            if (_cards.Count <= 1)
                return;

            _focusedIndex = Mathf.Clamp(_focusedIndex + direction, 0, _cards.Count - 1);
            RefreshFocus(immediate: false);
        }

        private void RefreshFocus(bool immediate)
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].SetFocusState(i == _focusedIndex);

            if (_cardContainer is not RectTransform containerRect)
                return;

            if (_viewport != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);

            float targetX = ComputeScrollOffsetX();

            if (immediate || !isActiveAndEnabled)
            {
                if (_scrollCoroutine != null)
                {
                    StopCoroutine(_scrollCoroutine);
                    _scrollCoroutine = null;
                }

                var pos = containerRect.anchoredPosition;
                pos.x = targetX;
                containerRect.anchoredPosition = pos;
                return;
            }

            if (_scrollCoroutine != null)
                StopCoroutine(_scrollCoroutine);
            _scrollCoroutine = StartCoroutine(ScrollToX(containerRect, targetX));
        }

        private float ComputeScrollOffsetX()
        {
            if (_cards.Count == 0)
                return 0f;

            float cardWidth = TrophyLayoutSpec.InstrumentCardSize.x;
            float cardStep = cardWidth + TrophyLayoutSpec.CardSpacing;

            if (_viewport == null || _cardContainer is not RectTransform containerRect)
                return -_focusedIndex * cardStep;

            float viewportWidth = _viewport.rect.width;
            if (viewportWidth <= 0f)
                return 0f;

            // Use the layout group's true width (includes trailing peek padding).
            float contentWidth = LayoutUtility.GetPreferredWidth(containerRect);
            if (contentWidth <= 0f)
            {
                contentWidth = _cards.Count * cardStep - TrophyLayoutSpec.CardSpacing
                               + TrophyLayoutSpec.CarouselPeekWidth;
            }

            float minOffset = Mathf.Min(0f, viewportWidth - contentWidth);
            float maxOffset = 0f;

            float focusedLeft = _focusedIndex * cardStep;
            float focusedRight = focusedLeft + cardWidth;

            // Default: align the focused card's left edge with the viewport's left edge.
            float offset = -focusedLeft;

            // If the focused card overflows the right edge, scroll left until it fits.
            if (offset + focusedRight > viewportWidth)
                offset = viewportWidth - focusedRight;

            // If the focused card overflows the left edge, scroll right until it fits.
            if (offset + focusedLeft < 0f)
                offset = -focusedLeft;

            return Mathf.Clamp(offset, minOffset, maxOffset);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_cards.Count == 0 || !isActiveAndEnabled)
                return;

            RefreshFocus(immediate: true);
        }

        private IEnumerator ScrollToX(RectTransform container, float targetX)
        {
            float startX = container.anchoredPosition.x;
            float elapsed = 0f;
            float duration = TrophyLayoutSpec.ScrollLerpDuration;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                var pos = container.anchoredPosition;
                pos.x = Mathf.Lerp(startX, targetX, t);
                container.anchoredPosition = pos;
                yield return null;
            }

            var finalPos = container.anchoredPosition;
            finalPos.x = targetX;
            container.anchoredPosition = finalPos;
            _scrollCoroutine = null;
        }

        private void ClearCards()
        {
            if (_scrollCoroutine != null)
            {
                StopCoroutine(_scrollCoroutine);
                _scrollCoroutine = null;
            }

            foreach (var card in _cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }

            _cards.Clear();
            _focusedIndex = 0;
        }

        private void OnDestroy()
        {
            ClearCards();
        }
    }
}
