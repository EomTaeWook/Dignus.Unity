// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.Unity.DependencyInjection;
using System;
using UnityEngine;

namespace Dignus.Unity.Framework
{
    public abstract class SceneBase : MonoBehaviour
    {
        protected virtual void Awake()
        {
            DignusUnitySceneManager.Instance.RegisterScene(this);
            OnAwakeScene();
        }

        protected void OnEnable()
        {
            OnShow();
        }

        protected void OnDisable()
        {
            OnHide();
        }
        protected abstract void OnAwakeScene();
        public abstract void OnDestroyScene();
        protected virtual void OnHide()
        {
        }
        protected virtual void OnShow()
        {
        }
    }

    public abstract class SceneBase<TController> : SceneBase where TController : ISceneController
    {
        public MonoBehaviour[] InjectedComponents = Array.Empty<MonoBehaviour>();
        public TController SceneController { get; private set; }

        protected override void Awake()
        {
            this.SceneController = DignusUnityServiceContainer.GetService<TController>(InjectedComponents);
            this.SceneController.BindScene(this);

            base.Awake();
        }
    }
}
