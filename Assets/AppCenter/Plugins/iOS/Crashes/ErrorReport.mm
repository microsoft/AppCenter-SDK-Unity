// Copyright (c) Microsoft Corporation. All rights reserved.
//
// Licensed under the MIT license.

#import "ErrorReport.h"
#import <AppCenterCrashes/AppCenterCrashes.h>

char* get_cstring(NSString* nsstring);

char* app_center_unity_crashes_error_report_incident_identifier(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return get_cstring([report incidentIdentifier]);
}

char* app_center_unity_crashes_error_report_reporter_key(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return get_cstring([report reporterKey]);
}

char* app_center_unity_crashes_error_report_signal(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return get_cstring([report signal]);
}

char* app_center_unity_crashes_error_report_exception_name(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return get_cstring([report exceptionName]);
}

char* app_center_unity_crashes_error_report_exception_reason(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return get_cstring([report exceptionReason]);
}

extern "C" char* app_center_unity_crashes_error_report_app_start_time(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return "";
}

extern "C" char* app_center_unity_crashes_error_report_app_error_time(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return "";
}

extern "C" void* app_center_unity_crashes_error_report_device(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return (__bridge void*)[report device];
}

extern "C" unsigned int app_center_unity_crashes_error_report_app_process_identifier(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return [report appProcessIdentifier];
}

extern "C" bool app_center_unity_crashes_error_report_is_app_kill(void* errorReport)
{
  MSErrorReport *report = (__bridge MSErrorReport*)errorReport;
  return [report isAppKill];
}

char* get_cstring(NSString* nsstring)
{
  // It seems that with (at least) IL2CPP, when returning a char* that is to be
  // converted to a System.String in C#, the char array is freed - which causes
  // a double-deallocation if ARC also tries to free it. To prevent this, we
  // must return a manually allocated copy of the string returned by "UTF8String"
  size_t cstringLength = [nsstring length] + 1; // +1 for '\0'
  const char *cstring = [nsstring UTF8String];
  char *cstringCopy = (char*)malloc(cstringLength);
  strncpy(cstringCopy, cstring, cstringLength);
  return cstringCopy;
}
