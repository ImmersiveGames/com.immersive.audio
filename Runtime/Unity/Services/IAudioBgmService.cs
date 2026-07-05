using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;

namespace Immersive.Audio.Unity.Services
{
    public interface IAudioBgmService
    {
        AudioBgmCueAsset ActiveCue { get; }

        AudioPlaybackResult Play(AudioBgmCueAsset cue);

        AudioPlaybackResult Stop();
    }
}
