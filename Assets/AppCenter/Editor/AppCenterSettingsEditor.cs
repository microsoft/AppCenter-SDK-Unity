// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AppCenterSettings))]
public class AppCenterSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw app secrets.
        Header("App Secrets");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("iOSAppSecret"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("AndroidAppSecret"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("UWPAppSecret"));

        // Draw modules.
        if (AppCenterSettings.Analytics != null)
        {
            Header("Analytics");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseAnalytics"));
        }
        if (AppCenterSettings.Crashes != null)
        {
            Header("Crashes");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("UseCrashes"));
        }
        if (AppCenterSettings.Distribute != null)
        {
            Header("Distribute");
            var serializedProperty = serializedObject.FindProperty("UseDistribute");
            EditorGUILayout.PropertyField(serializedProperty);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CustomApiUrl"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("CustomInstallUrl"));
        }
        if (AppCenterSettings.Push != null)
        {
            Header("Push");
            var serializedProperty = serializedObject.FindProperty("UsePush");
            EditorGUILayout.PropertyField(serializedProperty);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SenderId"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("EnableFirebaseAnalytics"));
#if !UNITY_2017_1_OR_NEWER
            if (serializedProperty.boolValue)
            {
                EditorGUILayout.HelpBox ("In Unity versions prior to 2017.1 you need to add required capabilities in Xcode manually.", MessageType.Info);
            }
#endif
        }

        // Draw other.
        Header("Other Setup");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("InitialLogLevel"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("CustomLogUrl"));
        serializedObject.ApplyModifiedProperties();
    }

    private static void Header(string label)
    {
        GUILayout.Label(label, EditorStyles.boldLabel);
        GUILayout.Space(-4);
    }
}
