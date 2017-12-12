// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[CustomEditor(typeof(AppCenterSettings))]
public class AppCenterSettingsEditor : Editor
{
    public const string SettingsPath = "Assets/AppCenter/AppCenterSettings.asset";

    public static AppCenterSettings Settings
    {
        get
        {
            return AssetDatabase.LoadAssetAtPath<AppCenterSettings>(SettingsPath);
        }
    }

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

    [PostProcessScene]
    static void AddStartupCodeToAndroid()
    {
        var settings = Settings;
        if (settings == null)
        {
            return;
        }
        var settingsMaker = new AppCenterSettingsMakerAndroid();
        settingsMaker.SetAppSecret(settings.AndroidAppSecret);
        if (settings.CustomLogUrl.UseCustomUrl)
        {
            settingsMaker.SetLogUrl(settings.CustomLogUrl.Url);
        }
        if (settings.UsePush)
        {
            settingsMaker.StartPushClass();
            settingsMaker.SetSenderId(settings.SenderId);
            if (settings.EnableFirebaseAnalytics)
            {
                settingsMaker.EnableFirebaseAnalytics();
            }
        }
        if (settings.UseAnalytics)
        {
            settingsMaker.StartAnalyticsClass();
        }
        if (settings.UseDistribute)
        {
            if (settings.CustomApiUrl.UseCustomUrl)
            {
                settingsMaker.SetApiUrl(settings.CustomApiUrl.Url);
            }
            if (settings.CustomInstallUrl.UseCustomUrl)
            {
                settingsMaker.SetInstallUrl(settings.CustomInstallUrl.Url);
            }
            settingsMaker.StartDistributeClass();
        }
        settingsMaker.SetLogLevel((int)settings.InitialLogLevel);
        settingsMaker.CommitSettings();
    }

    private static void Header(string label)
    {
        GUILayout.Label(label, EditorStyles.boldLabel);
        GUILayout.Space(-4);
    }
}
