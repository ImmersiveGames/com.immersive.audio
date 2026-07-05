using System.Collections.Generic;
using Immersive.Audio.Contracts;
using UnityEngine;

namespace Immersive.Audio.Unity.Hosts
{
    [DisallowMultipleComponent]
    public sealed class AudioListenerRuntimeHost : MonoBehaviour
    {
        [SerializeField] private Transform listenerTarget;
        [SerializeField] private AudioListenerDuplicatePolicy duplicatePolicy = AudioListenerDuplicatePolicy.ReportOnly;
        [SerializeField] private bool includeInactiveListeners = true;

        private AudioListener hostListener;

        public Transform ListenerTarget => listenerTarget;

        public AudioListenerDuplicatePolicy DuplicatePolicy => duplicatePolicy;

        public AudioListenerHostReport LastReport { get; private set; }

        private void Awake()
        {
            LastReport = EnsureListenerAndReport();
        }

        public AudioListenerHostReport EnsureListenerAndReport()
        {
            var issues = new List<AudioConfigurationIssue>();
            AudioListener ensuredListener = EnsureHostListener(issues);

            AudioListener[] listeners = FindSceneListeners();
            int totalListeners = listeners != null ? listeners.Length : 0;
            int enabledListeners = CountEnabledListeners(listeners);
            int duplicateEnabledListeners = CountEnabledDuplicates(listeners, ensuredListener);
            int disabledDuplicates = 0;

            if (duplicateEnabledListeners > 0)
            {
                issues.Add(
                    new AudioConfigurationIssue(
                        "audio_listener_duplicates_detected",
                        "Enabled duplicate AudioListeners were detected.",
                        nameof(duplicatePolicy)));

                if (duplicatePolicy == AudioListenerDuplicatePolicy.DisableDuplicates)
                {
                    disabledDuplicates = DisableDuplicateListeners(listeners, ensuredListener);
                    listeners = FindSceneListeners();
                    enabledListeners = CountEnabledListeners(listeners);
                    duplicateEnabledListeners = CountEnabledDuplicates(listeners, ensuredListener);
                }
            }

            AudioConfigurationStatus status = issues.Count > 0
                ? AudioConfigurationStatus.IssuesDetected
                : AudioConfigurationStatus.Resolved;

            if (ensuredListener == null)
            {
                status = AudioConfigurationStatus.Failed;
            }

            LastReport = new AudioListenerHostReport(
                status,
                duplicatePolicy,
                totalListeners,
                enabledListeners,
                duplicateEnabledListeners,
                disabledDuplicates,
                issues);

            return LastReport;
        }

        private AudioListener EnsureHostListener(List<AudioConfigurationIssue> issues)
        {
            Transform target = listenerTarget != null ? listenerTarget : transform;
            if (target == null)
            {
                issues.Add(
                    new AudioConfigurationIssue(
                        "audio_listener_target_missing",
                        "AudioListenerRuntimeHost requires a valid target transform.",
                        nameof(listenerTarget)));
                return null;
            }

            if (!target.TryGetComponent(out hostListener) || hostListener == null)
            {
                hostListener = target.gameObject.AddComponent<AudioListener>();
            }

            if (!hostListener.enabled)
            {
                hostListener.enabled = true;
            }

            return hostListener;
        }

        private AudioListener[] FindSceneListeners()
        {
            FindObjectsInactive inactiveMode = includeInactiveListeners
                ? FindObjectsInactive.Include
                : FindObjectsInactive.Exclude;

            return FindObjectsByType<AudioListener>(inactiveMode);
        }

        private static int CountEnabledListeners(AudioListener[] listeners)
        {
            if (listeners == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener != null && listener.enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnabledDuplicates(AudioListener[] listeners, AudioListener keep)
        {
            if (listeners == null || keep == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener != null && listener != keep && listener.enabled)
                {
                    count++;
                }
            }

            return count;
        }

        private static int DisableDuplicateListeners(AudioListener[] listeners, AudioListener keep)
        {
            if (listeners == null || keep == null)
            {
                return 0;
            }

            int disabled = 0;
            for (int i = 0; i < listeners.Length; i++)
            {
                AudioListener listener = listeners[i];
                if (listener == null || listener == keep || !listener.enabled)
                {
                    continue;
                }

                listener.enabled = false;
                disabled++;
            }

            return disabled;
        }
    }
}
