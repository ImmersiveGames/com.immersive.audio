using System;

namespace Immersive.Audio.Contracts
{
    public readonly struct AudioConfigurationIssue
    {
        public AudioConfigurationIssue(string code, string message, string memberName = null)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentException("Issue code cannot be null, empty, or whitespace.", nameof(code));
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Issue message cannot be null, empty, or whitespace.", nameof(message));
            }

            Code = code.Trim();
            Message = message.Trim();
            MemberName = string.IsNullOrWhiteSpace(memberName) ? string.Empty : memberName.Trim();
        }

        public string Code { get; }

        public string Message { get; }

        public string MemberName { get; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(MemberName)
                ? $"{Code}: {Message}"
                : $"{Code}({MemberName}): {Message}";
        }
    }
}
