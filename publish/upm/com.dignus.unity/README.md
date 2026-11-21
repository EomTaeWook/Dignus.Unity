# Dignus.Unity

[English overview](https://github.com/EomTaeWook/Dignus.Unity/blob/main/README.md) | [한국어 개요](https://github.com/EomTaeWook/Dignus.Unity/blob/main/README.ko.md)

Lightweight Unity integration for the Dignus framework. It provides dependency
injection helpers, scene lifecycle coordination, coroutine scheduling, resource
caching, object pooling, and bindable properties.

[한국어 API 가이드](https://github.com/EomTaeWook/Dignus.Unity/blob/main/publish/Dignus.Unity.ko.md)

## Requirements

- Unity 2022.3 or later
- A project that can load assets from Unity `Resources` folders

## Installation

### NuGet

Package page: [Dignus.Unity on NuGet.org](https://www.nuget.org/packages/Dignus.Unity)

```shell
dotnet add package Dignus.Unity --version 1.1.6
```

Or use `PackageReference`:

```xml
<PackageReference Include="Dignus.Unity" Version="1.1.6" />
```

NuGet resolves the `Dignus` dependency automatically.

### Unity Package Manager

Add the Git dependency to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dignus.unity": "https://github.com/EomTaeWook/Dignus.Unity.git?path=/publish/upm/com.dignus.unity#v1.1.6"
  }
}
```

The `path` query parameter must appear before the revision fragment. The Git
package already contains its compatible `Dignus.dll`; do not add another copy
of that assembly to the Unity project.

The published package contains:

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

## Runtime flow

1. Register application dependencies and build the service container once.
2. Load scenes through `DignusUnitySceneManager`.
3. `SceneBase.Awake()` resolves and binds the scene controller.
4. `OnAwakeScene()` initializes scene-specific state and UI.
5. Manager-controlled scene transitions invoke `OnDestroyScene()` for cleanup.

`OnDestroyScene()` is a Dignus.Unity lifecycle method. Unity does not invoke it
automatically when a GameObject is destroyed or when a scene is unloaded through
`UnityEngine.SceneManagement.SceneManager` directly.

## Dependency injection and scene setup

The following snippets are separate files in the consuming Unity project. Register the application assembly once before scene `Awake()` and call `Build()` before resolving services. The startup hook below uses the assembly containing the controller; register other required assemblies there as well.

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

`ISceneModel` carries Dignus's transient registration attribute. The concrete model below is registered by the assembly scan through that interface.

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

The controller is transient so separate scene instances do not share scene references. `[Inject]` on the base class's `Model` property supplies the concrete model.

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

Attach `LobbyScene` to a GameObject in the scene. Its inherited `Awake()` resolves the controller, binds the scene, and then invokes `OnAwakeScene()`. The scene base does not automatically call `Dispose()`; the example does so in `OnDestroyScene()`.

`OnShow()` and `OnHide()` follow `OnEnable()` and `OnDisable()`. They are separate from manager-driven `OnDestroyScene()`. If overriding the inherited `Awake()`, call `base.Awake()` to preserve controller resolution and binding. Use manager-driven scene transitions for the cleanup shown here; ordinary GameObject destruction does not invoke `OnDestroyScene()`.

### Runtime constructor arguments

`InjectedComponents` is optional. When populated, the activator matches those components to constructor parameters and resolves remaining dependencies from the container. Use this alternative controller declaration when the controller needs a scene component at construction time; do not include both declarations of `LobbySceneController` in one project.

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

Add the scene's `LobbyUI` component to `LobbyScene.InjectedComponents` in the Inspector. Configure it before `SceneBase.Awake()`; `OnAwakeScene()` is too late. Matching is based on assignable parameter types, and multiple matching runtime arguments cause an exception. If an injectable type has multiple public constructors, select one with `[InjectConstructor]`.

## Bindable properties

The model/controller example above demonstrates creation, subscription, value changes, initial display, and unsubscription. Subscription alone does not emit the current value. Invoke the display handler explicitly after subscribing.

`ValueChanged` is skipped when the current value is non-null and equals the assigned value. `NotifyChanged()` forces notification when the contained object changes without replacing the value. `IReadOnlyBindableProperty<T>` exposes the value and event without a setter. Consumers must unsubscribe when their owning component or controller ends its lifetime.

## Managers

### Coroutine manager

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

The scheduler advances from `Update()` using `Time.deltaTime` once per frame.
Timed waits follow scaled game time. With `Time.timeScale = 0`, timed waits
stop progressing, while frame waits and condition checks continue.

### Object pool

The extension methods handle parenting, activation, and deactivation:

```csharp
using Dignus.Unity.Extensions;

var bullet = this.InstantiateWithPool(bulletPrefab);
bullet.Recycle();
```

Only objects obtained from `DignusUnityObjectPool` can be recycled into it.
Returning an unrelated object throws `InvalidOperationException`.

The lower-level `Pop()` and `Push()` methods are also available, but callers are
responsible for setting the parent and active state when using them directly.

### Resource manager

Use `[PrefabPath]` to derive a `Resources` path from a component or asset type:

```csharp
using Dignus.Unity.Attributes;
using UnityEngine;

[PrefabPath("Prefabs/UI")]
public sealed class LobbyUI : MonoBehaviour
{
}

var prefab = DignusUnityResourceManager.Instance.LoadAsset<LobbyUI>();
```

An explicit path is also supported:

```csharp
var atlas = DignusUnityResourceManager.Instance.LoadAsset<UnityEngine.U2D.SpriteAtlas>("SpriteAtlases/Common");
```

Loaded assets are cached by resource path until `UnloadAsset()` is called.

### Scene manager

```csharp
var sceneManager = DignusUnitySceneManager.Instance;

sceneManager.LoadScene(SceneType.LobbyScene, scene =>
{
    // The loaded scene has completed Awake processing here.
});

sceneManager.LoadAdditiveScene(SceneType.HUD);
sceneManager.UnloadAdditiveScene(SceneType.HUD);
```

Load events are available for start, progress, and completion. Until an active
load-state guard is added to the manager, submit the next load request only after
the previous completion callback or completion event. Additive unload has no
completion callback.

Each managed scene should contain one authoritative `SceneBase`. If several
instances register during the same load, the last registered instance becomes
the callback value.

### Attribute-based resources and pooling

Save a prefab as `Assets/Resources/Prefabs/Bullet.prefab` and attach `Bullet` to its root. Resource paths omit the `Resources/` prefix and file extension. The default file name is the component type name; `[PrefabPath("Prefabs", "Bullet")]` also supplies it explicitly.

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

The explicit resource load only obtains the prefab. The no-argument extension uses the same attribute and attaches a pooled instance to the caller; these are alternative entry points. Pooled object state is not reset automatically. Keep reusable initialization in the appropriate activation or caller-owned code.

`DestroyPool(prefab)` clears that pool's active and inactive objects. `Clear()` clears all pools, and `ClearExceptActive()` removes inactive objects only. Do not use an object after its pool has destroyed it.

### Scene progress and completion

Add `LobbyScene` to Build Settings and call `OpenLobby()` from the existing application bootstrap or navigation flow after DI initialization. Keep this observer outside the scene being replaced if it must receive the entire transition. Issue the next load only after completion, as described above.

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

The progress handler receives the underlying asynchronous operation's progress. The completion callback is invoked separately; do not infer completion solely from a progress value. Its scene argument can be null when no `SceneBase` registered during loading. Event unsubscription and request callbacks have separate lifetimes.

## Known lifecycle boundaries

- Use the Dignus.Unity scene manager when `OnDestroyScene()` cleanup is required.
- Configure `InjectedComponents` before `SceneBase.Awake()` runs.
- Avoid issuing overlapping scene load requests.
- Unsubscribe bindable-property and manager events when their owner is disabled
  or disposed.
- Objects returned to the pool must originate from that pool.

## License

Dignus.Unity is released under the MIT License. See `LICENSE` for details.

