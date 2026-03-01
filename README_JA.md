# ObjectReference
[![Releases](https://img.shields.io/github/release/AndanteTribe/ObjectReference.svg)](https://github.com/AndanteTribe/ObjectReference/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/ObjectReference.svg)](./LICENSE)

[English](README.md) | 日本語

## 概要
**ObjectReference** は、Unity オブジェクトを非同期でロードするためのシンプルなインターフェースと実装を提供するライブラリです。

`IObjectReference<T>` インターフェースにより、直接シリアライズされた参照や Unity Addressables など、ロード元に関わらず統一された API でアセットをロードできます。`[SerializeReference]` 属性と組み合わせて使用でき、Unity Inspector 上でギアアイコンから参照タイプを切り替えられるカスタムプロパティドロワーが付属しています。

## 要件
- Unity 6000.0 以上
- （オプション）`AddressableObjectReference` を使用する場合は [Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) および [UniTask](https://github.com/Cysharp/UniTask)

## インストール
`Window > Package Manager` からPackage Managerウィンドウを開き、`[+] > Add package from git URL` を選択して以下のURLを入力します。

```
https://github.com/AndanteTribe/ObjectReference.git?path=src/ObjectReference.Unity/Packages/jp.andantetribe.objectreference
```

## クイックスタート

### `[SerializeReference]` を使用する方法（Inspectorで切り替え可能）

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using ObjectReference;
using UnityEngine;

public class ObjectReferenceSample : MonoBehaviour
{
    // ギアアイコンからInspectorで参照タイプを切り替えられます。
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

### `AddressableObjectReference` を直接使用する方法

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

| メソッド | 説明 |
|--------|------|
| `LoadAsync(CancellationToken cancellationToken)` | オブジェクトを非同期でロードします。 |
| `LoadAsync(IProgress<float> progress, CancellationToken cancellationToken)` | 進捗報告付きでオブジェクトを非同期にロードします。 |
| `Dispose()` | ロードされたアセットハンドルを解放します。 |

### `AddressableObjectReference<T>`

Unity Addressables を使用してアセットをロードする `IObjectReference<T>` の実装です。Addressables と UniTask が必要です。

| コンストラクタ | 説明 |
|-------------|------|
| `AddressableObjectReference(string address)` | 指定した Addressables アドレス文字列でインスタンスを生成します。 |

## ライセンス
このライブラリは、MITライセンスで公開しています。
