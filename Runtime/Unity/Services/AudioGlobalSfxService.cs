using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Services;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Contracts;
using UnityEngine;

namespace Immersive.Audio.Unity.Services
{
    [DisallowMultipleComponent]
    public sealed class AudioGlobalSfxService : MonoBehaviour, IAudioSfxService
    {
        private IAudioSettingsService settingsService;
        private AudioRoutingResolver routingResolver;
        private IPoolService poolService;
        private AudioConfigurationIssue? poolServiceIssue;
        private Transform playbackRoot;

        public void Initialize(
            IAudioSettingsService settings,
            AudioRoutingResolver routing,
            Transform root,
            IPoolService pool = null,
            AudioConfigurationIssue? poolIssue = null)
        {
            settingsService = settings;
            routingResolver = routing;
            playbackRoot = root != null ? root : transform;
            poolService = pool;
            poolServiceIssue = poolIssue;
        }

        public void SetPoolService(IPoolService pool, AudioConfigurationIssue? poolIssue = null)
        {
            poolService = pool;
            poolServiceIssue = poolIssue;
        }

        public AudioPlaybackResult Play(AudioSfxCueAsset cue)
        {
            return PlayInternal(cue, transform.position, false);
        }

        public AudioPlaybackResult PlayAt(AudioSfxCueAsset cue, Vector3 worldPosition)
        {
            return PlayInternal(cue, worldPosition, true);
        }

        private AudioPlaybackResult PlayInternal(AudioSfxCueAsset cue, Vector3 worldPosition, bool useProvidedPosition)
        {
            if (!ValidateCue(cue, out AudioPlaybackResult validationFailure))
            {
                return validationFailure;
            }

            AudioSettingsResolution settings = ResolveSettings();
            if (!settings.IsResolved)
            {
                return ToPlaybackFailure(ResolveSettingsFailureStatus(settings), settings.Issues);
            }

            AudioRoutingResolution routing = routingResolver != null
                ? routingResolver.ResolveSfxBus(cue)
                : AudioRoutingResolution.Failed(
                    new AudioConfigurationIssue(
                        "audio_routing_resolver_missing",
                        "AudioRoutingResolver is required before SFX playback.",
                        nameof(routingResolver)));

            if (!routing.IsResolved)
            {
                return ToPlaybackFailure(AudioPlaybackStatus.FailedInvalidRouting, routing.Issues);
            }

            if (cue.ExecutionMode == AudioSfxExecutionMode.Pooled)
            {
                return PlayPooled(cue, settings.Snapshot, worldPosition, useProvidedPosition);
            }

            return PlayDirect(cue, settings.Snapshot, worldPosition, useProvidedPosition);
        }

        private AudioPlaybackResult PlayDirect(
            AudioSfxCueAsset cue,
            AudioSettingsSnapshot settings,
            Vector3 worldPosition,
            bool useProvidedPosition)
        {
            Transform root = playbackRoot != null ? playbackRoot : transform;
            var playbackObject = new GameObject($"{cue.name}_SfxDirect");
            playbackObject.transform.SetParent(root, false);
            playbackObject.transform.position = useProvidedPosition ? worldPosition : root.position;

            AudioSource source = playbackObject.AddComponent<AudioSource>();
            if (source == null)
            {
                Destroy(playbackObject);
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingAudioSource,
                    new AudioConfigurationIssue(
                        "audio_source_missing",
                        "Failed to create AudioSource for direct SFX playback.",
                        nameof(AudioSource)));
            }

            ConfigureSource(source, cue, settings);

            DirectAudioPlaybackHandle handle = playbackObject.AddComponent<DirectAudioPlaybackHandle>();
            handle.Initialize(source, true);
            source.Play();

            return AudioPlaybackResult.Success(handle);
        }

        private AudioPlaybackResult PlayPooled(
            AudioSfxCueAsset cue,
            AudioSettingsSnapshot settings,
            Vector3 worldPosition,
            bool useProvidedPosition)
        {
            if (poolService == null || poolService.IsShutdown)
            {
                AudioConfigurationIssue issue = poolServiceIssue ?? new AudioConfigurationIssue(
                    "audio_pool_service_missing",
                    "Pooled SFX requires an explicit IPoolService.",
                    nameof(poolService));

                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingPoolService,
                    issue);
            }

