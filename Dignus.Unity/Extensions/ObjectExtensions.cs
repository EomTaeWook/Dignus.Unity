// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using UnityEngine;

namespace Dignus.Unity.Extensions
{
    public static class ObjectExtensions
    {
        public static bool IsNull(this GameObject gameObject)
        {
            if (gameObject == null)
            {
                return true;
            }
            return gameObject is null;
        }
        public static bool IsNull(this Component component)
        {
            if (component == null)
            {
                return true;
            }
            return component.gameObject.IsNull();
        }
    }
}
