# Dignus.Unity

[English](Dignus.Unity.md) | 한국어

[한국어 개요](../README.ko.md)

DI, 씬 수명 주기, 리소스 캐시, 풀, 코루틴과 바인딩을 제공한다. Unity 2022.3 이상과 Resources 기반 리소스 구성을 사용한다.

## 설치

NuGet 또는 UPM Git 의존성으로 설치한다. Git URL의 path는 버전 fragment 앞에 둔다. UPM은 호환 Dignus.dll을 포함하므로 같은 어셈블리를 중복 추가하지 않는다.

```shell
dotnet add package Dignus.Unity --version 1.1.6
```

```xml
<PackageReference Include="Dignus.Unity" Version="1.1.6" />
```

```json
{
  "dependencies": {
    "com.dignus.unity": "https://github.com/EomTaeWook/Dignus.Unity.git?path=/publish/upm/com.dignus.unity#v1.1.6"
  }
}
```

```text
publish/upm/com.dignus.unity/
  package.json                 package.json.meta
  README.md                    README.md.meta
  LICENSE                      LICENSE.meta
  Icon.jpg                     Icon.jpg.meta
  Runtime/                     Runtime.meta
    Dignus.Unity.dll           Dignus.Unity.dll.meta
    Dignus.dll                 Dignus.dll.meta
```

## 초기화와 씬 연결

아래 코드는 소비 프로젝트의 개별 파일이다. 씬 Awake 전에 필요한 어셈블리를 한 번씩 등록하고 Build를 마친다. ISceneModel은 인터페이스의 특성으로 Transient 등록을 제공한다. 컨트롤러는 씬 인스턴스마다 별도 참조를 갖도록 Transient로 등록하며 기본 클래스의 Inject Model이 모델을 제공한다.

```csharp
// ApplicationBootstrap.cs
using Dignus.Unity.DependencyInjection;
using UnityEngine;

public static class ApplicationBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        var container = DignusUnityServiceContainer.RegisterDependencies(typeof(LobbySceneController).Assembly);
        container.Build();
    }
}
```

```csharp
// LobbySceneModel.cs
using Dignus.Unity.Binding;
using Dignus.Unity.Framework;

public class LobbySceneModel : ISceneModel
{
    public BindableProperty<int> Gold
    {
        get;
    } = new BindableProperty<int>(0);
}
```

```csharp
// LobbySceneController.cs
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Attributes;
using Dignus.Unity.Framework;

[Injectable(LifeScope.Transient)]
public class LobbySceneController : SceneControllerBase<LobbyScene, LobbySceneModel>
{
    public void Initialize()
    {
        Model.Gold.ValueChanged += OnGoldChanged;
        OnGoldChanged(Model.Gold.Value);
    }

    public void AddGold(int amount)
    {
        Model.Gold.Value += amount;
    }

    public override void Dispose()
    {
        Model.Gold.ValueChanged -= OnGoldChanged;
        Scene = null;
    }

    private void OnGoldChanged(int gold)
    {
        Scene.ShowGold(gold);
    }
}
```

```csharp
// LobbyScene.cs
using Dignus.Unity.Framework;
using UnityEngine;

public class LobbyScene : SceneBase<LobbySceneController>
{
    protected override void OnAwakeScene()
    {
        SceneController.Initialize();
    }

    public void ShowGold(int gold)
    {
        Debug.Log(gold);
    }

    public override void OnDestroyScene()
    {
        if (SceneController != null)
        {
            SceneController.Dispose();
        }
    }
}
```

```csharp
// Alternative LobbySceneController.cs
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Attributes;
using Dignus.Unity.Framework;

[Injectable(LifeScope.Transient)]
public class LobbySceneController : SceneControllerBase<LobbyScene, LobbySceneModel>
{
    private readonly LobbyUI _lobbyUI;

    public LobbySceneController(LobbyUI lobbyUI)
    {
        _lobbyUI = lobbyUI;
    }

    public void Initialize()
    {
        _lobbyUI.ShowGold(Model.Gold.Value);
    }

    public override void Dispose()
    {
        Scene = null;
    }
}
```