            PoolDefinitionAsset poolDefinition = cue.PooledAudioSourcePool;
            if (poolDefinition == null)
            {
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingPoolDefinition,
                    new AudioConfigurationIssue(
                        "audio_pool_definition_missing",
                        "Pooled SFX requires an explicit PoolDefinitionAsset on the cue.",
                        nameof(cue.PooledAudioSourcePool)));
            }

            Transform root = playbackRoot != null ? playbackRoot : transform;
            GameObject rented;
            try
            {
                rented = poolService.Rent(poolDefinition, root);
            }
            catch (System.Exception exception)
            {
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedPoolRentFailed,
                    new AudioConfigurationIssue(
                        "audio_pool_rent_failed",
                        $"Pooled SFX rent failed: {exception.Message}",
                        nameof(poolService)));
            }

            if (rented == null)
            {
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedPoolRentFailed,
                    new AudioConfigurationIssue(
                        "audio_pool_rent_returned_null",
                        "Pooled SFX rent returned null.",
                        nameof(poolService)));
            }

            rented.transform.position = useProvidedPosition ? worldPosition : root.position;

            if (!rented.TryGetComponent(out AudioSource source) || source == null)
            {
                try
                {
                    poolService.Return(poolDefinition, rented);
                }
                catch
                {
                    // The playback failure below is still the primary explicit result for this call.
                }

                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingPooledAudioSource,
                    new AudioConfigurationIssue(
                        "audio_pooled_source_missing",
                        "Pooled SFX prefab requires an AudioSource.",
                        nameof(AudioSource)));
            }

            ConfigureSource(source, cue, settings);

            PooledAudioPlaybackHandle handle = rented.GetComponent<PooledAudioPlaybackHandle>();
            if (handle == null)
            {
                handle = rented.AddComponent<PooledAudioPlaybackHandle>();
            }

            handle.Initialize(poolService, poolDefinition, rented, source);
            source.Play();

            return AudioPlaybackResult.Success(handle);
        }

        private static bool ValidateCue(AudioSfxCueAsset cue, out AudioPlaybackResult failure)
        {
            if (cue == null)
            {
                failure = AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingCue,
                    new AudioConfigurationIssue(
                        "audio_sfx_cue_missing",
                        "AudioSfxCueAsset is required for SFX playback.",
                        nameof(cue)));
                return false;
            }

            if (cue.Clip == null)
            {
                failure = AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingClip,
                    new AudioConfigurationIssue(
                        "audio_sfx_clip_missing",
                        "AudioSfxCueAsset requires an AudioClip before playback.",
                        nameof(cue.Clip)));
                return false;
            }

            failure = default;
            return true;
        }

        private AudioSettingsResolution ResolveSettings()
        {
            if (settingsService == null)
            {
                return AudioSettingsResolution.Failed(
                    new AudioConfigurationIssue(
                        "audio_settings_service_missing",
                        "IAudioSettingsService is required before SFX playback.",
                        nameof(settingsService)));
            }

            return settingsService.Settings;
        }

        private static AudioPlaybackResult ToPlaybackFailure(
            AudioPlaybackStatus status,
            System.Collections.Generic.IReadOnlyList<AudioConfigurationIssue> issues)
        {
            if (issues == null)
            {
                return AudioPlaybackResult.Failure(status);
            }

            var copy = new AudioConfigurationIssue[issues.Count];
            for (int i = 0; i < issues.Count; i++)
            {
                copy[i] = issues[i];
            }

            return AudioPlaybackResult.Failure(status, copy);
        }

        private static AudioPlaybackStatus ResolveSettingsFailureStatus(AudioSettingsResolution settings)
        {
            for (int i = 0; i < settings.Issues.Count; i++)
            {
                if (settings.Issues[i].Code == "audio_defaults_missing")
                {
                    return AudioPlaybackStatus.FailedMissingDefaults;
                }
            }

            return AudioPlaybackStatus.FailedInvalidSettings;
        }

        private static void ConfigureSource(
            AudioSource source,
            AudioSfxCueAsset cue,
            AudioSettingsSnapshot settings)
        {
            source.playOnAwake = false;
            source.clip = cue.Clip;
            source.loop = cue.LoopMode == AudioLoopMode.On;
            source.pitch = Mathf.Clamp(cue.Pitch, AudioAuthoringRanges.MinPitch, AudioAuthoringRanges.MaxPitch);
            source.volume = Mathf.Clamp01(cue.Volume * settings.MasterVolume * settings.SfxVolume);
            source.spatialBlend = cue.PlaybackMode == AudioPlaybackMode.Spatial
                ? Mathf.Clamp01(cue.SpatialBlend)
                : 0f;
            source.minDistance = Mathf.Max(0f, cue.MinDistance);
            source.maxDistance = Mathf.Max(source.minDistance, cue.MaxDistance);
        }

    }
}
