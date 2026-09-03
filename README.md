# Dignus.Unity

Unity integration package for the Dignus ecosystem.

- Dependency injection bootstrap and scene architecture utilities
- Coroutine and object pool management
- Scene transition lifecycle helpers
- Reactive bindable properties for scene model/state sync

## Package (UPM)

This repository publishes a Unity package in:

- `publish/upm/com.dignus.unity`

### Install via Unity Package Manager

Add the package to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.dignus.unity": "https://github.com/EomTaeWook/Dignus.Unity.git?path=publish/upm/com.dignus.unity#1.1.2"
  }
}
```

If you pin to another tag or branch, replace `#1.1.2` with:

- `#main`
- `#v1.1.2`
- `#<commit-hash>`

## License

- This package is released under the MIT License.
- See [LICENSE](LICENSE).
