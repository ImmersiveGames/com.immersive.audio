# Immersive Audio Architecture

Status: Current
Last updated: 2026-08-19

This package is a standalone technical package for audio authoring and Unity audio services.

## Assembly Boundaries

### Runtime

`Immersive.Audio.Runtime` is the pure runtime assembly. It must not reference `UnityEngine`, Unity authoring assets, scene objects, framework lifecycle, logging sinks, pooling adapters, or editor APIs.

Allowed here:

- pure contracts;
- pure value objects;
- service interfaces that do not require Unity types;
- package metadata.

Pure authoring/runtime support types include:

- `AudioCueId`;
- `AudioBusKey`;
- `AudioBusKeys`;
- `AudioLoopMode`;
- `AudioPlaybackMode`;
- `AudioAuthoringRanges`;
- `AudioConfigurationStatus`;
- `AudioConfigurationIssue`;
- `AudioSettingsSnapshot`;
- `AudioSettingsResolution`;
- `AudioRoutingResolution`;
- `AudioRoutingSource`;
- `AudioListenerDuplicatePolicy`;
- `AudioListenerHostReport`;
- `IAudioPlaybackHandle`;
- `AudioPlaybackStatus`;
- `AudioPlaybackResult`;
- `AudioSfxExecutionMode`.

`Runtime/Services` may contain pure service contracts such as `IAudioSettingsService`. These contracts must not reference Unity assets or scene types.

### Runtime/Unity

`Immersive.Audio.Unity` is the Unity adapter assembly. It contains `ScriptableObject` authoring assets, Unity-facing services, optional hosts, and emitter/playback components.

This assembly references `Immersive.Audio.Runtime` and `Immersive.Pooling.Unity`. Pooling is used only for explicit pooled SFX.

Authoring assets:

- `AudioCueAsset`: abstract base for explicit cue identity, clip, volume, pitch, loop mode, and routing bus.
- `AudioSfxCueAsset`: SFX cue data for global/spatial playback, execution mode, spatial tuning, voice budget, retrigger cooldown, and optional pooled source definition.
- `AudioBgmCueAsset`: BGM cue data for loop mode and authored fade-in/fade-out values.
- `AudioDefaultsAsset`: required runtime defaults for volumes, bus keys, and fallback fade values.

These assets are data only. They do not play audio, create runtime services, resolve framework lifecycle, or create global authorities.

Unity services and hosts:

- `AudioSettingsService`: resolves `AudioSettingsSnapshot` from an explicit `AudioDefaultsAsset`.
- `AudioRoutingResolver`: resolves SFX/BGM bus keys from cue data and explicit defaults.
- `AudioListenerRuntimeHost`: ensures an `AudioListener` on a configured target or on the explicit package-owned persistent listener object and reports duplicate listeners.
- `AudioGlobalSfxService`: direct or explicitly pooled SFX playback.
- `AudioBgmService`: one dedicated BGM `AudioSource`, provider-idempotent same-cue playback, controlled single-source cue transition, and explicit fade-to-silence stop.
- `DirectAudioPlaybackHandle`: concrete Unity playback handle for direct playback.
- `PooledAudioPlaybackHandle`: pooled SFX handle that returns rented objects when playback stops or completes.
- `AudioRuntimeHost`: optional explicit composer for defaults, settings, routing, SFX, BGM, and optional listener ownership.

Playback failures return `AudioPlaybackResult` with explicit status and issues. The package must not use a hidden null/no-op handle or report a no-op as successful playback.

### Editor

`Immersive.Audio.Editor` is Editor-only. It may contain inspectors, validators, and authoring tools. It must not contain runtime behavior required by player builds.

## Prohibited Dependencies

Audio must not depend on:

- `com.immersive.framework`;
- `FrameworkRuntimeHost`;
- `GameApplication`;
- Route or Activity lifecycle;
- FIRSTGAME or QA project code;
- old `DependencyManager`;
- old `RuntimeModeConfig`;
- old `PreferencesRuntime`;
- old `DebugUtility`;
- project scenes, YAML, or `ProjectSettings`.

## Composition Policy

The package must not create a singleton, service locator, hidden bootstrap, or global dependency registry.

Services are composed explicitly by the consuming project or by an adapter outside this package boundary. Required configuration fails explicitly. Missing required services or assets must not be masked by silent fallback behavior.

## Explicit Configuration Policy

`AUDIO-F-RULE-001 - Explicit Audio Configuration`:

- `AudioDefaultsAsset` is required for settings and routing resolution.
- Missing defaults return explicit failed settings/playback evidence.
- Internal hardcoded values are allowed only as authoring ranges, Inspector initial values, and validation helpers.
- Hardcoded values must not become runtime substitutes for missing required assets.

Playback preserves this rule: missing `AudioDefaultsAsset` produces `FailedMissingDefaults`; invalid settings produce `FailedInvalidSettings`; invalid routing produces `FailedInvalidRouting`.

## Listener Host Policy

`AudioListenerRuntimeHost` owns listener safety for the Unity audio package. It supports two explicit modes:

