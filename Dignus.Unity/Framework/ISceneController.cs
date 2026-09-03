// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Unity.Framework
{
    public interface ISceneController
    {
        void BindScene(SceneBase scene);

        void Dispose();
    }
}
