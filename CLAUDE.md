# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CrossfadeAudio is a Unity 6.3+ Scriptable Audio Pipeline (SAP) library providing sample-accurate crossfade playback between two audio sources. Written in Japanese and English, this library focuses on BGM transitions with zero-exception, allocation-free audio processing.

**Target Unity Version**: 6.3.4f1+
**Required Packages**: `com.unity.burst`, `com.unity.collections`, `com.unity.mathematics`
**Optional Packages**: `com.unity.addressables` (separate asmdef)

## Building and Testing

This is a Unity package meant to be embedded in Unity projects. There are no standalone build commands - integration happens through Unity's package system.

**Assembly Definitions**:
- `CrossfadeAudio.Core.asmdef` - Core library (no external dependencies beyond Burst)
- `CrossfadeAudio.Addressables.asmdef` - Optional Addressables integration
- `CrossfadeAudio.Tests.Editor.asmdef` - EditMode tests
- `CrossfadeAudio.Tests.Runtime.asmdef` - PlayMode tests

### Running Tests

**In Unity Editor**:
1. Open Window > General > Test Runner
2. Select EditMode or PlayMode tab
3. Click "Run All" or select specific tests

**Test Coverage**:
- EditMode: NativeBufferPool, Resampler, CrossfadeCommand
- PlayMode: CrossfadeHandle, CrossfadePlayer integration

### CI/CD

GitHub Actions workflows are configured in `.github/workflows/`:
- `test.yml` - Runs EditMode and PlayMode tests on push/PR
- `release.yml` - Creates releases on version tags

## Architecture Overview

### Core Design Pattern: Asset-Control-Realtime Triplet

Every generator follows this SAP pattern:

1. **Asset** (ScriptableObject): Implements `IAudioGenerator`, holds editor configuration
2. **Control** (struct): Implements `IControl<TRealtime>`, runs on main thread, handles initialization and message passing
3. **Realtime** (struct): Implements `IRealtime`, runs on audio thread with `[BurstCompile]`, performs actual audio processing

**Critical Threading Constraint**:
- `Configure()` runs in a **Job context**, where `Allocator.Persistent` is forbidden
- Buffer allocation MUST happen in `CreateInstance()` (main thread) before Configure is called
- This is why CrossfadeGeneratorAsset pre-allocates buffers using NativeBufferPool.Rent()

### Layer Structure

```
Foundation
├── SapCompat          // Isolates SAP API boundary for version changes
├── NativeBufferPool   // Memory pooling for Control-side buffers
├── Resampler          // Sample rate conversion (Nearest/Linear/Hermite4)
└── ClipRequirements   // AudioClip.GetData() constraint validation

Core Generators
├── ClipGenerator      // Simple AudioClip playback with resampling
└── CrossfadeGenerator // Two-source mixer with crossfade curves

Integration
├── CrossfadeHandle    // Non-MonoBehaviour control surface ✅ Implemented
└── CrossfadePlayer    // MonoBehaviour wrapper ✅ Implemented

Components
└── CrossfadePlayer    // Inspector integration

Addressables (optional)
├── IPreloadableAudioGenerator  // Preload interface ✅ Implemented
└── AddressableClipGeneratorAsset // Addressables integration ✅ Implemented

Smoke (Development/Testing)
└── SineSmokeGenerator // Sine wave generator for SAP smoke testing
```

### CrossfadeGenerator Data Flow

```
CrossfadeGeneratorAsset (SO)
    ↓ CreateInstance() - main thread
    ├─ Allocate BufferDataA/B via NativeBufferPool.Rent()
    ├─ Create ChildA/B generator instances
    └─ Return GeneratorInstance
        ↓
CrossfadeGeneratorControl (Main Thread)
    ├─ Configure() - validates child formats, initializes realtime state
    ├─ OnMessage() - receives CrossfadeCommand, forwards via Pipe
    └─ Dispose() - destroys children, returns buffers to pool
        ↓ Pipe (lock-free communication)
CrossfadeGeneratorRealtime (Audio Thread)
    ├─ Update() - reads CrossfadeCommand from Pipe
    └─ Process() - fetches child buffers, applies crossfade curve, mixes output
```

