// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

#if UNITY_WSA_10_0 && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace Microsoft.AppCenter.Unity.Crashes.Internal
{
    class WrapperExceptionInternal
    {

        public static object Create()
        {
        }

        public static void SetType(object exception, string type)
        {
        }

        public static void SetMessage(object exception, string message)
        {
        }

        public static void SetStacktrace(object exception, string stacktrace)
        {
        }

        public static void SetInnerExceptions(object exception, object innerExceptions)
        {
        }

        public static void SetWrapperSdkName(object exception, string sdkName)
        {
        }
    }
}
#endif