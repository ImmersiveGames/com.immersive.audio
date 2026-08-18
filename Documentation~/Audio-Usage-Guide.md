# Immersive Audio Usage Guide

This guide explains how to use `com.immersive.audio` directly from Unity code and scenes.

## Quick Start

1. Create an `AudioDefaultsAsset`.
2. Create an `AudioRuntimeHost` in the scene.
3. Assign the defaults asset to the host.
4. Create a direct `AudioSfxCueAsset`.
5. Call `PlaySfx`.
6. Create an `AudioBgmCueAsset`.
7. Call `PlayBgm` and `StopBgm`.
8. For pooled SFX, create a `PoolRuntimeHost`, a `PoolDefinitionAsset`, and a prefab with an `AudioSource`.
9. Configure the SFX cue as `Pooled`.
10. Call `PlaySfx`.

## 1. Package Role

`com.immersive.audio` provides reusable Unity audio authoring assets and runtime services for SFX, pooled SFX, BGM, settings, routing metadata, and listener handling.

It is a standalone technical package. It does not depend on `com.immersive.framework`, game flow, routes, activities, FIRSTGAME, QA projects, or old Base/GameJam runtime composition.

## 2. Dependencies

Required:

- `com.immersive.pooling` for pooled SFX support in the Unity assembly.

Not required:

- `com.immersive.framework`;
- logging package;
- project-specific gameplay code.

Direct SFX and BGM do not require a configured pool service. Pooled SFX does.

## 3. Boundaries

- `Immersive.Audio.Runtime` contains pure contracts and value objects. It must not reference `UnityEngine`.
- `Immersive.Audio.Unity` contains Unity authoring assets, hosts, and services.
- `Immersive.Audio.Editor` is Editor-only.

The package does not use singletons, service locators, `Resources.Load`, hidden framework bootstrap, or silent fallback. It uses `DontDestroyOnLoad` only for the package-owned persistent `AudioListener` created by `AudioRuntimeHost` when listener persistence is enabled.

## 4. AudioDefaultsAsset

`AudioDefaultsAsset` is required runtime configuration. It stores default master, SFX, and BGM volumes, default SFX/BGM bus keys, and default fade values.

If `AudioDefaultsAsset` is missing, settings resolution and playback fail explicitly. The package does not substitute internal defaults at runtime.

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
- fade in and fade out values.

BGM uses a dedicated `AudioSource` and does not use pooling.

## 7. AudioRuntimeHost

`AudioRuntimeHost` is an optional explicit scene component. It receives:

- `AudioDefaultsAsset`;
- optional playback root;
- optional `PoolRuntimeHost` for pooled SFX.

It composes `AudioSettingsService`, `AudioRoutingResolver`, `AudioGlobalSfxService`, and `AudioBgmService`. It is not a singleton and does not persist itself across scenes.

By default, it also ensures one package-owned persistent `AudioListener`. This keeps listener availability independent from camera lifecycle, route scenes, activity roots, or UI scenes. The persistent listener object is named `ImmersiveAudioListener` and is retained across scene loads.

If an application requires BGM to continue while gameplay or flow scenes are unloaded, place the `AudioRuntimeHost` under an explicit persistent/session lifetime owned by the consuming project or framework adapter. Do not rely on `AudioRuntimeHost` to call `DontDestroyOnLoad` for itself; it intentionally does not.

For camera-orchestrated games, do not put `AudioListener` on gameplay cameras. Let `AudioRuntimeHost` own listener availability and keep camera orchestration visual-only unless a later spatial-audio policy explicitly changes that.

## 8. AudioListenerRuntimeHost

`AudioListenerRuntimeHost` can be used directly for explicit listener setup, but most games should rely on `AudioRuntimeHost`'s persistent listener guarantee. It ensures an `AudioListener` on its target or own GameObject and reports duplicate listeners.

Duplicate listeners are not destroyed. Duplicate enabled listeners are reported, and they can be disabled when the policy is `DisableDuplicates`. `AudioRuntimeHost` uses `DisableDuplicates` by default for the package-owned persistent listener.

## 9. Direct SFX

Direct SFX uses `AudioSfxCueAsset.ExecutionMode = Direct`. It creates a controlled temporary `AudioSource` under the configured playback root and returns an `IAudioPlaybackHandle`.

Direct SFX does not require `com.immersive.pooling` runtime configuration and does not fall back through a pool.

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
            Debug.LogWarning($"SFX failed. status='{result.Status}' issues='{string.Join(",", result.Issues)}'.");
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

If the cue requests pooled playback and no `IPoolService`, `PoolRuntimeHost`, or `PoolDefinitionAsset` is configured, playback fails explicitly. It does not play direct SFX as a hidden fallback.

Expected failure statuses include:

