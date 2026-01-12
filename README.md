<p align="center">
  <h1 align="center">🎵 SapCrossfadeAudio</h1>
  <p align="center">
    <strong>Sample-accurate crossfade library for Unity 6.3+ Scriptable Audio Pipeline</strong>
  </p>
  <p align="center">
    <a href="#features">Features</a> •
    <a href="#installation">Installation</a> •
    <a href="#quick-start">Quick Start</a> •
    <a href="#api-reference">API</a> •
    <a href="#architecture">Architecture</a> •
    <a href="Docs/SapCrossfadeAudio_DesignDocument_v1.1.0_Unity6.3_APIAligned.md">Design Doc</a>
  </p>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6.3.4f1+-black?logo=unity" alt="Unity 6.3.4f1+">
  <img src="https://img.shields.io/badge/Burst-Required-orange" alt="Burst Required">
  <img src="https://img.shields.io/badge/License-MIT-blue" alt="MIT License">
  <img src="https://img.shields.io/badge/Platform-Any-green" alt="Any Platform">
</p>

---

## Overview

**SapCrossfadeAudio** は Unity 6.3+ の Scriptable Audio Pipeline（SAP）を活用した、**サンプル精度のクロスフェード再生**を実現する専用ライブラリです。

BGM 遷移などのシーンで、**例外ゼロ**・**アロケーションフリー**な安定したリアルタイムオーディオ処理を提供します。

```csharp
// シンプルな2行でBGMクロスフェード
[SerializeField] CrossfadePlayer player;

player.CrossfadeToB(duration: 2f, CrossfadeCurve.EqualPower);
```

---

## Features

<table>
<tr>
<td width="50%">

### 🎯 Core Features

- **サンプル精度のクロスフェード** - 2ソースの精密なミキシング
- **複数カーブ対応** - Equal-Power / Linear / S-Curve
- **Burst 最適化** - SIMD による高速処理
- **ゼロアロケーション** - GC スパイクなし

</td>
<td width="50%">

### 🛡️ Robustness

- **例外ゼロポリシー** - 未設定/エラー時も無音で継続
- **SAP 互換層** - API 変更への耐性
- **冪等な Release** - 複数回呼び出しても安全
- **再 Configure 耐性** - デバイス切り替え対応

</td>
</tr>
<tr>
<td width="50%">

### ⚡ Performance

- **Control/Realtime 分離** - スレッドセーフ設計
- **メモリプーリング** - NativeBuffer 再利用
- **リサンプリング** - sampleRate 不一致を吸収

</td>
<td width="50%">

### 📦 Integration

- **外部依存ゼロ** - コアは Burst のみ
- **Addressables 対応** - 別 asmdef で分離
- **非 MonoBehaviour 対応** - DI/ステートマシン統合

</td>
</tr>
</table>

---

## Requirements

| 項目 | 要件 |
|------|------|
| **Unity** | 6.3.4f1 以降（SAP 対応） |
| **必須パッケージ** | `com.unity.burst` |
| **任意パッケージ** | `com.unity.addressables`（別 asmdef） |

---

## Installation

### Option 1: Git URL（推奨）

Unity Package Manager で以下の URL を追加:

```
https://github.com/tomoludens/unity-sap-crossfade-audio.git?path=Assets/Plugins/unity-sap-crossfade-audio
```

### Option 2: Manual

1. リポジトリをクローンまたはダウンロード
2. `Assets/Plugins/unity-sap-crossfade-audio` フォルダをプロジェクトにコピー

---

## Quick Start

### Step 1: Generator Assets の作成

1. **Project ウィンドウで右クリック**
2. **Create > SapCrossfadeAudio > Generators** から作成

```
ClipGenerator (Source A) ─┐
                          ├─► CrossfadeGenerator
ClipGenerator (Source B) ─┘
```

### Step 2: AudioSource への設定

```csharp
// Inspector で設定
[SerializeField] AudioSource audioSource;
[SerializeField] CrossfadeGeneratorAsset crossfadeGenerator;

void Start()
{
    audioSource.generator = crossfadeGenerator;
    audioSource.Play();
}
```

### Step 3: クロスフェード実行

```csharp
using SapCrossfadeAudio.Runtime.Core.Types;
using UnityEngine.Audio;

// CrossfadeCommand を送信
var command = CrossfadeCommand.Create(
    targetPosition01: 1f,      // 0=A, 1=B
    durationSeconds: 2f,       // フェード時間
    curve: CrossfadeCurve.EqualPower
);

ControlContext.builtIn.SendMessage(audioSource.generatorInstance, ref command);
```

