# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**CrossfadeAudio** is a Unity 6.3+ Scriptable Audio Pipeline (SAP) library for sample-accurate crossfading between two audio sources. The library is designed for BGM transitions with zero exceptions, allocation-free realtime processing, and Burst compilation.

- **Unity Version**: 6000.3.2f1 (requires Unity 6.3+)
- **Target API**: Unity Scriptable Audio Pipeline (SAP)
- **Language**: C# with Burst-compiled realtime code
- **Required Package**: com.unity.burst
- **Optional Package**: com.unity.addressables (separate asmdef if implemented)

## Repository Structure

```
Assets/Plugins/unity-sap-crossfade-audio/
├── CrossfadeAudio_DesignDocument_v1.1.0_Unity6.3_APIAligned.md  # Complete technical spec (Japanese)
└── [Source code to be implemented here]
```

**Current State**: This repository contains the design document but the actual implementation has not been created yet. All code should be placed in `Assets/Plugins/unity-sap-crossfade-audio/`.

## Architecture Layers

The library follows a strict layered architecture:

### 1. Foundation Layer
- **SapCompat**: Isolates SAP API changes across Unity versions (inline wrappers for Pipe, ControlContext, ChannelBuffer)
- **NativeBufferPool**: Control-side NativeArray pooling to reduce allocation spikes on reconfiguration
- **Resampler**: Burst-compatible sample rate conversion (Nearest/Linear/Hermite4)
- **ClipRequirements**: AudioClip.GetData validation (DecompressOnLoad only, no streaming clips)

### 2. Core Generators
- **ClipGenerator**: Basic AudioClip playback with resampling support
- **PagedClipGenerator**: Paged AudioClip playback for large files (avoids full PCM copy)
- **PcmStreamGenerator**: External PCM streaming via IPcmPageProvider interface
- **CrossfadeGenerator**: Two-source mixer with Equal-Power/Linear/S-Curve crossfade

### 3. Integration Surface
- **CrossfadeHandle**: Non-MonoBehaviour handle for controlling crossfades via ControlContext.builtIn
- **PreloadCoordinator**: (Optional) Asset preload/unload coordination

### 4. High-Level (Optional)
- **CrossfadePlayer**: MonoBehaviour wrapper providing Unity Inspector integration

## Critical Design Rules

### Realtime vs Control Separation
- **Control** (main thread): Resource allocation, AudioClip.GetData, Pipe.SendData, NativeArray management
- **Realtime** (audio thread): Burst-compiled, allocation-free, exception-free, no Unity API calls
- Communication: Control → Realtime via `Pipe.SendData`, Realtime reads via `Pipe.GetAvailableData`

### Zero Exception Policy
- Never throw exceptions in Realtime code
- Gracefully handle: null clips, load failures, mismatched sample rates, buffer underruns
- **Fallback**: Silent audio output on any error

### AudioClip.GetData Requirements
- Only works with `LoadType = DecompressOnLoad`
- **Fails** with streamed clips (LoadType.Streaming)
- GetData returns false and fills buffer with zeros on failure
- Always check `ClipRequirements.CanUseGetData()` before use

### Reconfiguration Safety
- `Configure()` may be called multiple times
- Always dispose/return pooled resources in Control.Dispose()
- Use `NativeBufferPool.Return()` instead of direct Dispose() for pooled buffers

### Ownership Rules (Addressables)
- AssetReference does NOT auto-load/release
- Explicit LoadAsync()/Release() required
- Release() must be idempotent (safe to call multiple times)

## Key Technical Points

### SAP Core Concepts
- `ControlContext.builtIn`: Standard Control context for managing generators
- `GeneratorInstance`: Created via `AudioSource.generator`, controlled via `generatorInstance`
- `ChannelBuffer`: 2D view (channels × frames) with interleaved internal layout
- `Pipe`: Lock-free Control ↔ Realtime communication channel

### Resampling
- **ResampleMode.Off**: Mismatch → silent fallback (legacy compat)
- **ResampleMode.Auto**: Mismatch → resample (recommended default)
- **ResampleMode.Force**: Always resample (testing/validation)
- Quality: Nearest / Linear / Hermite4

### Streaming Approaches
1. **PagedClipGenerator**: Pages AudioClip data in Control thread, feeds to Realtime ring buffer
2. **PcmStreamGenerator**: External PCM provider (IPcmPageProvider) for true streaming (network, decoder, etc.)

### Buffer Management
- Control side: Use `NativeBufferPool.Rent/Return` for persistent NativeArray buffers
- Realtime side: Read-only access to pre-allocated buffers, never allocate
- Guard frames: +1 frame per page for interpolation stability at boundaries

## Development Workflow

### When implementing generators:
1. Create ScriptableObject asset class implementing `IAudioGenerator`
2. Define Control struct implementing `GeneratorInstance.IControl<TRealtime>`
3. Define Realtime struct implementing `GeneratorInstance.IRealtime` with `[BurstCompile]`
4. Isolate SAP API calls via `SapCompat` static methods
5. Ensure Configure() cleans up previous allocations before creating new ones
6. Test reconfiguration by changing AudioSource.generator at runtime

### When adding new features:
- Maintain "crossfade specialization" scope (avoid general-purpose audio processing)
- Keep external dependencies optional (e.g., Addressables in separate asmdef)
- No audio decoding/encoding (external library responsibility)
- Provide PCM input interfaces, not format parsers

### Testing AudioClip compatibility:
```csharp
if (!ClipRequirements.CanUseGetData(clip))
{
    Debug.LogWarning($"Clip '{clip.name}' incompatible (streamed or compressed)");
    // Fall back to silence
}
```

## Common Patterns

### Sending commands from Control to Realtime:
```csharp
// In Control.OnMessage
var cmd = message.Get<CrossfadeCommand>();
var rtParams = new CrossfadeRealtimeParams(cmd.TargetPosition, cmd.DurationSeconds * sampleRate, cmd.Curve);
SapCompat.SendData(pipe, context, rtParams);
```

### Reading Pipe data in Realtime:
```csharp
// In Realtime.Update
var it = SapCompat.GetAvailableData(pipe, context);
foreach (var element in it)
{
    if (element.TryGetData(out MyData data))
    {
        // Process command
    }
}
```

### Using CrossfadeHandle:
```csharp
var handle = new CrossfadeHandle(audioSource.generatorInstance);
if (handle.IsValid)
{
    handle.TryCrossfadeToB(2.0f, CrossfadeCurve.EqualPower);
}
```

## Reference Documentation

The complete technical specification with code samples is in:
- `CrossfadeAudio_DesignDocument_v1.1.0_Unity6.3_APIAligned.md`

This document contains:
- Full struct/class definitions for all components
- Resampling algorithms
- Paging implementation details
- Safety checklists
- Usage examples

When implementing any component, refer to the design document for the exact struct layout, field names, and algorithmic details.
