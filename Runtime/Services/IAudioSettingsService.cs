using Immersive.Audio.Contracts;

namespace Immersive.Audio.Services
{
    public interface IAudioSettingsService
    {
        AudioSettingsResolution Settings { get; }
    }
}
