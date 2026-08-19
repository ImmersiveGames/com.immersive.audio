using System.Collections.Generic;
using Immersive.Audio.Authoring;
using UnityEditor;
using UnityEngine;

namespace Immersive.Audio.Editor
{
    [CustomEditor(typeof(AudioBgmCueAsset))]
    [CanEditMultipleObjects]
    internal sealed class AudioBgmCueAssetEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _cueId;
        private SerializedProperty _clip;
        private SerializedProperty _volume;
        private SerializedProperty _pitch;
        private SerializedProperty _loopMode;
        private SerializedProperty _routingBus;
        private SerializedProperty _fadeInSeconds;
        private SerializedProperty _fadeOutSeconds;
        private AudioAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _cueId =
                serializedObject.FindProperty("cueId");

            _clip =
                serializedObject.FindProperty("clip");

            _volume =
                serializedObject.FindProperty("volume");

            _pitch =
                serializedObject.FindProperty("pitch");

            _loopMode =
                serializedObject.FindProperty("loopMode");

            _routingBus =
                serializedObject.FindProperty("routingBus");

            _fadeInSeconds =
                serializedObject.FindProperty("fadeInSeconds");

            _fadeOutSeconds =
                serializedObject.FindProperty("fadeOutSeconds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            AudioAuthoringInspectorGui.ProductHeader(
                "BGM Cue",
                "Defines one reusable BGM cue.");

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
            AudioAuthoringInspectorGui.Section("Identity");

            EditorGUILayout.PropertyField(
                _cueId,
                new GUIContent(
                    "Cue Id",
                    "Explicit stable cue identity."));

            EditorGUILayout.PropertyField(
                _clip,
                new GUIContent(
                    "Audio Clip",
                    "AudioClip played by this cue."));

            AudioAuthoringInspectorGui.Section("Playback");

            EditorGUILayout.PropertyField(
                _volume,
                new GUIContent(
                    "Volume",
                    "Cue-level BGM volume."));

            EditorGUILayout.PropertyField(
                _pitch,
                new GUIContent(
                    "Pitch",
                    "Cue-level playback pitch."));

            EditorGUILayout.PropertyField(
                _loopMode,
                new GUIContent(
                    "Loop",
                    "Whether the physical BGM source loops this clip."));

            AudioAuthoringInspectorGui.Section("Transition");

            EditorGUILayout.PropertyField(
                _fadeInSeconds,
                new GUIContent(
                    "Fade In Seconds",
                    "Cue-specific fade-in duration."));

            EditorGUILayout.PropertyField(
                _fadeOutSeconds,
                new GUIContent(
                    "Fade Out Seconds",
                    "Cue-specific fade-out duration."));

            AudioAuthoringInspectorGui.Section("Routing");

            EditorGUILayout.PropertyField(
                _routingBus,
                new GUIContent(
                    "Routing Bus",
                    "Logical routing key."));
        }

        private string BuildIntentSummary()
        {
            if (_cueId == null ||
                _clip == null ||
                _loopMode == null)
            {
                return "Configure BGM cue.";
            }

            if (_cueId.hasMultipleDifferentValues ||
                _clip.hasMultipleDifferentValues ||
                _loopMode.hasMultipleDifferentValues)
            {
                return "Selected cues contain mixed values.";
            }

            string cueId =
                string.IsNullOrWhiteSpace(_cueId.stringValue)
                    ? "<missing Id>"
                    : _cueId.stringValue.Trim();

            string clip =
                _clip.objectReferenceValue != null
                    ? _clip.objectReferenceValue.name
                    : "<missing Clip>";

            string loop =
                _loopMode.enumDisplayNames[
                    _loopMode.enumValueIndex];

            return $"{cueId} → {clip}; loop={loop}.";
        }

        private void DrawConfigurationStatus()
        {
            AudioAuthoringInspectorGui.Section("Configuration");

            if (_cueId.hasMultipleDifferentValues ||
                _clip.hasMultipleDifferentValues ||
                _routingBus.hasMultipleDifferentValues)
            {
                AudioAuthoringInspectorGui.Status(
                    "Mixed selection");
                return;
            }

            bool hasError = false;

            if (string.IsNullOrWhiteSpace(
                    _cueId.stringValue))
            {
                hasError = true;

                EditorGUILayout.HelpBox(
                    "Cue Id is required.",
                    MessageType.Error);
            }

            if (_clip.objectReferenceValue == null)
            {
                hasError = true;

                EditorGUILayout.HelpBox(
                    "Audio Clip is required.",
                    MessageType.Error);
            }

            if (string.IsNullOrWhiteSpace(
                    _routingBus.stringValue))
            {
                hasError = true;

                EditorGUILayout.HelpBox(
                    "Routing Bus is required.",
                    MessageType.Error);
            }

            if (!hasError)
            {
                AudioAuthoringInspectorGui.Status("Ready");
            }
        }

        private void DrawValidation()
        {
            AudioAuthoringInspectorGui.Section("Validation");

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
                        ValidateCue(
                            targets[index]
                                as AudioBgmCueAsset));
                }
            }

            AudioAuthoringValidationGui.DrawIssues(
                _validationReport);
        }

        private static AudioAuthoringValidationReport
            ValidateCue(AudioBgmCueAsset cue)
        {
            var report =
                new AudioAuthoringValidationReport();

            if (cue == null)
            {
                report.AddError(
                    "BGM Cue asset is missing.",
                    null);
                return report;
            }

            var issues =
                new List<string>();

            cue.ValidateAuthoring(issues);

            for (int index = 0;
                 index < issues.Count;
                 index++)
            {
                report.AddError(
                    issues[index],
                    cue);
            }

            return report;
        }

        private void DrawAdvanced()
        {
            if (targets.Length != 1 ||
                !(target is AudioBgmCueAsset cue))
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
                    "Normalized Cue Id",
                    cue.CueIdValue);

                EditorGUILayout.TextField(
                    "Routing Bus Value",
                    cue.RoutingBusValue);

                EditorGUILayout.TextField(
                    "Asset Path",
                    AssetDatabase.GetAssetPath(cue));
            }

            EditorGUI.indentLevel--;
        }
    }
}
