using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Immersive.Audio.Editor
{
    internal sealed class AudioAuthoringValidationReport
    {
        private readonly List<AudioAuthoringValidationIssue> _issues =
            new List<AudioAuthoringValidationIssue>();

        internal IReadOnlyList<AudioAuthoringValidationIssue> Issues =>
            _issues;

        internal int ErrorCount => _issues.Count;

        internal bool IsValid => ErrorCount == 0;

        internal void AddError(
            string message,
            Object context)
        {
            _issues.Add(
                new AudioAuthoringValidationIssue(
                    message,
                    context));
        }

        internal void AddRange(
            AudioAuthoringValidationReport other)
        {
            if (other == null)
            {
                return;
            }

            for (int index = 0;
                 index < other._issues.Count;
                 index++)
            {
                _issues.Add(other._issues[index]);
            }
        }
    }

    internal readonly struct AudioAuthoringValidationIssue
    {
        internal AudioAuthoringValidationIssue(
            string message,
            Object context)
        {
            Message = message;
            Context = context;
        }

        internal string Message { get; }

        internal Object Context { get; }
    }

    internal static class AudioAuthoringValidationGui
    {
        internal static void DrawSummary(
            AudioAuthoringValidationReport report)
        {
            if (report == null)
            {
                EditorGUILayout.LabelField(
                    "Result",
                    "Not run");
                return;
            }

            EditorGUILayout.LabelField(
                "Result",
                report.IsValid
                    ? "Valid"
                    : $"Invalid — {report.ErrorCount} error(s)");
        }

        internal static void DrawIssues(
            AudioAuthoringValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            for (int index = 0;
                 index < report.Issues.Count;
                 index++)
            {
                AudioAuthoringValidationIssue issue =
                    report.Issues[index];

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(
                        issue.Message,
                        MessageType.Error);

                    using (new EditorGUI.DisabledScope(
                               issue.Context == null))
                    {
                        if (GUILayout.Button(
                                "Select",
                                GUILayout.Width(58f),
                                GUILayout.Height(38f)))
                        {
                            Selection.activeObject =
                                issue.Context;

                            EditorGUIUtility.PingObject(
                                issue.Context);
                        }
                    }
                }
            }
        }
    }
}
