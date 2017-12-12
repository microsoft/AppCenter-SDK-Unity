// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

using Microsoft.AppCenter.Unity.Crashes;
using Microsoft.AppCenter.Unity.Distribute;
using Microsoft.AppCenter.Unity.Push;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DemoOther : MonoBehaviour
{
    public Toggle DistributeEnabled;
    public Toggle PushEnabled;
    public Toggle CrashesEnabled;

    void OnEnable()
    {
        Push.IsEnabledAsync().ContinueWith(task =>
        {
            PushEnabled.isOn = task.Result;
        });
        Distribute.IsEnabledAsync().ContinueWith(task =>
        {
            DistributeEnabled.isOn = task.Result;
        });
        Crashes.IsEnabledAsync().ContinueWith(task =>
        {
            CrashesEnabled.isOn = task.Result;
        });

        Distribute.ReleaseAvailable = (releaseDetails) =>
        {
            return true;
        };
    }

    public void SetPushEnabled(bool enabled)
    {
        StartCoroutine(SetPushEnabledCoroutine(enabled));
    }

    private IEnumerator SetPushEnabledCoroutine(bool enabled)
    {
        yield return Push.SetEnabledAsync(enabled);
        var isEnabled = Push.IsEnabledAsync();
        yield return isEnabled;
        PushEnabled.isOn = isEnabled.Result;
    }

    public void SetDistributeEnabled(bool enabled)
    {
        StartCoroutine(SetDistributeEnabledCoroutine(enabled));
    }

    private IEnumerator SetDistributeEnabledCoroutine(bool enabled)
    {
        yield return Distribute.SetEnabledAsync(enabled);
        var isEnabled = Distribute.IsEnabledAsync();
        yield return isEnabled;
        DistributeEnabled.isOn = isEnabled.Result;
    }

    public void SetCrashesEnabled(bool enabled)
    {
        StartCoroutine(SetCrashesEnabledCoroutine(enabled));
    }

    private IEnumerator SetCrashesEnabledCoroutine(bool enabled)
    {
        yield return Crashes.SetEnabledAsync(enabled);
        var isEnabled = Crashes.IsEnabledAsync();
        yield return isEnabled;
        CrashesEnabled.isOn = isEnabled.Result;
    }
}
