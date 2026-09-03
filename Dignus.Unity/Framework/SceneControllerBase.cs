// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.DependencyInjection.Attributes;

namespace Dignus.Unity.Framework
{
    public abstract class SceneControllerBase<TScene> : ISceneController where TScene : SceneBase
    {
        public TScene Scene { get; set; }
        public void BindScene(TScene scene)
        {
            Scene = scene;
        }
        public abstract void Dispose();

        void ISceneController.BindScene(SceneBase scene)
        {
            BindScene(scene as TScene);
        }
    }

    public abstract class SceneControllerBase<TScene, TModel> : ISceneController
        where TScene : SceneBase
        where TModel : ISceneModel
    {
        [Inject]
        public TModel Model { get; set; }
        public TScene Scene { get; set; }

        public abstract void Dispose();

        public void BindScene(TScene scene)
        {
            Scene = scene;
        }

        void ISceneController.BindScene(SceneBase scene)
        {
            BindScene(scene as TScene);
        }
    }
}
