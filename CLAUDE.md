# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**SapCrossfadeAudio** is a Unity 6.3+ library for sample-accurate crossfading using the Scriptable Audio Pipeline (SAP). It provides zero-allocation, Burst-optimized audio processing with strict thread-safety guarantees.

Key characteristics:
- **Zero-exception policy**: Unset/error states output silence rather than throwing
- **Control/Realtime separation**: Main thread (Control) handles setup/commands; audio thread (Realtime) processes samples
- **Burst-compiled**: All realtime processing uses Burst for SIMD optimization
- **Memory pooling**: NativeBuffer reuse to prevent GC pressure during reconfiguration

## Running Tests

Unity Test Runner is the primary testing interface:

**In Unity Editor:**
1. Window > General > Test Runner
2. Select EditMode or PlayMode tab
3. Run all tests or select specific test classes

**Command Line:**
```bash
# EditMode tests (recommended for CI)
Unity.exe -runTests -batchmode -projectPath . -testPlatform EditMode -testResults results.xml

# PlayMode tests (requires AudioSource initialization)
Unity.exe -runTests -batchmode -projectPath . -testPlatform PlayMode -testResults results.xml
```

**Test categories:**
- **EditMode**: `NativeBufferPoolTests`, `ResamplerTests`, `CrossfadeCommandTests` (fast, no Unity runtime needed)
- **PlayMode**: `CrossfadeHandleTests`, `CrossfadePlayerTests` (requires AudioSource, slower)

## Architecture

### Layer Separation

```
High-Level (optional)
├── CrossfadePlayer (MonoBehaviour wrapper for Inspector)
└── CrossfadeHandle (lightweight struct for DI/service patterns)

Core Layer
├── CrossfadeGenerator (2-source mixer, main orchestrator)
└── ClipGenerator (AudioClip playback)

Foundation
├── SapCompat (SAP API boundary isolation)
├── NativeBufferPool (Persistent allocator memory reuse)
├── Resampler (sampleRate mismatch handling)
└── CrossfadeLogger (conditional logging)
```

### Thread Model

| Layer | Thread | Memory | Unity API | Exceptions | Burst |
|-------|--------|--------|-----------|------------|-------|
| **Control** | Main | Persistent ✅ | Allowed ✅ | Minimal ⚠️ | No ❌ |
| **Realtime** | Audio | Temp only ❌ | Forbidden ❌ | Never ❌ | Yes ✅ |

Communication: Control → Realtime via `Pipe.SendData()` (thread-safe command queue)

### Data Flow

1. **Asset → Instance**: `IAudioGenerator.CreateInstance()` allocates Control + Realtime structs
2. **Control thread**: Receives user commands, validates, sends via Pipe
3. **Realtime thread**: Reads Pipe in `Update()`, processes audio in `Process()`, writes to ChannelBuffer
4. **Child generators**: Recursively created (e.g., CrossfadeGenerator contains 2x ClipGenerator instances)

### Key Design Patterns

**Zero-exception in Realtime:**
- All error paths output silence (zero-fill buffers)
- Invalid state checks use early returns with `processedFrames = 0`
- Example: Missing AudioClip → log warning once in Control, output silence in Realtime

**Idempotent Release:**
- `NativeBufferPool.Return()` checks `IsCreated` before disposal
- Multiple `Release()` calls are safe (common in OnDestroy/cleanup)

**SAP Boundary Isolation (SapCompat):**
- All SAP API access goes through `SapCompat.cs`
- Isolates Unity version changes (e.g., `GeneratorInstance.Result` field names)
- Enables testing with mocked SAP types

## Assembly Structure

```
SapCrossfadeAudio.Core.asmdef
├── References: Unity.Burst, Unity.Collections, Unity.Mathematics
├── Contains: All core generators, Foundation layer
└── No unsafe code, auto-referenced

SapCrossfadeAudio.Addressables.asmdef
├── References: SapCrossfadeAudio.Core, Unity.Addressables, Unity.ResourceManager
├── Contains: AddressableClipGeneratorAsset, IPreloadableAudioGenerator
├── Define: CROSSFADEAUDIO_ADDRESSABLES (when Addressables 1.20.0+ present)
└── Separate asmdef prevents forced Addressables dependency

SapCrossfadeAudio.Editor.asmdef
├── Contains: NativeBufferPoolEditorCleanup (PlayMode stop + reload hooks)

SapCrossfadeAudio.Tests.Editor.asmdef
├── EditMode tests (NativeBufferPool, Resampler, CrossfadeCommand)

SapCrossfadeAudio.Tests.Runtime.asmdef
├── PlayMode tests (CrossfadeHandle, CrossfadePlayer integration)
```

## Critical Implementation Rules

### Memory Management

**NativeBufferPool lifecycle:**
- Automatically cleared on: PlayMode stop (Editor), Assembly reload (Editor), SubsystemRegistration, Application quit
- Manual clear: `NativeBufferPoolEditorCleanup.cs` hooks into Editor events
- Pool limits: 8 buffers per size, 8M total floats (≈32MB)

**When to use Persistent allocator:**
- Control thread ONLY
- CreateInstance(), Configure() methods
- Pre-allocate buffers before Realtime thread sees them