### SapCompat: Version Isolation Layer

Unity SAP APIs may change between 6.x versions. `SapCompat` isolates these points:

- `GetProcessedFrames()` - extracts frame count from GeneratorInstance.Result
- `IsShortWrite()` - detects when child didn't fulfill request (end of stream)
- `IsExhausted()` - detects complete exhaustion (0 frames)

When Unity SAP APIs change, update only this file.

## Key Implementation Constraints

### 1. Zero-Exception Policy

Never throw exceptions in Realtime code. Always degrade gracefully to silence:

- AudioClip not set → silence
- Load failed → silence
- Sample rate mismatch (with ResampleMode.Off) → silence
- Buffer inconsistency → silence
- Child generator not created → silence

### 2. Control/Realtime Thread Separation

| Context | Main Thread (Control) | Audio Thread (Realtime) |
|---------|----------------------|-------------------------|
| Memory allocation | ✅ Persistent allowed | ❌ Forbidden |
| Unity API | ✅ Allowed | ❌ Forbidden |
| Exceptions | ⚠️ Minimize | ❌ Forbidden |
| Burst | ❌ Not supported | ✅ Required |

### 3. Job Context in Configure()

`Configure()` runs in a **Job context** (since Unity 6.3), meaning:

- ❌ Cannot allocate with `Allocator.Persistent`
- ❌ Limited managed object access
- ✅ Only `Allocator.Temp` available
- **Solution**: Pre-allocate all buffers in `CreateInstance()` on main thread

### 4. AudioClip Constraints

Unity's `AudioClip.GetData()` behavior by LoadType:

| LoadType | GetData() | Recommendation |
|----------|-----------|----------------|
| DecompressOnLoad | ✅ Works | Recommended |
| CompressedInMemory | ⚠️ May work | Use with caution |
| Streaming | ❌ **Does not work** | Cannot use |

**Important**: Unity documentation explicitly states GetData "does not work with streamed audio clips". Use PagedClipGenerator for large files or PcmStreamGenerator for true streaming.

### 5. Ownership and Release Idempotency

