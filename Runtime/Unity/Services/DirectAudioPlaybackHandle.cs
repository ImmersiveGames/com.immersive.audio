using Immersive.Audio.Contracts;
using UnityEngine;

namespace Immersive.Audio.Unity.Services
{
    public sealed class DirectAudioPlaybackHandle : MonoBehaviour, IAudioPlaybackHandle
    {
        private AudioSource source;
        private bool destroyOwnerOnComplete;
        private bool completionDestroysOwner;

        public bool IsValid { get; private set; }

        public bool IsPlaying => IsValid && source != null && source.isPlaying;

        public void Initialize(AudioSource audioSource, bool destroyOwnerWhenComplete)
        {
            source = audioSource;
            destroyOwnerOnComplete = destroyOwnerWhenComplete;
            completionDestroysOwner = destroyOwnerWhenComplete;
            IsValid = source != null;
        }

        public AudioPlaybackResult Stop()
        {
            if (!IsValid)
            {
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedServiceNotReady,
                    new AudioConfigurationIssue(
                        "audio_playback_handle_invalid",
                        "Playback handle is no longer valid.",
                        nameof(IsValid)));
            }

            Complete(destroyOwnerOnComplete);
            return AudioPlaybackResult.Stopped();
        }

        private void Update()
        {
            if (!IsValid || source == null)
            {
                return;
            }

            if (!source.loop && !source.isPlaying)
            {
                Complete(completionDestroysOwner);
            }
        }

        private void OnDestroy()
        {
            IsValid = false;
            source = null;
        }

        private void Complete(bool destroyOwner)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }

            IsValid = false;

            if (destroyOwner && destroyOwnerOnComplete)
            {
                Destroy(gameObject);
            }
        }
    }
}
