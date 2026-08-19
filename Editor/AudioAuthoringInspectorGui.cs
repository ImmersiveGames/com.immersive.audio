using UnityEditor;
using UnityEngine;

namespace Immersive.Audio.Editor
{
    internal static class AudioAuthoringInspectorGui
    {
        internal static void ProductHeader(
            string title,
            string responsibility)
        {
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);

            if (!string.IsNullOrWhiteSpace(responsibility))
            {
                EditorGUILayout.LabelField(
                    responsibility,
                    EditorStyles.wordWrappedMiniLabel);
            }
        }

        internal static void IntentSummary(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(
                "Intent",
                EditorStyles.miniBoldLabel);

            EditorGUILayout.LabelField(
                text,
                EditorStyles.wordWrappedMiniLabel);
        }

        internal static void Section(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(
                title,
                EditorStyles.boldLabel);
        }

        internal static void Status(string value)
        {
            EditorGUILayout.LabelField("Status", value);
        }

        internal static bool AdvancedFoldout(bool expanded)
        {
            EditorGUILayout.Space(7f);

            return EditorGUILayout.Foldout(
                expanded,
                "Advanced / Debug",
                true);
        }
    }
}
