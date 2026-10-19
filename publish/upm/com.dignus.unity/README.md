# Dignus.Unity

Lightweight Unity integration for the Dignus framework. It provides dependency
injection helpers, scene lifecycle coordination, coroutine scheduling, resource
caching, object pooling, and bindable properties.

## Requirements

- Unity 2022.3 or later
- A project that can load assets from Unity `Resources` folders

## Installation

### NuGet

Package page: [Dignus.Unity on NuGet.org](https://www.nuget.org/packages/Dignus.Unity)

```shell
dotnet add package Dignus.Unity --version 1.1.5
```

Or use `PackageReference`:

```xml
<PackageReference Include="Dignus.Unity" Version="1.1.5" />
```

NuGet resolves the `Dignus` dependency automatically.

### Unity Package Manager

Add the Git dependency to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dignus.unity": "https://github.com/EomTaeWook/Dignus.Unity.git?path=/publish/upm/com.dignus.unity#v1.1.5"
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

## Dependency injection

Mark services and controllers with `[Injectable]`, then register the application
assembly once during startup:

```csharp
using Dignus.DependencyInjection.Attributes;
using Dignus.Unity;
using Dignus.Unity.DependencyInjection;
using Dignus.Unity.Framework;

[Injectable(LifeScope.Singleton)]
public sealed class LobbySceneController : SceneControllerBase<LobbyScene>
{
    private readonly GameClientService _gameClientService;

    public LobbySceneController(GameClientService gameClientService)
    {
        _gameClientService = gameClientService;
    }

    public override void Dispose()
    {
    }
}

public sealed class ApplicationManager : SingletonMonoBehaviour<ApplicationManager>
{
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        var container = DignusUnityServiceContainer.RegisterDependencies(GetType().Assembly);
        container.Build();
        _initialized = true;
    }
}
```

Resolve services only after `Build()` has completed:

```csharp
var controller = DignusUnityServiceContainer.GetService<LobbySceneController>();
```

If an injectable type has multiple public constructors, annotate the constructor
to use with `[InjectConstructor]`.

## Scene architecture

`SceneBase<TController>` resolves the controller before calling
`OnAwakeScene()` and binds the scene automatically:

```csharp
using Dignus.Unity.Framework;

public sealed class LobbyScene : SceneBase<LobbySceneController>
{
    protected override void OnAwakeScene()
    {
        SceneController.Initialize();
    }

    public override void OnDestroyScene()
    {
        SceneController.Dispose();
    }
}
```

### Runtime constructor arguments

Unity components assigned to the inherited `InjectedComponents` array are
matched to controller constructor parameters before the container is used as a
fallback:

```csharp
public sealed class LobbySceneController : SceneControllerBase<LobbyScene>
{
    private readonly LobbyUI _lobbyUI;

    public LobbySceneController(LobbyUI lobbyUI, GameClientService gameClientService)
    {
        _lobbyUI = lobbyUI;
    }

    public override void Dispose()
    {
    }
}
```

In the Unity Inspector, add the scene's `LobbyUI` component to
`LobbyScene.InjectedComponents`. Do not assign `InjectedComponents` inside
`OnAwakeScene()`?봠ontroller resolution has already happened by then.

## Bindable properties

```csharp
using Dignus.Unity.Binding;

public sealed class LobbySceneModel : ISceneModel
{
    public BindableProperty<int> Gold { get; } = new BindableProperty<int>(0);
}
```

Subscribe and unsubscribe with the Unity component lifecycle:

```csharp
private void OnEnable()
{
    _model.Gold.ValueChanged += OnGoldChanged;
    OnGoldChanged(_model.Gold.Value);
}

private void OnDisable()
{
    _model.Gold.ValueChanged -= OnGoldChanged;
}
```

Assigning an equal value does not raise `ValueChanged`. Call `NotifyChanged()`
when the contained object changed internally without replacing the value.

## Managers

### Coroutine manager

```csharp
DignusUnityCoroutineManager.Start(MyCoroutine());
DignusUnityCoroutineManager.Start(0.5f, MyCoroutine(), OnCompleted);
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
var atlas = DignusUnityResourceManager.Instance.LoadAsset<SpriteAtlas>(
    "SpriteAtlases/Common");
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

## Known lifecycle boundaries

- Use the Dignus.Unity scene manager when `OnDestroyScene()` cleanup is required.
- Configure `InjectedComponents` before `SceneBase.Awake()` runs.
- Avoid issuing overlapping scene load requests.
- Unsubscribe bindable-property and manager events when their owner is disabled
  or disposed.
- Objects returned to the pool must originate from that pool.

## License

Dignus.Unity is released under the MIT License. See `LICENSE` for details.

