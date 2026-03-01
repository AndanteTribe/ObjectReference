# ObjectReference
[![Releases](https://img.shields.io/github/release/AndanteTribe/ObjectReference.svg)](https://github.com/AndanteTribe/ObjectReference/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/ObjectReference.svg)](./LICENSE)

English | [日本語](README_JA.md)

## Overview
**ObjectReference** provides a simple interface and implementations for asynchronously loading Unity objects.

The `IObjectReference<T>` interface gives you a unified API for loading assets regardless of the underlying source — whether a direct serialized reference or Unity Addressables. A custom property drawer is included so you can configure the reference type directly in the Unity Inspector using `[SerializeReference]`.

## Requirements
- Unity 6000.0 or later
- (Optional) [Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) and [UniTask](https://github.com/Cysharp/UniTask) for `AddressableObjectReference`

## Installation
Open `Window > Package Manager`, select `[+] > Add package from git URL`, and enter the following URL:

```
https://github.com/AndanteTribe/ObjectReference.git?path=src/ObjectReference.Unity/Packages/jp.andantetribe.objectreference
```

## Quick Start

### Using with `[SerializeReference]` (Inspector-configurable)

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using ObjectReference;
using UnityEngine;

public class ObjectReferenceSample : MonoBehaviour
{
    // The type can be switched in the Inspector via the gear icon.
    [SerializeReference]
    private IObjectReference<GameObject> _reference;

    private async UniTask Start()
    {
        var obj = await _reference.LoadAsync(destroyCancellationToken);
        Instantiate(obj, Vector3.zero, Quaternion.identity);
    }

    private void OnDestroy()
    {
        _reference?.Dispose();
    }
}
```

### Using `AddressableObjectReference` directly

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using ObjectReference;
using UnityEngine;

public class AddressableSample : MonoBehaviour
{
    private readonly IObjectReference<GameObject> _reference
        = new AddressableObjectReference<GameObject>("assets/prefabs/MyPrefab.prefab");

    private async UniTask Start()
    {
        var prefab = await _reference.LoadAsync(destroyCancellationToken);
        Instantiate(prefab, Vector3.zero, Quaternion.identity);
    }

    private void OnDestroy()
    {
        _reference.Dispose();
    }
}
```

## API

### `IObjectReference<T>`

| Method | Description |
|--------|-------------|
| `LoadAsync(CancellationToken cancellationToken)` | Loads the object asynchronously. |
| `LoadAsync(IProgress<float> progress, CancellationToken cancellationToken)` | Loads the object asynchronously with progress reporting. |
| `Dispose()` | Releases the loaded asset handle. |

### `AddressableObjectReference<T>`

An `IObjectReference<T>` implementation that loads assets via Unity Addressables. Requires Addressables and UniTask.

| Constructor | Description |
|-------------|-------------|
| `AddressableObjectReference(string address)` | Creates an instance with the given Addressables address string. |

## License
This library is released under the MIT license.