---

## Usage Examples

### Basic: MonoBehaviour での制御

```csharp
using UnityEngine;
using SapCrossfadeAudio.Runtime.Core.Types;
using UnityEngine.Audio;

public class BgmController : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;

    public void CrossfadeToTrackB(float duration = 2f)
    {
        if (!ControlContext.builtIn.Exists(_audioSource.generatorInstance))
            return;

        var cmd = CrossfadeCommand.Create(1f, duration, CrossfadeCurve.EqualPower);
        ControlContext.builtIn.SendMessage(_audioSource.generatorInstance, ref cmd);
    }

    public void CrossfadeToTrackA(float duration = 2f)
    {
        if (!ControlContext.builtIn.Exists(_audioSource.generatorInstance))
            return;

        var cmd = CrossfadeCommand.Create(0f, duration, CrossfadeCurve.EqualPower);
        ControlContext.builtIn.SendMessage(_audioSource.generatorInstance, ref cmd);
    }
}
```

### Advanced: 即時切り替え

```csharp
// フェードなしで即座に切り替え
var cmd = CrossfadeCommand.Create(
    targetPosition01: 1f,
    durationSeconds: 0f,  // 即時
    curve: CrossfadeCurve.Linear
);
ControlContext.builtIn.SendMessage(audioSource.generatorInstance, ref cmd);
```

### Advanced: カスタムカーブの選択

```csharp
// Equal-Power: エネルギー一定（推奨）
CrossfadeCurve.EqualPower  // wA = cos(p × π/2), wB = sin(p × π/2)

// Linear: 線形
CrossfadeCurve.Linear      // wA = 1 - p, wB = p

// S-Curve: スムーズステップ
CrossfadeCurve.SCurve      // smoothstep 補間
```

### Recommended: CrossfadePlayer を使用

最も簡単な方法は、CrossfadePlayer コンポーネントを使用することです。

```csharp
using UnityEngine;
using SapCrossfadeAudio.Runtime.Core.Components;
using SapCrossfadeAudio.Runtime.Core.Types;

public class BgmManager : MonoBehaviour
{
    [SerializeField] private CrossfadePlayer _player;

    public void OnBattleStart()
    {
        _player.CrossfadeToB(2f, CrossfadeCurve.EqualPower);
    }

    public void OnBattleEnd()
    {
        _player.CrossfadeToA(3f, CrossfadeCurve.EqualPower);
    }

    public void OnMenuOpen()
    {
        _player.SetImmediate(0f); // 即時切り替え
    }
}
```

### CrossfadeHandle: 非 MonoBehaviour 制御

DI やステートマシンから制御する場合は CrossfadeHandle を使用します。

```csharp
using SapCrossfadeAudio.Runtime.Core.Integration;
using SapCrossfadeAudio.Runtime.Core.Types;

public class AudioService
{
    private CrossfadeHandle _handle;

    public void Initialize(AudioSource source)
    {
        _handle = CrossfadeHandle.FromAudioSource(source);
    }

    public void CrossfadeTo(float position, float duration)
    {
        if (_handle.IsValid)
        {
            _handle.TryCrossfade(position, duration, CrossfadeCurve.EqualPower);
        }
    }
}
```

### Addressables: 遅延ロード対応

Addressables を使用する場合は、事前ロードでヒッチを回避できます。

```csharp
using SapCrossfadeAudio.Addressables;

public class AddressableBgmManager : MonoBehaviour
{
    [SerializeField] private AddressableClipGeneratorAsset _generator;
    [SerializeField] private AudioSource _audioSource;

    async void Start()
    {
        // 事前ロード（ヒッチ回避）
        await _generator.PreloadAsync();

        if (_generator.IsReady)
        {
            _audioSource.generator = _generator;
            _audioSource.Play();
        }
    }

    void OnDestroy()
    {
        // 冪等な解放（何度呼んでも安全）
        _generator.Release();
    }
}
```

---

## API Reference

### CrossfadeCommand

クロスフェード操作を指示するコマンド構造体。

```csharp
public struct CrossfadeCommand
{
    public float TargetPosition01;   // 0.0 = Source A, 1.0 = Source B
    public float DurationSeconds;    // フェード時間（秒）
    public CrossfadeCurve Curve;     // フェードカーブ

    public static CrossfadeCommand Create(
        float targetPosition01,
        float durationSeconds,
        CrossfadeCurve curve);
}
```

### CrossfadeCurve

```csharp
public enum CrossfadeCurve
{
    EqualPower,  // エネルギー一定（推奨）
    Linear,      // 線形補間
    SCurve       // スムーズステップ
}
```

