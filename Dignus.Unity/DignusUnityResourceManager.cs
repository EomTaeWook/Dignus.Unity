// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Framework;
using Dignus.Unity.Attributes;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Dignus.Unity
{
    public class DignusUnityResourceManager : Singleton<DignusUnityResourceManager>
    {
        private readonly Dictionary<string, Object> _pathToResourceMap = new Dictionary<string, Object>();
        private readonly Dictionary<System.Type, string> _prefabToPathMap = new Dictionary<System.Type, string>();

        public T LoadAsset<T>() where T : Object
        {
            System.Type type = typeof(T);
            if (_prefabToPathMap.TryGetValue(type, out string path) == true)
            {
                return Instance.LoadAsset<T>(path);
            }
            PrefabPathAttribute attr = type.GetCustomAttribute<PrefabPathAttribute>();
            if (attr == null)
            {
                throw new System.InvalidOperationException($"failed to find {nameof(PrefabPathAttribute)}");
            }
            var fileName = attr.FileName ?? type.Name;
            if (attr.Path.EndsWith("/") == false)
            {
                path = $"{attr.Path}/{fileName}";
            }
            else
            {
                path = $"{attr.Path}{fileName}";
            }
            _prefabToPathMap.Add(type, path);
            return Instance.LoadAsset<T>(path);
        }
        public T LoadAsset<T>(string path) where T : Object
        {
            if (_pathToResourceMap.TryGetValue(path, out Object item) == false)
            {
                item = Resources.Load<T>(path);
                if (item == null)
                {
                    return default;
                }
                _pathToResourceMap.Add(path, item);
            }
            return item as T;
        }
        public void UnloadAsset(string path)
        {
            if (_pathToResourceMap.TryGetValue(path, out Object item))
            {
                Resources.UnloadAsset(item);
                _pathToResourceMap.Remove(path);
            }
        }
        public void UnloadAsset(Object obj)
        {
            var assetPaths = new ArrayQueue<string>();
            foreach (var kv in _pathToResourceMap)
            {
                if (kv.Value.Equals(obj))
                {
                    assetPaths.Add(kv.Key);
                }
            }

            foreach (var path in assetPaths)
            {
                UnloadAsset(path);
            }
        }
    }
}
