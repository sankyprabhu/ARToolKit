/*
 *  prefs.hpp
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

#ifndef prefs_hpp
#define prefs_hpp

#include "Calibration.hpp"


// Data upload.
#define CALIBRATION_PATTERN_TYPE_DEFAULT Calibration::CalibrationPatternType::CHESSBOARD

#ifdef __cplusplus
extern "C" {
#endif

void *initPreferences(void);
void showPreferences(void *preferences);
void preferencesFinal(void **preferences_p);

char *getPreferenceCameraOpenToken(void *preferences);
char *getPreferenceCameraResolutionToken(void *preferences);
bool getPreferenceCalibrationSave(void *preferences);
char *getPreferenceCalibrationServerUploadURL(void *preferences);
char *getPreferenceCalibrationServerAuthenticationToken(void *preferences);
Calibration::CalibrationPatternType getPreferencesCalibrationPatternType(void *preferences);
cv::Size getPreferencesCalibrationPatternSize(void *preferences);
float getPreferencesCalibrationPatternSpacing(void *preferences);
char *getPreferenceCalibSaveDir(void *preferences);

#ifdef __cplusplus
}
#endif
#endif /* prefs_hpp */
