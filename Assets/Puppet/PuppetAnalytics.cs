// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AppCenter.Unity.Analytics;
using UnityEngine;
using UnityEngine.UI;

public class PuppetAnalytics : MonoBehaviour
{
    public Toggle Enabled;
    public Toggle Paused;
    public InputField EventName;
    public GameObject EventProperty;
    public RectTransform EventPropertiesList;
    public Text StatusText;

    void OnEnable()
    {
        Analytics.IsEnabledAsync().ContinueWith(task =>
        {
            Enabled.isOn = task.Result;
        });
    }

    public void SetEnabled(bool enabled)
    {
        StartCoroutine(SetEnabledCoroutine(enabled));
    }

    public void Pause()
    {
        Debug.Log("Pausing the app analytics...");
        Analytics.Pause();
        StatusText.text = "Analytics paused.";
    }

    public void Resume()
    {
        Debug.Log("Resuming the app analytics...");
        Analytics.Resume();
        StatusText.text = "Analytics resumed.";
    }

    private IEnumerator SetEnabledCoroutine(bool enabled)
    {
        yield return Analytics.SetEnabledAsync(enabled);
        var isEnabled = Analytics.IsEnabledAsync();
        yield return isEnabled;
        Enabled.isOn = isEnabled.Result;
    }

    public void AddProperty()
    {
        var property = Instantiate(EventProperty);
        property.transform.SetParent(EventPropertiesList, false);
    }

    public void TrackEvent()
    {
        Analytics.TrackEvent(EventName.text, GetProperties());
    }

    private Dictionary<string, string> GetProperties()
    {
        var properties = EventPropertiesList.GetComponentsInChildren<PuppetEventProperty>();
        if (properties == null || properties.Length == 0)
        {
            return null;
        }
        return properties.ToDictionary(i => i.Key.text, i => i.Value.text);
    }
}