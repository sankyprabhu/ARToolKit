/*
 *  SettingsViewController.mm
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


#import "ARViewController.h"
#import "../prefs.hpp"
#import "SettingsViewController.h"

@interface SettingsViewController ()
- (IBAction)goBack:(id)sender;
- (IBAction)csatEdited:(id)sender;
- (IBAction)csuuEdited:(id)sender;
@property (nonatomic, strong) IBOutlet UITableView *tableView;

@property (nonatomic, strong) IBOutlet UITableViewCell *cameraResolutionCell;
@property (nonatomic, strong) IBOutlet UILabel *cameraResolutionSubLabel;
@property (nonatomic, strong) NSArray *cameraResPresets;

@property (nonatomic, strong) IBOutlet UITableViewCell *changeCameraCell;
@property (nonatomic, strong) IBOutlet UILabel *cameraSourceSubLabel;

@property (nonatomic, strong) IBOutlet UITableViewCell *paperSizeCell;
@property (nonatomic, strong) IBOutlet UILabel *paperSizeCellSubLabel;

@property (strong, nonatomic) IBOutlet UITableViewCell *saveCalibrationCell;
@property (weak, nonatomic) IBOutlet UISwitch *saveCalibrationSwitch;
@property (strong, nonatomic) IBOutlet UITableViewCell *uploadCalibrationUserCell;
@property (weak, nonatomic) IBOutlet UISwitch *saveCalibSwitch;
@property (weak, nonatomic) IBOutlet UISwitch *uploadCalibrationUserSwitch;
@property (weak, nonatomic) IBOutlet UITextField *calibrationServerUploadURL;
@property (weak, nonatomic) IBOutlet UITextField *calibrationServerAuthenticationToken;

@property (strong, nonatomic) IBOutlet UITableViewCell *uploadCalibrationCanonicalCell;
@property (weak, nonatomic) IBOutlet UISwitch *uploadCalibrationCanonicalSwitch;

@property (strong, nonatomic) IBOutlet UITableViewCell *calibrationPatternCell;
@property (weak, nonatomic) IBOutlet UISegmentedControl *calibrationPatternTypeControl;
- (IBAction)calibrationPatternTypeChanged:(UISegmentedControl *)sender;
@property (weak, nonatomic) IBOutlet UIStepper *calibrationPatternSizeWidthStepper;
@property (weak, nonatomic) IBOutlet UIStepper *calibrationPatternSizeHeightStepper;
- (IBAction)calibrationPatternSizeWidthChanged:(id)sender;
- (IBAction)calibrationPatternSizeHeightChanged:(id)sender;
@property (weak, nonatomic) IBOutlet UILabel *calibrationPatternSizeWidthLabel;
@property (weak, nonatomic) IBOutlet UILabel *calibrationPatternSizeHeightLabel;
@property (weak, nonatomic) IBOutlet UITextField *calibrationPatternSpacing;
- (IBAction)calibrationPatternSpacingChanged:(UITextField *)sender;

@end



@implementation SettingsViewController

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    self.cameraResPresets = [NSArray arrayWithObjects:@"cif", @"480p" /*@"vga"*/, @"720p", @"1080p", @"low", @"medium", @"high", nil];
    
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    
#if defined(ARTOOLKIT6_CSUU) && defined(ARTOOLKIT6_CSAT)
    BOOL uploadOn = [defaults boolForKey:kSettingCalibrationServerUploadCanonical];
    self.uploadCalibrationCanonicalSwitch.on = uploadOn;
#else
    BOOL uploadOn = [defaults boolForKey:kSettingCalibrationServerUploadUser];
    self.uploadCalibrationUserSwitch.on = uploadOn;
    if ([defaults objectForKey:kSettingCalibrationServerUploadURL] != nil) {
        [self.calibrationServerUploadURL setText:[defaults objectForKey:kSettingCalibrationServerUploadURL]];
    }
    [self.calibrationServerUploadURL setPlaceholder:@"https://example.com/upload.php"];
    if ([defaults objectForKey:kSettingCalibrationServerAuthenticationToken] != nil) {
        [self.calibrationServerAuthenticationToken setText:[defaults objectForKey:kSettingCalibrationServerAuthenticationToken]];
    }
    [self.calibrationServerAuthenticationToken setPlaceholder:@""];
