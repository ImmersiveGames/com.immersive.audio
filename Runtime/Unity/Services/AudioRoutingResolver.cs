using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;

namespace Immersive.Audio.Unity.Services
{
    public sealed class AudioRoutingResolver
    {
        private readonly AudioDefaultsAsset defaults;

        public AudioRoutingResolver(AudioDefaultsAsset defaults)
        {
            this.defaults = defaults;
        }

        public AudioRoutingResolution ResolveSfxBus(AudioSfxCueAsset cue)
        {
            return ResolveBus(
                cue != null ? cue.RoutingBusValue : string.Empty,
                defaults != null ? defaults.SfxBusValue : string.Empty,
                "audio_sfx_bus_missing",
                "SFX routing requires AudioDefaultsAsset with a valid SFX bus.");
        }

        public AudioRoutingResolution ResolveBgmBus(AudioBgmCueAsset cue)
        {
            return ResolveBus(
                cue != null ? cue.RoutingBusValue : string.Empty,
                defaults != null ? defaults.BgmBusValue : string.Empty,
                "audio_bgm_bus_missing",
                "BGM routing requires AudioDefaultsAsset with a valid BGM bus.");
        }

        private AudioRoutingResolution ResolveBus(
            string cueBus,
            string defaultBus,
            string missingDefaultCode,
            string missingDefaultMessage)
        {
            if (defaults == null)
            {
                return AudioRoutingResolution.Failed(
                    new AudioConfigurationIssue(
                        "audio_defaults_missing",
                        "AudioDefaultsAsset is required to resolve audio routing.",
                        nameof(defaults)));
            }

            if (!string.IsNullOrWhiteSpace(cueBus))
            {
                return AudioRoutingResolution.Resolved(
                    new AudioBusKey(cueBus),
                    AudioRoutingSource.Cue);
            }

            if (string.IsNullOrWhiteSpace(defaultBus))
            {
                return AudioRoutingResolution.Failed(
                    new AudioConfigurationIssue(
                        missingDefaultCode,
                        missingDefaultMessage,
                        nameof(defaultBus)));
            }

            return AudioRoutingResolution.Resolved(
                new AudioBusKey(defaultBus),
                AudioRoutingSource.Defaults);
        }
    }
}
