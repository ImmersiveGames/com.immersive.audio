using System.Collections.Generic;
using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using Immersive.Audio.Services;

namespace Immersive.Audio.Unity.Services
{
    public sealed class AudioSettingsService : IAudioSettingsService
    {
        public AudioSettingsService(AudioDefaultsAsset defaults)
        {
            Settings = Resolve(defaults);
        }

        public AudioSettingsResolution Settings { get; }

        public static AudioSettingsResolution Resolve(AudioDefaultsAsset defaults)
        {
            if (defaults == null)
            {
                return AudioSettingsResolution.Failed(
                    new AudioConfigurationIssue(
                        "audio_defaults_missing",
                        "AudioDefaultsAsset is required to resolve audio settings.",
                        nameof(defaults)));
            }

            var issues = new List<AudioConfigurationIssue>();
            ValidateVolume(issues, defaults.MasterVolume, nameof(defaults.MasterVolume));
            ValidateVolume(issues, defaults.SfxVolume, nameof(defaults.SfxVolume));
            ValidateVolume(issues, defaults.BgmVolume, nameof(defaults.BgmVolume));
            ValidateBus(issues, defaults.SfxBusValue, nameof(defaults.SfxBusValue));
            ValidateBus(issues, defaults.BgmBusValue, nameof(defaults.BgmBusValue));
            ValidateFade(issues, defaults.DefaultFadeInSeconds, nameof(defaults.DefaultFadeInSeconds));
            ValidateFade(issues, defaults.DefaultFadeOutSeconds, nameof(defaults.DefaultFadeOutSeconds));

            if (issues.Count > 0)
            {
                return AudioSettingsResolution.Failed(issues.ToArray());
            }

            return AudioSettingsResolution.Resolved(
                new AudioSettingsSnapshot(
                    defaults.MasterVolume,
                    defaults.SfxVolume,
                    defaults.BgmVolume,
                    new AudioBusKey(defaults.SfxBusValue),
                    new AudioBusKey(defaults.BgmBusValue),
                    defaults.DefaultFadeInSeconds,
                    defaults.DefaultFadeOutSeconds));
        }

        private static void ValidateVolume(List<AudioConfigurationIssue> issues, float value, string memberName)
        {
            if (value < AudioAuthoringRanges.MinVolume || value > AudioAuthoringRanges.MaxVolume)
            {
                issues.Add(
                    new AudioConfigurationIssue(
                        "audio_volume_out_of_range",
                        "Audio volume must be between 0 and 1.",
                        memberName));
            }
        }

        private static void ValidateBus(List<AudioConfigurationIssue> issues, string value, string memberName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add(
                    new AudioConfigurationIssue(
                        "audio_bus_missing",
                        "Audio bus key is required.",
                        memberName));
            }
        }

        private static void ValidateFade(List<AudioConfigurationIssue> issues, float value, string memberName)
        {
            if (value < 0f)
            {
                issues.Add(
                    new AudioConfigurationIssue(
                        "audio_fade_out_of_range",
                        "Audio fade value must be greater than or equal to zero.",
                        memberName));
            }
        }
    }
}
