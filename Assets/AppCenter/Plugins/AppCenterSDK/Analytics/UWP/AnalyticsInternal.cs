﻿// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

#if UNITY_WSA_10_0 && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Microsoft.AppCenter.Unity.Analytics.Internal
{
    using UWPAnalytics = Microsoft.AppCenter.Analytics.Analytics;

    class AnalyticsInternal
    {
        private static bool _warningLogged = false;

        public static void PrepareEventHandlers()
        {
        }

        public static Type GetNativeType()
        {
            return typeof(UWPAnalytics);
        }

        public static void TrackEvent(string eventName)
        {
            UWPAnalytics.TrackEvent(eventName);
        }

        public static void TrackEventWithProperties(string eventName, IDictionary<string, string> properties)
        {
            UWPAnalytics.TrackEvent(eventName, properties);
        }

        public static void TrackEventWithProperties(string eventName, EventProperties properties)
        {
            if (!_warningLogged)
            {
                Debug.LogWarning("Warning: Strongly typed properties are not supported on UWP platform. " +
                    "All property values will be converted to strings for this and all the future calls.");
                _warningLogged = true;
            }
            UWPAnalytics.TrackEvent(eventName, properties.GetRawObject());
        }

        public static AppCenterTask SetEnabledAsync(bool isEnabled)
        {
            return new AppCenterTask(UWPAnalytics.SetEnabledAsync(isEnabled));
        }

        public static AppCenterTask<bool> IsEnabledAsync()
        {
            return new AppCenterTask<bool>(UWPAnalytics.IsEnabledAsync());
        }

        public static Type GetTransmissionTarget(string transmissionTargetToken)
        {
            return null;
        }

        public static void Pause()
        {
        }

        public static void Resume()
        {
        }
    }
}
#endif