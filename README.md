# Immersive Audio

`com.immersive.audio` owns Unity audio services and authoring primitives for Immersive packages.

The package is independent from `com.immersive.framework`. Framework code may consume Audio later, but Audio must not know framework hosts, routes, activities, game flow, QA projects, or old Base 2.0 composition.

## Installation

Use the package source already configured by the consuming project. Resolve the installed Audio and Pooling versions from `Packages/manifest.json` and `packages-lock.json`; this README does not prescribe versions for discovery.

## Dependencies

The package depends on `com.immersive.pooling` for pooled SFX support in the Unity assembly. `Immersive.Audio.Runtime` remains pure and does not reference Unity or pooling concrete types. Audio does not depend on the Logging package.

## Usage Guide

Start with the [Audio Usage Guide](Documentation~/Audio-Usage-Guide.md) for setup steps, direct SFX, pooled SFX, BGM, explicit failure handling, current QA path, and common configuration errors. [Architecture](Documentation~/Architecture.md) is the supporting contract for ownership, lifecycle, mixer limits, and dated certification evidence.

## Find a capability

| Intent | Procedure/API | Validation |
|---|---|---|
| Author cue/default assets and configure settings | [Usage Guide: authoring and settings](Documentation~/Audio-Usage-Guide.md#4-audiodefaultsasset) | Asset validation and explicit `AudioSettingsResolution` |
| Play direct or pooled SFX | [Direct SFX](Documentation~/Audio-Usage-Guide.md#9-direct-sfx), [Pooled SFX](Documentation~/Audio-Usage-Guide.md#10-pooled-sfx) | Inspect `AudioPlaybackResult`; pooled path requires explicit Pooling composition |
| Play, transition, or explicitly stop BGM | [BGM procedure](Documentation~/Audio-Usage-Guide.md#11-bgm) | Inspect result and use the documented QA evidence; QA-NEW-005 does not test physical fades |
| Ensure a listener and choose playback ownership | [Listener/host](Documentation~/Audio-Usage-Guide.md#7-audioruntimehost), [Architecture](Documentation~/Architecture.md) | `AudioListenerHostReport`, explicit host lifetime |
| Understand failures, unsupported behavior, and QA scope | [Failures and QA](Documentation~/Audio-Usage-Guide.md#12-explicit-failures), [Certification evidence](Documentation~/Architecture.md#certification-status) | QA scope and observable limits stated in the guide |

## Authoring API

`AudioCueAsset` is the abstract base for Unity cue assets. It stores explicit cue identity, one clip, volume, pitch, loop mode, and a routing bus key.

`AudioSfxCueAsset` adds SFX-specific authoring data: global/spatial playback mode, direct/pooled execution mode, optional pooled `PoolDefinitionAsset`, spatial blend, min/max distance, simultaneous instance budget, and retrigger cooldown.

`AudioBgmCueAsset` adds BGM transition data: fade in and fade out seconds. Loop mode is inherited from `AudioCueAsset`.

`AudioDefaultsAsset` stores project-level authoring defaults for master/SFX/BGM volume, routing bus keys, and default fade values.

The pure runtime assembly contains small value objects and enums used by authoring: `AudioCueId`, `AudioBusKey`, `AudioBusKeys`, `AudioLoopMode`, `AudioPlaybackMode`, and `AudioAuthoringRanges`.

The public pure contracts also include `IAudioSettingsService`, `IAudioSfxService`, `IAudioBgmService`, `IAudioPlaybackHandle`, `AudioSettingsResolution`/`AudioSettingsSnapshot`, `AudioRoutingResolution`, `AudioPlaybackResult`/`AudioPlaybackStatus`, and `AudioConfigurationIssue`. The Unity adapter supplies `AudioSettingsService`, `AudioRoutingResolver`, `AudioGlobalSfxService`, `AudioBgmService`, the cue/default assets, listener/runtime hosts, and direct/pooled playback handles. See the [Usage Guide](Documentation~/Audio-Usage-Guide.md) for composition and the [Architecture contract](Documentation~/Architecture.md) for assembly ownership and limits.

## Settings And Routing

`AudioSettingsService` resolves runtime-facing settings from an explicit `AudioDefaultsAsset`. If the asset is missing or invalid, it returns `AudioSettingsResolution` with `AudioConfigurationStatus.Failed` and explicit issues.

`AudioRoutingResolver` resolves SFX and BGM bus keys from cue data first, then from the explicit defaults asset. If the defaults asset is missing or the final bus key is invalid, it returns `AudioRoutingResolution` with explicit issues. It does not resolve Unity mixers in F3.

## Listener Host

`AudioListenerRuntimeHost` can be used as a scene-authored Unity component, and it also backs the package-owned persistent listener created by `AudioRuntimeHost` when `Ensure Persistent Listener` is enabled. It ensures an `AudioListener` on the configured target, on the host GameObject, or on the package-owned `ImmersiveAudioListener` object. It reports duplicate enabled listeners and does not destroy duplicate listeners.

The scene-authored duplicate policy defaults to `ReportOnly`. The `AudioRuntimeHost` persistent-listener path defaults to `DisableDuplicates` so gameplay cameras and UI cameras do not compete with the package listener. The persistent listener is the only narrow use of `DontDestroyOnLoad` in the package; playback services and `AudioRuntimeHost` do not persist as globals.

## Explicit Configuration Rule

`AUDIO-F-RULE-001 - Explicit Audio Configuration`: `AudioDefaultsAsset` is required for settings and routing. Missing defaults are a failed result, not an implicit fallback. Hardcoded values may exist only as authoring ranges, Inspector initial values, or validation helpers.

## Direct Playback

`IAudioPlaybackHandle` exposes `IsValid`, `IsPlaying`, and `Stop()`.

`AudioPlaybackResult` carries `AudioPlaybackStatus`, an optional handle for successful playback, and explicit configuration issues for failures. Failed playback does not return a hidden no-op handle.

`AudioGlobalSfxService` plays direct and pooled SFX. Direct SFX creates a temporary controlled `AudioSource` object under the configured playback root. Pooled SFX requires `AudioSfxCueAsset.ExecutionMode = Pooled`, a cue-level `PoolDefinitionAsset`, and an explicit `IPoolService` supplied by `AudioRuntimeHost` or by direct service composition.

If a cue requests pooled playback and no pool service or pool definition is available, playback fails with an explicit `AudioPlaybackResult`. It never falls back to direct playback silently.

`AudioBgmService` owns a dedicated `AudioSource` for BGM. It validates cue, clip, settings, and routing before playing. It supports `Play`, `Stop`, loop mode from the cue, and simple fade in/out.

`AudioRuntimeHost` is an optional explicit Unity component. It receives `AudioDefaultsAsset`, may receive an explicit `PoolRuntimeHost`, composes `AudioSettingsService`, `AudioRoutingResolver`, `AudioGlobalSfxService`, and `AudioBgmService`, ensures a persistent listener by default, and exposes simple manual `PlaySfx`, `PlayBgm`, and `StopBgm` methods.

## Current limits

- No AudioMixer binding.
- No framework bootstrap.
- No singleton or global service locator.
- No persistent global playback service.
- No silent fallback behavior.
- No automatic pool creation or global pool lookup.
- No BGM pooling.
- No GameJam sample assets.
- Editor custom inspectors support the existing cue/default/host authoring assets; they do not add a mixer authoring surface.

## License

Licensed under the [MIT License](LICENSE.md).
