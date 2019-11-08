﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Microsoft.AppCenter.Unity.Push.Internal
{
    class PushInternal
    {
        private static AndroidJavaClass _push = new AndroidJavaClass("com.microsoft.appcenter.push.Push");
        private static AndroidJavaClass _unityListener = new AndroidJavaClass("com.microsoft.appcenter.pushdelegate.UnityAppCenterPushDelegate");
        private static bool IsAppCenterStart = false;
        private static bool IsWaitingToReply = false;

        public static void PrepareEventHandlers()
        {
            AppCenterBehavior.InitializingServices += Initialize;
            AppCenterBehavior.InitializedAppCenterAndServices += PostInitialize;
        }

        private static void Initialize()
        {
            _unityListener.CallStatic("setListener", new PushDelegate());
            IsAppCenterStart = true;
            
            // If `ReplayUnprocessedPushNotifications` was called before App Center start 
            // than need to call it again after App Center was started.
            if (IsWaitingToReply)
            {
                PushInternal.ReplayUnprocessedPushNotifications();
                IsWaitingToReply = false;
            }           
        }

        public static void StartPush()
        {
        }

        private static void PostInitialize()
        {
            var instance = _push.CallStatic<AndroidJavaObject>("getInstance");
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            instance.Call("onActivityResumed", activity);
        }

        public static AppCenterTask SetEnabledAsync(bool isEnabled)
        {
            var future = _push.CallStatic<AndroidJavaObject>("setEnabled", isEnabled);
            return new AppCenterTask(future);
        }

        public static AppCenterTask<bool> IsEnabledAsync()
        {
            var future = _push.CallStatic<AndroidJavaObject>("isEnabled");
            return new AppCenterTask<bool>(future);
        }

        public static void AddNativeType(List<IntPtr> nativeTypes)
        {
            nativeTypes.Add(AndroidJNI.FindClass("com/microsoft/appcenter/push/Push"));
        }

        public static void EnableFirebaseAnalytics()
        {
            _push.CallStatic("enableFirebaseAnalytics");
        }

        internal static void ReplayUnprocessedPushNotifications()
        {
            // Verify that the App Center was started, otherwise set a flag 
            // that needs call `ReplayUnprocessedPushNotifications` after the App Center will be started.
            if(!IsAppCenterStart)
            {
                IsWaitingToReply = true;
                return;
            }
            _unityListener.CallStatic("replayPushNotifications");
        }
    }
}
#endif