**When to use Temp allocator:**
- Realtime thread (Job context)
- Short-lived scratch buffers within Process()

### Logging

**CrossfadeLogger:**
- Uses `#if UNITY_EDITOR || DEVELOPMENT_BUILD` guards
- Caches type tags via `TypeTagCache` to reduce string allocation
- Context parameter provides clickable Inspector references

**Usage pattern:**
```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
CrossfadeLogger.LogWarning<MyType>("Message", context: this);
#endif
```

### AudioClip Constraints

| LoadType | GetData() | Usability |
|----------|-----------|-----------|
| **DecompressOnLoad** | ✅ Works | **Recommended** |
| CompressedInMemory | ⚠️ Sometimes | Unity version dependent |
| Streaming | ❌ Fails | Cannot use `GetData()` |

### Burst Compilation

**Realtime structs:**
- Must implement `GeneratorInstance.IRealtime`
- Decorate with `[BurstCompile(CompileSynchronously = true)]`
- No managed references (string, object, etc.)
- Math via `Unity.Mathematics` (not System.Math)

**Process() signature:**
```csharp
public GeneratorInstance.Result Process(
    in RealtimeContext context,
    ProcessorInstance.Pipe pipe,
    ChannelBuffer buffer,
    GeneratorInstance.Arguments args)
```

## Common Modification Scenarios

### Adding a New Generator

1. Create `MyGeneratorAsset : ScriptableObject, IAudioGenerator`
2. Create `MyGeneratorControl` (main thread state)
3. Create `MyGeneratorRealtime : GeneratorInstance.IRealtime` with `[BurstCompile]`
4. In `CreateInstance()`:
   - Allocate Persistent buffers on Control thread
   - Initialize Realtime struct
   - Call `context.AllocateGenerator(realtime, control, ...)`
5. Add CreateAssetMenu attribute for Inspector workflow

### Modifying Crossfade Behavior

**Crossfade curve formulas (in `CrossfadeGeneratorRealtime.cs`):**
- `EqualPower`: `wA = cos(p * π/2)`, `wB = sin(p * π/2)`
- `Linear`: `wA = 1 - p`, `wB = p`
- `SCurve`: `s = p² * (3 - 2p)`, then apply as Linear

**Command dispatch:**
- Commands sent via `CrossfadeHandle.TryCrossfade()` → `ControlContext.builtIn.SendMessage()`
- Realtime receives in `Update()` → `Pipe.GetAvailableData()`
- State updated before next `Process()` call

### Addressables Integration

**AddressableClipGeneratorAsset:**
- Separate asmdef prevents Addressables dependency for core library
- `PreloadAsync()` recommended to avoid `WaitForCompletion()` hitches
- `_allowSynchronousLoadFallback` defaults to false (must explicitly enable)

**Preload workflow:**
```csharp
await generatorAsset.PreloadAsync();
if (generatorAsset.IsReady)
    audioSource.generator = generatorAsset;
```

## Testing Guidelines

**EditMode test structure:**
- Fast, no Unity runtime overhead
- Use `[Test]` attribute, `UnityTest.Assert`
- Focus on pure logic: buffer pooling, resampling math, command validation

**PlayMode test structure:**
- Slower, requires Unity Audio system initialization
- Use `[UnityTest]` with `IEnumerator` for frame delays
- Test integration: AudioSource assignment, generator instance lifecycle

**Common test patterns:**
```csharp
// EditMode: Direct struct manipulation
[Test]
public void NativeBufferPool_Rent_ReturnsValidArray() {
    var buffer = NativeBufferPool.Rent(1024);
    Assert.IsTrue(buffer.IsCreated);
    NativeBufferPool.Return(ref buffer);
}

// PlayMode: AudioSource integration
[UnityTest]
public IEnumerator CrossfadeHandle_IsValid_ReturnsTrueWhenPlaying() {
    var go = new GameObject();
    var source = go.AddComponent<AudioSource>();
    source.generator = testGeneratorAsset;
    source.Play();
    yield return null; // Wait for generator instantiation

    var handle = CrossfadeHandle.FromAudioSource(source);
    Assert.IsTrue(handle.IsValid);
}
```

## Debugging Tips

**Silent audio output:**
1. Check AudioClip LoadType (must be DecompressOnLoad)
2. Verify `generatorInstance` exists: `ControlContext.builtIn.Exists(audioSource.generatorInstance)`
3. Enable Development Build to see CrossfadeLogger warnings
4. Check if child generators are properly set in Inspector

**Crossfade not working:**
1. Verify `ControlContext.builtIn.Exists()` before sending commands
2. Check duration ≥ 0 and target position in [0, 1]
3. Ensure AudioSource is playing (commands are no-op when stopped)

**Memory leaks (Editor):**
- `NativeBufferPool` uses Persistent allocator → must be manually cleared
- EditorCleanup hooks should trigger automatically, but verify with Memory Profiler
- Manual clear: `NativeBufferPool.Clear()` if needed

**Burst compilation issues:**
- Realtime structs cannot contain managed types (string, List, etc.)
- Use NativeArray, NativeList, or blittable structs only
- Check Burst Inspector (Jobs > Burst > Inspector) for compilation errors