#endif
    self.saveCalibSwitch.enabled = uploadOn;
    self.saveCalibSwitch.on = uploadOn ? [defaults boolForKey:kSettingCalibrationSave] : TRUE;

    if ([defaults objectForKey:kSettingCameraResolutionStr] != nil) [self.cameraResolutionSubLabel setText:[defaults objectForKey:kSettingCameraResolutionStr]];
    if ([defaults objectForKey:kSettingCameraSourceStr] != nil) [self.cameraSourceSubLabel setText:[defaults objectForKey:kSettingCameraSourceStr]];
    if ([defaults objectForKey:kSettingPaperSizeStr] != nil) [self.paperSizeCellSubLabel setText:[defaults objectForKey:kSettingPaperSizeStr]];
    
    Calibration::CalibrationPatternType patternType = CALIBRATION_PATTERN_TYPE_DEFAULT;
    NSString *patternTypeStr = [defaults objectForKey:kSettingCalibrationPatternType];
    if (patternTypeStr.length != 0) {
        if ([patternTypeStr isEqualToString:kCalibrationPatternTypeChessboardStr]) patternType = Calibration::CalibrationPatternType::CHESSBOARD;
        else if ([patternTypeStr isEqualToString:kCalibrationPatternTypeCirclesStr]) patternType = Calibration::CalibrationPatternType::CIRCLES_GRID;
        else if ([patternTypeStr isEqualToString:kCalibrationPatternTypeAsymmetricCirclesStr]) patternType = Calibration::CalibrationPatternType::ASYMMETRIC_CIRCLES_GRID;
    }
    switch (patternType) {
        case Calibration::CalibrationPatternType::CHESSBOARD: self.calibrationPatternTypeControl.selectedSegmentIndex = 0; break;
        case Calibration::CalibrationPatternType::CIRCLES_GRID: self.calibrationPatternTypeControl.selectedSegmentIndex = 1; break;
        case Calibration::CalibrationPatternType::ASYMMETRIC_CIRCLES_GRID: self.calibrationPatternTypeControl.selectedSegmentIndex = 2; break;
    }
    
    int w = (int)[defaults integerForKey:kSettingCalibrationPatternSizeWidth];
    int h = (int)[defaults integerForKey:kSettingCalibrationPatternSizeHeight];
    if (w < 1 || h < 1) {
        w = Calibration::CalibrationPatternSizes[patternType].width;
        h = Calibration::CalibrationPatternSizes[patternType].height;
    }
    self.calibrationPatternSizeWidthStepper.value = w;
    self.calibrationPatternSizeHeightStepper.value = h;
    [self calibrationPatternSizeWidthChanged:self.calibrationPatternSizeWidthStepper];
    [self calibrationPatternSizeHeightChanged:self.calibrationPatternSizeHeightStepper];
    
    float f = [defaults floatForKey:kSettingCalibrationPatternSpacing];
    if (f <= 0.0f) f = Calibration::CalibrationPatternSpacings[patternType];
    self.calibrationPatternSpacing.text = [NSString stringWithFormat:@"%.2f", f];
}

- (IBAction)goBack:(id)sender
{
    [self.presentingViewController dismissViewControllerAnimated:YES completion:nil];
    [[NSNotificationCenter defaultCenter] postNotificationName:PreferencesChangedNotification object:self];
}

- (IBAction)saveCalibChanged:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setBool:self.saveCalibSwitch.on forKey:kSettingCalibrationSave];
}

