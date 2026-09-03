using Assets.Scripts.Internals;
using Dignus.Unity;
using Dignus.Unity.Framework;

namespace Assets.Scripts.Scenes.Title
{
    internal class TitleScene : SceneBase
    {
        public override void OnDestroyScene()
        {
            
        }

        protected override void OnAwakeScene()
        {
            DignusUnitySceneManager.Instance.LoadScene(SceneType.MainScene.ToString());
        }
    }
}