- scene-authored host: a component placed by the consuming project;
- package-owned persistent listener: created by `AudioRuntimeHost` when `Ensure Persistent Listener` is enabled.

The package-owned persistent listener is a narrow, explicit use of `DontDestroyOnLoad` for listener infrastructure only. It is not a service locator, does not expose global playback authority, and does not make `AudioRuntimeHost` itself persistent.

The listener host does not destroy duplicate listeners. Duplicate enabled listeners are reported. Disabling duplicates is allowed only when the configured policy is `DisableDuplicates`; the package-owned persistent listener uses that policy by default.

## Pooling Policy

Pooled SFX depends on `com.immersive.pooling` through explicit Unity composition. The audio package consumes `IPoolService` and `PoolDefinitionAsset`; it does not create a global pool, search for a pool service, use `Resources.Load`, or fall back to direct playback when pooled configuration is missing.

A pooled cue must have an explicit pool definition whose prefab contains an `AudioSource`. Missing pool service, pool definition, prefab source, rent, or return failures are surfaced as explicit `AudioPlaybackResult` failures. BGM does not use pooling.

## Runtime Host Policy

`AudioRuntimeHost` is optional and explicit. It may create child GameObjects under its configured playback root to hold direct playback services and sources. It must not register global services, act as a singleton, or depend on framework lifecycle.

By default, `AudioRuntimeHost` can ensure a package-owned persistent `AudioListener`. This makes listener availability independent from camera lifetime. The persistent listener object is limited to listener ownership; BGM/SFX playback authority and game-specific intent remain composition-owned.

`AudioRuntimeHost` intentionally does not call `DontDestroyOnLoad` for itself. If BGM must survive transient scene unload/load, the consuming project or framework adapter must place the host under an explicit application/session/persistent-content lifetime that outlives those scenes.

## BGM Continuity Policy

`AUDIO-F-RULE-002 - Sticky Confirmed BGM Presentation` defines the current BGM continuity contract.

The audio package owns physical BGM playback and transition behavior. It does not own Route, Activity, game-flow, or other higher-level lifecycle semantics.

For continuity-capable composition:

- once a cue is successfully playing, it remains the physical presentation until an explicit later `Play` or `Stop` changes it;
- absence of a new higher-level request means no provider call and therefore no physical mutation;
- requesting the same cue is provider-idempotent and succeeds without restarting the clip or resetting playback position;
- requesting a different cue performs a controlled single-source transition: the current cue fades out, the source is reconfigured, and the next cue fades in;
- explicit `Stop` transitions the current cue to silence using its fade-out/default fade and then clears playback state;
- owner exit, object destruction, or transient scene unload must not be translated by an adapter into an implicit `Stop`;
- continuity across scene lifetime requires the `AudioRuntimeHost` / `AudioBgmService` authority to outlive those transient scenes through explicit composition.

Higher-level orchestration should distinguish:

```text
Unspecified / No Request -> no provider mutation; preserve physical playback
Play(cue)                -> apply or transition to the requested cue
Silence / Stop           -> transition explicitly to silence
```

`null`, missing authoring, owner exit, or absence of a higher-level binding must not be silently promoted to `Stop` by an adapter.

### Current implementation

`AudioBgmService` uses one dedicated `AudioSource`.

Same-cue behavior:

```text
Play(A) while A is active
  -> Succeeded
  -> no source restart
  -> playback position preserved
```

Different-cue behavior:

```text
A playing
  -> Play(B)
  -> A remains playing while fade-out begins
  -> source reaches zero
  -> source is reconfigured to B
  -> B starts and fades in
```

This is a sequential single-source fade-out/switch/fade-in transition. It is not a simultaneous dual-source crossfade.

`ActiveCue` represents the latest explicit target. During a cue-to-cue transition the physical source may still be finishing the previous cue for a short time.

`Stop()` accepts explicit silence immediately as an operation result, keeps the current source alive during the configured fade-out, then stops and clears the source when the fade completes.

### Certification status

BGM-CONTINUITY-1 is implemented and technically certified by the external QAFramework integration surface on 2026-08-19.

Certified physical provider cases:

```text
same-cue-no-restart                  PASS
different-cue-no-abrupt-cut          PASS
different-cue-transition-completes  PASS
explicit-stop-fades-to-silence       PASS
```

The same QA run closed `30/30` across Core Audio, Framework BGM semantics, ADR-013A rejection/retry behavior, and physical continuity. A real Framework Route A -> Route B lifecycle transition with an explicitly persistent audio authority and no new BGM request also completed successfully while the BGM remained playing.

This evidence certifies the provider behavior used by that integration. Higher-level Route/Activity sticky intent semantics remain owned by the consuming framework, not by this package.

## Mixer Policy

Routing is currently resolved metadata only. Unity `AudioMixer` binding remains deferred until a dedicated mixer/routing cut defines the public authoring language and failure behavior.

## Old AudioRuntime Policy

The old GameJam AudioRuntime is reference-only. Its composer, installer, sample assets, QA harness, global DI usage, and runtime architecture must not be copied into this package.
