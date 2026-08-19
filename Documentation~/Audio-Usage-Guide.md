# Immersive Audio Usage Guide

Status: Current
Last updated: 2026-08-19

This guide explains how to use `com.immersive.audio` directly from Unity code and scenes.

## Quick Start

1. Create an `AudioDefaultsAsset`.
2. Create an `AudioRuntimeHost` in the composition lifetime that should own playback.
3. Assign the defaults asset to the host.
4. Create an `AudioSfxCueAsset` or `AudioBgmCueAsset`.
5. Call `PlaySfx`, `PlayBgm`, or `StopBgm` explicitly.
6. If BGM must survive transient scene changes, keep the `AudioRuntimeHost` under an explicit longer-lived application/session/persistent-content owner.
7. For pooled SFX, configure `PoolRuntimeHost`, `PoolDefinitionAsset`, and an `AudioSource` prefab explicitly.

## 1. Package Role

`com.immersive.audio` provides reusable Unity audio authoring assets and runtime services for SFX, pooled SFX, BGM, settings, routing metadata, and listener handling.

It is a standalone technical package. It does not depend on `com.immersive.framework`, game flow, Routes, Activities, FIRSTGAME, QA projects, or old Base/GameJam runtime composition.

## 2. Dependencies

Required by the Unity assembly:

- `com.immersive.pooling` for explicit pooled SFX support.

Not required:

- `com.immersive.framework`;
- project-specific gameplay code;
- a global audio manager.

Direct SFX and BGM do not require a configured pool service. Pooled SFX does.

## 3. Boundaries

- `Immersive.Audio.Runtime` contains pure contracts and value objects and must not reference `UnityEngine`.
- `Immersive.Audio.Unity` contains Unity authoring assets, hosts, playback services, and adapters.
- `Immersive.Audio.Editor` is Editor-only.

The package does not use singletons, service locators, `Resources.Load`, hidden framework bootstrap, or silent runtime fallback.

`DontDestroyOnLoad` is used only for the optional package-owned persistent `AudioListener`. `AudioRuntimeHost` itself intentionally does not become persistent automatically.

## 4. AudioDefaultsAsset

`AudioDefaultsAsset` is required runtime configuration. It stores default master, SFX, and BGM volumes, default SFX/BGM bus keys, and default fade values.

If `AudioDefaultsAsset` is missing, settings resolution and playback fail explicitly. The package does not substitute hidden runtime defaults.

## 5. AudioSfxCueAsset

`AudioSfxCueAsset` describes one SFX cue:

- cue id;
- `AudioClip`;
- volume and pitch;
- direct or pooled execution mode;
- global or spatial playback mode;
- optional routing bus;
- optional pooled `PoolDefinitionAsset`.

If the cue has no clip, playback returns `FailedMissingClip`.

## 6. AudioBgmCueAsset

`AudioBgmCueAsset` describes one BGM cue:

- cue id;
- `AudioClip`;
- volume and pitch;
- loop mode;
- optional routing bus;
- fade-in and fade-out values.

BGM uses one dedicated `AudioSource` and does not use pooling.

## 7. AudioRuntimeHost

`AudioRuntimeHost` is an optional explicit Unity component. It receives:

- `AudioDefaultsAsset`;
- optional playback root;
- optional `PoolRuntimeHost` for pooled SFX;
- optional persistent-listener policy.

It composes `AudioSettingsService`, `AudioRoutingResolver`, `AudioGlobalSfxService`, and `AudioBgmService`. It is not a singleton and does not persist itself across scenes.

If BGM must continue while gameplay/flow scenes unload, place the `AudioRuntimeHost` under an explicit application/session/persistent-content lifetime that survives those scenes.

Do not assume a Route, Activity, gameplay object, or the object that first requested the cue owns the lifetime of the resulting BGM playback.

## 8. AudioListenerRuntimeHost

`AudioListenerRuntimeHost` can be used directly for explicit listener setup. `AudioRuntimeHost` can also create a package-owned persistent listener when enabled.

Duplicate listeners are reported. They are not destroyed. Duplicate enabled listeners may be disabled only when the explicit policy is `DisableDuplicates`.

For camera-orchestrated games, prefer a dedicated listener authority rather than coupling listener lifetime to transient gameplay cameras unless the project has an explicit spatial-audio policy that requires it.

## 9. Direct SFX

Direct SFX uses `AudioSfxCueAsset.ExecutionMode = Direct`. It creates a controlled temporary `AudioSource` under the configured playback root and returns an `IAudioPlaybackHandle`.

```csharp
using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using UnityEngine;

public sealed class ExamplePlaySfxButton : MonoBehaviour
{
    [SerializeField] private AudioRuntimeHost audioHost;
    [SerializeField] private AudioSfxCueAsset cue;

    public void Play()
    {
        AudioPlaybackResult result = audioHost.PlaySfx(cue);

        if (!result.Succeeded)
        {
            Debug.LogWarning($"SFX failed. status='{result.Status}'.");
        }
    }
}
```

## 10. Pooled SFX

Pooled SFX is explicit. Configure:

1. A prefab with an `AudioSource`.
2. A `PoolDefinitionAsset` pointing to that prefab.
3. A `PoolRuntimeHost` with that pool definition.
4. An `AudioRuntimeHost` that references the `PoolRuntimeHost`.
5. An `AudioSfxCueAsset` with `ExecutionMode = Pooled`.
6. The cue's `PooledAudioSourcePool` set to the same `PoolDefinitionAsset`.

If pooled playback is requested without valid pool configuration, playback fails explicitly. It does not silently fall back to direct SFX.

Expected failure statuses include:

