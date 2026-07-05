namespace Immersive.Audio.Contracts
{
    public interface IAudioPlaybackHandle
    {
        bool IsValid { get; }

        bool IsPlaying { get; }

        AudioPlaybackResult Stop();
    }
}
