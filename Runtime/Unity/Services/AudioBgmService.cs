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
        private Coroutine transitionRoutine;
        private AudioBgmCueAsset sourceCue;

        /// <summary>
        /// Latest explicitly requested BGM cue. During a controlled cue-to-cue transition the
        /// physical AudioSource may still be finishing the previous cue for a short time.
        /// </summary>
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

            if (!source.isPlaying)
            {
                sourceCue = null;
                ActiveCue = null;
            }
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

            EnsureActiveHandle();

            // Repeating the same explicit request is provider-idempotent. If a transition to this
            // cue is already in flight, leave it alone. If the cue is physically playing while a
            // stop fade is in flight, cancel that fade and restore the authored target volume
            // without restarting the clip or losing playback position.
            if (ReferenceEquals(ActiveCue, cue)
                && (transitionRoutine != null
                    || (ReferenceEquals(sourceCue, cue) && source.isPlaying)))
            {
                return AudioPlaybackResult.Success(activeHandle);
            }

            float targetVolume = ResolveTargetVolume(cue, settings.Snapshot);
            float fadeIn = ResolveFadeSeconds(cue.FadeInSeconds, settings.Snapshot.DefaultFadeInSeconds);

            if (ReferenceEquals(sourceCue, cue) && source.isPlaying)
            {
                StopTransitionRoutine();
                ActiveCue = cue;
                RestoreCurrentCueVolume(targetVolume, fadeIn);
                return AudioPlaybackResult.Success(activeHandle);
            }

            AudioBgmCueAsset previousSourceCue = sourceCue;
            bool hasPhysicalPlayback = previousSourceCue != null && source.isPlaying;

            StopTransitionRoutine();
            ActiveCue = cue;

            if (!hasPhysicalPlayback)
            {
                StartCue(cue, settings.Snapshot, targetVolume, fadeIn);
                return AudioPlaybackResult.Success(activeHandle);
            }

            float fadeOut = ResolveFadeSeconds(
                previousSourceCue.FadeOutSeconds,
                settings.Snapshot.DefaultFadeOutSeconds);

            if (!isActiveAndEnabled || fadeOut <= 0f)
            {
                SwitchCueImmediately(cue, settings.Snapshot, targetVolume, fadeIn);
                return AudioPlaybackResult.Success(activeHandle);
            }

            transitionRoutine = StartCoroutine(
                TransitionCueRoutine(cue, settings.Snapshot, targetVolume, fadeOut, fadeIn));
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

            if (!source.isPlaying && sourceCue == null && ActiveCue == null)
            {
                return AudioPlaybackResult.Stopped();
            }

            StopTransitionRoutine();
            ActiveCue = null;

            if (!source.isPlaying || sourceCue == null)
            {
                ResetPlaybackState();
                return AudioPlaybackResult.Stopped();
            }

            AudioSettingsResolution settings = ResolveSettings();
            float defaultFadeOut = settings.IsResolved
                ? settings.Snapshot.DefaultFadeOutSeconds
                : 0f;
            float fadeOut = ResolveFadeSeconds(sourceCue.FadeOutSeconds, defaultFadeOut);

            if (fadeOut > 0f && isActiveAndEnabled)
            {
                transitionRoutine = StartCoroutine(StopAfterFadeRoutine(fadeOut));
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

        private void EnsureActiveHandle()
        {
            if (activeHandle == null)
            {
                activeHandle = gameObject.AddComponent<DirectAudioPlaybackHandle>();
            }

            activeHandle.Initialize(source, false);
        }

        private void StartCue(
            AudioBgmCueAsset cue,
            AudioSettingsSnapshot settings,
            float targetVolume,
            float fadeIn)
        {
            ConfigureSource(cue, settings);
            sourceCue = cue;
            EnsureActiveHandle();

            if (fadeIn > 0f && isActiveAndEnabled)
            {
                source.volume = 0f;
                source.Play();
                transitionRoutine = StartCoroutine(FadeCurrentCueRoutine(targetVolume, fadeIn));
                return;
            }

            source.volume = targetVolume;
            source.Play();
        }

        private void SwitchCueImmediately(
            AudioBgmCueAsset cue,
            AudioSettingsSnapshot settings,
            float targetVolume,
            float fadeIn)
        {
            ConfigureSource(cue, settings);
            sourceCue = cue;
            EnsureActiveHandle();

            if (fadeIn > 0f && isActiveAndEnabled)
            {
                source.volume = 0f;
                source.Play();
                transitionRoutine = StartCoroutine(FadeCurrentCueRoutine(targetVolume, fadeIn));
                return;
            }

            source.volume = targetVolume;
            source.Play();
        }

        private void RestoreCurrentCueVolume(float targetVolume, float fadeIn)
        {
            EnsureActiveHandle();
            if (!source.isPlaying)
            {
                source.Play();
            }

            if (fadeIn > 0f && source.volume < targetVolume && isActiveAndEnabled)
            {
                transitionRoutine = StartCoroutine(FadeCurrentCueRoutine(targetVolume, fadeIn));
                return;
            }

            source.volume = targetVolume;
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

        private IEnumerator TransitionCueRoutine(
            AudioBgmCueAsset nextCue,
            AudioSettingsSnapshot settings,
            float targetVolume,
            float fadeOut,
            float fadeIn)
        {
            if (source.volume > 0f)
            {
                yield return FadeVolumeRoutine(0f, fadeOut);
            }

            // A later Play/Stop request stops this coroutine before reaching this point.
            ConfigureSource(nextCue, settings);
            sourceCue = nextCue;
            EnsureActiveHandle();

            if (fadeIn > 0f)
            {
                source.volume = 0f;
                source.Play();
                yield return FadeVolumeRoutine(targetVolume, fadeIn);
            }
            else
            {
                source.volume = targetVolume;
                source.Play();
            }

            transitionRoutine = null;
        }

        private IEnumerator FadeCurrentCueRoutine(float targetVolume, float seconds)
        {
            yield return FadeVolumeRoutine(targetVolume, seconds);
            transitionRoutine = null;
        }

        private IEnumerator StopAfterFadeRoutine(float seconds)
        {
            yield return FadeVolumeRoutine(0f, seconds);
            ResetPlaybackState();
            transitionRoutine = null;
        }

        private IEnumerator FadeVolumeRoutine(float targetVolume, float seconds)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, Mathf.Clamp01(elapsed / seconds));
                yield return null;
            }

            source.volume = targetVolume;
        }

        private void StopImmediate()
        {
            StopTransitionRoutine();
            ResetPlaybackState();
        }

        private void ResetPlaybackState()
        {
            source.Stop();
            source.clip = null;
            sourceCue = null;
            ActiveCue = null;
        }

        private void StopTransitionRoutine()
        {
            if (transitionRoutine == null)
            {
                return;
            }

            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
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
