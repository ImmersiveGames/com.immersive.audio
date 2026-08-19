using System.Collections.Generic;
using Immersive.Audio.Authoring;
using Immersive.Audio.Unity.Hosts;
using UnityEditor;
using UnityEngine;

namespace Immersive.Audio.Editor
{
    [CustomEditor(typeof(AudioRuntimeHost))]
    [CanEditMultipleObjects]
    internal sealed class AudioRuntimeHostEditor :
        UnityEditor.Editor
    {
        private SerializedProperty _defaults;
        private SerializedProperty _playbackRoot;
        private SerializedProperty _poolRuntimeHost;
        private SerializedProperty _composeOnAwake;
        private SerializedProperty _ensurePersistentListener;
        private SerializedProperty _listenerDuplicatePolicy;
        private SerializedProperty _includeInactiveListenersForListenerReport;

        private AudioAuthoringValidationReport _validationReport;
        private bool _showAdvanced;

        private void OnEnable()
        {
            _defaults = serializedObject.FindProperty("defaults");
            _playbackRoot = serializedObject.FindProperty("playbackRoot");
            _poolRuntimeHost = serializedObject.FindProperty("poolRuntimeHost");
            _composeOnAwake = serializedObject.FindProperty("composeOnAwake");
            _ensurePersistentListener =
                serializedObject.FindProperty("ensurePersistentListener");
            _listenerDuplicatePolicy =
                serializedObject.FindProperty("listenerDuplicatePolicy");
            _includeInactiveListenersForListenerReport =
                serializedObject.FindProperty(
                    "includeInactiveListenersForListenerReport");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            AudioAuthoringInspectorGui.ProductHeader(
                "Audio Runtime Host",
                null);

            EditorGUI.BeginChangeCheck();
            DrawPrimaryAuthoring();
            bool authoringChanged = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (authoringChanged)
            {
                _validationReport = null;
            }

            DrawValidation();

            _showAdvanced =
                AudioAuthoringInspectorGui.AdvancedFoldout(
                    _showAdvanced);

            if (_showAdvanced)
            {
                serializedObject.UpdateIfRequiredOrScript();

                EditorGUI.BeginChangeCheck();
                DrawAdvancedAuthoring();
                bool advancedChanged = EditorGUI.EndChangeCheck();

                serializedObject.ApplyModifiedProperties();

                if (advancedChanged)
                {
                    _validationReport = null;
                }

                DrawRuntimeEvidence();
            }
        }

        private void DrawPrimaryAuthoring()
        {
            AudioAuthoringInspectorGui.Section("Audio");

            EditorGUILayout.PropertyField(
                _defaults,
                new GUIContent(
                    "Audio Defaults",
                    "Required shared Audio settings used by the physical Audio runtime host."));

            if (_defaults != null &&
                !_defaults.hasMultipleDifferentValues &&
                _defaults.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Audio Defaults is required.",
                    MessageType.Error);
            }

            AudioAuthoringInspectorGui.Section("Listener");

            EditorGUILayout.PropertyField(
                _ensurePersistentListener,
                new GUIContent(
                    "Manage Persistent Listener",
                    "Own the package-managed persistent AudioListener authority."));

            if (_ensurePersistentListener != null &&
                !_ensurePersistentListener.hasMultipleDifferentValues &&
                _ensurePersistentListener.boolValue)
            {
                EditorGUILayout.PropertyField(
                    _listenerDuplicatePolicy,
                    new GUIContent(
                        "Duplicate Policy",
                        "Defines how enabled duplicate AudioListeners are handled."));
            }

            AudioAuthoringInspectorGui.Section("Pooling");

            EditorGUILayout.PropertyField(
                _poolRuntimeHost,
                new GUIContent(
                    "Pool Runtime Host",
                    "Optional pooling provider used by pooled SFX requests. Leave empty when pooled SFX is not required."));
        }

        private void DrawValidation()
        {
            AudioAuthoringInspectorGui.Section("Validation");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        new GUIContent(
                            "Validate",
                            "Validate the authored Audio Runtime Host configuration without changing runtime composition."),
                        GUILayout.Width(90f)))
                {
                    RunValidation();
                }

                EditorGUILayout.LabelField(
                    ValidationStatus(),
                    EditorStyles.miniLabel);
            }

            if (_validationReport != null &&
                !_validationReport.IsValid &&
                _validationReport.Issues.Count > 0)
            {
                AudioAuthoringValidationIssue issue =
                    _validationReport.Issues[0];

                EditorGUILayout.HelpBox(
                    issue.Message,
                    MessageType.Error);
            }
        }

        private string ValidationStatus()
        {
            if (_validationReport == null)
            {
                return "Not Validated";
            }

            return _validationReport.IsValid
                ? "Valid"
                : "Issue";
        }

        private void RunValidation()
        {
            _validationReport =
                new AudioAuthoringValidationReport();

            for (int index = 0;
                 index < targets.Length;
                 index++)
            {
                _validationReport.AddRange(
                    ValidateHost(
                        targets[index] as AudioRuntimeHost));
            }
        }

        private static AudioAuthoringValidationReport
            ValidateHost(AudioRuntimeHost host)
        {
            var report =
                new AudioAuthoringValidationReport();

            if (host == null)
            {
                report.AddError(
                    "Audio Runtime Host is missing.",
                    null);
                return report;
            }

            if (host.Defaults == null)
            {
                report.AddError(
                    "Audio Defaults is required.",
                    host);
                return report;
            }

            var issues = new List<string>();
            host.Defaults.ValidateAuthoring(issues);

            for (int index = 0;
                 index < issues.Count;
                 index++)
            {
                report.AddError(
                    $"Audio Defaults '{host.Defaults.name}': {issues[index]}",
                    host.Defaults);
            }

            return report;
        }

        private void DrawAdvancedAuthoring()
        {
            EditorGUILayout.PropertyField(
                _playbackRoot,
                new GUIContent(
                    "Playback Root",
                    "Optional explicit playback root. None lets runtime reuse or create AudioPlayback."));

            EditorGUILayout.PropertyField(
                _composeOnAwake,
                new GUIContent(
                    "Compose On Awake",
                    "Compose Audio services during Awake. When disabled, composition occurs lazily on first playback request."));

            EditorGUILayout.PropertyField(
                _includeInactiveListenersForListenerReport,
                new GUIContent(
                    "Include Inactive Listeners",
                    "Include inactive AudioListeners when detecting and reporting duplicates."));
        }

        private void DrawRuntimeEvidence()
        {
            if (targets.Length != 1 ||
                !(target is AudioRuntimeHost host))
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                "Runtime",
                EditorStyles.miniBoldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField(
                    "Settings",
                    host.Settings.IsResolved
                        ? "Resolved"
                        : "Not Resolved");

                EditorGUILayout.TextField(
                    "SFX Service",
                    host.SfxService != null
                        ? "Available"
                        : "Not Composed");

                EditorGUILayout.TextField(
                    "BGM Service",
                    host.BgmService != null
                        ? "Available"
                        : "Not Composed");

                EditorGUILayout.TextField(
                    "Pool Service",
                    host.PoolService != null
                        ? "Available"
                        : "Unavailable");
            }
        }
    }
}
