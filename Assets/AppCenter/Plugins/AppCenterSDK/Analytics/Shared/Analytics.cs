﻿// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AppCenter.Unity.Analytics.Internal;

namespace Microsoft.AppCenter.Unity.Analytics
{
#if UNITY_IOS || UNITY_ANDROID
    using RawType = System.IntPtr;
#else
    using RawType = System.Type;
#endif

    public class Analytics
    {
        public static void PrepareEventHandlers()
        {
            AnalyticsInternal.PrepareEventHandlers();
        }

        public static RawType GetNativeType()
        {
            return AnalyticsInternal.GetNativeType();
        }

        public static void TrackEvent(string eventName, IDictionary<string, string> properties = null)
        {
            if (properties == null)
            {
                AnalyticsInternal.TrackEvent(eventName);
            }
            else
            {
                AnalyticsInternal.TrackEventWithProperties(eventName, properties);
            }
        }

        public static AppCenterTask<bool> IsEnabledAsync()
        {
            return AnalyticsInternal.IsEnabledAsync();
        }

        public static AppCenterTask SetEnabledAsync(bool enabled)
        {
            return AnalyticsInternal.SetEnabledAsync(enabled);
        }

        public static TransmissionTarget GetTransmissionTarget(string transmissionTargetToken)
        {
            var internalObject = AnalyticsInternal.GetTransmissionTarget(transmissionTargetToken);
            if (internalObject == null)
            {
                return null;
            }
            return new TransmissionTarget(internalObject);
        }

        public static void Pause()
        {
            AnalyticsInternal.Pause();
        }

        public static void Resume()
        {
            AnalyticsInternal.Resume();
        }
    }
}
