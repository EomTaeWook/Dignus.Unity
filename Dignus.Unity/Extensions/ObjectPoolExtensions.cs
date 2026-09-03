// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using UnityEngine;

namespace Dignus.Unity.Extensions
{
    public static class DignusObjectPoolExtensions
    {
        public static void Recycle<T>(this GameObject gameObject) where T : Component
        {
            Recycle(gameObject.GetComponent<T>());
        }
        public static void Recycle<T>(this T component) where T : Component
        {
            component.gameObject.transform.SetParent(DignusUnityObjectPool.Instance.transform, false);
            component.gameObject.SetActive(false);
            DignusUnityObjectPool.Instance.Push(component);
        }
        public static void Recycle(this GameObject gameObject)
        {
            gameObject.transform.SetParent(DignusUnityObjectPool.Instance.transform, false);
            gameObject.SetActive(false);
            DignusUnityObjectPool.Instance.Push(gameObject);
        }

        public static T InstantiateWithPool<T>(this MonoBehaviour caller) where T : Component
        {
            var prefab = DignusUnityResourceManager.Instance.LoadAsset<T>();
            if (prefab == null)
            {
                return null;
            }
            T item;
            item = DignusUnityObjectPool.Instance.Pop<T>(prefab);

            item.gameObject.transform.SetParent(caller.transform, false);
            item.gameObject.SetActive(true);
            return item;
        }

        public static T InstantiateWithPool<T>(this MonoBehaviour caller, string path) where T : Component
        {
            var prefab = DignusUnityResourceManager.Instance.LoadAsset<T>(path);
            if (prefab == null)
            {
                return null;
            }
            T item;
            item = DignusUnityObjectPool.Instance.Pop<T>(prefab);
            item.transform.SetParent(caller.transform, false);
            item.gameObject.SetActive(true);
            return item;
        }

        public static T InstantiateWithPool<T>(this MonoBehaviour caller, T prefab) where T : Component
        {
            T item = DignusUnityObjectPool.Instance.Pop<T>(prefab);
            if(item == null)
            {
                return null;
            }
            item.transform.SetParent(caller.transform, false);
            item.gameObject.SetActive(true);
            return item;
        }

        public static T InstantiateWithPool<T>(this MonoBehaviour caller, GameObject prefab) where T : Component
        {
            T item = DignusUnityObjectPool.Instance.Pop<T>(prefab);
            if (item == null)
            {
                return null;
            }
            item.transform.SetParent(caller.transform, false);
            item.gameObject.SetActive(true);
            return item;
        }
    }
}
