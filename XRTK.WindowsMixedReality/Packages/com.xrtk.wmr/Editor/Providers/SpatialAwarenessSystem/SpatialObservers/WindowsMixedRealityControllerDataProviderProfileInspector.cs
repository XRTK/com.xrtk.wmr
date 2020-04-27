// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;
using XRTK.Editor.Extensions;
using XRTK.Editor.Profiles.InputSystem.Controllers;
using XRTK.WindowsMixedReality.Profiles;

namespace XRTK.WindowsMixedReality.Editor.Providers.SpatialAwarenessSystem.SpatialObservers
{
    [CustomEditor(typeof(WindowsMixedRealityControllerDataProviderProfile))]
    public class WindowsMixedRealityControllerDataProviderProfileInspector : BaseMixedRealityControllerDataProviderProfileInspector
    {
        private static readonly GUIContent GestureSettingsFoldoutHeader = new GUIContent("Windows Gesture Settings");

        private SerializedProperty manipulationGestures;
        private SerializedProperty useRailsNavigation;
        private SerializedProperty navigationGestures;
        private SerializedProperty railsNavigationGestures;
        private SerializedProperty windowsGestureAutoStart;

        protected override void OnEnable()
        {
            base.OnEnable();

            manipulationGestures = serializedObject.FindProperty(nameof(manipulationGestures));
            useRailsNavigation = serializedObject.FindProperty(nameof(useRailsNavigation));
            navigationGestures = serializedObject.FindProperty(nameof(navigationGestures));
            railsNavigationGestures = serializedObject.FindProperty(nameof(railsNavigationGestures));
            windowsGestureAutoStart = serializedObject.FindProperty(nameof(windowsGestureAutoStart));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space();

            if (windowsGestureAutoStart.FoldoutWithBoldLabelPropertyField(GestureSettingsFoldoutHeader))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(manipulationGestures);
                EditorGUILayout.PropertyField(navigationGestures);
                EditorGUILayout.PropertyField(useRailsNavigation);
                EditorGUILayout.PropertyField(railsNavigationGestures);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}