// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using UnityEngine;

namespace Dignus.Unity
{
    public class SingletonMonoBehaviour<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance = null;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    var obj = GameObject.Find(typeof(T).Name);
                    if (obj == null)
                    {
                        obj = new GameObject(typeof(T).Name);
                        _instance = obj.AddComponent<T>();
                    }
                    else
                    {
                        _instance = obj.GetComponent<T>();
                    }
                }
                return _instance;
            }
        }
        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            OnAwake();
        }
        protected virtual void OnAwake()
        {
        }
    }
}