When using Addressables:
- Explicit `LoadAsync()` / `Release()` required (Unity doesn't auto-manage)
- `Release()` must be idempotent (safe to call multiple times)
- Document who loads and who releases for each asset

## Crossfade Implementation Details

### Fade Curves

| Curve | Formula | Use Case |
|-------|---------|----------|
| EqualPower | wA = cos(p×π/2), wB = sin(p×π/2) | Recommended - maintains constant energy |
| Linear | wA = 1-p, wB = p | Simple linear blend |
| SCurve | s = p²×(3-2p), wA = 1-s, wB = s | Smooth start/end |

### CrossfadeCommand Structure

```csharp
public struct CrossfadeCommand
{
    public float TargetPosition01;   // 0.0=A, 1.0=B
    public float DurationSeconds;    // Fade duration
    public CrossfadeCurve Curve;     // Curve type
}
```

Sent from main thread via `OnMessage()` → `Pipe.SendData()`, received in `Realtime.Update()`.

### CrossfadeHandle Usage

```csharp
// Get handle from AudioSource
var handle = CrossfadeHandle.FromAudioSource(audioSource);

// Check validity before use
if (handle.IsValid)
{
    handle.TryCrossfadeToB(2f, CrossfadeCurve.EqualPower);
}
```

### CrossfadePlayer Usage

```csharp
// Attach to GameObject with AudioSource
[SerializeField] CrossfadePlayer player;

// Simple crossfade
player.CrossfadeToB(2f, CrossfadeCurve.EqualPower);
player.CrossfadeToA(1.5f, CrossfadeCurve.Linear);
player.SetImmediate(0.5f); // Instant position change
```

## Memory Management

### NativeBufferPool

Control-side buffer pooling to reduce allocation spikes:

```csharp
// Rent a buffer
var buffer = NativeBufferPool.Rent(length: requiredFloats);

// Use buffer...

// Return when done (in Control.Dispose)
NativeBufferPool.Return(ref buffer);
```

**Limits**:
- Per-size limit: 8 buffers per size
- Total limit: 8M floats ≈ 32MB

## Resampling

When child generator sample rate doesn't match output:

**ResampleMode**:
- `Off` - No resampling, mismatch → silence
- `Auto` - Resample only if needed (default)
- `Force` - Always resample

**ResampleQuality**:
- `Nearest` - Nearest neighbor (fastest)
- `Linear` - Linear interpolation (recommended)
- `Hermite4` - 4-point Hermite (highest quality)

## Addressables Integration

### AddressableClipGeneratorAsset

For projects using Addressables:

```csharp
// Preload before playback to avoid hitches
await addressableGenerator.PreloadAsync();

// Check if ready
if (addressableGenerator.IsReady)
{
    audioSource.generator = addressableGenerator;
    audioSource.Play();
}

// Release when done
addressableGenerator.Release(); // Idempotent - safe to call multiple times
```

## Implementation Status

### ✅ Implemented

- **CrossfadeHandle** - Non-MonoBehaviour control surface
- **CrossfadePlayer** - MonoBehaviour wrapper for Inspector integration
- **AddressableClipGeneratorAsset** - Addressables integration with preload support
- **IPreloadableAudioGenerator** - Interface for preloadable generators
- **Test Suite** - EditMode and PlayMode tests

### 🔜 Not Yet Implemented

- **PagedClipGenerator** - Paged AudioClip playback for large files
- **PcmStreamGenerator** - External PCM streaming

### Performance Considerations

- Burst compilation enabled for Realtime structs with `FloatMode.Fast`, `FloatPrecision.Medium`
- `[MethodImpl(AggressiveInlining)]` used for critical paths
- Buffer clearing every frame has cost - only clear required ranges

## Development Guidelines

### When Adding New Generators

1. Follow Asset-Control-Realtime triplet pattern
2. Pre-allocate all buffers in CreateInstance() (not Configure)
3. Use SapCompat for all SAP API interactions
4. Never throw exceptions in Realtime code
5. Return buffers to NativeBufferPool in Control.Dispose()
6. Validate format compatibility before processing

### When Adding Tests

1. EditMode tests go in `Tests/Editor/` with `[TestFixture]` attribute
2. PlayMode tests go in `Tests/Runtime/`
3. Use `[SetUp]` and `[TearDown]` for resource management
4. Test both success and failure paths

### When Modifying SAP Integration

Only modify `SapCompat.cs` to maintain isolation. Add inline documentation for why changes are needed.

### Namespace Convention

All code uses `TomoLudens.CrossfadeAudio.Runtime.Core` namespace (root: `TomoLudens`).

## Documentation References

- Detailed technical design: `Docs/CrossfadeAudio_DesignDocument_v1.1.0_Unity6.3_APIAligned.md`
- Comprehensive architecture overview: `README.md` (Japanese)
- Unity SAP Documentation: docs.unity3d.com/Manual/audio-scriptable-processors-concepts.html

## Future Roadmap (from design doc)

**Short-term** (maintaining crossfade focus):
- ~~Complete CrossfadeHandle implementation~~ ✅ Done
- Enhanced error diagnostics in Control (dev build only)
- Full Configure() stability for Job context

**Mid-term**:
- Internal resampling in PcmStreamGenerator
- Stricter paged clip supply tracking (generation IDs)
- Fade completion callbacks
- Fade command queueing

**Out of Scope** (external library responsibility):
- Audio codec decoding (mp3/ogg)
- Network I/O
- 3D spatial audio (Unity built-in)
- Audio effects (reverb, etc.)
