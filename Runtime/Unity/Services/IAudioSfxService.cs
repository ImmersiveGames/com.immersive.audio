using Immersive.Audio.Authoring;
using Immersive.Audio.Contracts;
using UnityEngine;

namespace Immersive.Audio.Unity.Services
{
    public interface IAudioSfxService
    {
        AudioPlaybackResult Play(AudioSfxCueAsset cue);

        AudioPlaybackResult PlayAt(AudioSfxCueAsset cue, Vector3 worldPosition);
    }
}
