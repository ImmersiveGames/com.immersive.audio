using Immersive.Audio.Contracts;
using Immersive.Pooling.Unity.Authoring;
using Immersive.Pooling.Unity.Contracts;
using UnityEngine;

namespace Immersive.Audio.Unity.Services
{
    public sealed class PooledAudioPlaybackHandle : MonoBehaviour, IAudioPlaybackHandle
    {
        private IPoolService poolService;
        private PoolDefinitionAsset poolDefinition;
        private AudioSource source;
        private GameObject pooledInstance;
        private bool returned;

        public bool IsValid { get; private set; }

        public bool IsPlaying => IsValid && !returned && source != null && source.isPlaying;

        public void Initialize(
            IPoolService pool,
            PoolDefinitionAsset definition,
            GameObject instance,
            AudioSource audioSource)
        {
            poolService = pool;
            poolDefinition = definition;
            pooledInstance = instance;
            source = audioSource;
            returned = false;
            IsValid = poolService != null && poolDefinition != null && pooledInstance != null && source != null;
        }

        public AudioPlaybackResult Stop()
        {
            if (!IsValid)
            {
                return AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedServiceNotReady,
                    new AudioConfigurationIssue(
                        "audio_pooled_handle_invalid",
                        "Pooled playback handle is no longer valid.",
                        nameof(IsValid)));
            }

            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }

            return ReturnToPool()
                ? AudioPlaybackResult.Stopped()
                : AudioPlaybackResult.Failure(
                    AudioPlaybackStatus.FailedPoolReturnFailed,
                    new AudioConfigurationIssue(
                        "audio_pool_return_failed",
                        "Pooled audio source could not be returned to its pool.",
                        nameof(poolService)));
        }

        private void Update()
        {
            if (!IsValid || returned || source == null)
            {
                return;
            }

            if (!source.loop && !source.isPlaying)
            {
                ReturnToPool();
            }
        }

        private bool ReturnToPool()
        {
            if (returned)
            {
                return true;
            }

            returned = true;
            IsValid = false;

            if (source != null)
            {
                source.Stop();
                source.clip = null;
            }

            if (poolService == null || poolDefinition == null || pooledInstance == null)
            {
                return false;
            }

            try
            {
                return poolService.Return(poolDefinition, pooledInstance);
            }
            catch
            {
                return false;
            }
        }
    }
}
