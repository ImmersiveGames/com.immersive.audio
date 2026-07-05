using System;
using System.Collections.Generic;

namespace Immersive.Audio.Contracts
{
    public readonly struct AudioListenerHostReport
    {
        public AudioListenerHostReport(
            AudioConfigurationStatus status,
            AudioListenerDuplicatePolicy duplicatePolicy,
            int totalListeners,
            int enabledListeners,
            int duplicateEnabledListeners,
            int disabledDuplicates,
            IReadOnlyList<AudioConfigurationIssue> issues)
        {
            Status = status;
            DuplicatePolicy = duplicatePolicy;
            TotalListeners = Math.Max(0, totalListeners);
            EnabledListeners = Math.Max(0, enabledListeners);
            DuplicateEnabledListeners = Math.Max(0, duplicateEnabledListeners);
            DisabledDuplicates = Math.Max(0, disabledDuplicates);
            Issues = issues ?? Array.Empty<AudioConfigurationIssue>();
        }

        public AudioConfigurationStatus Status { get; }

        public AudioListenerDuplicatePolicy DuplicatePolicy { get; }

        public int TotalListeners { get; }

        public int EnabledListeners { get; }

        public int DuplicateEnabledListeners { get; }

        public int DisabledDuplicates { get; }

        public IReadOnlyList<AudioConfigurationIssue> Issues { get; }

        public bool HasIssues => Issues.Count > 0;
    }
}
