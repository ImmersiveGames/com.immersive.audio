using System.Collections.Generic;
using Immersive.Audio.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Audio.Editor
{
    [CustomEditor(typeof(AudioDefaultsAsset))]
    [CanEditMultipleObjects]
    internal sealed class AudioDefaultsAssetEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _masterVolume;
        private SerializedProperty _sfxVolume;
        private SerializedProperty _bgmVolume;
        private SerializedProperty _masterBus;
        private SerializedProperty _sfxBus;
        private SerializedProperty _bgmBus;
        private SerializedProperty _defaultFadeInSeconds;
        private SerializedProperty _defaultFadeOutSeconds;
        private AudioAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _masterVolume =
                serializedObject.FindProperty("masterVolume");

            _sfxVolume =
                serializedObject.FindProperty("sfxVolume");

            _bgmVolume =
                serializedObject.FindProperty("bgmVolume");

            _masterBus =
                serializedObject.FindProperty("masterBus");

            _sfxBus =
                serializedObject.FindProperty("sfxBus");

            _bgmBus =
                serializedObject.FindProperty("bgmBus");

            _defaultFadeInSeconds =
                serializedObject.FindProperty(
                    "defaultFadeInSeconds");

            _defaultFadeOutSeconds =
                serializedObject.FindProperty(
                    "defaultFadeOutSeconds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            AudioAuthoringInspectorGui.ProductHeader(
                "Audio Defaults",
                "Defines shared Audio volume, routing and BGM transition defaults.");

            AudioAuthoringInspectorGui.IntentSummary(
                BuildIntentSummary());

            EditorGUI.BeginChangeCheck();
            DrawPrimaryAuthoring();
            bool changed =
                EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (changed)
            {
                _validationReport = null;
            }

            DrawConfigurationStatus();
            DrawValidation();

            _showAdvanced =
                AudioAuthoringInspectorGui.AdvancedFoldout(
                    _showAdvanced);

            if (_showAdvanced)
            {
                DrawAdvanced();
            }
        }

        private void DrawPrimaryAuthoring()
        {
            AudioAuthoringInspectorGui.Section(
                "Volume");

            EditorGUILayout.PropertyField(
                _masterVolume,
                new GUIContent("Master Volume"));

            EditorGUILayout.PropertyField(
                _sfxVolume,
                new GUIContent("SFX Volume"));

            EditorGUILayout.PropertyField(
                _bgmVolume,
                new GUIContent("BGM Volume"));

            AudioAuthoringInspectorGui.Section(
                "Routing");

            EditorGUILayout.PropertyField(
                _masterBus,
                new GUIContent(
                    "Master Bus",
                    "Logical Master routing key."));

            EditorGUILayout.PropertyField(
                _sfxBus,
                new GUIContent(
                    "SFX Bus",
                    "Logical fallback SFX routing key."));

            EditorGUILayout.PropertyField(
                _bgmBus,
                new GUIContent(
                    "BGM Bus",
                    "Logical fallback BGM routing key."));

            AudioAuthoringInspectorGui.Section(
                "BGM Transition");

            EditorGUILayout.PropertyField(
                _defaultFadeInSeconds,
                new GUIContent("Default Fade In Seconds"));

            EditorGUILayout.PropertyField(
                _defaultFadeOutSeconds,
                new GUIContent("Default Fade Out Seconds"));
        }

        private string BuildIntentSummary()
        {
            if (_masterBus == null ||
                _sfxBus == null ||
                _bgmBus == null)
            {
                return "Configure shared Audio defaults.";
            }

            if (_masterBus.hasMultipleDifferentValues ||
                _sfxBus.hasMultipleDifferentValues ||
                _bgmBus.hasMultipleDifferentValues)
            {
                return "Selected assets contain mixed defaults.";
            }

            return
                $"Master={Format(_masterBus.stringValue)}, SFX={Format(_sfxBus.stringValue)}, BGM={Format(_bgmBus.stringValue)}.";
        }

        private void DrawConfigurationStatus()
        {
            AudioAuthoringInspectorGui.Section(
                "Configuration");

            if (_masterBus.hasMultipleDifferentValues ||
                _sfxBus.hasMultipleDifferentValues ||
                _bgmBus.hasMultipleDifferentValues)
            {
                AudioAuthoringInspectorGui.Status(
                    "Mixed selection");
                return;
            }

            bool hasError = false;

            if (string.IsNullOrWhiteSpace(
                    _masterBus.stringValue))
            {
                hasError = true;
                EditorGUILayout.HelpBox(
                    "Master Bus is required.",
                    MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(
                    _sfxBus.stringValue))
            {
                hasError = true;
                EditorGUILayout.HelpBox(
                    "SFX Bus is required.",
                    MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(
                    _bgmBus.stringValue))
            {
                hasError = true;
                EditorGUILayout.HelpBox(
                    "BGM Bus is required.",
                    MessageType.Error);
            }

            if (!hasError)
            {
                AudioAuthoringInspectorGui.Status("Ready");
            }
        }

        private void DrawValidation()
        {
            AudioAuthoringInspectorGui.Section(
                "Validation");

            AudioAuthoringValidationGui.DrawSummary(
                _validationReport);

            if (GUILayout.Button("Validate Configuration"))
            {
                _validationReport =
                    new AudioAuthoringValidationReport();

                for (int index = 0;
                     index < targets.Length;
                     index++)
                {
                    _validationReport.AddRange(
                        ValidateDefaults(
                            targets[index]
                                as AudioDefaultsAsset));
                }
            }

            AudioAuthoringValidationGui.DrawIssues(
                _validationReport);
        }

        private static AudioAuthoringValidationReport
            ValidateDefaults(AudioDefaultsAsset defaults)
        {
            var report =
                new AudioAuthoringValidationReport();

            if (defaults == null)
            {
                report.AddError(
                    "Audio Defaults asset is missing.",
                    null);
                return report;
            }

            var issues =
                new List<string>();

            defaults.ValidateAuthoring(issues);

            for (int index = 0;
                 index < issues.Count;
                 index++)
            {
                report.AddError(
                    issues[index],
                    defaults);
            }

            return report;
        }

        private void DrawAdvanced()
        {
            if (targets.Length != 1 ||
                !(target is AudioDefaultsAsset defaults))
            {
                EditorGUILayout.LabelField(
                    "Evidence",
                    "Single selection only");
                return;
            }

            EditorGUI.indentLevel++;

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Master Bus Value",
                    defaults.MasterBusValue);

                EditorGUILayout.TextField(
                    "SFX Bus Value",
                    defaults.SfxBusValue);

                EditorGUILayout.TextField(
                    "BGM Bus Value",
                    defaults.BgmBusValue);

                EditorGUILayout.TextField(
                    "Asset Path",
                    AssetDatabase.GetAssetPath(defaults));
            }

            EditorGUI.indentLevel--;
        }

        private static string Format(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<missing>"
                : value.Trim();
        }
    }
}
