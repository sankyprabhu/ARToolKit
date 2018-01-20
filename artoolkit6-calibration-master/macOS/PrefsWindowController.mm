/*
 *  PrefsWindowController.mm
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


#import "../prefs.hpp"
#import "PrefsWindowController.h"
#import <AR6/ARVideo/video.h>
#import "../calib_camera.h"

static NSString *const kSettingCalibrationPatternType = @"calibrationPatternType";
static NSString *const kSettingCalibrationPatternSizeWidth = @"calibrationPatternSizeWidth";
static NSString *const kSettingCalibrationPatternSizeHeight = @"calibrationPatternSizeHeight";
static NSString *const kSettingCalibrationPatternSpacing = @"calibrationPatternSpacing";
static NSString *const kSettingCalibrationSave = @"calibrationSave";
static NSString *const kSettingCalibrationServerUploadCanonical = @"calibrationServerUploadCanonical";
static NSString *const kSettingCalibrationServerUploadUser = @"calibrationServerUploadUser";
static NSString *const kSettingCalibrationServerUploadURL = @"calibrationServerUploadURL";
static NSString *const kSettingCalibrationServerAuthenticationToken = @"calibrationServerAuthenticationToken";
static NSString *const kSettingCalibSaveDir = @"kSettingCalibSaveDir";

static NSString *const kCalibrationPatternTypeChessboardStr = @"Chessboard";
static NSString *const kCalibrationPatternTypeCirclesStr = @"Circles";
static NSString *const kCalibrationPatternTypeAsymmetricCirclesStr = @"Asymmetric circles";

@interface PrefsWindowController ()
{
    IBOutlet NSButton *showPrefsOnStartup;
    __weak IBOutlet NSButton *saveCalibrationSwitch;
    __weak IBOutlet NSButton *uploadCalibrationCanonicalSwitch;
    __weak IBOutlet NSButton *uploadCalibrationUserSwitch;
    IBOutlet NSTextField *calibrationServerUploadURL;
    IBOutlet NSTextField *calibrationServerAuthenticationToken;
    IBOutlet NSPopUpButton *cameraInputPopup;
    IBOutlet NSPopUpButton *cameraPresetPopup;
    __weak IBOutlet NSStepper *calibrationPatternSizeWidthStepper;
    __weak IBOutlet NSTextField *calibrationPatternSizeWidthLabel;
    __weak IBOutlet NSStepper *calibrationPatternSizeHeightStepper;
    __weak IBOutlet NSTextField *calibrationPatternSizeHeightLabel;
    __weak IBOutlet NSTextField *calibrationPatternSpacing;
    __weak IBOutlet NSSegmentedControl *calibrationPatternTypeControl;
    __weak IBOutlet NSTextField *calibSaveDirLabel;
}
- (IBAction)calibrationPatternTypeChanged:(NSSegmentedControl *)sender;
- (IBAction)okSelected:(NSButton *)sender;
- (IBAction)calibSaveDirSelectButton:(NSButton *)sender;
@end

@implementation PrefsWindowController

- (instancetype)initWithWindowNibName:(NSString *)windowNibName
{
    return [super initWithWindowNibName:windowNibName];
}

- (instancetype)initWithWindow:(NSWindow *)window
{
    id ret;
    if ((ret = [super initWithWindow:window])) {
        // Customisation here.
    }
    return (ret);
}

- (void)windowDidLoad {
    [super windowDidLoad];
    
    // Pre-process, selecting options etc.
    NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];
    
    // Populate the camera input popup.
    ARVideoSourceInfoListT *sil = ar2VideoCreateSourceInfoList("-module=AVFoundation");
    if (!sil) {
        ARLOGe("Unable to get ARVideoSourceInfoListT.\n");
        cameraInputPopup.enabled = FALSE;
    } else if (sil->count == 0) {
        ARLOGe("No video sources connected.\n");
        cameraInputPopup.enabled = FALSE;
    } else {
        NSString *cot = [defaults stringForKey:@"cameraOpenToken"];
        int selectedItemIndex = 0;
        for (int i = 0; i < sil->count; i++) {
            [cameraInputPopup addItemWithTitle:@(sil->info[i].name)];
            [cameraInputPopup itemAtIndex:i].representedObject = @(sil->info[i].open_token);
            if (cot && sil->info[i].open_token && strcmp(cot.UTF8String, sil->info[i].open_token) == 0) {
                selectedItemIndex = i;
            }
        }
        [cameraInputPopup selectItemAtIndex:selectedItemIndex];
        cameraInputPopup.enabled = TRUE;
    }
    
    NSString *cp = [defaults stringForKey:@"cameraPreset"];
    if (cp) [cameraPresetPopup selectItemWithTitle:cp];
    
#if defined(ARTOOLKIT6_CSUU) && defined(ARTOOLKIT6_CSAT)
    BOOL uploadOn = [defaults boolForKey:kSettingCalibrationServerUploadCanonical];
    uploadCalibrationCanonicalSwitch.state = uploadOn;
#else
    BOOL uploadOn = [defaults boolForKey:kSettingCalibrationServerUploadUser];
    uploadCalibrationUserSwitch.state = uploadOn;
#endif
    saveCalibrationSwitch.enabled = uploadOn;
    saveCalibrationSwitch.state = (uploadOn ? [defaults boolForKey:kSettingCalibrationSave] : TRUE);
    NSString *csuu = [defaults stringForKey:kSettingCalibrationServerUploadURL];
    calibrationServerUploadURL.stringValue = (csuu ? csuu : @"");
    calibrationServerUploadURL.placeholderString = @"https://example.com/upload.php";
    NSString *csat = [defaults stringForKey:kSettingCalibrationServerAuthenticationToken];
    calibrationServerAuthenticationToken.stringValue = (csat ? csat : @"");
    calibrationServerAuthenticationToken.placeholderString = @"";
    
    showPrefsOnStartup.state = [defaults boolForKey:@"showPrefsOnStartup"];
    
    Calibration::CalibrationPatternType patternType = CALIBRATION_PATTERN_TYPE_DEFAULT;
    NSString *patternTypeStr = [defaults objectForKey:kSettingCalibrationPatternType];
    if (patternTypeStr.length != 0) {
        if ([patternTypeStr isEqualToString:kCalibrationPatternTypeChessboardStr]) patternType = Calibration::CalibrationPatternType::CHESSBOARD;
        else if ([patternTypeStr isEqualToString:kCalibrationPatternTypeCirclesStr]) patternType = Calibration::CalibrationPatternType::CIRCLES_GRID;
        else if ([patternTypeStr isEqualToString:kCalibrationPatternTypeAsymmetricCirclesStr]) patternType = Calibration::CalibrationPatternType::ASYMMETRIC_CIRCLES_GRID;
    }
    switch (patternType) {
        case Calibration::CalibrationPatternType::CHESSBOARD: calibrationPatternTypeControl.selectedSegment = 0; break;
        case Calibration::CalibrationPatternType::CIRCLES_GRID: calibrationPatternTypeControl.selectedSegment = 1; break;
        case Calibration::CalibrationPatternType::ASYMMETRIC_CIRCLES_GRID: calibrationPatternTypeControl.selectedSegment = 2; break;
    }
    
    int w = (int)[defaults integerForKey:kSettingCalibrationPatternSizeWidth];
    int h = (int)[defaults integerForKey:kSettingCalibrationPatternSizeHeight];
    if (w < 1 || h < 1) {
        w = Calibration::CalibrationPatternSizes[patternType].width;
        h = Calibration::CalibrationPatternSizes[patternType].height;
    }
    calibrationPatternSizeWidthStepper.intValue = w;
    calibrationPatternSizeHeightStepper.intValue = h;
    [calibrationPatternSizeWidthLabel takeIntValueFrom:calibrationPatternSizeWidthStepper];
    [calibrationPatternSizeHeightLabel takeIntValueFrom:calibrationPatternSizeHeightStepper];
    
    float f = [defaults floatForKey:kSettingCalibrationPatternSpacing];
    if (f <= 0.0f) f = Calibration::CalibrationPatternSpacings[patternType];
    calibrationPatternSpacing.stringValue = [NSString stringWithFormat:@"%.2f", f];
    
    char *dir = getPreferenceCalibSaveDir(NULL);
    NSURL *dirURL = [NSURL fileURLWithPath:[NSString stringWithUTF8String:dir]];
    free(dir);
    calibSaveDirLabel.stringValue = dirURL.lastPathComponent;
}

- (IBAction)saveCalibrationChanged:(NSButton *)sender
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    [defaults setBool:sender.state forKey:kSettingCalibrationSave];
}

- (IBAction)uploadCalibrationUserChanged:(NSButton *)sender
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    [defaults setBool:sender.state forKey:kSettingCalibrationServerUploadUser];
    saveCalibrationSwitch.enabled = sender.state;
    if (sender.state) saveCalibrationSwitch.state = [defaults boolForKey:kSettingCalibrationSave];
    else saveCalibrationSwitch.state = TRUE;
}

- (IBAction)uploadCalibrationCanonicalChanged:(NSButton *)sender
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    [defaults setBool:sender.state forKey:kSettingCalibrationServerUploadCanonical];
    saveCalibrationSwitch.enabled = sender.state;
    if (sender.state) saveCalibrationSwitch.state = [defaults boolForKey:kSettingCalibrationSave];
    else saveCalibrationSwitch.state = TRUE;
}

- (IBAction)calibrationPatternTypeChanged:(NSSegmentedControl *)sender
{
    Calibration::CalibrationPatternType patternType;
    switch (sender.selectedSegment) {
        case 0:
            patternType = Calibration::CalibrationPatternType::CHESSBOARD;
            break;
        case 1:
            patternType = Calibration::CalibrationPatternType::CIRCLES_GRID;
            break;
        case 2:
            patternType = Calibration::CalibrationPatternType::ASYMMETRIC_CIRCLES_GRID;
            break;
        default:
            patternType = CALIBRATION_PATTERN_TYPE_DEFAULT;
            break;
    }
    calibrationPatternSizeWidthStepper.intValue = Calibration::CalibrationPatternSizes[patternType].width;
    calibrationPatternSizeHeightStepper.intValue = Calibration::CalibrationPatternSizes[patternType].height;
    [calibrationPatternSizeWidthLabel takeIntValueFrom:calibrationPatternSizeWidthStepper];
    [calibrationPatternSizeHeightLabel takeIntValueFrom:calibrationPatternSizeHeightStepper];
    calibrationPatternSpacing.stringValue = [NSString stringWithFormat:@"%.2f", Calibration::CalibrationPatternSpacings[patternType]];
}

- (BOOL)windowShouldClose:(id)sender
{
    return (YES);
}

- (void)windowWillClose:(NSNotification *)notification
{
    // Post-process selected options.
}

- (IBAction)okSelected:(NSButton *)sender {
    
    NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];
    NSString *cot = cameraInputPopup.selectedItem.representedObject;
    [defaults setObject:cameraPresetPopup.selectedItem.title forKey:@"cameraPreset"];
    [defaults setObject:cot forKey:@"cameraOpenToken"];
    [defaults setObject:calibrationServerUploadURL.stringValue forKey:kSettingCalibrationServerUploadURL];
    [defaults setObject:calibrationServerAuthenticationToken.stringValue forKey:kSettingCalibrationServerAuthenticationToken];
    [defaults setBool:showPrefsOnStartup.state forKey:@"showPrefsOnStartup"];
    switch (calibrationPatternTypeControl.selectedSegment) {
        case 0:
            [defaults setObject:kCalibrationPatternTypeChessboardStr forKey:kSettingCalibrationPatternType];
            break;
        case 1:
            [defaults setObject:kCalibrationPatternTypeCirclesStr forKey:kSettingCalibrationPatternType];
            break;
        case 2:
            [defaults setObject:kCalibrationPatternTypeAsymmetricCirclesStr forKey:kSettingCalibrationPatternType];
            break;
        default:
            [defaults setObject:nil forKey:kSettingCalibrationPatternType];
            break;
    }
    [defaults setInteger:calibrationPatternSizeWidthStepper.intValue forKey:kSettingCalibrationPatternSizeWidth];
    [defaults setInteger:calibrationPatternSizeHeightStepper.intValue forKey:kSettingCalibrationPatternSizeHeight];
    [defaults setFloat:calibrationPatternSpacing.floatValue forKey:kSettingCalibrationPatternSpacing];
    
    [NSApp stopModal];
    [self close];
    
    SDL_Event event;
    SDL_zero(event);
    event.type = gSDLEventPreferencesChanged;
    event.user.code = (Sint32)0;
    event.user.data1 = NULL;
    event.user.data2 = NULL;
    SDL_PushEvent(&event);
}

- (IBAction)calibSaveDirSelectButton:(NSButton *)sender
{
    NSOpenPanel *openDlg = [NSOpenPanel openPanel];
    openDlg.canChooseFiles = NO;
    openDlg.canChooseDirectories = YES;
    openDlg.allowsMultipleSelection = NO;
    openDlg.canCreateDirectories = YES;
    openDlg.prompt = @"Select";
    openDlg.delegate = self;
    [openDlg beginSheetModalForWindow:sender.window completionHandler:^(NSInteger result) {
        if (result == NSFileHandlingPanelOKButton) {
            NSURL *dirURL = [[openDlg URLs] objectAtIndex:0];
            NSUserDefaults* defaults = [NSUserDefaults standardUserDefaults];
            [defaults setObject:dirURL.path forKey:kSettingCalibSaveDir];
            calibSaveDirLabel.stringValue = dirURL.lastPathComponent;
        }
    }];
}

// NSOpenSavePanelDelegate method.
- (BOOL)panel:(id)sender shouldEnableURL:(NSURL *)url
{
    return [[NSFileManager defaultManager] isWritableFileAtPath:[url path]];
}

-(void) showHelp:(id)sender
{
    [[NSWorkspace sharedWorkspace] openURL:[NSURL URLWithString:@"https://github.com/artoolkit/ar6-wiki/wiki/Camera-calibration-macOS"]];
}

@end

//
// C interface to our ObjC preferences class.
//

void *initPreferences(void)
{
    // Register the preference defaults early.
    [[NSUserDefaults standardUserDefaults] registerDefaults:[NSDictionary                                                           dictionaryWithContentsOfFile:[[NSBundle mainBundle] pathForResource:@"prefDefaults" ofType:@"plist"]]];

    //NSLog(@"showPrefsOnStartup=%s.\n", ([[NSUserDefaults standardUserDefaults] boolForKey:@"showPrefsOnStartup"] ? "true" : "false"));
    
    PrefsWindowController *pwc = [[PrefsWindowController alloc] initWithWindowNibName:@"PrefsWindow"];
    
    // Register the Preferences menu item in the app menu.
    NSMenu *appMenu = [NSApp.mainMenu itemAtIndex:0].submenu;
    for (NSMenuItem *mi in appMenu.itemArray) {
        if ([mi.title isEqualToString:@"Preferences…"]) {
            mi.target = pwc;
            mi.action = @selector(showWindow:);
            mi.enabled = TRUE;
            break;
        }
    }
    
    // Add the Help menu and an item for the app.
    NSMenu *helpMenu = [[NSMenu alloc] initWithTitle:@"Help"];
    NSMenuItem *helpMenu0 = [[NSMenuItem alloc] init];
    helpMenu0.submenu = helpMenu;
    NSString *appName = [[NSBundle mainBundle] objectForInfoDictionaryKey:@"CFBundleName"];
    NSMenuItem *helpMenuItem = [[NSMenuItem alloc] initWithTitle:[appName stringByAppendingString:@" Help"] action:@selector(showHelp:) keyEquivalent:@"?"];
    helpMenuItem.target = pwc;
    [helpMenu addItem:helpMenuItem];
    [NSApp.mainMenu addItem:helpMenu0];
    NSApp.helpMenu = helpMenu;
    
    if ([[NSUserDefaults standardUserDefaults] boolForKey:@"showPrefsOnStartup"]) {
        showPreferences((__bridge void *)pwc);
    }
    return ((void *)CFBridgingRetain(pwc));
}

void showPreferences(void *preferences)
{
    PrefsWindowController *pwc = (__bridge PrefsWindowController *)preferences;
    if (pwc) {
        [pwc showWindow:pwc];
        [pwc.window makeKeyAndOrderFront:pwc];
        //[NSApp runModalForWindow:pwc.window];
        //NSLog(@"Back from modal\n");
    }
}

char *getPreferenceCameraOpenToken(void *preferences)
{
    NSString *cot = [[NSUserDefaults standardUserDefaults] stringForKey:@"cameraOpenToken"];
    if (cot.length != 0) return (strdup(cot.UTF8String));
    return NULL;
}

char *getPreferenceCameraResolutionToken(void *preferences)
{
    NSString *cp = [[NSUserDefaults standardUserDefaults] stringForKey:@"cameraPreset"];
    if (cp.length != 0) {
        return (strdup([NSString stringWithFormat:@"-preset=%@", cp].UTF8String));
    }
    return NULL;
}

bool getPreferenceCalibrationSave(void *preferences)
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    
#if defined(ARTOOLKIT6_CSUU) && defined(ARTOOLKIT6_CSAT)
    BOOL uploadOn = [defaults boolForKey:kSettingCalibrationServerUploadCanonical];
#else
    BOOL uploadOn = [defaults boolForKey:kSettingCalibrationServerUploadUser];
#endif
    return (uploadOn ? [defaults boolForKey:kSettingCalibrationSave] : TRUE);
}

char *getPreferenceCalibSaveDir(void *preferences)
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    
    NSString *csd = [defaults stringForKey:kSettingCalibSaveDir];
    if (csd.length != 0) return (strdup(csd.UTF8String));
    return (arUtilGetResourcesDirectoryPath(AR_UTIL_RESOURCES_DIRECTORY_BEHAVIOR_USE_USER_ROOT));
}

char *getPreferenceCalibrationServerUploadURL(void *preferences)
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    
#if defined(ARTOOLKIT6_CSUU) && defined(ARTOOLKIT6_CSAT)
    if (![defaults boolForKey:kSettingCalibrationServerUploadCanonical]) return (NULL);
    return (strdup(ARTOOLKIT6_CSUU));
#else
    if (![defaults boolForKey:kSettingCalibrationServerUploadUser]) return (NULL);
    NSString *csuu = [defaults stringForKey:kSettingCalibrationServerUploadURL];
    if (csuu.length != 0) return (strdup(csuu.UTF8String));
    return (NULL);
#endif
}

char *getPreferenceCalibrationServerAuthenticationToken(void *preferences)
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    
#if defined(ARTOOLKIT6_CSUU) && defined(ARTOOLKIT6_CSAT)
    if (![defaults boolForKey:kSettingCalibrationServerUploadCanonical]) return (NULL);
    return (strdup(ARTOOLKIT6_CSAT));
#else
    if (![defaults boolForKey:kSettingCalibrationServerUploadUser]) return (NULL);
    NSString *csat = [defaults stringForKey:kSettingCalibrationServerAuthenticationToken];
    if (csat.length != 0) return (strdup(csat.UTF8String));
    return (NULL);
#endif
}

Calibration::CalibrationPatternType getPreferencesCalibrationPatternType(void *preferences)
{
    Calibration::CalibrationPatternType patternType = CALIBRATION_PATTERN_TYPE_DEFAULT;
    NSString *patternTypeStr = [[NSUserDefaults standardUserDefaults] objectForKey:kSettingCalibrationPatternType];
    if (patternTypeStr.length != 0) {
        if ([patternTypeStr isEqualToString:kCalibrationPatternTypeChessboardStr]) patternType = Calibration::CalibrationPatternType::CHESSBOARD;
        else if ([patternTypeStr isEqualToString:kCalibrationPatternTypeCirclesStr]) patternType = Calibration::CalibrationPatternType::CIRCLES_GRID;
        else if ([patternTypeStr isEqualToString:kCalibrationPatternTypeAsymmetricCirclesStr]) patternType = Calibration::CalibrationPatternType::ASYMMETRIC_CIRCLES_GRID;
    }
    return patternType;
}

cv::Size getPreferencesCalibrationPatternSize(void *preferences)
{
    int w = (int)[[NSUserDefaults standardUserDefaults] integerForKey:kSettingCalibrationPatternSizeWidth];
    int h = (int)[[NSUserDefaults standardUserDefaults] integerForKey:kSettingCalibrationPatternSizeHeight];
    if (w > 0 && h > 0) return cv::Size(w, h);
    
    return Calibration::CalibrationPatternSizes[getPreferencesCalibrationPatternType(preferences)];
}

float getPreferencesCalibrationPatternSpacing(void *preferences)
{
    float f = [[NSUserDefaults standardUserDefaults] floatForKey:kSettingCalibrationPatternSpacing];
    if (f > 0.0f) return f;
    
    return Calibration::CalibrationPatternSpacings[getPreferencesCalibrationPatternType(preferences)];
}

void preferencesFinal(void **preferences_p)
{
    if (preferences_p) {
        CFRelease(*preferences_p);
        *preferences_p = NULL;
    }
}

