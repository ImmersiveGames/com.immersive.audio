# Immersive Audio Architecture

This package is a standalone technical package for audio authoring and Unity audio services.

## Assembly Boundaries

### Runtime

`Immersive.Audio.Runtime` is the pure runtime assembly. It must not reference `UnityEngine`, Unity authoring assets, scene objects, framework lifecycle, logging sinks, pooling adapters, or editor APIs.

Allowed here:

- pure contracts;
- pure value objects;
- service interfaces that do not require Unity types;
- package metadata.

F2 added pure authoring support types here:

- `AudioCueId`;
- `AudioBusKey`;
- `AudioBusKeys`;
- `AudioLoopMode`;
- `AudioPlaybackMode`;
- `AudioAuthoringRanges`.

F3 adds pure result/status types here:

- `AudioConfigurationStatus`;
- `AudioConfigurationIssue`;
- `AudioSettingsSnapshot`;
- `AudioSettingsResolution`;
- `AudioRoutingResolution`;
- `AudioRoutingSource`;
- `AudioListenerDuplicatePolicy`;
- `AudioListenerHostReport`.

F4 adds pure playback contracts:

- `IAudioPlaybackHandle`;
- `AudioPlaybackStatus`;
- `AudioPlaybackResult`.

F5 adds pure SFX execution intent:

- `AudioSfxExecutionMode`.

`Runtime/Services` may contain pure service contracts such as `IAudioSettingsService`. These contracts must not reference Unity assets or scene types.

### Runtime/Unity

`Immersive.Audio.Unity` is the Unity adapter assembly. It may contain `ScriptableObject` authoring assets, future Unity-facing services, optional hosts, and emitter components.

This assembly references `Immersive.Audio.Runtime` and `Immersive.Pooling.Unity`. Pooling is used only for explicit pooled SFX. Logging can be added later only when a concrete service needs explicit diagnostics.

F2 added these authoring assets:

- `AudioCueAsset`: abstract base for explicit cue identity, clip, volume, pitch, loop mode, and routing bus.
- `AudioSfxCueAsset`: SFX cue data for global/spatial playback, spatial tuning, voice budget, and retrigger cooldown.
- `AudioBgmCueAsset`: BGM cue data for transition fades, with loop mode inherited from the base cue.
- `AudioDefaultsAsset`: authoring defaults for volumes, bus keys, and fade values.

These assets are data only. They do not play audio, create `AudioSource`, resolve services, or create runtime hosts.

F3 adds Unity services and a listener host:

- `AudioSettingsService`: resolves `AudioSettingsSnapshot` from an explicit `AudioDefaultsAsset`.
- `AudioRoutingResolver`: resolves SFX/BGM bus keys from cue data and explicit defaults.
- `AudioListenerRuntimeHost`: component/runtime helper that ensures an `AudioListener` on a configured target or on the package-owned persistent listener object and reports duplicate listeners.

These classes do not implement SFX playback, BGM playback, mixer binding, pooling, framework lifecycle, or service location.

F4 adds direct playback Unity services:

- `AudioGlobalSfxService`: direct SFX playback with controlled temporary `AudioSource` objects.
- `AudioBgmService`: one dedicated BGM `AudioSource`, basic play/stop, cue loop mode, and simple fade in/out.
- `DirectAudioPlaybackHandle`: concrete Unity playback handle for direct playback.
- `AudioRuntimeHost`: optional explicit composer for defaults, settings, routing, direct SFX, and BGM.

Playback failures return `AudioPlaybackResult` with explicit status and issues. The package must not use a hidden `NullAudioPlaybackHandle` or report a no-op handle as successful playback.

F5 adds pooled SFX Unity integration:

- `AudioSfxCueAsset.ExecutionMode` selects `Direct` or `Pooled`.
- `AudioSfxCueAsset.PooledAudioSourcePool` points to the explicit `PoolDefinitionAsset` used for pooled SFX.
- `AudioGlobalSfxService` accepts an optional explicit `IPoolService`.
- `PooledAudioPlaybackHandle` returns rented objects to the pool when playback stops or finishes.
- `AudioRuntimeHost` may receive a configured `PoolRuntimeHost` and inject its service into SFX playback.

