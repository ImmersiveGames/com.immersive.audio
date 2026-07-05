using System.Collections.Generic;
using Immersive.Audio.Contracts;
using UnityEngine;

namespace Immersive.Audio.Authoring
{
    public abstract class AudioCueAsset : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string cueId;

        [Header("Clip")]
        [SerializeField] private AudioClip clip;

        [Header("Playback Defaults")]
        [SerializeField] [Range(AudioAuthoringRanges.MinVolume, AudioAuthoringRanges.MaxVolume)]
        private float volume = AudioAuthoringRanges.DefaultVolume;

        [SerializeField] [Min(AudioAuthoringRanges.MinPitch)]
        private float pitch = AudioAuthoringRanges.DefaultPitch;

        [SerializeField] private AudioLoopMode loopMode = AudioLoopMode.Off;

        [Header("Routing")]
        [SerializeField] private string routingBus = AudioBusKeys.Master;

        public string CueIdValue => Normalize(cueId);

        public AudioClip Clip => clip;

        public float Volume => volume;

        public float Pitch => pitch;

        public AudioLoopMode LoopMode => loopMode;

        public string RoutingBusValue => Normalize(routingBus);

        public AudioCueId CueId => new AudioCueId(CueIdValue);

        public AudioBusKey RoutingBus => new AudioBusKey(RoutingBusValue);

        public virtual void ValidateAuthoring(List<string> issues)
        {
            if (issues == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(cueId))
            {
                issues.Add($"{name} requires an explicit cue id.");
            }

            if (clip == null)
            {
                issues.Add($"{name} requires an AudioClip.");
            }

            if (volume < AudioAuthoringRanges.MinVolume || volume > AudioAuthoringRanges.MaxVolume)
            {
                issues.Add($"{name} requires volume between 0 and 1.");
            }

            if (pitch < AudioAuthoringRanges.MinPitch || pitch > AudioAuthoringRanges.MaxPitch)
            {
                issues.Add($"{name} requires pitch between 0.01 and 3.");
            }

            if (string.IsNullOrWhiteSpace(routingBus))
            {
                issues.Add($"{name} requires a routing bus.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