- (IBAction)uploadOnChanged:(id)sender
{
#if defined(ARTOOLKIT6_CSUU) && defined(ARTOOLKIT6_CSAT)
    BOOL uploadOn = self.uploadCalibrationCanonicalSwitch.on;
    [[NSUserDefaults standardUserDefaults] setBool:uploadOn forKey:kSettingCalibrationServerUploadCanonical];
#else
    BOOL uploadOn = self.uploadCalibrationUserSwitch.on;
    [[NSUserDefaults standardUserDefaults] setBool:uploadOn forKey:kSettingCalibrationServerUploadUser];
#endif
    self.saveCalibSwitch.enabled = uploadOn;
    self.saveCalibSwitch.on = uploadOn ? [[NSUserDefaults standardUserDefaults] boolForKey:kSettingCalibrationSave] : TRUE;
 }

- (IBAction)csatEdited:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:self.calibrationServerAuthenticationToken.text forKey:kSettingCalibrationServerAuthenticationToken];
}

- (IBAction)csuuEdited:(id)sender
{
    [[NSUserDefaults standardUserDefaults] setObject:self.calibrationServerUploadURL.text forKey:kSettingCalibrationServerUploadURL];
}

- (void)selectCameraResolution
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    UIAlertController *alertController = [UIAlertController alertControllerWithTitle:@"Select Camera Resolution" message:nil preferredStyle:UIAlertControllerStyleActionSheet];
    [alertController addAction:[UIAlertAction actionWithTitle:@"Cancel" style:UIAlertActionStyleCancel handler:nil]];
    for (NSString *preset in self.cameraResPresets) {
        [alertController addAction:[UIAlertAction actionWithTitle:preset style:UIAlertActionStyleDefault handler:^(UIAlertAction *action) {
            [defaults setObject:preset forKey:kSettingCameraResolutionStr];
            [self.cameraResolutionSubLabel setText:preset]; }]];
    }
    [alertController.popoverPresentationController setSourceView:self.cameraResolutionCell];
    [self presentViewController:alertController animated:YES completion:^(void) {
        [self.tableView deselectRowAtIndexPath:[NSIndexPath indexPathForRow:0 inSection:1] animated:YES];
        [defaults synchronize];
    }];
}

- (void)selectCameraSource
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    UIAlertController *alertController = [UIAlertController alertControllerWithTitle:@"Select Camera" message:nil preferredStyle:UIAlertControllerStyleActionSheet];
    [alertController addAction:[UIAlertAction actionWithTitle:@"Cancel" style:UIAlertActionStyleCancel handler:nil]];
    [alertController addAction:[UIAlertAction actionWithTitle:kCameraSourceFront style:UIAlertActionStyleDefault handler:^(UIAlertAction *action) {
        [defaults setObject:kCameraSourceFront forKey:kSettingCameraSourceStr];
        [self.cameraSourceSubLabel setText:kCameraSourceFront];
    }]];
    [alertController addAction:[UIAlertAction actionWithTitle:kCameraSourceRear style:UIAlertActionStyleDefault handler:^(UIAlertAction *action) {
        [defaults setObject:kCameraSourceRear forKey:kSettingCameraSourceStr];
        [self.cameraSourceSubLabel setText:kCameraSourceRear];
    }]];
    [alertController.popoverPresentationController setSourceView:self.changeCameraCell];
    [self presentViewController:alertController animated:YES completion:^(void) {
        [self.tableView deselectRowAtIndexPath:[NSIndexPath indexPathForRow:1 inSection:1] animated:YES];
        [defaults synchronize];
    }];
}

