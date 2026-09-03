// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Coroutine;
using System;
using System.Collections;
using UnityEngine;

namespace Dignus.Unity.Coroutine
{
    public class DignusUnityCoroutineManager : SingletonMonoBehaviour<DignusUnityCoroutineManager>
    {
        private readonly CoroutineHandler _coroutineHandler = new CoroutineHandler();

        public static void Start(CoroutineHandle coroutineHandle, Action onCompleteCallback = null)
        {
            Start(0, coroutineHandle, onCompleteCallback);
        }
        public static void Start(float delay, CoroutineHandle coroutineHandle, Action onCompleteCallback = null)
        {
            Instance._coroutineHandler.Start(delay, coroutineHandle, onCompleteCallback);
        }
        public static CoroutineHandle Start(IEnumerator enumerator, Action onCompleteCallback = null)
        {
            return Start(0, enumerator, onCompleteCallback);
        }

        public static CoroutineHandle Start(float delay, IEnumerator enumerator, Action onCompleteCallback = null)
        {
            return Instance._coroutineHandler.Start(delay, enumerator, onCompleteCallback);
        }

        private void FixedUpdate()
        {
            _coroutineHandler.UpdateCoroutines(Time.fixedDeltaTime);
        }
    }
}
