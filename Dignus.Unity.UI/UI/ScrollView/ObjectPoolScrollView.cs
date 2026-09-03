// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Unity.Coroutine;
using Dignus.Unity.Extensions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dignus.Unity.UI.ScrollView
{
    public class ObjectPoolScrollView : MonoBehaviour
    {
#pragma warning disable CS0649
        [SerializeField]
        private ScrollRect _scrollRect;
        [SerializeField]
        private ScrollViewItemGo _prefab;
        [SerializeField]
        private Vector2 _startPosition;
        [SerializeField]
        private int _bufferedItemsCount;
#pragma warning restore CS0649

        private RectTransform _content;
        private float _itemSize;
        private int _totalVisibleItemCount;

        private readonly List<IScrollViewData> _scrollDataList = new List<IScrollViewData>();
        private readonly List<ScrollViewItemGo> _activeItemList = new List<ScrollViewItemGo>();

        private int _lastStartIndex;
        private int _lastEndIndex;

        private bool _isVertical;

        public void SetData(List<IScrollViewData> items)
        {
            _scrollDataList.Clear();
            _scrollDataList.AddRange(items);
        }
        public IReadOnlyCollection<ScrollViewItemGo> GetActiveItems()
        {
            return _activeItemList;
        }
        private void CalculateItemSizeAndTotalVisibleItems()
        {
            _isVertical = _scrollRect.vertical;
            if (_isVertical)
            {
                _itemSize = _prefab.GetComponent<RectTransform>().rect.height;
                _totalVisibleItemCount = Mathf.CeilToInt(_scrollRect.viewport.rect.height / (_itemSize + _startPosition.y));
            }
            else
            {
                _itemSize = _prefab.GetComponent<RectTransform>().rect.width;
                _totalVisibleItemCount = Mathf.CeilToInt(_scrollRect.viewport.rect.width / (_itemSize + _startPosition.x));
            }

            _totalVisibleItemCount += _bufferedItemsCount;
        }
        private void AdjustContentSize()
        {
            if (_isVertical)
            {
                _content.sizeDelta = new Vector2(_content.sizeDelta.x + _startPosition.x, _scrollDataList.Count * _itemSize - _startPosition.y);
            }
            else
            {
                _content.sizeDelta = new Vector2(_scrollDataList.Count * _itemSize + _startPosition.x, _content.sizeDelta.y - _startPosition.y);
            }
        }

        public void Refresh()
        {
            UpdateVisibleItems();
        }
        private IEnumerator InternalInit()
        {
            yield return null;
            _content = _scrollRect.content;
            _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            CalculateItemSizeAndTotalVisibleItems();
            AdjustContentSize();

            for (int i = 0; i < _totalVisibleItemCount; ++i)
            {
                AddNewItemToScrollView(_scrollDataList[i], i);
            }
            _lastEndIndex = _totalVisibleItemCount;
        }
        private void OnDestroy()
        {
            _scrollRect.onValueChanged.RemoveAllListeners();
            foreach (var item in _activeItemList)
            {
                item.Recycle();
            }
            _activeItemList.Clear();
            _scrollDataList.Clear();
        }
        private void AddNewItemToScrollView(IScrollViewData data, int index, bool addToStart = false)
        {
            var newItem = DignusUnityObjectPool.Instance.Pop<ScrollViewItemGo>(_prefab);
            if (addToStart)
            {
                _activeItemList.Insert(0, newItem);
            }
            else
            {
                _activeItemList.Add(newItem);
            }

            newItem.transform.SetParent(_content, false);
            UpdateItemPosition(newItem, index);
            newItem.SetData(data);
            newItem.gameObject.SetActive(true);
        }
        private void UpdateVisibleItems()
        {
            int startIndex;
            int endIndex;
            if (_isVertical == true)
            {
                startIndex = Mathf.FloorToInt((_scrollRect.content.anchoredPosition.y + _startPosition.y) / _itemSize);
                endIndex = Mathf.CeilToInt((_scrollRect.content.anchoredPosition.y + _scrollRect.viewport.rect.height + _startPosition.y) / _itemSize);
            }
            else
            {
                startIndex = Mathf.FloorToInt(_scrollRect.content.anchoredPosition.x / (_itemSize + _startPosition.x));
                endIndex = Mathf.CeilToInt((_scrollRect.content.anchoredPosition.x + _scrollRect.viewport.rect.width) / (_itemSize + _startPosition.x));
            }

            startIndex = Mathf.Max(0, startIndex);
            endIndex = Mathf.Min(_scrollDataList.Count, endIndex);

            if (startIndex == _lastStartIndex && endIndex == _lastEndIndex)
            {
                return;
            }
            var needAdd = false;
            if (startIndex > _lastStartIndex)
            {
                if (_activeItemList.Count > 0 && endIndex > _lastEndIndex)
                {
                    needAdd = true;
                }
                else if (_activeItemList.Count > _totalVisibleItemCount)
                {
                    needAdd = true;
                }

                if (needAdd == false)
                {
                    return;
                }

                var itemToRemove = _activeItemList[0];
                itemToRemove.Recycle();
                _activeItemList.Remove(itemToRemove);
                _lastEndIndex++;

                int dataIndex = startIndex + _activeItemList.Count;
                if (dataIndex < _scrollDataList.Count)
                {
                    AddNewItemToScrollView(_scrollDataList[dataIndex], dataIndex);
                    _lastStartIndex++;
                }
            }
            else if (endIndex < _lastEndIndex)
            {
                if (_activeItemList.Count > 0 && _lastStartIndex > startIndex)
                {
                    needAdd = true;
                }

                if (_lastStartIndex == 0)
                {
                    needAdd = false;
                }

                if (needAdd == false)
                {
                    return;
                }

                var itemToRemove = _activeItemList[_activeItemList.Count - 1];
                itemToRemove.Recycle();
                _activeItemList.Remove(itemToRemove);
                _lastStartIndex--;

                AddNewItemToScrollView(_scrollDataList[_lastStartIndex], _lastStartIndex, true);
                _lastEndIndex--;
            }
        }

        private void UpdateItemPosition(ScrollViewItemGo item, int index)
        {
            if (_isVertical == true)
            {
                RectTransform itemRect = item.GetComponent<RectTransform>();
                float posY = -_itemSize * index;
                posY += _startPosition.y;
                itemRect.anchoredPosition = new Vector2(0, posY);
                itemRect.pivot = itemRect.anchorMax = itemRect.anchorMin = new Vector2(0.5F, 1);
            }
            else
            {
                RectTransform itemRect = item.GetComponent<RectTransform>();
                float posX = _itemSize * index;
                posX += _startPosition.x;
                itemRect.anchoredPosition = new Vector2(posX, 0);
                itemRect.pivot = itemRect.anchorMax = itemRect.anchorMin = new Vector2(1, 0.5F);
            }
        }
        private void OnScrollValueChanged(Vector2 vector2)
        {
            UpdateVisibleItems();
        }
        private void Awake()
        {
            DignusUnityCoroutineManager.Start(InternalInit());
        }
    }
}
