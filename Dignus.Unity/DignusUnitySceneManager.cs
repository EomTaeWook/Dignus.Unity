// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Collections;
using Dignus.Framework;
using Dignus.Unity.Coroutine;
using Dignus.Unity.Framework;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

namespace Dignus.Unity
{
    public class DignusUnitySceneManager : Singleton<DignusUnitySceneManager>
    {
        public event SceneLoadCompletedHandler OnSceneLoadCompleted;
        public event SceneLoadProgressHandler OnSceneLoadingProgress;
        public event SceneLoadStartedHandler OnSceneLoadStarted;

        public delegate void SceneLoadStartedHandler(string sceneName);
        public delegate void SceneLoadProgressHandler(string sceneName, float progress);
        public delegate void SceneLoadCompletedHandler(string sceneName);

        public string PreviousSceneName { get; private set; }
        public string CurrentSceneName { get; private set; }

        public SceneBase CurrentScene { get; private set; }

        private readonly ArrayQueue<SceneLoadInfo> _sceneQueue = new ArrayQueue<SceneLoadInfo>();
        private readonly ArrayQueue<SceneUnloadInfo> _sceneUnloadQueue = new ArrayQueue<SceneUnloadInfo>();
        private LoadSceneMode _currentLoadMode = LoadSceneMode.Single;
        private SceneBase _loadedScene;

        private struct SceneLoadInfo
        {
            public string SceneName { get; set; }
            public Action<SceneBase> CompleteCallback { get; set; }
            public LoadSceneMode LoadMode { get; set; }
        }

        private struct SceneUnloadInfo
        {
            public string SceneName { get; set; }
        }

        internal void RegisterScene(SceneBase sceneBase)
        {
            _loadedScene = sceneBase;

            if (_currentLoadMode == LoadSceneMode.Single)
            {
                CurrentScene = sceneBase;
            }
        }

        public void LoadScene(string sceneName, Action<SceneBase> completeCallback = null)
        {
            EnqueueScene(sceneName, LoadSceneMode.Single, completeCallback);
        }

        public void LoadScene(Enum sceneType, Action<SceneBase> completeCallback = null)
        {
            if (sceneType == null)
            {
                throw new ArgumentNullException(nameof(sceneType));
            }

            LoadScene(sceneType.ToString(), completeCallback);
        }

        public void LoadAdditiveScene(string sceneName, Action<SceneBase> completeCallback = null)
        {
            EnqueueScene(sceneName, LoadSceneMode.Additive, completeCallback);
        }

        public void LoadAdditiveScene(Enum sceneType, Action<SceneBase> completeCallback = null)
        {
            if (sceneType == null)
            {
                throw new ArgumentNullException(nameof(sceneType));
            }

            LoadAdditiveScene(sceneType.ToString(), completeCallback);
        }

        public void UnloadAdditiveScene(string sceneName)
        {
            ValidateAdditiveUnload(sceneName);
            EnqueueSceneUnload(sceneName);
        }

        public void UnloadAdditiveScene(Enum sceneType)
        {
            if (sceneType == null)
            {
                throw new ArgumentNullException(nameof(sceneType));
            }

            UnloadAdditiveScene(sceneType.ToString());
        }

        private void ValidateAdditiveUnload(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                throw new ArgumentException("sceneName cannot be null or whitespace.", nameof(sceneName));
            }

            if (string.Equals(CurrentSceneName, sceneName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Cannot unload the current single scene '{sceneName}' with UnloadAdditiveScene. " +
                    "Use LoadScene(...) to change single scene.");
            }

            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid() && string.Equals(activeScene.name, sceneName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Cannot unload the active scene '{sceneName}' with UnloadAdditiveScene. " +
                    "Use LoadScene(...) to switch the active scene instead.");
            }
        }

        private void EnqueueScene(string sceneName, LoadSceneMode loadMode, Action<SceneBase> completeCallback)
        {
            var sceneLoadInfo = new SceneLoadInfo()
            {
                SceneName = sceneName,
                CompleteCallback = completeCallback,
                LoadMode = loadMode
            };

            if (_sceneQueue.Count == 0)
            {
                _sceneQueue.Add(sceneLoadInfo);
                DignusUnityCoroutineManager.Start(LoadScene());
            }
            else
            {
                _sceneQueue.Add(sceneLoadInfo);
            }
        }

        private void EnqueueSceneUnload(string sceneName)
        {
            var sceneUnloadInfo = new SceneUnloadInfo()
            {
                SceneName = sceneName
            };

            if (_sceneUnloadQueue.Count == 0)
            {
                _sceneUnloadQueue.Add(sceneUnloadInfo);
                DignusUnityCoroutineManager.Start(UnloadScene());
            }
            else
            {
                _sceneUnloadQueue.Add(sceneUnloadInfo);
            }
        }

        private IEnumerator LoadScene()
        {
            while (_sceneQueue.TryRead(out SceneLoadInfo sceneLoadInfo) == true)
            {
                if (sceneLoadInfo.LoadMode == LoadSceneMode.Single)
                {
                    PreviousSceneName = CurrentSceneName;

                    if (CurrentScene != null)
                    {
                        CurrentScene.OnDestroyScene();
                    }
                }

                _currentLoadMode = sceneLoadInfo.LoadMode;
                _loadedScene = null;

                OnSceneLoadStarted?.Invoke(sceneLoadInfo.SceneName);

                var currentAsyncOperation = SceneManager.LoadSceneAsync(sceneLoadInfo.SceneName, sceneLoadInfo.LoadMode);

                while (currentAsyncOperation.isDone == false)
                {
                    OnSceneLoadingProgress?.Invoke(sceneLoadInfo.SceneName, currentAsyncOperation.progress);
                    yield return null;
                }

                if (sceneLoadInfo.LoadMode == LoadSceneMode.Single)
                {
                    CurrentSceneName = sceneLoadInfo.SceneName;
                }

                OnSceneLoadCompleted?.Invoke(sceneLoadInfo.SceneName);
                sceneLoadInfo.CompleteCallback?.Invoke(_loadedScene);
            }
        }

        private IEnumerator UnloadScene()
        {
            while (_sceneUnloadQueue.TryRead(out SceneUnloadInfo sceneUnloadInfo) == true)
            {
                var targetScene = SceneManager.GetSceneByName(sceneUnloadInfo.SceneName);

                if (targetScene.IsValid() == false || targetScene.isLoaded == false)
                {
                    continue;
                }

                foreach (var rootObject in targetScene.GetRootGameObjects())
                {
                    foreach (var component in rootObject.GetComponentsInChildren<SceneBase>(true))
                    {
                        component.OnDestroyScene();
                    }
                }

                var asyncOperation = SceneManager.UnloadSceneAsync(targetScene);

                while (asyncOperation.isDone == false)
                {
                    yield return null;
                }

                if (_loadedScene != null && _loadedScene.gameObject.scene.name == sceneUnloadInfo.SceneName)
                {
                    _loadedScene = null;
                }

                if (CurrentScene != null && CurrentScene.gameObject.scene.name == sceneUnloadInfo.SceneName)
                {
                    CurrentScene = null;
                }

                if (CurrentSceneName == sceneUnloadInfo.SceneName)
                {
                    CurrentSceneName = null;
                }

            }
        }
    }
}
