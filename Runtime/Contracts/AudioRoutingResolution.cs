using System;
using System.Collections.Generic;

namespace Immersive.Audio.Contracts
{
    public readonly struct AudioRoutingResolution
    {
        private AudioRoutingResolution(
            AudioConfigurationStatus status,
            AudioBusKey bus,
            AudioRoutingSource source,
            IReadOnlyList<AudioConfigurationIssue> issues)
        {
            Status = status;
            Bus = bus;
            Source = source;
            Issues = issues ?? Array.Empty<AudioConfigurationIssue>();
        }

        public AudioConfigurationStatus Status { get; }

        public AudioBusKey Bus { get; }

        public AudioRoutingSource Source { get; }

        public IReadOnlyList<AudioConfigurationIssue> Issues { get; }

        public bool IsResolved => Status == AudioConfigurationStatus.Resolved;

        public static AudioRoutingResolution Resolved(AudioBusKey bus, AudioRoutingSource source)
        {
            return new AudioRoutingResolution(
                AudioConfigurationStatus.Resolved,
                bus,
                source,
                Array.Empty<AudioConfigurationIssue>());
        }

        public static AudioRoutingResolution Failed(params AudioConfigurationIssue[] issues)
        {
            return new AudioRoutingResolution(
                AudioConfigurationStatus.Failed,
                default,
                AudioRoutingSource.None,
                issues ?? Array.Empty<AudioConfigurationIssue>());
        }
    }
}
