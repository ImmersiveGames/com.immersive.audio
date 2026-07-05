using System;
using System.Collections.Generic;

namespace Immersive.Audio.Contracts
{
    public readonly struct AudioSettingsResolution
    {
        private AudioSettingsResolution(
            AudioConfigurationStatus status,
            AudioSettingsSnapshot snapshot,
            IReadOnlyList<AudioConfigurationIssue> issues)
        {
            Status = status;
            Snapshot = snapshot;
            Issues = issues ?? Array.Empty<AudioConfigurationIssue>();
        }

        public AudioConfigurationStatus Status { get; }

        public AudioSettingsSnapshot Snapshot { get; }

        public IReadOnlyList<AudioConfigurationIssue> Issues { get; }

        public bool IsResolved => Status == AudioConfigurationStatus.Resolved;

        public static AudioSettingsResolution Resolved(AudioSettingsSnapshot snapshot)
        {
            return new AudioSettingsResolution(
                AudioConfigurationStatus.Resolved,
                snapshot,
                Array.Empty<AudioConfigurationIssue>());
        }

        public static AudioSettingsResolution Failed(params AudioConfigurationIssue[] issues)
        {
            return new AudioSettingsResolution(
                AudioConfigurationStatus.Failed,
                default,
                issues ?? Array.Empty<AudioConfigurationIssue>());
        }
    }
}
