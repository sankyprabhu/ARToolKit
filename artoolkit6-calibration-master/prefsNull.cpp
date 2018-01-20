/*
 *  prefsNull.cpp
 *  ARToolKit6
 *
 *  This file is part of ARToolKit.
 *
 *  Copyright 2017-2017 Daqri LLC. All Rights Reserved.
 *
 *  Author(s): Philip Lamb
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */


#include <stdio.h>
#include <AR6/AR/config.h>
#include "prefs.hpp"

#if !TARGET_PLATFORM_MACOS && !TARGET_PLATFORM_LINUX && !TARGET_PLATFORM_IOS

void *initPreferences(void)
{
    return (NULL);
}

void preferencesFinal(void **preferences_p)
{
}

void showPreferences(void *preferences)
{
}

char *getPreferenceCameraOpenToken(void *preferences)
{
    return NULL;
}

char *getPreferenceCameraResolutionToken(void *preferences)
{
    return NULL;
}

bool getPreferenceCalibrationSave(void *preferences)
{
    return false;
}

char *getPreferenceCalibrationServerUploadURL(void *preferences)
{
    return NULL;
}

char *getPreferenceCalibrationServerAuthenticationToken(void *preferences)
{
    return NULL;
}

Calibration::CalibrationPatternType getPreferencesCalibrationPatternType(void *preferences)
{
    return CALIBRATION_PATTERN_TYPE_DEFAULT;
}

cv::Size getPreferencesCalibrationPatternSize(void *preferences)
{
    return Calibration::CalibrationPatternSizes[CALIBRATION_PATTERN_TYPE_DEFAULT];
}

float getPreferencesCalibrationPatternSpacing(void *preferences)
{
    return Calibration::CalibrationPatternSpacings[CALIBRATION_PATTERN_TYPE_DEFAULT];
}
#endif

#if !TARGET_PLATFORM_MACOS && !TARGET_PLATFORM_LINUX
char *getPreferenceCalibSaveDir(void *preferences)
{
    return arUtilGetResourcesDirectoryPath(AR_UTIL_RESOURCES_DIRECTORY_BEHAVIOR_USE_USER_ROOT);
}
#endif