- `FailedMissingPoolService`;
- `FailedMissingPoolDefinition`;
- `FailedPoolRentFailed`;
- `FailedMissingPooledAudioSource`;
- `FailedPoolReturnFailed`.

## 11. BGM

BGM uses `AudioBgmService` through `AudioRuntimeHost`.

```csharp
using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Unity.Hosts;
using UnityEngine;

public sealed class ExampleBgmControls : MonoBehaviour
{
    [SerializeField] private AudioRuntimeHost audioHost;
    [SerializeField] private AudioBgmCueAsset bgmCue;

    public void PlayBgm()
    {
        AudioPlaybackResult result = audioHost.PlayBgm(bgmCue);

        if (!result.Succeeded)
        {
            Debug.LogWarning($"BGM failed. status='{result.Status}'.");
        }
    }

    public void StopBgm()
    {
        audioHost.StopBgm();
    }
}
```

### BGM continuity contract

BGM is a sticky physical presentation when the playback authority outlives transient content.

```text
No provider call  -> current physical BGM remains unchanged
Play(same cue)    -> Succeeded; no restart; playback position preserved
Play(other cue)   -> controlled fade-out / switch / fade-in
Stop              -> explicit fade-to-silence, then source clear
```

A higher-level consumer should distinguish:

- no BGM opinion / no request;
- play this cue;
- explicit silence/stop.

Do not translate missing authoring, owner exit, scene unload, or a `null` higher-level declaration into `StopBgm` unless silence is actually intended.

For scene-to-scene continuity, the `AudioRuntimeHost`, `AudioBgmService`, and dedicated BGM source must live under a composition lifetime that survives the transient scenes.

### Same cue

A repeated request for the currently confirmed/active cue is provider-idempotent:

```text
Play(A)
Play(A)
  -> Succeeded
  -> no clip restart
  -> no playback-position reset
```

A same-cue request can also cancel an in-flight stop fade and restore authored target volume without restarting the clip.

### Different cue

The current implementation uses one dedicated source:

```text
A playing
  -> Play(B)
  -> A fades out while still playing
  -> source reaches zero
  -> source reconfigured to B
  -> B starts and fades in
```

This is a sequential single-source transition, not a dual-source simultaneous crossfade.

`ActiveCue` is the latest explicitly requested target. During the fade-out stage the physical source may still be playing the previous cue.

### Explicit stop

`StopBgm()` is an explicit request for silence. When fade-out is configured, the source remains playing while fading to zero and is cleared after the fade completes.

## 12. Explicit Failures

Playback returns `AudioPlaybackResult`. Do not ignore failed results.

Common statuses:

- `Succeeded`;
- `FailedMissingCue`;
- `FailedMissingClip`;
- `FailedMissingDefaults`;
- `FailedInvalidSettings`;
- `FailedInvalidRouting`;
- `FailedMissingAudioSource`;
- `FailedServiceNotReady`;
- `Stopped`.

There is no null/no-op playback handle reported as success.

## 13. QA Harness and Certification

The canonical QA harness lives outside this package in QAFramework:

```text
Assets/ImmersiveFrameworkQA/Audio
```

Use the Framework entry path rather than opening `QA_Audio.unity` as a standalone runtime entrypoint:

1. Run `Immersive Framework -> QA -> Setup -> Audio -> Configure Audio QA`.
2. For setup-idempotence checks, run the same setup a second time.
3. Enter Play Mode through the normal Framework bootstrap.
4. From `QA Hub`, request `Audio QA`.
5. Run `Run All Audio QA`.

BGM-CONTINUITY-1 certification recorded on 2026-08-19:

```text
Core Audio         7/7 PASS
Framework BGM     14/14 PASS
ADR-013A            5/5 PASS
Audio continuity    4/4 PASS
TOTAL              30/30 PASS
FAILED               0
```

Physical continuity cases:

```text
same-cue-no-restart                  PASS
different-cue-no-abrupt-cut          PASS
different-cue-transition-completes  PASS
explicit-stop-fades-to-silence       PASS
```

The same QA composition also proved a real Framework Route A -> Route B transition where the persistent audio authority remained in Framework Persistent Content, Route B published no BGM request, and the already-playing BGM continued across the scene/lifecycle change.

That lifecycle semantic belongs to the Framework adapter; the provider behavior it relies on is implemented here.

## 14. Setup Checklist

- Create an `AudioDefaultsAsset`.
- Add an `AudioRuntimeHost` to the composition surface that should own playback.
- Assign the defaults asset.
- If BGM must survive scene changes, ensure the host outlives those transient scenes.
- Choose listener ownership explicitly.
- Create SFX/BGM cues with valid clips.
- Call `PlayBgm` only for an explicit play request.
- Call `StopBgm` only for explicit silence.
- Do not convert absence of higher-level BGM intent into `StopBgm`.
- For pooled SFX, configure `PoolRuntimeHost`, `PoolDefinitionAsset`, and an `AudioSource` prefab explicitly.

## 15. Common Errors

- Missing `AudioDefaultsAsset`: playback fails with missing defaults.
- Missing cue clip: playback fails with missing clip.
- Pooled cue without pool service: playback fails with missing pool service.
- Pooled cue without pool definition: playback fails with missing pool definition.
- Pooled prefab without `AudioSource`: playback fails with missing pooled audio source.
- Placing the only BGM playback host under a transient scene while expecting BGM to survive scene unload: give the playback authority an explicit longer lifetime.
- Calling `StopBgm` on owner exit or because a later scene has no BGM declaration: no request is not silence.
- Expecting simultaneous dual-source crossfade: current cue-to-cue transition is sequential on one dedicated source.
- Expecting mixer binding: current routing is metadata only.
- Expecting framework bootstrap: this package remains independent and is composed explicitly.
