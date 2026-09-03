// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Dignus.Unity
{
    public class DignusUnityObjectPool : SingletonMonoBehaviour<DignusUnityObjectPool>
    {
        private class PoolItem
        {
            public GameObject GameObject { get; set; }

            public Component Component { get; set; }

            public Pool Pool { get; set; }
        }

        private class Pool : ObjectPoolBase<PoolItem>
        {
            private readonly GameObject _prefab;

            public Pool(GameObject prefab)
            {
                this._prefab = prefab;
            }

            public override PoolItem CreateItem()
            {
                var go = Instantiate(_prefab);
                var poolItem = new PoolItem()
                {
                    GameObject = go,
                    Pool = this,
                };
                Instance._goToPoolItemMap.Add(go, poolItem);
                return poolItem;
            }

            public T Pop<T>() where T : Component
            {
                var poolItem = base.Pop();
                if (poolItem.Component == null)
                {
                    poolItem.Component = poolItem.GameObject.GetComponent<T>();
                }
                return poolItem.Component as T;
            }

            public override void Remove(PoolItem item)
            {
                Instance._goToPoolItemMap.Remove(item.GameObject);
                Destroy(item.GameObject);
            }
        }

        private readonly Dictionary<GameObject, PoolItem> _goToPoolItemMap = new Dictionary<GameObject, PoolItem>();
        private readonly Dictionary<GameObject, Pool> _poolContainer = new Dictionary<GameObject, Pool>();

        private Pool GetOrCreatePool(GameObject item)
        {
            if (_poolContainer.TryGetValue(item, out Pool pool) == false)
            {
                pool = new Pool(item);
                _poolContainer.Add(item, pool);
            }
            return pool;
        }

        public GameObject Pop(GameObject item)
        {
            var pool = GetOrCreatePool(item);
            return pool.Pop().GameObject;
        }
        public T Pop<T>(GameObject item) where T : Component
        {
            var pool = GetOrCreatePool(item);
            return pool.Pop<T>();
        }
        public T Pop<T>(Component item) where T : Component
        {
            return Pop<T>(item.gameObject);
        }

        public void Push(GameObject item)
        {
            if (_goToPoolItemMap.TryGetValue(item, out PoolItem poolItem) == false)
            {
                var pool = new Pool(item);
                _poolContainer.Add(item, pool);
                poolItem = new PoolItem()
                {
                    GameObject = item,
                    Pool = pool,
                };

            }
            poolItem.Pool.Push(poolItem);
        }
        public void Push(Component item)
        {
            Push(item.gameObject);
        }
        public void DestroyPool(GameObject prefab)
        {
            if (_poolContainer.TryGetValue(prefab, out Pool pool) == true)
            {
                pool.Clear();
            }
        }
        public void Clear()
        {
            foreach (var kv in _poolContainer)
            {
                var pool = kv.Value;
                pool.Clear();
            }
        }
        public void ClearExceptActive()
        {
            foreach (var kv in _poolContainer)
            {
                var pool = kv.Value;
                pool.ClearExceptActive();
            }
        }
    }
}
