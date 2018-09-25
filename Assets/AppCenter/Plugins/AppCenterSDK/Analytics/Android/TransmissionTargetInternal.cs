// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

#if UNITY_ANDROID && !UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using Microsoft.AppCenter.Unity.Analytics.Internal;
using Microsoft.AppCenter.Unity.Internal.Utility;

namespace Microsoft.AppCenter.Unity.Analytics
{
    public class TransmissionTargetInternal
    {
        public static void TrackEvent(AndroidJavaObject transmissionTarget, string eventName)
        {
            transmissionTarget.Call("trackEvent", eventName);
        }

        public static void TrackEventWithProperties(AndroidJavaObject transmissionTarget, string eventName, IDictionary<string, string> properties)
        {
            var propertiesMap = JavaStringMapHelper.ConvertToJava(properties);
            transmissionTarget.Call("trackEvent", eventName, properties);
        }

        public static AppCenterTask SetEnabledAsync(AndroidJavaObject transmissionTarget, bool enabled)
        {
            var future = transmissionTarget.Call<AndroidJavaObject>("setEnabledAsync", enabled);
            return new AppCenterTask(future);
        }

        public static AppCenterTask<bool> IsEnabledAsync(AndroidJavaObject transmissionTarget)
        {
            var future = transmissionTarget.Call<AndroidJavaObject>("isEnabledAsync");
            return new AppCenterTask<bool>(future);
        }

        public static AndroidJavaObject GetTransmissionTarget(AndroidJavaObject transmissionTargetParent, string transmissionTargetToken) 
        {
            return transmissionTargetParent.Call<AndroidJavaObject>("getTransmissionTarget", transmissionTargetToken);
        }

        public static AndroidJavaObject GetPropertyConfigurator(AndroidJavaObject transmissionTargetParent) 
        {
            return transmissionTargetParent.Call<AndroidJavaObject>("getPropertyConfigurator");
        }
    }
}
#endif