- `FailedMissingPoolService`;
- `FailedMissingPoolDefinition`;
- `FailedPoolRentFailed`;
- `FailedMissingPooledAudioSource`;
- `FailedPoolReturnFailed`.

## 11. BGM

BGM uses `AudioBgmService` through `AudioRuntimeHost`. It supports play, stop, cue loop mode, and basic fade values.

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
            Debug.LogWarning($"BGM failed. status='{result.Status}' issues='{string.Join(",", result.Issues)}'.");
        }
    }

    public void StopBgm()
    {
        audioHost.StopBgm();
    }
}
```

### BGM continuity contract

For applications that orchestrate music across transient gameplay scenes, BGM is treated as a sticky confirmed presentation. Once a cue has been successfully established, absence of a later request does not mean stop or silence.

The required semantics are:

```text
No new request -> keep the current confirmed BGM unchanged
Same cue       -> keep playback unchanged; do not restart it
Different cue  -> perform a controlled transition to the new cue
Explicit stop  -> transition explicitly to silence
```

The object or scene that originally caused a BGM request does not own the lifetime of the resulting confirmed playback. If that object or scene exits while the playback authority remains alive, the BGM continues until a later explicit request changes it or requests silence.

A higher-level consumer must therefore distinguish:

- no BGM opinion / no request;
- play this cue;
- explicit silence/stop.

Do not use a missing binding, owner exit, scene unload, or `null` higher-level declaration as an implicit request for silence.

For scene-to-scene continuity, the `AudioRuntimeHost`, `AudioBgmService`, and dedicated BGM `AudioSource` must live under a composition lifetime that survives the transient scenes. The package does not create that persistent application/session owner automatically.

### Current implementation limitation

The current F4 `AudioBgmService` is still a basic one-source `Play`/`Stop` implementation with fade values. The full sticky continuity behavior above, especially controlled cue-to-cue transitions without abrupt interruption, is a required target for the next BGM continuity runtime cut and is not yet certified by QA.

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

## 13. QA Harness

The QA harness lives outside the package at:

```text
Assets/ImmersiveFrameworkQA/Audio
```

Canonical flow:

1. Run `Immersive Framework QA > Audio > Create or Refresh Audio QA Scene`.
2. Open `Assets/ImmersiveFrameworkQA/Audio/Scenes/QA_Audio.unity`.
3. Enter Play Mode.
4. Click `Run All Audio Smokes`.

The builder also ensures generated audio clips and cue assignments. The separate repair menu is maintenance-only.

The BGM continuity contract above requires an additional lifecycle regression that proves a persistent playback authority across transient scene unload/load and verifies no-request, same-cue, different-cue, and explicit-silence behavior. The existing F4 smoke does not by itself certify that contract.

## 14. Setup Checklist

- Create an `AudioDefaultsAsset`.
- Add an `AudioRuntimeHost` to the scene or explicit persistent/session composition surface that should own playback.
- Assign the defaults asset to the host.
- If BGM must survive scene changes, ensure the host's composition lifetime outlives those transient scenes.
- Leave `Ensure Persistent Listener` enabled unless the project has a stronger explicit listener owner.
- Do not add `AudioListener` to route/activity cameras by default.
- Create a direct `AudioSfxCueAsset`.
- Assign an `AudioClip` to the cue.
- Call `PlaySfx`.
- Create an `AudioBgmCueAsset`.
- Call `PlayBgm` and `StopBgm` only for explicit playback changes; absence of a new higher-level BGM request must not be translated into `StopBgm`.
- For pooled SFX, create `PoolRuntimeHost`, `PoolDefinitionAsset`, and a prefab with `AudioSource`.
- Set the SFX cue to `Pooled`.
- Assign the cue's `PooledAudioSourcePool`.
- Assign the `PoolRuntimeHost` to the `AudioRuntimeHost`.

## 15. Common Errors

- Missing `AudioDefaultsAsset`: playback fails with missing defaults.
- Missing cue clip: playback fails with missing clip.
- Pooled cue without pool service: playback fails with missing pool service.
- Pooled cue without pool definition: playback fails with missing pool definition.
- Pooled prefab without `AudioSource`: playback fails with missing pooled audio source.
- Placing the only BGM playback host under a transient scene while expecting music to survive that scene's unload: the playback authority must have an explicit longer lifetime.
- Treating absence of a new BGM request as `StopBgm`: no request means preserve the current confirmed presentation.
- Expecting mixer routing: current routing is metadata only; real `AudioMixer` binding is not implemented.
- Expecting framework bootstrap: this package is independent and must be composed explicitly.
- Adding `AudioListener` to cameras while the persistent listener is enabled: duplicates are reported and may be disabled by policy. Prefer listener ownership through `AudioRuntimeHost`.