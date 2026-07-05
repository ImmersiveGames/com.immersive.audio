using System;

namespace Immersive.Audio.Contracts
{
    public readonly struct AudioCueId : IEquatable<AudioCueId>
    {
        public AudioCueId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Audio cue id cannot be null, empty, or whitespace.", nameof(value));
            }

            Value = value.Trim();
        }

        public string Value { get; }

        public bool Equals(AudioCueId other)
        {
            return string.Equals(Value, other.Value, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is AudioCueId other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        }

        public override string ToString()
        {
            return Value ?? string.Empty;
        }
    }
}
