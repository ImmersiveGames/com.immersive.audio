using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Services;
using Immersive.Audio.Unity.Services;
using Immersive.Pooling.Unity.Contracts;
using Immersive.Pooling.Unity.Hosts;
using UnityEngine;

namespace Immersive.Audio.Unity.Hosts
{
    [DisallowMultipleComponent]
    public sealed class AudioRuntimeHost : MonoBehaviour
    {
        [SerializeField] private AudioDefaultsAsset defaults;
        [SerializeField] private Transform playbackRoot;
        [SerializeField] private PoolRuntimeHost poolRuntimeHost;
        [SerializeField] private bool composeOnAwake = true;
        [SerializeField] private bool ensurePersistentListener = true;
        [SerializeField] private AudioListenerDuplicatePolicy listenerDuplicatePolicy = AudioListenerDuplicatePolicy.DisableDuplicates;
        [SerializeField] private bool includeInactiveListenersForListenerReport = true;

        private AudioSettingsService settingsService;
        private AudioRoutingResolver routingResolver;
        private AudioGlobalSfxService sfxService;
        private AudioBgmService bgmService;
        private AudioConfigurationIssue? poolServiceIssue;

        public AudioDefaultsAsset Defaults => defaults;

        public AudioSettingsResolution Settings => settingsService != null
            ? settingsService.Settings
            : AudioSettingsResolution.Failed(
                new AudioConfigurationIssue(
                    "audio_runtime_host_not_composed",
                    "AudioRuntimeHost has not composed AudioSettingsService.",
                    nameof(settingsService)));

        public IAudioSfxService SfxService => sfxService;

        public IAudioBgmService BgmService => bgmService;

        public IPoolService PoolService => poolRuntimeHost != null && poolRuntimeHost.IsInitialized
            ? poolRuntimeHost.Service
            : null;

        public bool EnsurePersistentListener => ensurePersistentListener;

        public AudioListenerHostReport ListenerReport { get; private set; }

        private void Awake()
        {
            EnsurePersistentListenerIfRequested();

            if (composeOnAwake)
            {
                Compose();
            }
        }

        public AudioSettingsResolution Compose()
        {
            EnsurePersistentListenerIfRequested();

            Transform root = EnsurePlaybackRoot();

            settingsService = new AudioSettingsService(defaults);
            routingResolver = new AudioRoutingResolver(defaults);
            IPoolService poolService = ResolvePoolService(out poolServiceIssue);

            sfxService = GetOrCreateChildComponent<AudioGlobalSfxService>("AudioSfxService");
            sfxService.Initialize(settingsService, routingResolver, root, poolService, poolServiceIssue);

            bgmService = GetOrCreateChildComponent<AudioBgmService>("AudioBgmService");
            AudioSource bgmSource = GetOrCreateAudioSource(bgmService.gameObject);
            bgmService.Initialize(settingsService, routingResolver, bgmSource);

            return settingsService.Settings;
        }

        public AudioPlaybackResult PlaySfx(AudioSfxCueAsset cue)
        {
            EnsureComposed();
            return sfxService != null
                ? sfxService.Play(cue)
                : AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedServiceNotReady,
                    new AudioConfigurationIssue(
                        "audio_sfx_service_missing",
                        "AudioGlobalSfxService is not composed.",
                        nameof(sfxService)));
        }

        public AudioPlaybackResult PlayBgm(AudioBgmCueAsset cue)
        {
            EnsureComposed();
            return bgmService != null
                ? bgmService.Play(cue)
                : AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedServiceNotReady,
                    new AudioConfigurationIssue(
                        "audio_bgm_service_missing",
                        "AudioBgmService is not composed.",
                        nameof(bgmService)));
        }

        public AudioPlaybackResult StopBgm()
        {
            EnsureComposed();
            return bgmService != null
                ? bgmService.Stop()
                : AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedServiceNotReady,
                    new AudioConfigurationIssue(
                        "audio_bgm_service_missing",
                        "AudioBgmService is not composed.",
                        nameof(bgmService)));
        }

        private void EnsureComposed()
        {
            if (settingsService == null || routingResolver == null || sfxService == null || bgmService == null)
            {
                Compose();
            }
        }

        private void EnsurePersistentListenerIfRequested()
        {
            if (!ensurePersistentListener)
            {
                return;
            }

            ListenerReport = AudioListenerRuntimeHost.EnsurePersistentListenerAndReport(
                listenerDuplicatePolicy,
                includeInactiveListenersForListenerReport);
        }

        private Transform EnsurePlaybackRoot()
        {
            if (playbackRoot != null)
            {
                return playbackRoot;
            }

            Transform existing = transform.Find("AudioPlayback");
            if (existing != null)
            {
                playbackRoot = existing;
                return playbackRoot;
            }

            var rootObject = new GameObject("AudioPlayback");
            rootObject.transform.SetParent(transform, false);
            playbackRoot = rootObject.transform;
            return playbackRoot;
        }

        private IPoolService ResolvePoolService(out AudioConfigurationIssue? issue)
        {
            issue = null;

            if (poolRuntimeHost == null)
            {
                return null;
            }

            try
            {
                if (!poolRuntimeHost.IsInitialized)
                {
                    poolRuntimeHost.Initialize();
                }

                return poolRuntimeHost.Service;
            }
            catch (System.Exception exception)
            {
                issue = new AudioConfigurationIssue(
                    "audio_pool_runtime_host_failed",
                    $"PoolRuntimeHost could not provide IPoolService: {exception.Message}",
                    nameof(poolRuntimeHost));
                return null;
            }
        }

        private T GetOrCreateChildComponent<T>(string childName) where T : Component
        {
            Transform child = playbackRoot != null ? playbackRoot.Find(childName) : null;
            if (child == null)
            {
                var childObject = new GameObject(childName);
                childObject.transform.SetParent(EnsurePlaybackRoot(), false);
                child = childObject.transform;
            }

            if (child.TryGetComponent(out T component) && component != null)
            {
                return component;
            }

            return child.gameObject.AddComponent<T>();
        }

        private static AudioSource GetOrCreateAudioSource(GameObject owner)
        {
            if (owner.TryGetComponent(out AudioSource existing) && existing != null)
            {
                return existing;
            }

            return owner.AddComponent<AudioSource>();
        }
    }
}
