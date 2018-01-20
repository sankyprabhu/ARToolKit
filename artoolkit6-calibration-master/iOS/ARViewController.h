/*
 *  ARViewController.h
 *  ARToolKit6 Camera Calibration Utility
 *
 *  This file is part of ARToolKit.
 *
 *  Copyright 2015-2017 Daqri, LLC.
 *  Copyright 2008-2015 ARToolworks, Inc.
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

#import <UIKit/UIKit.h>
#import <GLKit/GLKit.h>

extern NSString *const PreferencesChangedNotification;

@interface ARViewController : UIViewController <GLKViewDelegate, UIDocumentInteractionControllerDelegate>

- (IBAction)handleBackButton:(id)sender;
- (IBAction)handleAddButton:(id)sender;
- (IBAction)handleMenuButton:(id)sender;
@property (nonatomic, retain) IBOutlet GLKView *glkView;
@property (weak, nonatomic) IBOutlet UIBarButtonItem *menuButton;

@end