- (void)selectPaperSize
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    UIAlertController *alertController = [UIAlertController alertControllerWithTitle:@"Paper Size" message:nil preferredStyle:UIAlertControllerStyleActionSheet];
    [alertController addAction:[UIAlertAction actionWithTitle:@"Cancel" style:UIAlertActionStyleCancel handler:nil]];
    [alertController addAction:[UIAlertAction actionWithTitle:kPaperSizeA4Str style:UIAlertActionStyleDefault handler:^(UIAlertAction *action) {
        [defaults setObject:kPaperSizeA4Str forKey:kSettingPaperSizeStr];
        [self.paperSizeCellSubLabel setText:kPaperSizeA4Str]; }]];
    [alertController addAction:[UIAlertAction actionWithTitle:kPaperSizeUSLetterStr style:UIAlertActionStyleDefault handler:^(UIAlertAction *action) {
        [defaults setObject:kPaperSizeUSLetterStr forKey:kSettingPaperSizeStr];
        [self.paperSizeCellSubLabel setText:kPaperSizeUSLetterStr]; }]];
    [alertController.popoverPresentationController setSourceView:self.paperSizeCell];
    [self presentViewController:alertController animated:YES completion:^(void) {
        [self.tableView deselectRowAtIndexPath:[NSIndexPath indexPathForRow:0 inSection:2] animated:YES];
        [defaults synchronize];
    }];
}

- (IBAction)calibrationPatternTypeChanged:(UISegmentedControl *)sender
{
    NSUserDefaults *defaults = [NSUserDefaults standardUserDefaults];
    Calibration::CalibrationPatternType patternType;
    switch (sender.selectedSegmentIndex) {
        case 0:
            [defaults setObject:kCalibrationPatternTypeChessboardStr forKey:kSettingCalibrationPatternType];
            patternType = Calibration::CalibrationPatternType::CHESSBOARD;
            break;
        case 1:
            [defaults setObject:kCalibrationPatternTypeCirclesStr forKey:kSettingCalibrationPatternType];
            patternType = Calibration::CalibrationPatternType::CIRCLES_GRID;
            break;
        case 2:
            [defaults setObject:kCalibrationPatternTypeAsymmetricCirclesStr forKey:kSettingCalibrationPatternType];
            patternType = Calibration::CalibrationPatternType::ASYMMETRIC_CIRCLES_GRID;
            break;
        default:
            [defaults setObject:nil forKey:kSettingCalibrationPatternType];
            patternType = CALIBRATION_PATTERN_TYPE_DEFAULT;
            break;
    }
    self.calibrationPatternSizeWidthStepper.value = Calibration::CalibrationPatternSizes[patternType].width;
    self.calibrationPatternSizeHeightStepper.value = Calibration::CalibrationPatternSizes[patternType].height;
    self.calibrationPatternSpacing.text = [NSString stringWithFormat:@"%.2f", Calibration::CalibrationPatternSpacings[patternType]];
    [self calibrationPatternSizeWidthChanged:self.calibrationPatternSizeWidthStepper];
    [self calibrationPatternSizeHeightChanged:self.calibrationPatternSizeHeightStepper];
    [self calibrationPatternSpacingChanged:self.calibrationPatternSpacing];
}

- (IBAction)calibrationPatternSizeWidthChanged:(UIStepper *)sender
{
    self.calibrationPatternSizeWidthLabel.text = [NSString stringWithFormat:@"%d", (int)sender.value];
    [[NSUserDefaults standardUserDefaults] setInteger:(int)sender.value forKey:kSettingCalibrationPatternSizeWidth];
}

- (IBAction)calibrationPatternSizeHeightChanged:(UIStepper *)sender
{
    self.calibrationPatternSizeHeightLabel.text = [NSString stringWithFormat:@"%d", (int)sender.value];
    [[NSUserDefaults standardUserDefaults] setInteger:(int)sender.value forKey:kSettingCalibrationPatternSizeHeight];
}

- (IBAction)calibrationPatternSpacingChanged:(UITextField *)sender
{
    [[NSUserDefaults standardUserDefaults] setFloat:[sender.text floatValue] forKey:kSettingCalibrationPatternSpacing];
}


- (NSInteger)numberOfSectionsInTableView:(UITableView *)tableView
{
    return 4;
}

