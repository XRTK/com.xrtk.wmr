// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using XRTK.Inspectors.Profiles;
using XRTK.Inspectors.Utilities;
using XRTK.WindowsMixedReality.Profiles;

namespace XRTK.WindowsMixedReality.Inspectors
{
    [CustomEditor(typeof(WindowsMixedRealityControllerDataProviderProfile))]
    public class WindowsMixedRealityControllerDataProviderProfileInspector : BaseMixedRealityProfileInspector
    {
        private SerializedProperty manipulationGestures;
        private SerializedProperty useRailsNavigation;
        private SerializedProperty navigationGestures;
        private SerializedProperty railsNavigationGestures;
        private SerializedProperty windowsGestureAutoStart;

        // Global hand settings overrides
        private SerializedProperty handMeshingEnabled;
        private SerializedProperty handRayType;
        private SerializedProperty handPhysicsEnabled;
        private SerializedProperty useTriggers;
        private SerializedProperty boundsMode;

        protected override void OnEnable()
        {
            base.OnEnable();

            manipulationGestures = serializedObject.FindProperty(nameof(manipulationGestures));
            useRailsNavigation = serializedObject.FindProperty(nameof(useRailsNavigation));
            navigationGestures = serializedObject.FindProperty(nameof(navigationGestures));
            railsNavigationGestures = serializedObject.FindProperty(nameof(railsNavigationGestures));
            windowsGestureAutoStart = serializedObject.FindProperty(nameof(windowsGestureAutoStart));

            handMeshingEnabled = serializedObject.FindProperty(nameof(handMeshingEnabled));
            handRayType = serializedObject.FindProperty(nameof(handRayType));
            handPhysicsEnabled = serializedObject.FindProperty(nameof(handPhysicsEnabled));
            useTriggers = serializedObject.FindProperty(nameof(useTriggers));
            boundsMode = serializedObject.FindProperty(nameof(boundsMode));
        }

        public override void OnInspectorGUI()
        {
            RenderHeader();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Windows Mixed Reality Controller Data Provider Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This profile aids in configuring additional platform settings for the registered controller data provider. This can be anything from additional gestures or platform specific settings.", MessageType.Info);

            ThisProfile.CheckProfileLock();
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Windows Gesture Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(windowsGestureAutoStart);
            EditorGUILayout.PropertyField(manipulationGestures);
            EditorGUILayout.PropertyField(navigationGestures);
            EditorGUILayout.PropertyField(useRailsNavigation);
            EditorGUILayout.PropertyField(railsNavigationGestures);
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(handMeshingEnabled);
            EditorGUILayout.PropertyField(handRayType);
            EditorGUILayout.PropertyField(handPhysicsEnabled);
            EditorGUILayout.PropertyField(useTriggers);
            EditorGUILayout.PropertyField(boundsMode);
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}