### CrossfadeGeneratorAsset

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `sourceA` | `ScriptableObject` | ソース A（IAudioGenerator） |
| `sourceB` | `ScriptableObject` | ソース B（IAudioGenerator） |
| `initialPosition01` | `float` | 初期フェード位置（0-1） |
| `initialCurve` | `CrossfadeCurve` | 初期カーブ |
| `defaultFadeSeconds` | `float` | デフォルトフェード秒数 |

### CrossfadeHandle

非 MonoBehaviour から CrossfadeGenerator を操作するための軽量ハンドル。

```csharp
public readonly struct CrossfadeHandle
{
    public bool IsValid { get; }
    public bool TryCrossfade(float target, float duration, CrossfadeCurve curve);
    public bool TryCrossfadeToA(float duration, CrossfadeCurve curve);
    public bool TryCrossfadeToB(float duration, CrossfadeCurve curve);
    public bool TrySetImmediate(float position);

    public static CrossfadeHandle FromAudioSource(AudioSource source);
}
```

### CrossfadePlayer

Inspector 統合用の MonoBehaviour ラッパー。

| メソッド | 説明 |
|---------|------|
| `Play()` | Generator を設定して再生開始 |
| `Stop()` | 再生停止 |
| `CrossfadeToA(duration, curve)` | Source A へクロスフェード |
| `CrossfadeToB(duration, curve)` | Source B へクロスフェード |
| `Crossfade(target, duration, curve)` | 指定位置へクロスフェード |
| `SetImmediate(position)` | 即座に位置を設定 |

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `Handle` | `CrossfadeHandle` | 現在の操作ハンドル |
| `IsPlaying` | `bool` | 再生中かどうか |
| `AudioSource` | `AudioSource` | 内部の AudioSource |

### AddressableClipGeneratorAsset

Addressables を使用した AudioClip ジェネレーター。

| メソッド | 説明 |
|---------|------|
| `PreloadAsync()` | アセットを事前ロード |
| `Release()` | アセットを解放（冪等） |

| プロパティ | 型 | 説明 |
|-----------|-----|------|
| `IsReady` | `bool` | ロード済みで再生可能か |

---

## Architecture

```mermaid
flowchart TB
    subgraph HighLevel["High-Level (任意)"]
        CrossfadePlayer["CrossfadePlayer<br/>(MonoBehaviour wrapper)"]
    end

    subgraph CoreLayer["Core Layer"]
        CrossfadeGenerator["CrossfadeGenerator<br/>(2ソースミックス)"]
        ClipGenerator["ClipGenerator<br/>(AudioClip 再生)"]
    end

    subgraph Foundation["Foundation"]
        SapCompat["SapCompat<br/>(SAP境界隔離)"]
        NativeBufferPool["NativeBufferPool<br/>(メモリ再利用)"]
        Resampler["Resampler<br/>(sampleRate 吸収)"]
    end

    HighLevel --> CoreLayer
    CoreLayer --> Foundation
```

### Data Flow

```
Asset (SO)  ──CreateInstance()──►  GeneratorInstance
                                         │
              ┌──────────────────────────┼──────────────────────────┐
              ▼                          ▼                          ▼
         Control              Pipe      Realtime        Buffer     Output
        (Main Thread) ───────────────► (Audio Thread) ──────────► AudioSource
```

### Design Principles

| 原則 | Control（メイン） | Realtime（オーディオ） |
|------|------------------|----------------------|
| メモリ確保 | ✅ Persistent | ❌ 禁止 |
| Unity API | ✅ 許可 | ❌ 禁止 |
| 例外送出 | ⚠️ 最小限 | ❌ 禁止 |
| Burst | ❌ 非対応 | ✅ 対応 |

---

## Crossfade Curves

| カーブ | 特性 | 数式 | 用途 |
|--------|------|------|------|
| **EqualPower** | エネルギー一定 | `wA = cos(p × π/2)`<br>`wB = sin(p × π/2)` | 推奨（音量の谷がない） |
| **Linear** | 線形 | `wA = 1 - p`<br>`wB = p` | シンプル |
| **SCurve** | スムーズ | `s = p² × (3 - 2p)` | 滑らかな遷移 |

---

## Technical Notes

### AudioClip の制約

| LoadType | GetData | 備考 |
|----------|---------|------|
| **DecompressOnLoad** | ✅ | 推奨 |
| CompressedInMemory | ⚠️ | 動作する場合あり |
| Streaming | ❌ | **動作しない**（Unity 仕様） |

