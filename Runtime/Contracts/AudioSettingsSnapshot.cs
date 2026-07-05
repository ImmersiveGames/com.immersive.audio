namespace Immersive.Audio.Contracts
{
    public readonly struct AudioSettingsSnapshot
    {
        public AudioSettingsSnapshot(
            float masterVolume,
            float sfxVolume,
            float bgmVolume,
            AudioBusKey defaultSfxBus,
            AudioBusKey defaultBgmBus,
            float defaultFadeInSeconds,
            float defaultFadeOutSeconds)
        {
            MasterVolume = masterVolume;
            SfxVolume = sfxVolume;
            BgmVolume = bgmVolume;
            DefaultSfxBus = defaultSfxBus;
            DefaultBgmBus = defaultBgmBus;
            DefaultFadeInSeconds = defaultFadeInSeconds;
            DefaultFadeOutSeconds = defaultFadeOutSeconds;
        }

        public float MasterVolume { get; }

        public float SfxVolume { get; }

        public float BgmVolume { get; }

        public AudioBusKey DefaultSfxBus { get; }

        public AudioBusKey DefaultBgmBus { get; }

        public float DefaultFadeInSeconds { get; }

        public float DefaultFadeOutSeconds { get; }
    }
}
