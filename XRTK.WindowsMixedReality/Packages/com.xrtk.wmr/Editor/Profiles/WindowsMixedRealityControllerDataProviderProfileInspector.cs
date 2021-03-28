// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEditor;
using UnityEngine;
using XRTK.Editor.Extensions;
using XRTK.Editor.Profiles.InputSystem.Controllers;
using XRTK.WindowsMixedReality.Profiles;

namespace XRTK.WindowsMixedReality.Editor.Profiles
{
    [CustomEditor(typeof(WindowsMixedRealityControllerDataProviderProfile))]
    public class WindowsMixedRealityControllerDataProviderProfileInspector : BaseMixedRealityHandControllerDataProviderProfileInspector
    {
        private static readonly GUIContent gestureSettingsFoldoutHeader = new GUIContent("Gesture Settings");
        private static readonly GUIContent windowsGestureAutoStartLabel = new GUIContent("Start Behaviour");

        private SerializedProperty windowsGestureAutoStart;
        private SerializedProperty manipulationGestures;
        private SerializedProperty navigationGestures;
        private SerializedProperty useRailsNavigation;
        private SerializedProperty railsNavigationGestures;

        protected override void OnEnable()
        {
            base.OnEnable();

            windowsGestureAutoStart = serializedObject.FindProperty(nameof(windowsGestureAutoStart));
            manipulationGestures = serializedObject.FindProperty(nameof(manipulationGestures));
            navigationGestures = serializedObject.FindProperty(nameof(navigationGestures));
            useRailsNavigation = serializedObject.FindProperty(nameof(useRailsNavigation));
            railsNavigationGestures = serializedObject.FindProperty(nameof(railsNavigationGestures));
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (windowsGestureAutoStart.FoldoutWithBoldLabelPropertyField(gestureSettingsFoldoutHeader, windowsGestureAutoStartLabel))
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