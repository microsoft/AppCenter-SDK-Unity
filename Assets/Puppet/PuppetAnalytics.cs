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
    public Toggle TransmissionEnabled;
    public InputField EventName;
    public GameObject EventProperty;
    public RectTransform EventPropertiesList;
    private string TransmissionTargetToken = "2932e5d1bff54f62935e7352e98a7819-59ba431f-6bf7-44f5-a1d3-a24a6f6dd544-7075";

    void OnEnable()
    {
        Analytics.IsEnabledAsync().ContinueWith(task =>
        {
            Enabled.isOn = task.Result;
        });

        WrapperTransmissionTarget transmissionTarget = Analytics.GetTransmissionTarget(TransmissionTargetToken);
        transmissionTarget.IsEnabledAsync().ContinueWith(task => 
        {
            TransmissionEnabled.isOn = task.Result;
        });
    }

    public void SetEnabled(bool enabled)
    {
        StartCoroutine(SetEnabledCoroutine(enabled));
    }

    public void SetTransmissionEnabled(bool enabled)
    {
        StartCoroutine(SetTransmissionEnabledCoroutine(enabled));
    }

    private IEnumerator SetEnabledCoroutine(bool enabled)
    {
        yield return Analytics.SetEnabledAsync(enabled);
        var isEnabled = Analytics.IsEnabledAsync();
        yield return isEnabled;
        Enabled.isOn = isEnabled.Result;
    }

    private IEnumerator SetTransmissionEnabledCoroutine(bool enabled)
    {
        WrapperTransmissionTarget transmissionTarget = Analytics.GetTransmissionTarget(TransmissionTargetToken);
        yield return transmissionTarget.SetEnabledAsync(enabled);
        var isEnabled = transmissionTarget.IsEnabledAsync();
        yield return isEnabled;
        TransmissionEnabled.isOn = isEnabled.Result;
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

    public void TrackEventTransmission() 
    { 
        WrapperTransmissionTarget transmissionTarget = Analytics.GetTransmissionTarget(TransmissionTargetToken);
        Dictionary<string, string> properties = GetProperties();
        if (properties == null) 
        {
            transmissionTarget.TrackEvent(EventName.text);
        }
        else
        {
            transmissionTarget.TrackEventWithProperties(EventName.text, GetProperties());
        }
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
