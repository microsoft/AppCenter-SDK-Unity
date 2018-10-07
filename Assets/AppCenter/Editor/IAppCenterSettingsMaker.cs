﻿// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

public interface IAppCenterSettingsMaker
{
    bool IsAnalyticsAvailable();
    bool IsCrashesAvailable();
    bool IsDistributeAvailable();
    bool IsPushAvailable();
    void StartAnalyticsClass();
    void StartCrashesClass();
    void StartDistributeClass();
    void StartPushClass();
    void SetAppSecret(AppCenterSettings settings);
    void SetTransmissionTargetToken(string transmissionTargetToken);
    void SetLogLevel(int logLevel);
    bool IsStartFromAppCenterBehavior(AppCenterSettingsAdvanced advancedSettings);
    void SetStartupType(int startupType);
    void SetSenderId(string senderId);
    void SetLogUrl(string logUrl);
    void SetApiUrl(string apiUrl);
    void SetInstallUrl(string installUrl);
    void EnableFirebaseAnalytics();
    void CommitSettings();
}
