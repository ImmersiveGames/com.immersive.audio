using System.Collections.Generic;
using Immersive.Audio.Contracts;
using UnityEngine;

namespace Immersive.Audio.Authoring
{
    [CreateAssetMenu(
        fileName = "AudioDefaults",
        menuName = "Immersive/Audio/Audio Defaults",
        order = 12)]
    public sealed class AudioDefaultsAsset : ScriptableObject
    {
        [Header("Volume Defaults")]
        [SerializeField] [Range(AudioAuthoringRanges.MinVolume, AudioAuthoringRanges.MaxVolume)]
        private float masterVolume = AudioAuthoringRanges.DefaultVolume;

        [SerializeField] [Range(AudioAuthoringRanges.MinVolume, AudioAuthoringRanges.MaxVolume)]
        private float sfxVolume = AudioAuthoringRanges.DefaultVolume;

        [SerializeField] [Range(AudioAuthoringRanges.MinVolume, AudioAuthoringRanges.MaxVolume)]
        private float bgmVolume = AudioAuthoringRanges.DefaultVolume;

        [Header("Routing Defaults")]
        [SerializeField] private string masterBus = AudioBusKeys.Master;

        [SerializeField] private string sfxBus = AudioBusKeys.Sfx;

        [SerializeField] private string bgmBus = AudioBusKeys.Bgm;

        [Header("Transition Defaults")]
        [SerializeField] [Min(0f)] private float defaultFadeInSeconds = 1f;

        [SerializeField] [Min(0f)] private float defaultFadeOutSeconds = 1f;

        public float MasterVolume => masterVolume;

        public float SfxVolume => sfxVolume;

        public float BgmVolume => bgmVolume;

        public string MasterBusValue => Normalize(masterBus);

        public string SfxBusValue => Normalize(sfxBus);

        public string BgmBusValue => Normalize(bgmBus);

        public AudioBusKey MasterBus => new AudioBusKey(MasterBusValue);

        public AudioBusKey SfxBus => new AudioBusKey(SfxBusValue);

        public AudioBusKey BgmBus => new AudioBusKey(BgmBusValue);

        public float DefaultFadeInSeconds => defaultFadeInSeconds;

        public float DefaultFadeOutSeconds => defaultFadeOutSeconds;

        public void ValidateAuthoring(List<string> issues)
        {
            if (issues == null)
            {
                return;
            }

            ValidateVolume(issues, masterVolume, nameof(masterVolume));
            ValidateVolume(issues, sfxVolume, nameof(sfxVolume));
            ValidateVolume(issues, bgmVolume, nameof(bgmVolume));
            ValidateBus(issues, masterBus, nameof(masterBus));
            ValidateBus(issues, sfxBus, nameof(sfxBus));
            ValidateBus(issues, bgmBus, nameof(bgmBus));

            if (defaultFadeInSeconds < 0f)
            {
                issues.Add($"{name} requires default fade in seconds >= 0.");
            }

            if (defaultFadeOutSeconds < 0f)
            {
                issues.Add($"{name} requires default fade out seconds >= 0.");
            }
        }

        private void ValidateVolume(List<string> issues, float value, string fieldName)
        {
            if (value < AudioAuthoringRanges.MinVolume || value > AudioAuthoringRanges.MaxVolume)
            {
                issues.Add($"{name} requires {fieldName} between 0 and 1.");
            }
        }

        private void ValidateBus(List<string> issues, string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add($"{name} requires {fieldName}.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
