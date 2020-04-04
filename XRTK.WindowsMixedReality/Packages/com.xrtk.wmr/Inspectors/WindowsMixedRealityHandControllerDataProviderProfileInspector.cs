// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using XRTK.Inspectors.Profiles;
using XRTK.WindowsMixedReality.Profiles;

namespace XRTK.WindowsMixedReality.Inspectors
{
    [CustomEditor(typeof(WindowsMixedRealityHandControllerDataProviderProfile))]
    public class WindowsMixedRealityHandControllerDataProviderProfileInspector : BaseMixedRealityProfileInspector
    {
        // Global hand settings overrides
        private SerializedProperty handMeshingEnabled;
        private SerializedProperty handPhysicsEnabled;
        private SerializedProperty useTriggers;
        private SerializedProperty boundsMode;

        protected override void OnEnable()
        {
            base.OnEnable();

            handMeshingEnabled = serializedObject.FindProperty(nameof(handMeshingEnabled));
            handPhysicsEnabled = serializedObject.FindProperty(nameof(handPhysicsEnabled));
            useTriggers = serializedObject.FindProperty(nameof(useTriggers));
            boundsMode = serializedObject.FindProperty(nameof(boundsMode));
        }

        public override void OnInspectorGUI()
        {
            RenderHeader();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Windows Mixed Reality Hand Controller Data Provider Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This profile aids in configuring additional platform settings for the registered controller data provider.", MessageType.Info);

            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(handMeshingEnabled);
            EditorGUILayout.PropertyField(handPhysicsEnabled);
            EditorGUILayout.PropertyField(useTriggers);
            EditorGUILayout.PropertyField(boundsMode);
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}