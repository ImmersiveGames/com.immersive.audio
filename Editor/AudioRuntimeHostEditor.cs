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
            _defaults =
                serializedObject.FindProperty("defaults");

            _playbackRoot =
                serializedObject.FindProperty("playbackRoot");

            _poolRuntimeHost =
                serializedObject.FindProperty("poolRuntimeHost");

            _composeOnAwake =
                serializedObject.FindProperty("composeOnAwake");

            _ensurePersistentListener =
                serializedObject.FindProperty(
                    "ensurePersistentListener");

            _listenerDuplicatePolicy =
                serializedObject.FindProperty(
                    "listenerDuplicatePolicy");

            _includeInactiveListenersForListenerReport =
                serializedObject.FindProperty(
                    "includeInactiveListenersForListenerReport");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            AudioAuthoringInspectorGui.ProductHeader(
                "Audio Runtime Host",
                "Owns physical Audio runtime composition for its authored lifetime.");

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
                "Runtime Composition");

            EditorGUILayout.PropertyField(
                _defaults,
                new GUIContent(
                    "Audio Defaults",
                    "Required shared Audio settings."));

            EditorGUILayout.PropertyField(
                _playbackRoot,
                new GUIContent(
                    "Playback Root",
                    "Optional explicit playback root. None lets runtime reuse/create AudioPlayback."));

            EditorGUILayout.PropertyField(
                _poolRuntimeHost,
                new GUIContent(
                    "Pool Runtime Host",
                    "Optional. Required only for pooled SFX requests."));

            EditorGUILayout.PropertyField(
                _composeOnAwake,
                new GUIContent(
                    "Compose On Awake",
                    "Compose Audio services when this host awakes."));

            AudioAuthoringInspectorGui.Section(
                "Listener");

            EditorGUILayout.PropertyField(
                _ensurePersistentListener,
                new GUIContent(
                    "Ensure Persistent Listener",
                    "Ensure the package-owned persistent AudioListener authority."));

            if (_ensurePersistentListener != null &&
                !_ensurePersistentListener.hasMultipleDifferentValues &&
                _ensurePersistentListener.boolValue)
            {
                EditorGUILayout.PropertyField(
                    _listenerDuplicatePolicy,
                    new GUIContent("Duplicate Policy"));

                EditorGUILayout.PropertyField(
                    _includeInactiveListenersForListenerReport,
                    new GUIContent("Include Inactive Listeners"));
            }
        }

        private string BuildIntentSummary()
        {
            if (_defaults == null ||
                _composeOnAwake == null ||
                _ensurePersistentListener == null)
            {
                return "Configure Audio runtime composition.";
            }

            if (_defaults.hasMultipleDifferentValues ||
                _composeOnAwake.hasMultipleDifferentValues ||
                _ensurePersistentListener.hasMultipleDifferentValues)
            {
                return "Selected hosts contain mixed composition intent.";
            }

            string defaults =
                _defaults.objectReferenceValue != null
                    ? _defaults.objectReferenceValue.name
                    : "<missing Defaults>";

            string compose =
                _composeOnAwake.boolValue
                    ? "Compose on Awake"
                    : "Lazy compose";

            string listener =
                _ensurePersistentListener.boolValue
                    ? "Persistent Listener"
                    : "No listener ownership";

            return $"{defaults}; {compose}; {listener}.";
        }

        private void DrawConfigurationStatus()
        {
            AudioAuthoringInspectorGui.Section(
                "Configuration");

            if (_defaults.hasMultipleDifferentValues)
            {
                AudioAuthoringInspectorGui.Status(
                    "Mixed selection");
                return;
            }

            if (_defaults.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "Audio Defaults is required.",
                    MessageType.Error);
                return;
            }

            AudioAuthoringInspectorGui.Status("Ready");
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
                        ValidateHost(
                            targets[index]
                                as AudioRuntimeHost));
                }
            }

            AudioAuthoringValidationGui.DrawIssues(
                _validationReport);
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

            var issues =
                new List<string>();

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

        private void DrawAdvanced()
        {
            if (targets.Length != 1 ||
                !(target is AudioRuntimeHost host))
            {
                EditorGUILayout.LabelField(
                    "Runtime Evidence",
                    "Single selection only");
                return;
            }

            EditorGUILayout.LabelField(
                "Runtime Evidence",
                EditorStyles.miniBoldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField(
                    "State",
                    "Available in Play Mode");
                return;
            }

            EditorGUI.indentLevel++;

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
                        : "Unavailable / Not Required");
            }

            EditorGUI.indentLevel--;
        }
    }
}
