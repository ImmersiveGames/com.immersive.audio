using System.Collections.Generic;
using UnityEngine;

namespace Immersive.Audio.Authoring
{
    [CreateAssetMenu(
        fileName = "AudioBgmCue",
        menuName = "Immersive/Audio/BGM Cue",
        order = 11)]
    public sealed class AudioBgmCueAsset : AudioCueAsset
    {
        [Header("BGM Transition")]
        [SerializeField] [Min(0f)] private float fadeInSeconds = 1f;

        [SerializeField] [Min(0f)] private float fadeOutSeconds = 1f;

        public float FadeInSeconds => fadeInSeconds;

        public float FadeOutSeconds => fadeOutSeconds;

        public override void ValidateAuthoring(List<string> issues)
        {
            base.ValidateAuthoring(issues);

            if (issues == null)
            {
                return;
            }

            if (fadeInSeconds < 0f)
            {
                issues.Add($"{name} requires fade in seconds >= 0.");
            }

            if (fadeOutSeconds < 0f)
            {
                issues.Add($"{name} requires fade out seconds >= 0.");
            }
        }
    }
}
