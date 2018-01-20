/*
 *  SettingsViewController.h
 *  ARToolKit6
 *
 *  This file is part of ARToolKit.
 *
 *  Copyright 2015-2017 Daqri LLC. All Rights Reserved.
 *
 *  Author(s): Philip Lamb, Patrick Felong.
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


#import <UIKit/UIKit.h>

static NSString* const kSettingCameraResolutionStr = @"SettingCameraResolution";
static NSString* const kSettingCameraSourceStr = @"SettingCameraSource";
static NSString* const kSettingPaperSizeStr = @"SettingPaperSize";
static NSString *const kSettingCalibrationPatternType = @"calibrationPatternType";
static NSString *const kSettingCalibrationPatternSizeWidth = @"calibrationPatternSizeWidth";
static NSString *const kSettingCalibrationPatternSizeHeight = @"calibrationPatternSizeHeight";
static NSString *const kSettingCalibrationPatternSpacing = @"calibrationPatternSpacing";
static NSString *const kSettingCalibrationSave = @"calibrationSave";
static NSString *const kSettingCalibrationServerUploadCanonical = @"calibrationServerUploadCanonical";
static NSString *const kSettingCalibrationServerUploadUser = @"calibrationServerUploadUser";
static NSString *const kSettingCalibrationServerUploadURL = @"calibrationServerUploadURL";
static NSString *const kSettingCalibrationServerAuthenticationToken = @"calibrationServerAuthenticationToken";

static NSString* const kCameraSourceFront = @"Front";
static NSString* const kCameraSourceRear = @"Rear";

static NSString* const kPaperSizeA4Str = @"A4";
static NSString* const kPaperSizeUSLetterStr = @"US Letter";

static NSString *const kCalibrationPatternTypeChessboardStr = @"Chessboard";
static NSString *const kCalibrationPatternTypeCirclesStr = @"Circles";
static NSString *const kCalibrationPatternTypeAsymmetricCirclesStr = @"Asymmetric circles";

@interface SettingsViewController : UIViewController <UITableViewDelegate, UITableViewDataSource>

@end
