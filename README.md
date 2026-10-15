# Dignus.Unity

Unity integration package for the Dignus ecosystem.

- Dependency injection bootstrap and scene architecture utilities
- Coroutine and object pool management
- Scene transition lifecycle helpers
- Reactive bindable properties for scene model/state sync

Requires Unity 2022.3 or later.

## NuGet Installation

NuGet package: [Dignus.Unity](https://www.nuget.org/packages/Dignus.Unity)

```shell
dotnet add package Dignus.Unity --version 1.1.3
```

Or add a package reference directly:

```xml
<PackageReference Include="Dignus.Unity" Version="1.1.3" />
```

NuGet resolves the `Dignus` dependency automatically.

## Unity Package Manager Installation (GitHub URL)

Use this when installing directly from a GitHub repository.

The package is located in a repository subfolder, so install it with the `path` query parameter:

```json
{
  "dependencies": {
    "com.dignus.unity": "https://github.com/EomTaeWook/Dignus.Unity.git?path=/publish/upm/com.dignus.unity#v1.1.4"
  }
}
```

The Git package already contains its compatible `Dignus.dll`. Do not add another
copy of that assembly to the Unity project.

## Package (UPM)

This repository publishes a Unity package in:

- `publish/upm/com.dignus.unity`

All package assets and folders include committed `.meta` files so Unity can
import the package from its immutable cache.

## Library API Usage

Library setup, API usage, and code examples are documented in the [Dignus.Unity API guide](publish/Dignus.Unity.md).

## License

- This package is released under the MIT License.
- See [LICENSE](LICENSE).