- (NSString *)tableView:(UITableView *)tableView titleForHeaderInSection:(NSInteger)section
{
    if (section == 0) return @"CAMERA SETTINGS";
    if (section == 1) return @"PRINT SETTINGS";
    if (section == 2) return @"CALIBRATION SAVING SETTINGS";
    if (section == 3) return @"CALIBRATION PATTERN SETTINGS";
    return nil;
}

- (NSInteger)tableView:(UITableView *)tableView numberOfRowsInSection:(NSInteger)section
{
    if (section == 0) return 2;
    if (section == 1) return 1;
    if (section == 2) return 2;
    if (section == 3) return 1;
   return 0;
}

- (UITableViewCell *)tableView:(UITableView *)tableView cellForRowAtIndexPath:(NSIndexPath *)indexPath
{
    if (indexPath.section == 0) {
        if (indexPath.row == 0) return self.cameraResolutionCell;
        if (indexPath.row == 1) return self.changeCameraCell;
    } else if (indexPath.section == 1) {
        if (indexPath.row == 0) return self.paperSizeCell;
    } else if (indexPath.section == 2) {
        if (indexPath.row == 0) return self.saveCalibrationCell;
#if defined(ARTOOLKIT6_CSUU) && defined(ARTOOLKIT6_CSAT)
        if (indexPath.row == 1) return self.uploadCalibrationCanonicalCell;
#else
        if (indexPath.row == 1) return self.uploadCalibrationUserCell;
#endif
    } else if (indexPath.section == 3) {
        if (indexPath.row == 0) return self.calibrationPatternCell;
    }
    return nil;
}

- (void)tableView:(UITableView *)tableView didSelectRowAtIndexPath:(NSIndexPath *)indexPath
{
    if (indexPath.section == 0) {
        if (indexPath.row == 0) [self selectCameraResolution];
        if (indexPath.row == 1) [self selectCameraSource];
    }
    if (indexPath.section == 1) {
        if (indexPath.row == 0) [self selectPaperSize];
    }
}

- (CGFloat)tableView:(UITableView *)tableView heightForRowAtIndexPath:(NSIndexPath *)indexPath
{
    if (indexPath.section == 0) {
        if (indexPath.row == 0) return 62.0f;
        if (indexPath.row == 1) return 62.0f;
    } else if (indexPath.section == 1) {
        if (indexPath.row == 0) return 62.0f;
    } else if (indexPath.section == 2) {
        if (indexPath.row == 0) return 46.0f;
#if defined(ARTOOLKIT6_CSUU) && defined(ARTOOLKIT6_CSAT)
        if (indexPath.row == 1) return 46.0f;
#else
        if (indexPath.row == 1) return 180.0f;
#endif
    } else if (indexPath.section == 3) {
        if (indexPath.row == 0) return 186.0f;
   }
    return 0;
}

- (void)didReceiveMemoryWarning
{
    [super didReceiveMemoryWarning];
    // Dispose of any resources that can be recreated.
}

@end

void *initPreferences(void)
{
    [[NSUserDefaults standardUserDefaults] registerDefaults:[NSDictionary                                                           dictionaryWithContentsOfFile:[[NSBundle mainBundle] pathForResource:@"prefDefaults" ofType:@"plist"]]];
    return (NULL);
}

void showPreferences(void *preferences)
{
}

char *getPreferenceCameraOpenToken(void *preferences)
{
    NSString *cameraSource = [[NSUserDefaults standardUserDefaults] objectForKey:kSettingCameraSourceStr];
    if (cameraSource.length != 0) {
        if      ([cameraSource isEqualToString:kCameraSourceFront]) return strdup("-position=front");
        else if ([cameraSource isEqualToString:kCameraSourceRear]) return strdup("-position=rear");
    }
    return NULL;
}

char *getPreferenceCameraResolutionToken(void *preferences)
{
    NSString *cameraResolution = [[NSUserDefaults standardUserDefaults] objectForKey:kSettingCameraResolutionStr];
    if (cameraResolution.length != 0) {
        return (strdup([NSString stringWithFormat:@"-preset=%@", cameraResolution].UTF8String));
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
}

