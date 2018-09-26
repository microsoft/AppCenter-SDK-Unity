// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

using System.Collections;
using Microsoft.AppCenter.Unity;
using UnityEngine;
using UnityEngine.UI;
using Microsoft.AppCenter.Unity.Crashes;
using AOT;

public class PuppetAppCenter : MonoBehaviour
{
    public Toggle Enabled;
    public Dropdown LogLevel;
    public PuppetConfirmationDialog userConfirmationDialog;

    static PuppetAppCenter instance;

    public GameObject CustomProperty;
    public RectTransform PropertiesList;

    public void AddProperty()
    {
        var property = Instantiate(CustomProperty);
        property.transform.SetParent(PropertiesList, false);
    }

    public void Send()
    {
        AppCenter.SetCustomProperties(GetProperties());
    }

    private CustomProperties GetProperties()
    {
        var properties = PropertiesList.GetComponentsInChildren<PuppetCustomProperty>();
        if (properties == null || properties.Length == 0)
        {
            return null;
        }
        var result = new CustomProperties();
        foreach (var i in properties)
        {
            i.Set(result);
        }
        return result;
    }

    private void Awake()
    {
        Crashes.ShouldProcessErrorReport = ShouldProcessErrorReportHandler;
        Crashes.ShouldAwaitUserConfirmation = UserConfirmationHandler;
        instance = this;
    }

    [MonoPInvokeCallback(typeof(Crashes.UserConfirmationHandler))]
    public static bool UserConfirmationHandler()
    {
        instance.userConfirmationDialog.Show();
        return true;
    }

    [MonoPInvokeCallback(typeof(Crashes.ShouldProcessErrorReportHandler))]
    public static bool ShouldProcessErrorReportHandler(ErrorReport errorReport)
    {
        return true;
    }

    void OnEnable()
    {
        AppCenter.IsEnabledAsync().ContinueWith(task =>
        {
            Enabled.isOn = task.Result;
        });
        AppCenter.GetInstallIdAsync().ContinueWith(task =>
        {
            if (task.Result.HasValue)
            {
                print("Install ID = " + task.Result.ToString());
            }
        });
        LogLevel.value = AppCenter.LogLevel - Microsoft.AppCenter.Unity.LogLevel.Verbose;
    }

    public void SetEnabled(bool enabled)
    {
        StartCoroutine(SetEnabledCoroutine(enabled));
    }

    private IEnumerator SetEnabledCoroutine(bool enabled)
    {
        yield return AppCenter.SetEnabledAsync(enabled);
        var isEnabled = AppCenter.IsEnabledAsync();
        yield return isEnabled;
        Enabled.isOn = isEnabled.Result;
    }

    public void SetLogLevel(int logLevel)
    {
        AppCenter.LogLevel = Microsoft.AppCenter.Unity.LogLevel.Verbose + logLevel;
    }
}
