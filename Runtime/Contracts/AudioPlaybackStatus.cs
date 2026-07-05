namespace Immersive.Audio.Contracts
{
    public enum AudioPlaybackStatus
    {
        Succeeded = 0,
        FailedMissingCue = 1,
        FailedMissingClip = 2,
        FailedMissingDefaults = 3,
        FailedInvalidSettings = 4,
        FailedInvalidRouting = 5,
        FailedMissingAudioSource = 6,
        FailedAlreadyPlaying = 7,
        FailedServiceNotReady = 8,
        Stopped = 9,
        FailedMissingPoolService = 10,
        FailedMissingPoolDefinition = 11,
        FailedPoolRentFailed = 12,
        FailedMissingPooledAudioSource = 13,
        FailedPoolReturnFailed = 14
    }
}
