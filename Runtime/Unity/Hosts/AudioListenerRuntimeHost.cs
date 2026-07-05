using System.Collections.Generic;
using Immersive.Audio.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Immersive.Audio.Unity.Hosts
{
    [DisallowMultipleComponent]
    public sealed class AudioListenerRuntimeHost : MonoBehaviour
    {
        private const string PersistentListenerObjectName = "ImmersiveAudioListener";

        private static AudioListenerRuntimeHost persistentHost;

        [SerializeField] private Transform listenerTarget;
        [SerializeField] private AudioListenerDuplicatePolicy duplicatePolicy = AudioListenerDuplicatePolicy.ReportOnly;
        [SerializeField] private bool includeInactiveListeners = true;
        [SerializeField] private bool persistAcrossScenes;
        [SerializeField] private bool enforceOnSceneEvents = true;

        private AudioListener hostListener;
        private bool subscribedToSceneEvents;

        public Transform ListenerTarget => listenerTarget;

        public AudioListenerDuplicatePolicy DuplicatePolicy => duplicatePolicy;

        public bool PersistAcrossScenes => persistAcrossScenes;

        public bool EnforceOnSceneEvents => enforceOnSceneEvents;

        public AudioListenerHostReport LastReport { get; private set; }

        private void Awake()
        {
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
                SubscribeSceneEvents();
            }

            LastReport = EnsureListenerAndReport();
        }

        private void OnEnable()
        {
            if (persistAcrossScenes)
            {
                SubscribeSceneEvents();
            }
        }

        private void OnDisable()
        {
            UnsubscribeSceneEvents();
        }

        private void OnDestroy()
        {
            if (persistentHost == this)
            {
                persistentHost = null;
            }

            UnsubscribeSceneEvents();
        }

        public static AudioListenerHostReport EnsurePersistentListenerAndReport(
            AudioListenerDuplicatePolicy policy,
            bool includeInactive)
        {
            AudioListenerRuntimeHost host = ResolvePersistentHost(policy, includeInactive);
            return host != null
                ? host.EnsureListenerAndReport()
                : new AudioListenerHostReport(
                    AudioConfigurationStatus.Failed,
                    policy,
                    0,
                    0,
                    0,
                    0,
                    new[]
                    {
                        new AudioConfigurationIssue(
                            "audio_persistent_listener_host_missing",
                            "Persistent AudioListener host could not be created.",
                            nameof(persistentHost))
                    });
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

        private static AudioListenerRuntimeHost ResolvePersistentHost(
            AudioListenerDuplicatePolicy policy,
            bool includeInactive)
        {
            if (persistentHost != null)
            {
                persistentHost.duplicatePolicy = policy;
                persistentHost.includeInactiveListeners = includeInactive;
                persistentHost.persistAcrossScenes = true;
                persistentHost.enforceOnSceneEvents = true;
                persistentHost.SubscribeSceneEvents();
                return persistentHost;
            }

            persistentHost = FindExistingPersistentHost(includeInactive);
            if (persistentHost == null)
            {
                var hostObject = new GameObject(PersistentListenerObjectName);
                DontDestroyOnLoad(hostObject);
                persistentHost = hostObject.AddComponent<AudioListenerRuntimeHost>();
            }

            persistentHost.listenerTarget = persistentHost.transform;
            persistentHost.duplicatePolicy = policy;
            persistentHost.includeInactiveListeners = includeInactive;
            persistentHost.persistAcrossScenes = true;
            persistentHost.enforceOnSceneEvents = true;
            persistentHost.SubscribeSceneEvents();

            return persistentHost;
        }

        private static AudioListenerRuntimeHost FindExistingPersistentHost(bool includeInactive)
        {
            FindObjectsInactive inactiveMode = includeInactive
                ? FindObjectsInactive.Include
                : FindObjectsInactive.Exclude;

            AudioListenerRuntimeHost[] hosts = FindObjectsByType<AudioListenerRuntimeHost>(inactiveMode, FindObjectsSortMode.None);
            if (hosts == null)
            {
                return null;
            }

            for (int i = 0; i < hosts.Length; i++)
            {
                AudioListenerRuntimeHost host = hosts[i];
                if (host == null)
                {
                    continue;
                }

                if (host.persistAcrossScenes || host.gameObject.name == PersistentListenerObjectName)
                {
                    return host;
                }
            }

            return null;
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

            return FindObjectsByType<AudioListener>(inactiveMode, FindObjectsSortMode.None);
        }

        private void SubscribeSceneEvents()
        {
            if (subscribedToSceneEvents || !enforceOnSceneEvents)
            {
                return;
            }

            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
            subscribedToSceneEvents = true;
        }

        private void UnsubscribeSceneEvents()
        {
            if (!subscribedToSceneEvents)
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            subscribedToSceneEvents = false;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LastReport = EnsureListenerAndReport();
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            LastReport = EnsureListenerAndReport();
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
