// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#if UNITY_WSA_10_0 && !UNITY_EDITOR
using System;
using System.Collections.Generic;
using Windows.ApplicationModel.Activation;

namespace Microsoft.AppCenter.Unity.Push.Internal
{
    using UWPPush = Microsoft.AppCenter.Push.Push;
    using WSAApplication = UnityEngine.WSA.Application;

    class PushInternal
    {
        public static List<PushNotificationReceivedEventArgs> _unprocessedPushNotifications = new List<PushNotificationReceivedEventArgs>();
        public static readonly object _lockObject = new object();
        private static string _prevIdString = "";
        private static int _idLength = Guid.NewGuid().ToString().Length;
        private static bool IsAppCenterStart = false;
        private static bool IsWaitingToReply = false;

        public static void PrepareEventHandlers()
        {
            AppCenterBehavior.InitializingServices += Initialize;
        }

        public static void StartPush()
        {
        }

        private static void Initialize()
        {
            Microsoft.AppCenter.Utils.ApplicationLifecycleHelper.Instance.ApplicationResuming += (s, e) =>
            {
                WSAApplication.InvokeOnAppThread(new UnityEngine.WSA.AppCallbackItem(() =>
               {
                   var arguments = WSAApplication.arguments;
                   if (arguments.Contains("appcenterunity"))
                   {
                       var idPrefix = "\"appcenterunity\":\"";
                       var idStartIdx = arguments.IndexOf(idPrefix) + idPrefix.Length;
                       var idString = arguments.Substring(idStartIdx, _idLength);
                       if (idString != _prevIdString)
                       {
                           _prevIdString = idString;
                           UWPPush.Instance.InstanceCheckLaunchedFromNotification(WSAApplication.arguments);
                       }
                   }
               }), false);
            };
            UWPPush.PushNotificationReceived += (sender, e) =>
            {
                var eventArgs = new PushNotificationReceivedEventArgs
                {
                    Message = e.Message,
                    Title = e.Title,
                    CustomData = e.CustomData
                };
                HandlePushNotification(eventArgs);
            };

            // If `ReplayUnprocessedPushNotifications` was called before App Center start 
            // than need to call it again after App Center was started.
            IsAppCenterStart = true;
            if (IsWaitingToReply)
            {
                PushInternal.ReplayUnprocessedPushNotifications();
                IsWaitingToReply = false;
            }
        }

        public static void AddNativeType(List<Type> nativeTypes)
        {
            nativeTypes.Add(typeof(UWPPush));
        }

        public static AppCenterTask SetEnabledAsync(bool isEnabled)
        {
            return new AppCenterTask(UWPPush.SetEnabledAsync(isEnabled));
        }

        public static AppCenterTask<bool> IsEnabledAsync()
        {
            return new AppCenterTask<bool>(UWPPush.IsEnabledAsync());
        }

        public static void HandlePushNotification(PushNotificationReceivedEventArgs eventArgs)
        {
            lock (_lockObject)
            {
                if (_unprocessedPushNotifications != null)
                {
                    _unprocessedPushNotifications.Add(eventArgs);

                    // Don't want to invoke push callback inside lock.
                    eventArgs = null;
                }
            }

            // If eventArgs isn't null, it must not have been added to queue.
            if (eventArgs != null)
            {
                Push.NotifyPushNotificationReceived(eventArgs);
            }
        }

        public static void EnableFirebaseAnalytics()
        {
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
            List<PushNotificationReceivedEventArgs> unprocessedPushNotificationsCopy = null;
            lock (_lockObject)
            {
                if (_unprocessedPushNotifications != null)
                {
                    // Don't want to invoke push callback inside lock, so make
                    // a copy.
                    unprocessedPushNotificationsCopy = new List<PushNotificationReceivedEventArgs>(_unprocessedPushNotifications);
                    _unprocessedPushNotifications = null;
                }
            }
            if (unprocessedPushNotificationsCopy != null)
            {
                foreach (var notification in unprocessedPushNotificationsCopy)
                {
                    Push.NotifyPushNotificationReceived(notification);
                }
            }
        }
    }
}
#endif
