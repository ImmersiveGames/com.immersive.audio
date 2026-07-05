using System.Collections.Generic;
using Immersive.Audio.Contracts;
using Immersive.Pooling.Unity.Authoring;
using UnityEngine;

namespace Immersive.Audio.Authoring
{
    [CreateAssetMenu(
        fileName = "AudioSfxCue",
        menuName = "Immersive/Audio/SFX Cue",
        order = 10)]
    public sealed class AudioSfxCueAsset : AudioCueAsset
    {
        [Header("SFX Emission")]
        [SerializeField] private AudioPlaybackMode playbackMode = AudioPlaybackMode.Global;

        [SerializeField] private AudioSfxExecutionMode executionMode = AudioSfxExecutionMode.Direct;

        [SerializeField] [Range(0f, 1f)] private float spatialBlend;

        [SerializeField] [Min(0f)] private float minDistance = 1f;

        [SerializeField] [Min(0f)] private float maxDistance = 40f;

        [Header("SFX Voice Policy")]
        [SerializeField] private PoolDefinitionAsset pooledAudioSourcePool;

        [SerializeField] [Min(1)] private int maxSimultaneousInstances = 1;

        [SerializeField] [Min(0f)] private float retriggerCooldownSeconds;

        public AudioPlaybackMode PlaybackMode => playbackMode;

        public AudioSfxExecutionMode ExecutionMode => executionMode;

        public PoolDefinitionAsset PooledAudioSourcePool => pooledAudioSourcePool;

        public float SpatialBlend => spatialBlend;

        public float MinDistance => minDistance;

        public float MaxDistance => maxDistance;

        public int MaxSimultaneousInstances => maxSimultaneousInstances;

        public float RetriggerCooldownSeconds => retriggerCooldownSeconds;

        public override void ValidateAuthoring(List<string> issues)
        {
            base.ValidateAuthoring(issues);

            if (issues == null)
            {
                return;
            }

            if (playbackMode == AudioPlaybackMode.Spatial && maxDistance < minDistance)
            {
                issues.Add($"{name} requires max distance greater than or equal to min distance.");
            }

            if (maxSimultaneousInstances < 1)
            {
                issues.Add($"{name} requires at least one simultaneous SFX instance.");
            }

            if (executionMode == AudioSfxExecutionMode.Pooled && pooledAudioSourcePool == null)
            {
                issues.Add($"{name} requires a pooled audio source pool when execution mode is Pooled.");
            }
        }
    }
}
