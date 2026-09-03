// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using UnityEngine;

namespace Dignus.Unity.UI.ScrollView
{
    public abstract class ScrollViewItemGo : MonoBehaviour, IScrollViewItem
    {
        public abstract void SetData(IScrollViewData data);
    }
}
