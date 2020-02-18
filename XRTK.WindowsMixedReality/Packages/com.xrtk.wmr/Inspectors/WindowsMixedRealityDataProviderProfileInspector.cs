// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;
using XRTK.Inspectors.Profiles;
using XRTK.Inspectors.Utilities;
using XRTK.WindowsMixedReality.Profiles;

namespace XRTK.WindowsMixedReality.Inspectors
{
    [CustomEditor(typeof(WindowsMixedRealityControllerDataProviderProfile))]
    public class WindowsMixedRealityDataProviderProfileInspector : BaseMixedRealityProfileInspector
    {
        private SerializedProperty windowsManipulationGestureSettings;
        private SerializedProperty useRailsNavigation;
        private SerializedProperty windowsNavigationGestureSettings;
        private SerializedProperty windowsRailsNavigationGestures;
        private SerializedProperty windowsGestureAutoStart;

        protected override void OnEnable()
        {
            base.OnEnable();

            windowsManipulationGestureSettings = serializedObject.FindProperty("manipulationGestures");
            useRailsNavigation = serializedObject.FindProperty("useRailsNavigation");
            windowsNavigationGestureSettings = serializedObject.FindProperty("navigationGestures");
            windowsRailsNavigationGestures = serializedObject.FindProperty("railsNavigationGestures");
            windowsGestureAutoStart = serializedObject.FindProperty("windowsGestureAutoStart");
        }

        public override void OnInspectorGUI()
        {
            MixedRealityInspectorUtility.RenderMixedRealityToolkitLogo();

            if (ThisProfile.ParentProfile != null &&
                GUILayout.Button("Back to Controller Data Providers"))
            {
                Selection.activeObject = ThisProfile.ParentProfile;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Windows Mixed Reality Controller Data Provider Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This profile aids in configuring additional platform settings for the registered controller data provider. This can be anything from additional gestures or platform specific settings.", MessageType.Info);

            ThisProfile.CheckProfileLock();

            serializedObject.Update();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Windows Gesture Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(windowsGestureAutoStart);
            EditorGUILayout.PropertyField(windowsManipulationGestureSettings);
            EditorGUILayout.PropertyField(windowsNavigationGestureSettings);
            EditorGUILayout.PropertyField(useRailsNavigation);
            EditorGUILayout.PropertyField(windowsRailsNavigationGestures);
            serializedObject.ApplyModifiedProperties();
        }
    }
}