### Thread Safety

- **Main → Audio**: `Pipe.SendData()` で安全に通信
- **Audio → Main**: 直接通信は非推奨

### Memory Management

- **バッファプール上限**: 8M floats ≒ 32MB
- **サイズ別上限**: 8 個/サイズ

---

## Project Structure

```
SapCrossfadeAudio/
├── Runtime/
│   └── Core/
│       ├── SapCrossfadeAudio.Core.asmdef  # 外部依存なし
│       ├── Foundation/
│       │   ├── SapCompat.cs              # SAP 境界隔離
│       │   ├── NativeBufferPool.cs       # メモリプーリング
│       │   └── Resampling/               # リサンプリング
│       ├── Types/
│       │   ├── CrossfadeCommand.cs
│       │   └── CrossfadeCurve.cs
│       ├── Generators/
│       │   ├── Clip/                     # AudioClip 再生
│       │   └── Crossfade/                # クロスフェード
│       ├── Integration/
│       │   └── CrossfadeHandle.cs        # 非MonoBehaviour制御
│       └── Components/
│           └── CrossfadePlayer.cs        # MonoBehaviourラッパー
├── Addressables/                         # 任意（別 asmdef）
│   ├── IPreloadableAudioGenerator.cs
│   └── AddressableClipGeneratorAsset.cs
└── Tests/
    ├── Editor/                           # EditModeテスト
    └── Runtime/                          # PlayModeテスト
```

---

## Testing

### テストの実行

**Unity Editor で実行**:
1. Window > General > Test Runner を開く
2. EditMode または PlayMode タブを選択
3. "Run All" をクリック、または特定のテストを選択

**テストカバレッジ**:
- EditMode: NativeBufferPool, Resampler, CrossfadeCommand
- PlayMode: CrossfadeHandle, CrossfadePlayer 統合

### CI/CD

GitHub Actions でテストが自動実行されます:
- `test.yml` - push/PR 時に EditMode/PlayMode テストを実行
- `release.yml` - タグ push 時にリリースを作成

---

## When to Use This Library

### ✅ Best For

- **BGM 遷移** - シームレスな音楽切り替え
- **サンプル精度が必要な演出** - 厳密なタイミング制御
- **Burst 最適化が必要** - 低レイテンシ要件

### ❌ Consider Alternatives

| 要件 | 推奨 |
|------|------|
| 一般的な BGM フェード | AudioMixer |
| 複雑なインタラクティブ音楽 | FMOD / Wwise |
| 3D 空間音響 | Unity Audio |

---

## Troubleshooting

### よくある問題

<details>
<summary><strong>音が出ない</strong></summary>

1. AudioSource が Play() されているか確認
2. AudioClip の LoadType が `DecompressOnLoad` か確認
3. `generatorInstance` が有効か確認:
   ```csharp
   if (!ControlContext.builtIn.Exists(audioSource.generatorInstance))
       Debug.LogWarning("Generator not active");
   ```

</details>

<details>
<summary><strong>クロスフェードが反映されない</strong></summary>

1. コマンド送信前に `Exists()` チェック
2. `DurationSeconds` が 0 以上か確認
3. `TargetPosition01` が 0-1 の範囲か確認

</details>

<details>
<summary><strong>Burst コンパイルエラー</strong></summary>

1. `com.unity.burst` パッケージがインストールされているか確認
2. Unity 6.3.4f1 以降を使用しているか確認

</details>

---

## Contributing

バグ報告や機能リクエストは [Issues](../../issues) へお願いします。

プルリクエストも歓迎です！

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

```
MIT License
Copyright (c) 2026 Tomo Ludens
```

---

## Acknowledgments
- [New in Unity 6.3 - Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/WhatsNewUnity63.html)
- [Scriptable audio pipeline - Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/audio-scriptable-processors.html)
- [Scriptable processors concepts - Unity Manual](https://docs.unity3d.com/Manual/audio-scriptable-processors-concepts.html)
- [Example: Create a root output - Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/audio-scriptable-processors-example-creating-a-root-output.html)
- [AudioSettings.GetDSPBufferSize - Unity Scripting API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/AudioSettings.GetDSPBufferSize.html)
- [Audio in Web - Unity Manual](https://docs.unity3d.com/6000.3/Documentation/Manual/webgl-audio.html)
- [Unity 6000.3.0f1 Release Notes - Unity](https://unity.com/releases/editor/whats-new/6000.3.0)
- [Unity SAP Documentation](https://docs.unity3d.com/6000.4/Documentation/Manual/audio-scriptable-processors.html)