If pooled playback is requested without a configured pool service or pool definition, the service returns explicit failures such as `FailedMissingPoolService` or `FailedMissingPoolDefinition`. It must not silently play the cue through direct SFX.

F6 does not change runtime architecture. It consolidates public usage documentation in `Documentation~/Audio-Usage-Guide.md` and keeps the QA clip repair flow outside the package runtime.

### Editor

`Immersive.Audio.Editor` is Editor-only. It may contain future inspectors, validators, and authoring tools. It must not contain runtime behavior required by player builds.

## Prohibited Dependencies

Audio must not depend on:

- `com.immersive.framework`;
- `FrameworkRuntimeHost`;
- `GameApplication`;
- `Route` or `Activity`;
- `FIRSTGAME` or QA project code;
- old `DependencyManager`;
- old `RuntimeModeConfig`;
- old `PreferencesRuntime`;
- old `DebugUtility`;
- project scenes, YAML, or `ProjectSettings`.

## Composition Policy

The package must not create a singleton, service locator, hidden bootstrap, or global dependency registry.

Future services must be composed explicitly by the consuming project or by a framework adapter outside this package boundary. Required configuration must fail fast. Missing services must not be masked by silent fallback behavior.

## Explicit Configuration Policy

`AUDIO-F-RULE-001 - Explicit Audio Configuration`:

- `AudioDefaultsAsset` is required for settings and routing resolution.
- Missing defaults must return `AudioConfigurationStatus.Failed` with an explicit issue.
- Internal hardcoded values are allowed only as authoring ranges, Inspector initial values, and validation helpers.
- Hardcoded values must not become runtime substitutes for missing required assets.

F4 services preserve this rule: missing `AudioDefaultsAsset` produces `FailedMissingDefaults`; invalid settings produce `FailedInvalidSettings`; invalid routing produces `FailedInvalidRouting`.

## Listener Host Policy

`AudioListenerRuntimeHost` owns listener safety for the Unity audio package. It supports two explicit modes:

- scene-authored host: a component placed by the consuming project on a GameObject;
- package-owned persistent host: created by `AudioRuntimeHost` when `Ensure Persistent Listener` is enabled.

The package-owned persistent listener is a narrow, explicit use of `DontDestroyOnLoad` for audio infrastructure only. It is not a service locator, does not expose a global playback service, and does not make `AudioRuntimeHost` itself persistent. Its purpose is to keep one valid `AudioListener` alive while cameras, routes, activities, or scenes are opened and closed.

The listener host must not destroy duplicate listeners. Duplicate enabled listeners are reported by default. Disabling duplicates is allowed only when the component or `AudioRuntimeHost` policy is explicitly set to `DisableDuplicates`; `AudioRuntimeHost` defaults to `DisableDuplicates` for the package-owned persistent listener so camera-authored listeners do not compete with the audio root.

## Pooling Policy

Pooled SFX depends on `com.immersive.pooling` through explicit Unity composition. The audio package consumes `IPoolService` and `PoolDefinitionAsset`; it does not create a global pool, search for a pool service, use `Resources.Load`, or fall back to direct playback when pooled configuration is missing.

`PoolDefinitionAsset` should point to a prefab with an `AudioSource`. Missing prefab/source or rent failures are surfaced as explicit `AudioPlaybackResult` failures. BGM does not use pooling.

## Runtime Host Policy

`AudioRuntimeHost` is optional and explicit. It may create child GameObjects under itself to hold direct playback services and sources. It must not register global services, act as a singleton, or depend on framework lifecycle.

By default, `AudioRuntimeHost` also ensures a package-owned persistent `AudioListener` through `AudioListenerRuntimeHost`. This makes listener availability independent from cameras. The persistent object is limited to listener ownership; playback services and game-specific BGM/SFX decisions remain scene/composition-owned.

## Mixer Policy

F4 routing is resolved metadata only. Unity `AudioMixer` binding is intentionally deferred until a dedicated routing/mixer cut defines the public authoring language and failure behavior.

## Old AudioRuntime Policy

The old GameJam AudioRuntime is reference-only. Its concepts may inform future cuts, but its composer, installer, sample assets, QA harness, global DI usage, and runtime architecture must not be copied into this package.
