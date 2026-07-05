using System;
using System.Collections.Generic;

namespace Immersive.Audio.Contracts
{
    public readonly struct AudioPlaybackResult
    {
        private AudioPlaybackResult(
            AudioPlaybackStatus status,
            IAudioPlaybackHandle handle,
            IReadOnlyList<AudioConfigurationIssue> issues)
        {
            Status = status;
            Handle = handle;
            Issues = issues ?? Array.Empty<AudioConfigurationIssue>();
        }

        public AudioPlaybackStatus Status { get; }

        public IAudioPlaybackHandle Handle { get; }

        public IReadOnlyList<AudioConfigurationIssue> Issues { get; }

        public bool Succeeded => Status == AudioPlaybackStatus.Succeeded;

        public static AudioPlaybackResult Success(IAudioPlaybackHandle handle)
        {
            if (handle == null)
            {
                throw new ArgumentNullException(nameof(handle));
            }

            return new AudioPlaybackResult(
                AudioPlaybackStatus.Succeeded,
                handle,
                Array.Empty<AudioConfigurationIssue>());
        }

        public static AudioPlaybackResult Failure(AudioPlaybackStatus status, params AudioConfigurationIssue[] issues)
        {
            if (status == AudioPlaybackStatus.Succeeded)
            {
                throw new ArgumentException("Failure result cannot use Succeeded status.", nameof(status));
            }

            return new AudioPlaybackResult(
                status,
                null,
                issues ?? Array.Empty<AudioConfigurationIssue>());
        }

        public static AudioPlaybackResult Stopped()
        {
            return new AudioPlaybackResult(
                AudioPlaybackStatus.Stopped,
                null,
                Array.Empty<AudioConfigurationIssue>());
        }
    }
}