```csharp
// LobbyUI.cs
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public void ShowGold(int gold)
    {
        Debug.Log(gold);
    }
}
```

LobbyScene을 씬 GameObject에 붙인다. 상속된 Awake는 컨트롤러 조회·BindScene 후 OnAwakeScene을 호출한다. OnShow·OnHide는 OnEnable·OnDisable을 따른다. Awake를 재정의하면 base.Awake도 호출한다.

컨트롤러 Dispose는 자동 호출하지 않으므로 OnDestroyScene에서 호출한다. OnDestroyScene은 DignusUnitySceneManager의 씬 전환 경로에서 호출하는 메서드이며 일반 GameObject 파괴나 Unity SceneManager 직접 언로드가 자동 호출하지 않는다.

### 선택적 생성자 인자

InjectedComponents에 Inspector로 연결한 컴포넌트를 생성자 인자에 우선 사용하고 나머지는 컨테이너에서 조회한다. 대체 컨트롤러 예제를 기존 컨트롤러와 동시에 선언하지 않는다. 할당은 Awake 전에 완료하며 OnAwakeScene은 늦다. 대입 가능한 런타임 인자가 둘 이상이면 예외이고 공개 생성자가 여러 개면 InjectConstructor로 하나를 선택한다.

```csharp
// Alternative LobbySceneController.cs
using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Attributes;
using Dignus.Unity.Framework;

[Injectable(LifeScope.Transient)]
public class LobbySceneController : SceneControllerBase<LobbyScene, LobbySceneModel>
{
    private readonly LobbyUI _lobbyUI;

    public LobbySceneController(LobbyUI lobbyUI)
    {
        _lobbyUI = lobbyUI;
    }

    public void Initialize()
    {
        _lobbyUI.ShowGold(Model.Gold.Value);
    }

    public override void Dispose()
    {
        Scene = null;
    }
}
```

```csharp
// LobbyUI.cs
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    public void ShowGold(int gold)
    {
        Debug.Log(gold);
    }
}
```

## 바인딩

위 모델·컨트롤러 예제처럼 생성 → 구독 → 초기 표시 → 값 변경 → 해제를 연결한다. 구독만으로 현재 값이 통지되지 않는다. 기존 값이 null이 아니고 새 값과 같으면 알림을 생략한다. NotifyChanged는 내부 변경을 강제로 통지하며 IReadOnlyBindableProperty는 값과 이벤트만 노출한다. 소유자 종료 때 구독을 해제한다.

## 코루틴

Update에서 Time.deltaTime으로 갱신한다. 시간 대기는 배율을 따르므로 timeScale=0이면 시간이 진행하지 않지만 프레임 대기·조건 확인은 계속된다.

```csharp
// CoroutineExample.cs
using Dignus.Unity.Coroutine;
using System.Collections;
using UnityEngine;

public class CoroutineExample : MonoBehaviour
{
    private void Start()
    {
        DignusUnityCoroutineManager.Start(0.5f, RunSequence(), OnCompleted);
    }

    private IEnumerator RunSequence()
    {
        Debug.Log("Started");
        yield return null;
        Debug.Log("Continued");
    }

    private void OnCompleted()
    {
        Debug.Log("Completed");
    }
}
```

## 풀

확장 메서드는 부모 연결과 활성·비활성 전환을 처리한다. 풀에서 생성한 객체만 반환하며 다른 객체는 InvalidOperationException이다. Pop·Push를 직접 호출하면 부모와 활성 상태는 호출자가 관리한다.

```csharp
using Dignus.Unity.Extensions;

var bullet = this.InstantiateWithPool(bulletPrefab);
bullet.Recycle();
```

## 리소스

PrefabPath로 Resources 상대 경로를 구성한다. 명시적 경로도 지원하며 로드한 리소스는 UnloadAsset까지 경로별 캐시에 보관한다.

