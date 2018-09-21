// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

#import <Foundation/Foundation.h>

@class MSAnalyticsTransmissionTarget;

extern "C" MSAnalyticsTransmissionTarget* appcenter_unity_transmission_target_create();
extern "C" void appcenter_unity_transmission_target_track_event(MSAnalyticsTransmissionTarget *transmission, char* eventName);
extern "C" void appcenter_unity_transmission_target_set_enabled(MSAnalyticsTransmissionTarget *transmission, BOOL enabled);
extern "C" BOOL appcenter_unity_transmission_target_is_enabled(MSAnalyticsTransmissionTarget *transmission);
extern "C" void appcenter_unity_transmission_target_track_event_with_props(MSAnalyticsTransmissionTarget *transmission, char* eventName, char** keys, char** values, int count);
