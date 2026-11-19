# Dignus.Unity

English | [한국어](README.ko.md)

Unity integration for dependency injection, scene lifecycles, resources, object pools, coroutines, and binding.

## Requirements

Unity 2022.3 or later. Attribute-based resource loading uses Unity Resources folders.

## Installation

```xml
<PackageReference Include="Dignus.Unity" Version="1.1.6" />
```

Unity Package Manager Git URL:

```text
https://github.com/EomTaeWook/Dignus.Unity.git?path=/publish/upm/com.dignus.unity#v1.1.6
```

## Quick start

Run the following in startup code before scene Awake(), registering each required application assembly once.

```csharp
using Dignus.Unity.DependencyInjection;

var container = DignusUnityServiceContainer.RegisterDependencies(typeof(LobbySceneController).Assembly);
container.Build();
```

## Usage

Follow the guide's complete bootstrap/model/controller/scene example. Configure optional InjectedComponents before Awake(). Dispose controller-owned resources through OnDestroyScene() on manager-driven transitions.

## Documentation

- [API and connected examples](publish/Dignus.Unity.md)
- [UPM package guide](publish/upm/com.dignus.unity/README.md)

## License

MIT License. See [LICENSE](LICENSE).