```csharp
using Dignus.Unity.Attributes;
using UnityEngine;

[PrefabPath("Prefabs/UI")]
public sealed class LobbyUI : MonoBehaviour
{
}

var prefab = DignusUnityResourceManager.Instance.LoadAsset<LobbyUI>();
```

```csharp
var atlas = DignusUnityResourceManager.Instance.LoadAsset<UnityEngine.U2D.SpriteAtlas>("SpriteAtlases/Common");
```

## 씬 관리자

시작·진행·완료 이벤트와 완료 콜백을 제공한다. 진행 중 로드를 겹쳐 요청하지 말고 앞선 완료 콜백·이벤트 뒤에 다음 요청을 보낸다. 추가 씬 언로드에는 완료 콜백이 없다. 씬마다 대표 SceneBase 하나를 사용하며 여러 인스턴스가 등록하면 마지막 등록값이 콜백에 전달된다.

```csharp
var sceneManager = DignusUnitySceneManager.Instance;

sceneManager.LoadScene(SceneType.LobbyScene, scene =>
{
    // The loaded scene has completed Awake processing here.
});

sceneManager.LoadAdditiveScene(SceneType.HUD);
sceneManager.UnloadAdditiveScene(SceneType.HUD);
```

## 특성 기반 리소스와 풀 연결

Assets/Resources/Prefabs/Bullet.prefab의 루트에 Bullet을 붙인다. 경로에서 Resources와 확장자는 생략하며 기본 파일명은 타입명이다. 명시적으로 PrefabPath의 두 번째 인자로 파일명을 지정할 수도 있다.

```csharp
// Bullet.cs
using Dignus.Unity.Attributes;
using UnityEngine;

[PrefabPath("Prefabs")]
public class Bullet : MonoBehaviour
{
}
```

```csharp
// Inside a MonoBehaviour, with Dignus.Unity and Dignus.Unity.Extensions imported.
var prefab = DignusUnityResourceManager.Instance.LoadAsset<Bullet>();
var bullet = this.InstantiateWithPool<Bullet>();
if (bullet != null)
{
    bullet.Recycle();
}
```

LoadAsset은 프리팹만 가져오고 InstantiateWithPool은 같은 특성으로 풀의 객체를 얻어 호출자에 붙인다. 두 방식은 대체 진입점이다. 재사용 상태를 자동 초기화하지 않는다. DestroyPool은 해당 풀의 활성·비활성 객체, Clear는 모든 풀, ClearExceptActive는 보관 객체만 제거한다. 파괴된 객체를 다시 사용하지 않는다.

## 진행률과 완료 콜백

LobbyScene을 Build Settings에 추가하고 DI 초기화 후 OpenLobby를 호출한다. 전환 전체를 관찰할 노드는 교체되는 씬 밖에 둔다. 이벤트 구독 해제와 로드 요청 콜백은 수명이 별개다.

```csharp
// SceneNavigation.cs
using Dignus.Unity;
using Dignus.Unity.Framework;
using UnityEngine;

public class SceneNavigation : MonoBehaviour
{
    private void OnEnable()
    {
        DignusUnitySceneManager.Instance.OnSceneLoadingProgress += OnLoadingProgress;
    }

    private void OnDisable()
    {
        DignusUnitySceneManager.Instance.OnSceneLoadingProgress -= OnLoadingProgress;
    }

    public void OpenLobby()
    {
        DignusUnitySceneManager.Instance.LoadScene("LobbyScene", OnLobbyLoaded);
    }

    private void OnLoadingProgress(string sceneName, float progress)
    {
        Debug.Log($"{sceneName}: {progress}");
    }

    private void OnLobbyLoaded(SceneBase scene)
    {
        if (scene is LobbyScene lobbyScene)
        {
            Debug.Log(lobbyScene.name);
        }
    }
}
```

진행률은 내부 비동기 작업의 값이며 그 값만으로 완료를 판단하지 않는다. SceneBase 등록이 없으면 콜백 인자는 null일 수 있다. 비활성·해제 시 바인딩·관리자 이벤트 구독도 해제한다.

## 라이선스

MIT 라이선스. [LICENSE](../LICENSE)를 참조한다.
