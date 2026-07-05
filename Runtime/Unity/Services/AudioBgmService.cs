using System.Collections;
using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Services;
using UnityEngine;

namespace Immersive.Audio.Unity.Services
{
    [DisallowMultipleComponent]
    public sealed class AudioBgmService : MonoBehaviour, IAudioBgmService
    {
        private IAudioSettingsService settingsService;
        private AudioRoutingResolver routingResolver;
        private AudioSource source;
        private DirectAudioPlaybackHandle activeHandle;
        private Coroutine fadeRoutine;

        public AudioBgmCueAsset ActiveCue { get; private set; }

        public void Initialize(
            IAudioSettingsService settings,
            AudioRoutingResolver routing,
            AudioSource dedicatedSource)
        {
            settingsService = settings;
            routingResolver = routing;
            source = dedicatedSource != null ? dedicatedSource : GetOrCreateAudioSource();
            source.playOnAwake = false;
        }

        public AudioPlaybackResult Play(AudioBgmCueAsset cue)
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
                ? routingResolver.ResolveBgmBus(cue)
                : AudioRoutingResolution.Failed(
                    new AudioConfigurationIssue(
                        "audio_routing_resolver_missing",
                        "AudioRoutingResolver is required before BGM playback.",
                        nameof(routingResolver)));

            if (!routing.IsResolved)
            {
                return ToPlaybackFailure(AudioPlaybackStatus.FailedInvalidRouting, routing.Issues);
            }

            if (source == null)
            {
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingAudioSource,
                    new AudioConfigurationIssue(
                        "audio_source_missing",
                        "AudioBgmService requires a dedicated AudioSource.",
                        nameof(source)));
            }

            if (source.isPlaying && ActiveCue == cue)
            {
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedAlreadyPlaying,
                    new AudioConfigurationIssue(
                        "audio_bgm_already_playing",
                        "Requested BGM cue is already playing.",
                        nameof(cue)));
            }

            StopFadeRoutine();
            ConfigureSource(cue, settings.Snapshot);
            ActiveCue = cue;

            if (activeHandle == null)
            {
                activeHandle = gameObject.AddComponent<DirectAudioPlaybackHandle>();
            }

            activeHandle.Initialize(source, false);
            source.volume = ResolveTargetVolume(cue, settings.Snapshot);

            float fadeIn = ResolveFadeSeconds(cue.FadeInSeconds, settings.Snapshot.DefaultFadeInSeconds);
            if (fadeIn > 0f)
            {
                float targetVolume = source.volume;
                source.volume = 0f;
                source.Play();
                fadeRoutine = StartCoroutine(FadeVolumeRoutine(targetVolume, fadeIn));
            }
            else
            {
                source.Play();
            }

            return AudioPlaybackResult.Success(activeHandle);
        }

        public AudioPlaybackResult Stop()
        {
            if (source == null)
            {
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingAudioSource,
                    new AudioConfigurationIssue(
                        "audio_source_missing",
                        "AudioBgmService requires a dedicated AudioSource.",
                        nameof(source)));
            }

            if (!source.isPlaying && ActiveCue == null)
            {
                return AudioPlaybackResult.Stopped();
            }

            float fadeOut = ActiveCue != null ? ActiveCue.FadeOutSeconds : 0f;
            if (fadeOut > 0f && isActiveAndEnabled)
            {
                StopFadeRoutine();
                fadeRoutine = StartCoroutine(StopAfterFadeRoutine(fadeOut));
                return AudioPlaybackResult.Stopped();
            }

            StopImmediate();
            return AudioPlaybackResult.Stopped();
        }

        private AudioSource GetOrCreateAudioSource()
        {
            if (TryGetComponent(out AudioSource existing) && existing != null)
            {
                return existing;
            }

            return gameObject.AddComponent<AudioSource>();
        }

        private static bool ValidateCue(AudioBgmCueAsset cue, out AudioPlaybackResult failure)
        {
            if (cue == null)
            {
                failure = AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingCue,
                    new AudioConfigurationIssue(
                        "audio_bgm_cue_missing",
                        "AudioBgmCueAsset is required for BGM playback.",
                        nameof(cue)));
                return false;
            }

            if (cue.Clip == null)
            {
                failure = AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedMissingClip,
                    new AudioConfigurationIssue(
                        "audio_bgm_clip_missing",
                        "AudioBgmCueAsset requires an AudioClip before playback.",
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
                        "IAudioSettingsService is required before BGM playback.",
                        nameof(settingsService)));
            }

            return settingsService.Settings;
        }

        private void ConfigureSource(AudioBgmCueAsset cue, AudioSettingsSnapshot settings)
        {
            source.Stop();
            source.clip = cue.Clip;
            source.loop = cue.LoopMode == AudioLoopMode.On;
            source.pitch = Mathf.Clamp(cue.Pitch, AudioAuthoringRanges.MinPitch, AudioAuthoringRanges.MaxPitch);
            source.volume = ResolveTargetVolume(cue, settings);
            source.spatialBlend = 0f;
        }

        private static float ResolveTargetVolume(AudioBgmCueAsset cue, AudioSettingsSnapshot settings)
        {
            return Mathf.Clamp01(cue.Volume * settings.MasterVolume * settings.BgmVolume);
        }

        private static float ResolveFadeSeconds(float cueFadeSeconds, float defaultFadeSeconds)
        {
            return cueFadeSeconds >= 0f ? cueFadeSeconds : Mathf.Max(0f, defaultFadeSeconds);
        }

        private IEnumerator FadeVolumeRoutine(float targetVolume, float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(0f, targetVolume, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            source.volume = targetVolume;
            fadeRoutine = null;
        }

        private IEnumerator StopAfterFadeRoutine(float seconds)
        {
            float startVolume = source.volume;
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            ResetPlaybackState();
            fadeRoutine = null;
        }

        private void StopImmediate()
        {
            StopFadeRoutine();
            ResetPlaybackState();
        }

        private void ResetPlaybackState()
        {
            source.Stop();
            source.clip = null;
            ActiveCue = null;
        }

        private void StopFadeRoutine()
        {
            if (fadeRoutine == null)
            {
                return;
            }

            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
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
    }
}
