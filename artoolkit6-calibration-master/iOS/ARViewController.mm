/*
 *  ARViewController.mm
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


#import "ARViewController.h"
#import <OpenGLES/ES2/glext.h>
#import "CameraFocusView.h"
#import "SettingsViewController.h"
#ifdef DEBUG
#  import <unistd.h>
#  import <sys/param.h>
#endif

#include <AR6/AR/ar.h>
#include <AR6/ARVideoSource.h>
#include <AR6/ARView.h>
#include <AR6/ARUtil/system.h>
#include <AR6/ARUtil/thread_sub.h>
#include <AR6/ARUtil/time.h>
#include <AR6/ARUtil/file_utils.h>
#include <AR6/ARG/arg.h>
#include <AR6/ARG/arg_mtx.h>
#include <AR6/ARG/arg_shader_gl.h>

#include "fileUploader.h"
#include "Calibration.hpp"
#include "flow.hpp"
#include "Eden/EdenMessage.h"
#include "Eden/EdenGLFont.h"


#include "prefs.hpp"

//#import "draw.h"

// ============================================================================
//	Constants
// ============================================================================

// Indices of GL ES program uniforms.
enum {
    UNIFORM_MODELVIEW_PROJECTION_MATRIX,
    UNIFORM_COLOR,
    UNIFORM_COUNT
};
// Indices of of GL ES program attributes.
enum {
    ATTRIBUTE_VERTEX,
    ATTRIBUTE_COUNT
};


#define      CALIB_IMAGE_NUM               10
#define      SAVE_FILENAME                 "camera_para.dat"

// Data upload.
#define QUEUE_DIR "queue"
#define QUEUE_INDEX_FILE_EXTENSION "upload"



#ifdef __APPLE__
#  include <CommonCrypto/CommonDigest.h>
#  define MD5 CC_MD5
#  define MD5_DIGEST_LENGTH CC_MD5_DIGEST_LENGTH
#  define MD5_COUNT_t CC_LONG
#else
//#include <openssl/md5.h>
// Rather than including full OpenSSL header tree, just provide prototype for MD5().
// Usage is here: https://www.openssl.org/docs/manmaster/man3/MD5.html .
#  define MD5_DIGEST_LENGTH 16
#  define MD5_COUNT_t size_t
#  ifdef __cplusplus
extern "C" {
#  endif
    unsigned char *MD5(const unsigned char *d, size_t n, unsigned char *md);
#  ifdef __cplusplus
}
#  endif
#endif

#define FONT_SIZE 18.0f
#define UPLOAD_STATUS_HIDE_AFTER_SECONDS 9.0f

NSString *const PreferencesChangedNotification = @"PreferencesChangedNotification";

static void saveParam(const ARParam *param, ARdouble err_min, ARdouble err_avg, ARdouble err_max, void *userdata);

@interface ARViewController () {

    // Prefs.
    int gPreferencesCalibImageCountMax;
    Calibration::CalibrationPatternType gCalibrationPatternType;
    cv::Size gCalibrationPatternSize;
    float gCalibrationPatternSpacing;

    void *gPreferences;
    //Uint32 gSDLEventPreferencesChanged;
    bool gCalibrationSave;
    char *gPreferenceCameraOpenToken;
    char *gPreferenceCameraResolutionToken;
    char *gCalibrationServerUploadURL;
    char *gCalibrationServerAuthenticationToken;

    //
    // Calibration.
    //
    
    Calibration *gCalibration;
    
    //
    // Data upload.
    //
    
    char *gFileUploadQueuePath;
    FILE_UPLOAD_HANDLE_t *fileUploadHandle;
    


    // Video acquisition and rendering.
    ARVideoSource *vs;
    ARView *vv;
    bool gPostVideoSetupDone;
    bool gCameraIsFrontFacing;
    long gFrameCount;
    BOOL gGotFrame;

    // Window and GL context.
    int contextWidth;
    int contextHeight;
    bool contextWasUpdated;
    int32_t gViewport[4];
    int gDisplayOrientation; // range [0-3]. 1=landscape.
    float gDisplayDPI;
    GLint uniforms[UNIFORM_COUNT];
    GLuint program;
    CameraFocusView *focusView;

    // Main state.
    struct timeval gStartTime;

    // Corner finder results copy, for display to user.
    ARGL_CONTEXT_SETTINGS_REF gArglSettingsCornerFinderImage;
}

@property (strong, nonatomic) EAGLContext *context;
@property (strong, nonatomic) UIDocumentInteractionController *docInteractionController;

// Re-implemented properties from GLKViewController.
@property (strong, nonatomic) CADisplayLink *displayLink;
@property (nonatomic) NSInteger animationFrameInterval;
@property (nonatomic) NSInteger preferredFramesPerSecond;
@property (nonatomic, getter=isPaused) BOOL paused;
@property (nonatomic) BOOL pauseOnWillResignActive;
@property (nonatomic) BOOL resumeOnDidBecomeActive;


- (void)setupGL;
- (void)tearDownGL;
@end

@implementation ARViewController

- (void)viewDidLoad
{
    [super viewDidLoad];
    
    // Init instance variables.
    gPreferencesCalibImageCountMax = CALIB_IMAGE_NUM;
    gCalibrationPatternType = CALIBRATION_PATTERN_TYPE_DEFAULT;
    gCalibrationPatternSize = Calibration::CalibrationPatternSizes[gCalibrationPatternType];
    gCalibrationPatternSpacing = Calibration::CalibrationPatternSpacings[gCalibrationPatternType];
    gPreferences = NULL;
    //gSDLEventPreferencesChanged = 0;
    gPreferenceCameraOpenToken = NULL;
    gPreferenceCameraResolutionToken = NULL;
    gCalibrationServerUploadURL = NULL;
    gCalibrationServerAuthenticationToken = NULL;
    gCalibration = nullptr;
    gFileUploadQueuePath = NULL;
    fileUploadHandle = NULL;
    vs = nullptr;
    vv = nullptr;
    gPostVideoSetupDone = false;
    gCameraIsFrontFacing = false;
    gFrameCount = 0L;
    gGotFrame = FALSE;
    contextWidth = 0;
    contextHeight = 0;
    contextWasUpdated = false;
    gViewport[0] =  gViewport[1] = gViewport[2] = gViewport[3] = 0;
    gDisplayOrientation = 0; // range [0-3]. 0=portrait, 1=landscape.
    gDisplayDPI = 72.0f;
    uniforms[0] = 0;
    program = 0;
    gArglSettingsCornerFinderImage = NULL;
    
    // Init reimplemented GLKViewController properties.
    _displayLink = nil;
    _animationFrameInterval = 2;
    _preferredFramesPerSecond = 30;
    _paused = YES;
    _pauseOnWillResignActive = NO;
    self.pauseOnWillResignActive = YES;
    _resumeOnDidBecomeActive = NO;
    self.resumeOnDidBecomeActive = YES;
    
#ifdef DEBUG
    arLogLevel = AR_LOG_LEVEL_DEBUG;
#endif

    // Preferences.
    gPreferences = initPreferences();
    gPreferenceCameraOpenToken = getPreferenceCameraOpenToken(gPreferences);
    gPreferenceCameraResolutionToken = getPreferenceCameraResolutionToken(gPreferences);
    gCalibrationSave = getPreferenceCalibrationSave(gPreferences);
    gCalibrationServerUploadURL = getPreferenceCalibrationServerUploadURL(gPreferences);
    gCalibrationServerAuthenticationToken = getPreferenceCalibrationServerAuthenticationToken(gPreferences);
    gCalibrationPatternType = getPreferencesCalibrationPatternType(gPreferences);
    gCalibrationPatternSize = getPreferencesCalibrationPatternSize(gPreferences);
    gCalibrationPatternSpacing = getPreferencesCalibrationPatternSpacing(gPreferences);
    
    [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(rereadPreferences) name:PreferencesChangedNotification object:nil];
    
    self.context = [[EAGLContext alloc] initWithAPI:kEAGLRenderingAPIOpenGLES2];
    if (!self.context) {
        NSLog(@"Failed to create ES context");
    }
    self.glkView.context = self.context;
    
    if (!focusView) focusView = [[CameraFocusView alloc] initWithFrame:self.view.frame];
    
    asprintf(&gFileUploadQueuePath, "%s/%s", arUtilGetResourcesDirectoryPath(AR_UTIL_RESOURCES_DIRECTORY_BEHAVIOR_USE_APP_CACHE_DIR), QUEUE_DIR);
    // Check for QUEUE_DIR and create if not already existing.
    if (!fileUploaderCreateQueueDir(gFileUploadQueuePath)) {
        ARLOGe("Error: Could not create queue directory.\n");
        exit(-1);
    }
    
    if (gCalibrationServerUploadURL) {
        fileUploadHandle = fileUploaderInit(gFileUploadQueuePath, QUEUE_INDEX_FILE_EXTENSION, gCalibrationServerUploadURL, UPLOAD_STATUS_HIDE_AFTER_SECONDS);
        if (!fileUploadHandle) {
            ARLOGe("Error: Could not initialise fileUploadHandle.\n");
        }
        fileUploaderTickle(fileUploadHandle);
    }
    
    // Calibration prefs.
    ARLOGi("Calbration pattern size X = %d\n", gCalibrationPatternSize.width);
    ARLOGi("Calbration pattern size Y = %d\n", gCalibrationPatternSize.height);
    ARLOGi("Calbration pattern spacing = %f\n", gCalibrationPatternSpacing);
    ARLOGi("Calibration image count maximum = %d\n", gPreferencesCalibImageCountMax);

    [self setupGL];
    
    // Get start time.
    gettimeofday(&gStartTime, NULL);
    
    [self startVideo];
}

- (void)viewWillAppear:(BOOL)animated
{
    [super viewWillAppear:animated];

    [self setPaused:NO];
    
    // Extra view setup.
    [self.view addSubview:focusView];
    
}

- (void)viewDidLayoutSubviews
{
    [self.glkView bindDrawable];
    contextWidth = (int)self.glkView.drawableWidth;
    contextHeight = (int)self.glkView.drawableHeight;
    contextWasUpdated = true;
}

- (void)viewDidDisappear:(BOOL)animated
{
    // Extra view cleanup.
    [focusView removeFromSuperview];

    [self setPaused:YES];
    
    [super viewDidDisappear:animated];
}


- (void)dealloc
{    
    fileUploaderFinal(&fileUploadHandle);

    [self tearDownGL];
    if ([EAGLContext currentContext] == self.context) {
        [EAGLContext setCurrentContext:nil];
    }
    
    [[NSNotificationCenter defaultCenter] removeObserver:self name:PreferencesChangedNotification object:nil];
    
    free(gPreferenceCameraOpenToken);
    free(gPreferenceCameraResolutionToken);
    free(gCalibrationServerUploadURL);
    free(gCalibrationServerAuthenticationToken);
    preferencesFinal(&gPreferences);
    
    // Cleanup reimplemented GLKViewController properties.
    self.pauseOnWillResignActive = NO;
    self.resumeOnDidBecomeActive = NO;
}

- (void)didReceiveMemoryWarning
{
    [super didReceiveMemoryWarning];

    if ([self isViewLoaded] && ([[self view] window] == nil)) {
        self.view = nil;
        
        [self tearDownGL];
        
        if ([EAGLContext currentContext] == self.context) {
            [EAGLContext setCurrentContext:nil];
        }
        self.context = nil;
    }

    // Dispose of any resources that can be recreated.
}

- (BOOL)prefersStatusBarHidden {
    return YES;
}

#pragma mark - GLKViewController methods

- (void)setAnimationFrameInterval:(NSInteger)animationFrameInterval {
    if (animationFrameInterval == _animationFrameInterval) {
        return;
    }
    _animationFrameInterval = animationFrameInterval;
    [self.displayLink invalidate];
    CADisplayLink *aDisplayLink = [CADisplayLink displayLinkWithTarget:self selector:@selector(executeRunLoop)];
    [aDisplayLink setFrameInterval:_animationFrameInterval];
    [aDisplayLink addToRunLoop:[NSRunLoop currentRunLoop] forMode:NSDefaultRunLoopMode];
    self.displayLink = aDisplayLink;
}

- (void)setPreferredFramesPerSecond:(NSInteger)preferredFramesPerSecond {
    _preferredFramesPerSecond = preferredFramesPerSecond;
    if (_preferredFramesPerSecond < 2) {
        self.animationFrameInterval = 60;
        return;
    }
    if (_preferredFramesPerSecond > 30) {
        self.animationFrameInterval = 1;
        return;
    }
    self.animationFrameInterval = 60 / _preferredFramesPerSecond;
}

- (void)setPaused:(BOOL)paused {
    if (paused) {
        if (!_paused) {
            [self.displayLink invalidate];
            self.displayLink = nil;
            _paused = YES;
        }
        return;
    }
    if (_paused) {
        CADisplayLink *aDisplayLink = [CADisplayLink displayLinkWithTarget:self selector:@selector(executeRunLoop)];
        [aDisplayLink setFrameInterval:_animationFrameInterval];
        [aDisplayLink addToRunLoop:[NSRunLoop currentRunLoop] forMode:NSDefaultRunLoopMode];
        self.displayLink = aDisplayLink;
        _paused = NO;
    }
}

- (void)setPauseOnWillResignActive:(BOOL)pauseOnWillResignActive {
    if (pauseOnWillResignActive == _pauseOnWillResignActive) {
        return;
    }
    _pauseOnWillResignActive = pauseOnWillResignActive;
    if (pauseOnWillResignActive) {
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(stopAnimation) name:UIApplicationWillResignActiveNotification object:nil];
    } else {
        [[NSNotificationCenter defaultCenter] removeObserver:self name:UIApplicationWillResignActiveNotification object:nil];
    }
}

- (void)setResumeOnDidBecomeActive:(BOOL)resumeOnDidBecomeActive {
    if (resumeOnDidBecomeActive == _resumeOnDidBecomeActive) {
        return;
    }
    _resumeOnDidBecomeActive = resumeOnDidBecomeActive;
    if (resumeOnDidBecomeActive) {
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(startAnimation) name:UIApplicationDidBecomeActiveNotification object:nil];
    } else {
        [[NSNotificationCenter defaultCenter] removeObserver:self name:UIApplicationDidBecomeActiveNotification object:nil];
    }
}

- (void)startAnimation {
    [self setPaused:NO];
}

- (void)stopAnimation {
    [self setPaused:YES];
}

- (void)executeRunLoop {
    [self.glkView bindDrawable];
    if ([self respondsToSelector:@selector(update)]) {
        [self performSelector:@selector(update)];
    }
    [self.glkView display];
}

#pragma mark - AR methods

- (void)startVideo
{
    char buf[256];
    snprintf(buf, sizeof(buf), "%s %s", (gPreferenceCameraOpenToken ? gPreferenceCameraOpenToken : ""), (gPreferenceCameraResolutionToken ? gPreferenceCameraResolutionToken : ""));
    
    vs = new ARVideoSource;
    if (!vs) {
        ARLOGe("Error: Unable to create video source.\n");
        [self quit:-1];
    } else {
        vs->configure(buf, true, NULL, NULL, 0);
        if (!vs->open()) {
            ARLOGe("Error: Unable to open video source.\n");
            EdenMessageShow(((const unsigned char *)NSLocalizedString(@"VideoOpenError",@"Welcome message when unable to open video source").UTF8String));
        }
    }
    gPostVideoSetupDone = false;
}

- (void)stopVideo
{
    gGotFrame = FALSE;

    // Stop calibration flow.
    flowStopAndFinal();
    
    if (gCalibration) {
        delete gCalibration;
        gCalibration = nullptr;
    }
    
    if (gArglSettingsCornerFinderImage) {
        arglCleanup(gArglSettingsCornerFinderImage); // Clean up any left-over ARGL data.
        gArglSettingsCornerFinderImage = NULL;
    }

    delete vv;
    vv = nullptr;
    delete vs;
    vs = nullptr;
}

- (void) rereadPreferences
{
    // Re-read preferences.
    gCalibrationSave = getPreferenceCalibrationSave(gPreferences);
    char *csuu = getPreferenceCalibrationServerUploadURL(gPreferences);
    if (csuu && gCalibrationServerUploadURL && strcmp(gCalibrationServerUploadURL, csuu) == 0) {
        free(csuu);
    } else {
        free(gCalibrationServerUploadURL);
        gCalibrationServerUploadURL = csuu;
        fileUploaderFinal(&fileUploadHandle);
        if (csuu) {
            fileUploadHandle = fileUploaderInit(gFileUploadQueuePath, QUEUE_INDEX_FILE_EXTENSION, gCalibrationServerUploadURL, UPLOAD_STATUS_HIDE_AFTER_SECONDS);
            if (!fileUploadHandle) {
                ARLOGe("Error: Could not initialise fileUploadHandle.\n");
            }
        }
    }
    char *csat = getPreferenceCalibrationServerAuthenticationToken(gPreferences);
    if (csat && gCalibrationServerAuthenticationToken && strcmp(gCalibrationServerAuthenticationToken, csat) == 0) {
        free(csat);
    } else {
        free(gCalibrationServerAuthenticationToken);
        gCalibrationServerAuthenticationToken = csat;
    }
    bool changedCameraSettings = false;
    char *crt = getPreferenceCameraResolutionToken(gPreferences);
    if (crt && gPreferenceCameraResolutionToken && strcmp(gPreferenceCameraResolutionToken, crt) == 0) {
        free(crt);
    } else {
        free(gPreferenceCameraResolutionToken);
        gPreferenceCameraResolutionToken = crt;
        changedCameraSettings = true;
    }
    char *cot = getPreferenceCameraOpenToken(gPreferences);
    if (cot && gPreferenceCameraOpenToken && strcmp(gPreferenceCameraOpenToken, cot) == 0) {
        free(cot);
    } else {
        free(gPreferenceCameraOpenToken);
        gPreferenceCameraOpenToken = cot;
        changedCameraSettings = true;
    }
    Calibration::CalibrationPatternType patternType = getPreferencesCalibrationPatternType(gPreferences);
    cv::Size patternSize = getPreferencesCalibrationPatternSize(gPreferences);
    float patternSpacing = getPreferencesCalibrationPatternSpacing(gPreferences);
    if (patternType != gCalibrationPatternType || patternSize != gCalibrationPatternSize || patternSpacing != gCalibrationPatternSpacing) {
        gCalibrationPatternType = patternType;
        gCalibrationPatternSize = patternSize;
        gCalibrationPatternSpacing = patternSpacing;
        changedCameraSettings = true;
    }

    if (changedCameraSettings) {
        // Changing camera settings requires complete cancelation of calibration flow,
        // closing of video source, and re-init.
        [self stopVideo];
        [self startVideo];
    }
}


- (void)setupGL
{
    [EAGLContext setCurrentContext:self.context];

    // Library setup.
    int contextsActiveCount = 1;
    EdenMessageInit(contextsActiveCount);
    EdenGLFontInit(contextsActiveCount);
    EdenGLFontSetFont(EDEN_GL_FONT_ID_Stroke_Roman);
    EdenGLFontSetupFontForContext(0, EDEN_GL_FONT_ID_Stroke_Roman);
    EdenGLFontSetSize(FONT_SIZE);
    
    // Get start time.
    gettimeofday(&gStartTime, NULL);
    
    if (!program) {
        GLuint vertShader = 0, fragShader = 0;
        // A simple shader pair which accepts just a vertex position. Fixed color, no lighting.
        const char vertShaderString[] =
        "attribute vec4 position;\n"
        "uniform vec4 color;\n"
        "uniform mat4 modelViewProjectionMatrix;\n"
        
        "varying vec4 colorVarying;\n"
        "void main()\n"
        "{\n"
        "gl_Position = modelViewProjectionMatrix * position;\n"
        "colorVarying = color;\n"
        "}\n";
        const char fragShaderString[] =
        "#ifdef GL_ES\n"
        "precision mediump float;\n"
        "#endif\n"
        "varying vec4 colorVarying;\n"
        "void main()\n"
        "{\n"
        "gl_FragColor = colorVarying;\n"
        "}\n";
        
        if (program) arglGLDestroyShaders(0, 0, program);
        program = glCreateProgram();
        if (!program) {
            ARLOGe("draw: Error creating shader program.\n");
            return;
        }
        
        if (!arglGLCompileShaderFromString(&vertShader, GL_VERTEX_SHADER, vertShaderString)) {
            ARLOGe("draw: Error compiling vertex shader.\n");
            arglGLDestroyShaders(vertShader, fragShader, program);
            program = 0;
            return;
        }
        if (!arglGLCompileShaderFromString(&fragShader, GL_FRAGMENT_SHADER, fragShaderString)) {
            ARLOGe("draw: Error compiling fragment shader.\n");
            arglGLDestroyShaders(vertShader, fragShader, program);
            program = 0;
            return;
        }
        glAttachShader(program, vertShader);
        glAttachShader(program, fragShader);
        
        glBindAttribLocation(program, ATTRIBUTE_VERTEX, "position");
        if (!arglGLLinkProgram(program)) {
            ARLOGe("draw: Error linking shader program.\n");
            arglGLDestroyShaders(vertShader, fragShader, program);
            program = 0;
            return;
        }
        arglGLDestroyShaders(vertShader, fragShader, 0); // After linking, shader objects can be deleted.
        
        // Retrieve linked uniform locations.
        uniforms[UNIFORM_MODELVIEW_PROJECTION_MATRIX] = glGetUniformLocation(program, "modelViewProjectionMatrix");
        uniforms[UNIFORM_COLOR] = glGetUniformLocation(program, "color");
    }
}

- (void) drawBackgroundWidth:(const float)width height:(const float)height x:(const float)x y:(const float)y border:(const bool)drawBorder projection:(GLfloat [16])p
{
    GLfloat vertices[4][2];
    GLfloat colorBlack50[4] = {0.0f, 0.0f, 0.0f, 0.5f}; // 50% transparent black.
    GLfloat colorWhite[4] = {1.0f, 1.0f, 1.0f, 1.0f}; // Opaque white.
    
    vertices[0][0] = x; vertices[0][1] = y;
    vertices[1][0] = width + x; vertices[1][1] = y;
    vertices[2][0] = width + x; vertices[2][1] = height + y;
    vertices[3][0] = x; vertices[3][1] = height + y;
    
    glUseProgram(program);
    glUniformMatrix4fv(uniforms[UNIFORM_MODELVIEW_PROJECTION_MATRIX], 1, GL_FALSE, p);
    glStateCacheDisableDepthTest();
    glStateCacheBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
    glStateCacheEnableBlend();
    
    glVertexAttribPointer(ATTRIBUTE_VERTEX, 2, GL_FLOAT, GL_FALSE, 0, vertices);
    glEnableVertexAttribArray(ATTRIBUTE_VERTEX);
    glUniform4fv(uniforms[UNIFORM_COLOR], 1, colorBlack50);
    glDrawArrays(GL_TRIANGLE_FAN, 0, 4);
    if (drawBorder) {
        glUniform4fv(uniforms[UNIFORM_COLOR], 1, colorWhite);
        glLineWidth(1.0f);
        glDrawArrays(GL_LINE_LOOP, 0, 4);
    }
}

#if 0
// An animation while we're waiting.
// Designed to be drawn on background of at least 3xsquareSize wide and tall.
- (void) drawBusyIndicator(int positionX, int positionY, int squareSize, struct timeval *tp)
{
    const GLfloat square_vertices [4][2] = { {0.5f, 0.5f}, {squareSize - 0.5f, 0.5f}, {squareSize - 0.5f, squareSize - 0.5f}, {0.5f, squareSize - 0.5f} };
    int i;
    
    int hundredthSeconds = (int)tp->tv_usec / 1E4;
    
    // Set up drawing.
    glPushMatrix();
    glLoadIdentity();
    glDisable(GL_DEPTH_TEST);
    glDisable(GL_LIGHTING);
    glDisable(GL_TEXTURE_2D);
    glDisable(GL_BLEND);
    glVertexPointer(2, GL_FLOAT, 0, square_vertices);
    glEnableClientState(GL_VERTEX_ARRAY);
    glDisableClientState(GL_NORMAL_ARRAY);
    glClientActiveTexture(GL_TEXTURE0);
    glDisableClientState(GL_TEXTURE_COORD_ARRAY);
    
    for (i = 0; i < 4; i++) {
        glLoadIdentity();
        glTranslatef((float)(positionX + ((i + 1)/2 != 1 ? -squareSize : 0.0f)), (float)(positionY + (i / 2 == 0 ? 0.0f : -squareSize)), 0.0f); // Order: UL, UR, LR, LL.
        if (i == hundredthSeconds / 25) {
            char r, g, b;
            int secDiv255 = (int)tp->tv_usec / 3921;
            int secMod6 = tp->tv_sec % 6;
            if (secMod6 == 0) {
                r = 255; g = secDiv255; b = 0;
            } else if (secMod6 == 1) {
                r = secDiv255; g = 255; b = 0;
            } else if (secMod6 == 2) {
                r = 0; g = 255; b = secDiv255;
            } else if (secMod6 == 3) {
                r = 0; g = secDiv255; b = 255;
            } else if (secMod6 == 4) {
                r = secDiv255; g = 0; b = 255;
            } else {
                r = 255; g = 0; b = secDiv255;
            }
            glColor4ub(r, g, b, 255);
            glDrawArrays(GL_TRIANGLE_FAN, 0, 4);
        }
        glColor4ub(255, 255, 255, 255);
        glDrawArrays(GL_LINE_LOOP, 0, 4);
    }
    
    glPopMatrix();
}
#endif

- (void)tearDownGL
{
    [EAGLContext setCurrentContext:self.context];
    
    if (program) {
        glDeleteProgram(program);
    }
}

- (void)quit:(int)rc
{
    exit(rc);
}

- (void)update
{
    if (vs->isOpen()) {
        if (vs->captureFrame()) {
            gFrameCount++; // Increment ARToolKit FPS counter.
#ifdef DEBUG
            if (gFrameCount % 150 == 0) {
                ARLOGi("*** Camera - %f (frame/sec)\n", (double)gFrameCount/arUtilTimer());
                gFrameCount = 0;
                arUtilTimerReset();
            }
#endif
            gGotFrame = TRUE;
        }
        
    } // vs->isOpen()

}



// GLKViewDelegate
- (void)glkView:(GLKView *)view drawInRect:(CGRect)rect
{
    GLfloat p[16], m[16];
    int i;
    struct timeval time;
    float left, right, bottom, top;
    GLfloat *vertices = NULL;
    GLint vertexCount;
    
    if (gGotFrame) {
        // Get frame time.
        gettimeofday(&time, NULL);
        
        if (!gPostVideoSetupDone) {
            
            [ARViewController displayToastWithMessage:[NSString stringWithFormat:@"Camera: %dx%d", vs->getVideoWidth(), vs->getVideoHeight()]];
            
            gCameraIsFrontFacing = false;
            AR2VideoParamT *vid = vs->getAR2VideoParam();
            
            if (vid->module == AR_VIDEO_MODULE_AVFOUNDATION) {
                int frontCamera;
                if (ar2VideoGetParami(vid, AR_VIDEO_PARAM_AVFOUNDATION_CAMERA_POSITION, &frontCamera) >= 0) {
                    gCameraIsFrontFacing = (frontCamera == AR_VIDEO_AVFOUNDATION_CAMERA_POSITION_FRONT);
                }
            }
            bool contentRotate90, contentFlipV, contentFlipH;
            if (gDisplayOrientation == 1) { // Landscape with top of device at left.
                contentRotate90 = false;
                contentFlipV = gCameraIsFrontFacing;
                contentFlipH = gCameraIsFrontFacing;
            } else if (gDisplayOrientation == 2) { // Portrait upside-down.
                contentRotate90 = true;
                contentFlipV = !gCameraIsFrontFacing;
                contentFlipH = true;
            } else if (gDisplayOrientation == 3) { // Landscape with top of device at right.
                contentRotate90 = false;
                contentFlipV = !gCameraIsFrontFacing;
                contentFlipH = (!gCameraIsFrontFacing);
            } else /*(gDisplayOrientation == 0)*/ { // Portait
                contentRotate90 = true;
                contentFlipV = gCameraIsFrontFacing;
                contentFlipH = false;
            }
            
            // Setup a route for rendering the color background image.
            vv = new ARView;
            if (!vv) {
                ARLOGe("Error: unable to create video view.\n");
                //quit(-1);
            }
            vv->setRotate90(contentRotate90);
            vv->setFlipH(contentFlipH);
            vv->setFlipV(contentFlipV);
            vv->setScalingMode(ARView::ScalingMode::SCALE_MODE_FIT);
            vv->initWithVideoSource(*vs, contextWidth, contextHeight);
            ARLOGi("Content %dx%d (wxh) will display in GL context %dx%d%s.\n", vs->getVideoWidth(), vs->getVideoHeight(), contextWidth, contextHeight, (contentRotate90 ? " rotated" : ""));
            vv->getViewport(gViewport);
            
            // Setup a route for rendering the mono background image.
            ARParam idealParam;
            arParamClear(&idealParam, vs->getVideoWidth(), vs->getVideoHeight(), AR_DIST_FUNCTION_VERSION_DEFAULT);
            if ((gArglSettingsCornerFinderImage = arglSetupForCurrentContext(&idealParam, AR_PIXEL_FORMAT_MONO)) == NULL) {
                ARLOGe("Unable to setup argl.\n");
                //quit(-1);
            }
            if (!arglDistortionCompensationSet(gArglSettingsCornerFinderImage, FALSE)) {
                ARLOGe("Unable to setup argl.\n");
                //quit(-1);
            }
            arglSetRotate90(gArglSettingsCornerFinderImage, contentRotate90);
            arglSetFlipV(gArglSettingsCornerFinderImage, contentFlipV);
            arglSetFlipH(gArglSettingsCornerFinderImage, contentFlipH);
            
            //
            // Calibration init.
            //
            
            gCalibration = new Calibration(gCalibrationPatternType, gPreferencesCalibImageCountMax, gCalibrationPatternSize, gCalibrationPatternSpacing, vs->getVideoWidth(), vs->getVideoHeight());
            if (!gCalibration) {
                ARLOGe("Error initialising calibration.\n");
                exit (-1);
            }
            
            if (!flowInitAndStart(gCalibration, saveParam, (__bridge void *)self)) {
                ARLOGe("Error: Could not initialise and start flow.\n");
                //quit(-1);
            }
            
            // For FPS statistics.
            arUtilTimerReset();
            gFrameCount = 0;
            
            gPostVideoSetupDone = true;
        } // !gPostVideoSetupDone
        
        if (contextWasUpdated) {
            vv->setContextSize({contextWidth, contextHeight});
            vv->getViewport(gViewport);
        }
        
        FLOW_STATE state = flowStateGet();
        if (state == FLOW_STATE_WELCOME || state == FLOW_STATE_DONE || state == FLOW_STATE_CALIBRATING) {
            
            // Upload the frame to OpenGL.
            // Now done as part of the draw call.
            
        } else if (state == FLOW_STATE_CAPTURING) {
            
            gCalibration->frame(vs);
            
        }
        
        // The display has changed.
        gGotFrame = FALSE;
    }
   
    
    // Clean the OpenGL context.
    glClearColor(0.0, 0.0, 0.0, 1.0);
    glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
    
    //
    // Setup for drawing video frame.
    //
    glViewport(gViewport[0], gViewport[1], gViewport[2], gViewport[3]);
    
    FLOW_STATE state = flowStateGet();
    if (state == FLOW_STATE_WELCOME || state == FLOW_STATE_DONE || state == FLOW_STATE_CALIBRATING) {
        
        // Display the current frame
        vv->draw(vs);
        
    } else if (state == FLOW_STATE_CAPTURING) {
        
        // Grab a lock while we're using the data to prevent it being changed underneath us.
        int cornerFoundAllFlag;
        std::vector<cv::Point2f> corners;
        ARUint8 *videoFrame;
        gCalibration->cornerFinderResultsLockAndFetch(&cornerFoundAllFlag, corners, &videoFrame);
        
        // Display the current frame.
        if (videoFrame) arglPixelBufferDataUpload(gArglSettingsCornerFinderImage, videoFrame);
        arglDispImage(gArglSettingsCornerFinderImage, NULL);
        
        //
        // Setup for drawing on top of video frame, in video pixel coordinates.
        //
        mtxLoadIdentityf(p);
        if (vv->rotate90()) mtxRotatef(p, 90.0f, 0.0f, 0.0f, -1.0f);
        if (vv->flipV()) {
            bottom = (float)vs->getVideoHeight();
            top = 0.0f;
        } else {
            bottom = 0.0f;
            top = (float)vs->getVideoHeight();
        }
        if (vv->flipH()) {
            left = (float)vs->getVideoWidth();
            right = 0.0f;
        } else {
            left = 0.0f;
            right = (float)vs->getVideoWidth();
        }
        mtxOrthof(p, left, right, bottom, top, -1.0f, 1.0f);
        mtxLoadIdentityf(m);
        glStateCacheDisableDepthTest();
        glStateCacheDisableBlend();
        
        // Draw the crosses marking the corner positions.
        const float colorRed[4] = {1.0f, 0.0f, 0.0f, 1.0f};
        const float colorGreen[4] = {0.0f, 1.0f, 0.0f, 1.0f};
        vertexCount = (GLint)corners.size()*4;
        if (vertexCount > 0) {
            float fontSizeScaled = FONT_SIZE * (float)vs->getVideoHeight()/(float)(gViewport[(gDisplayOrientation % 2) == 1 ? 3 : 2]);
            EdenGLFontSetSize(fontSizeScaled);
            EdenGLFontSetColor(cornerFoundAllFlag ? colorRed : colorGreen);
            arMalloc(vertices, GLfloat, vertexCount*2); // 2 coords per vertex.
            for (i = 0; i < corners.size(); i++) {
                vertices[i*8    ] = corners[i].x - 5.0f;
                vertices[i*8 + 1] = vs->getVideoHeight() - corners[i].y - 5.0f;
                vertices[i*8 + 2] = corners[i].x + 5.0f;
                vertices[i*8 + 3] = vs->getVideoHeight() - corners[i].y + 5.0f;
                vertices[i*8 + 4] = corners[i].x - 5.0f;
                vertices[i*8 + 5] = vs->getVideoHeight() - corners[i].y + 5.0f;
                vertices[i*8 + 6] = corners[i].x + 5.0f;
                vertices[i*8 + 7] = vs->getVideoHeight() - corners[i].y - 5.0f;
                
                unsigned char buf[12]; // 10 digits in INT32_MAX, plus sign, plus null.
                sprintf((char *)buf, "%d\n", i);
                
                GLfloat mvp[16];
                mtxLoadMatrixf(mvp, p);
                mtxMultMatrixf(mvp, m);
                mtxTranslatef(mvp, corners[i].x, vs->getVideoHeight() - corners[i].y, 0.0f);
                mtxRotatef(mvp, (float)(gDisplayOrientation - 1) * -90.0f, 0.0f, 0.0f, 1.0f); // Orient the text to the user.
                EdenGLFontDrawLine(0, mvp, buf, 0.0f, 0.0f, H_OFFSET_VIEW_LEFT_EDGE_TO_TEXT_LEFT_EDGE, V_OFFSET_VIEW_BOTTOM_TO_TEXT_BASELINE); // These alignment modes don't require setting of EdenGLFontSetViewSize().
            }
            EdenGLFontSetSize(FONT_SIZE);
            const float colorWhite[4] = {1.0f, 1.0f, 1.0f, 1.0f};
            EdenGLFontSetColor(colorWhite);
        }
        
        gCalibration->cornerFinderResultsUnlock();
        
        if (vertexCount > 0) {
            glUseProgram(program);
            GLfloat mvp[16];
            mtxLoadMatrixf(mvp, p);
            mtxMultMatrixf(mvp, m);
            glUniformMatrix4fv(uniforms[UNIFORM_MODELVIEW_PROJECTION_MATRIX], 1, GL_FALSE, mvp);
            glUniform4fv(uniforms[UNIFORM_COLOR], 1, cornerFoundAllFlag ? colorRed : colorGreen);
            
            glVertexAttribPointer(ATTRIBUTE_VERTEX, 2, GL_FLOAT, GL_FALSE, 0, vertices);
            glEnableVertexAttribArray(ATTRIBUTE_VERTEX);

            glLineWidth(2.0f);
            glDrawArrays(GL_LINES, 0, vertexCount);
            free(vertices);
        }
    }
    
    //
    // Setup for drawing on top of video frame, in viewPort coordinates.
    //
#if 0
    mtxLoadIdentityf(p);
    bottom = 0.0f;
    top = (float)(viewPort[viewPortIndexHeight]);
    left = 0.0f;
    right = (float)(viewPort[viewPortIndexWidth]);
    mtxOrthof(p, left, right, bottom, top, -1.0f, 1.0f);
    mtxLoadIdentityf(m);
    
    EdenGLFontSetViewSize(right, top);
    EdenMessageSetViewSize(right, top, gDisplayDPI);
#endif
    
    //
    // Setup for drawing on screen, with correct orientation for user.
    //
    glViewport(0, 0, contextWidth, contextHeight);
    mtxLoadIdentityf(p);
    bottom = 0.0f;
    top = (float)contextHeight;
    left = 0.0f;
    right = (float)contextWidth;
    mtxOrthof(p, left, right, bottom, top, -1.0f, 1.0f);
    mtxLoadIdentityf(m);
    
    EdenGLFontSetViewSize(right, top);
    EdenMessageSetViewSize(right, top);
    EdenMessageSetBoxParams(600.0f, 20.0f);
    float statusBarHeight = EdenGLFontGetHeight() + 4.0f; // 2 pixels above, 2 below.
  
    // Draw status bar with centred status message.
    if (statusBarMessage[0]) {
        [self drawBackgroundWidth:right height:statusBarHeight x:0.0f y:0.0f border:false projection:p];
        glStateCacheDisableBlend();
        EdenGLFontDrawLine(0, p, statusBarMessage, 0.0f, 2.0f, H_OFFSET_VIEW_CENTER_TO_TEXT_CENTER, V_OFFSET_VIEW_BOTTOM_TO_TEXT_BASELINE);
    }
    
    // If background tasks are proceeding, draw a status box.
    if (fileUploadHandle) {
        char uploadStatus[UPLOAD_STATUS_BUFFER_LEN];
        int status = fileUploaderStatusGet(fileUploadHandle, uploadStatus, &time);
        if (status > 0) {
            const int squareSize = (int)(16.0f * (float)gDisplayDPI / 160.f) ;
            float x, y, w, h;
            float textWidth = EdenGLFontGetLineWidth((unsigned char *)uploadStatus);
            w = textWidth + 3*squareSize + 2*4.0f /*text margin*/ + 2*4.0f /* box margin */;
            h = MAX(FONT_SIZE, 3*squareSize) + 2*4.0f /* box margin */;
            x = right - (w + 2.0f);
            y = statusBarHeight + 2.0f;
            [self drawBackgroundWidth:w height:h x:x y:y border:true projection:p];
            //        if (status == 1) drawBusyIndicator((int)(x + 4.0f + 1.5f*squareSize), (int)(y + 4.0f + 1.5f*squareSize), squareSize, &time);
            EdenGLFontDrawLine(0, p, (unsigned char *)uploadStatus, x + 4.0f + 3*squareSize, y + (h - FONT_SIZE)/2.0f, H_OFFSET_VIEW_LEFT_EDGE_TO_TEXT_LEFT_EDGE, V_OFFSET_VIEW_BOTTOM_TO_TEXT_BASELINE);
        }
    }
    
    // If a message should be onscreen, draw it.
    if (gEdenMessageDrawRequired) EdenMessageDraw(0, p);
}

// Save parameters file and index file with info about it, then signal thread that it's ready for upload.
- (void) saveParam2:(const ARParam *)param err_min:(ARdouble)err_min err_avg:(ARdouble)err_avg err_max:(ARdouble)err_max
{
    int i;
#define SAVEPARAM_PATHNAME_LEN MAXPATHLEN
    char indexPathname[SAVEPARAM_PATHNAME_LEN];
    char paramPathname[SAVEPARAM_PATHNAME_LEN];
    char indexUploadPathname[SAVEPARAM_PATHNAME_LEN];
    
    // Get the current time. It will be used for file IDs, plus a timestamp for the parameters file.
    time_t ourClock = time(NULL);
    if (ourClock == (time_t)-1) {
        ARLOGe("Error reading time and date.\n");
        return;
    }
    //struct tm *timeptr = localtime(&ourClock);
    struct tm *timeptr = gmtime(&ourClock);
    if (!timeptr) {
        ARLOGe("Error converting time and date to UTC.\n");
        return;
    }
    int ID = timeptr->tm_hour*10000 + timeptr->tm_min*100 + timeptr->tm_sec;
    
    // Save the parameter file.
    snprintf(paramPathname, SAVEPARAM_PATHNAME_LEN, "%s/%s/%06d-camera_para.dat", arUtilGetResourcesDirectoryPath(AR_UTIL_RESOURCES_DIRECTORY_BEHAVIOR_USE_APP_CACHE_DIR), QUEUE_DIR, ID);
    
    if (arParamSave(paramPathname, 1, param) < 0) {
        
        ARLOGe("Error writing camera_para.dat file.\n");
        
    } else {
        
        bool goodWrite = true;
        
        // Get main device identifier and focal length from video module.
        char *device_id = NULL;
        char *focal_length = NULL;
        
        AR2VideoParamT *vid = vs->getAR2VideoParam();
        if (ar2VideoGetParams(vid, AR_VIDEO_PARAM_DEVICEID, &device_id) < 0 || !device_id) {
            ARLOGe("Error fetching camera device identification.\n");
            goodWrite = false;
        }
        
        if (goodWrite) {
            if (vid->module == AR_VIDEO_MODULE_AVFOUNDATION) {
                int focalPreset;
                ar2VideoGetParami(vid, AR_VIDEO_PARAM_AVFOUNDATION_FOCUS_PRESET, &focalPreset);
                switch (focalPreset) {
                    case AR_VIDEO_AVFOUNDATION_FOCUS_MACRO:
                        focal_length = strdup("0.01");
                        break;
                    case AR_VIDEO_AVFOUNDATION_FOCUS_0_3M:
                        focal_length = strdup("0.3");
                        break;
                    case AR_VIDEO_AVFOUNDATION_FOCUS_1_0M:
                        focal_length = strdup("1.0");
                        break;
                    case AR_VIDEO_AVFOUNDATION_FOCUS_INF:
                        focal_length = strdup("1000000.0");
                        break;
                    default:
                        break;
                }
            }
            if (!focal_length) {
                // Not known at present, so just send 0.000.
                focal_length = strdup("0.000");
            }
        }
        
        if (goodWrite && gCalibrationSave) {

            // Assemble the filename.
            char calibrationSavePathname[SAVEPARAM_PATHNAME_LEN];
            char *tmp = arUtilGetResourcesDirectoryPath(AR_UTIL_RESOURCES_DIRECTORY_BEHAVIOR_USE_TMP_DIR);
            snprintf(calibrationSavePathname, SAVEPARAM_PATHNAME_LEN, "%s/camera_para-", tmp);
            free(tmp);
            size_t len = strlen(calibrationSavePathname);
            int i = 0;
            while (device_id[i] && (len + i + 2 < SAVEPARAM_PATHNAME_LEN)) {
                calibrationSavePathname[len + i] = (device_id[i] == '/' || device_id[i] == '\\' ? '_' : device_id[i]);
                i++;
            }
            calibrationSavePathname[len + i] = '\0';
            len = strlen(calibrationSavePathname);
            snprintf(&calibrationSavePathname[len], SAVEPARAM_PATHNAME_LEN - len, "-0-%dx%d", vs->getVideoWidth(), vs->getVideoHeight()); // camera_index is always 0 for desktop platforms.
            len = strlen(calibrationSavePathname);
            if (strcmp(focal_length, "0.000") != 0) {
                snprintf(&calibrationSavePathname[len], SAVEPARAM_PATHNAME_LEN - len, "-%s", focal_length);
                len = strlen(calibrationSavePathname);
            }
            snprintf(&calibrationSavePathname[len], SAVEPARAM_PATHNAME_LEN - len, ".dat");
    
            if (cp_f(paramPathname, calibrationSavePathname) != 0) {
                ARLOGe("Error saving calibration to '%s'", calibrationSavePathname);
                ARLOGperror(NULL);
            } else {
                [self showSaveCalibrationDialog:[NSString stringWithUTF8String:calibrationSavePathname]];
            }
        }
        
        // Check for early exit.
        if (!goodWrite || !gCalibrationServerUploadURL) {
            if (remove(paramPathname) < 0) {
                ARLOGe("Error removing temporary file '%s'.\n", paramPathname);
                ARLOGperror(NULL);
            }
            free(device_id);
            free(focal_length);
            return;
        };
        
        //
        // Write an upload index file with the data for the server database entry.
        //
        
        // Open the file.
        snprintf(indexPathname, SAVEPARAM_PATHNAME_LEN, "%s/%s/%06d-index", arUtilGetResourcesDirectoryPath(AR_UTIL_RESOURCES_DIRECTORY_BEHAVIOR_USE_APP_CACHE_DIR), QUEUE_DIR, ID);
        FILE *fp;
        if (!(fp = fopen(indexPathname, "wb"))) {
            ARLOGe("Error opening upload index file '%s'.\n", indexPathname);
            goodWrite = false;
        }
        
        // File name.
        if (goodWrite) fprintf(fp, "file,%s\n", paramPathname);
        
        // UTC date and time, in format "1999-12-31 23:59:59 UTC".
        if (goodWrite) {
            char timestamp[26+8] = "";
            if (!strftime(timestamp, sizeof(timestamp), "%Y-%m-%d %H:%M:%S +0000", timeptr)) { // Use explicit "+0000" rather than %z because %z is undefined either UTC or local time zone when timestamp is created with gmtime().
                ARLOGe("Error formatting time and date.\n");
                goodWrite = false;
            } else {
                fprintf(fp, "timestamp,%s\n", timestamp);
            }
        }
        
        // OS: name/arch/version.
        if (goodWrite) {
            char *os_name = arUtilGetOSName();
            char *os_arch = arUtilGetCPUName();
            char *os_version = arUtilGetOSVersion();
            fprintf(fp, "os_name,%s\nos_arch,%s\nos_version,%s\n", os_name, os_arch, os_version);
            free(os_name);
            free(os_arch);
            free(os_version);
        }
        
        // Camera identifier.
        if (goodWrite) {
            fprintf(fp, "device_id,%s\n", device_id);
        }
        
        // Focal length in metres.
        if (goodWrite) {
            fprintf(fp, "focal_length,%s\n", focal_length);
        }
        
        // Camera index.
        if (goodWrite) {
            char camera_index[12]; // 10 digits in INT32_MAX, plus sign, plus null.
            snprintf(camera_index, 12, "%d", 0); // Always zero for desktop platforms.
            fprintf(fp, "camera_index,%s\n", camera_index);
        }
        
        // Front or rear facing.
        if (goodWrite) {
            char camera_face[6]; // "front" or "rear", plus null.
            snprintf(camera_face, 6, "%s", (gCameraIsFrontFacing ? "front" : "rear"));
            fprintf(fp, "camera_face,%s\n", camera_face);
        }
        
        // Camera dimensions.
        if (goodWrite) {
            char camera_width[12]; // 10 digits in INT32_MAX, plus sign, plus null.
            char camera_height[12]; // 10 digits in INT32_MAX, plus sign, plus null.
            snprintf(camera_width, 12, "%d", vs->getVideoWidth());
            snprintf(camera_height, 12, "%d", vs->getVideoHeight());
            fprintf(fp, "camera_width,%s\n", camera_width);
            fprintf(fp, "camera_height,%s\n", camera_height);
        }
        
        // Calibration error.
        if (goodWrite) {
            char err_min_ascii[12];
            char err_avg_ascii[12];
            char err_max_ascii[12];
            snprintf(err_min_ascii, 12, "%f", err_min);
            snprintf(err_avg_ascii, 12, "%f", err_avg);
            snprintf(err_max_ascii, 12, "%f", err_max);
            fprintf(fp, "err_min,%s\n", err_min_ascii);
            fprintf(fp, "err_avg,%s\n", err_avg_ascii);
            fprintf(fp, "err_max,%s\n", err_max_ascii);
        }
        
        // IP address will be derived from connect.
        
        // Hash the shared secret.
        if (goodWrite) {
            unsigned char ss_md5[MD5_DIGEST_LENGTH];
            char ss_ascii[MD5_DIGEST_LENGTH*2 + 1]; // space for null terminator.
            if (!MD5((unsigned char *)gCalibrationServerAuthenticationToken, (MD5_COUNT_t)strlen(gCalibrationServerAuthenticationToken), ss_md5)) {
                ARLOGe("Error calculating md5.\n");
                goodWrite = false;
            } else {
                for (i = 0; i < MD5_DIGEST_LENGTH; i++) snprintf(&(ss_ascii[i*2]), 3, "%.2hhx", ss_md5[i]);
                fprintf(fp, "ss,%s\n", ss_ascii);
            }
        }
        
        // Done writing index file.
        fclose(fp);
        
        if (goodWrite) {
            // Rename the file with QUEUE_INDEX_FILE_EXTENSION file extension so it's picked up in uploader.
            snprintf(indexUploadPathname, SAVEPARAM_PATHNAME_LEN, "%s." QUEUE_INDEX_FILE_EXTENSION, indexPathname);
            if (rename(indexPathname, indexUploadPathname) < 0) {
                ARLOGe("Error renaming temporary file '%s'.\n", indexPathname);
                goodWrite = false;
            } else {
                // Kick off an upload handling cycle.
                fileUploaderTickle(fileUploadHandle);
            }
        }
        
        if (!goodWrite) {
            // If something went wrong, delete the index and param files.
            if (remove(indexPathname) < 0) {
                ARLOGe("Error removing temporary file '%s'.\n", indexPathname);
                ARLOGperror(NULL);
            }
            if (remove(paramPathname) < 0) {
                ARLOGe("Error removing temporary file '%s'.\n", paramPathname);
                ARLOGperror(NULL);
            }
        }
        
        free(device_id);
        free(focal_length);
    }
}

#pragma mark - User interaction methods.

- (IBAction)handleBackButton:(id)sender {
    flowHandleEvent(EVENT_BACK_BUTTON);
}

- (IBAction)handleAddButton:(id)sender {
    flowHandleEvent(EVENT_TOUCH);
}

- (IBAction)handleMenuButton:(id)sender {
    UIAlertController *alertController = [UIAlertController alertControllerWithTitle:nil message:nil preferredStyle:UIAlertControllerStyleActionSheet];
    [alertController addAction:[UIAlertAction actionWithTitle:@"Cancel" style:UIAlertActionStyleCancel handler:nil]];
    [alertController addAction:[UIAlertAction actionWithTitle:@"Settings" style:UIAlertActionStyleDefault handler:
                                ^(UIAlertAction *action) {
                                    [self presentViewController:[[SettingsViewController alloc] init] animated:YES completion:nil];
                                }
                                ]];
    [alertController addAction:[UIAlertAction actionWithTitle:@"Help" style:UIAlertActionStyleDefault handler:
                                ^(UIAlertAction *action) {
                                    [[UIApplication sharedApplication] openURL:[NSURL URLWithString:@"https://github.com/artoolkit/ar6-wiki/wiki/Camera-calibration-iOS"]];
                                }
                                ]];
    [alertController addAction:[UIAlertAction actionWithTitle:@"Print" style:UIAlertActionStyleDefault handler:
                                ^(UIAlertAction *action) {
                                    [self showPrintDialog];
                                }
                                ]];
    alertController.modalPresentationStyle = UIModalPresentationPopover;
    [alertController.popoverPresentationController setBarButtonItem:self.menuButton];
    [self presentViewController:alertController animated:YES completion:nil];
}

- (void)showSaveCalibrationDialog:(NSString *)paramFilePath
{
    NSURL *paramFileURL = [NSURL fileURLWithPath:paramFilePath];
    
    if (!self.docInteractionController) {
        self.docInteractionController = [UIDocumentInteractionController interactionControllerWithURL:paramFileURL];
        self.docInteractionController.delegate = self;
    } else {
        self.docInteractionController.URL = paramFileURL;
    }
    [self.docInteractionController presentOptionsMenuFromBarButtonItem:self.menuButton animated:YES];
}

- (void)showPrintDialog
{
    NSString *pdfFileName = @"printa4";
    NSString *paperSizeStr;
    if ([[NSUserDefaults standardUserDefaults] objectForKey:kSettingPaperSizeStr]) {
        paperSizeStr = [[NSUserDefaults standardUserDefaults] objectForKey:kSettingPaperSizeStr];
        if ([paperSizeStr isEqualToString:kPaperSizeUSLetterStr]) pdfFileName = @"printusletter";
    }
    
    NSURL *pagesURL = [NSURL fileURLWithPath:[[NSBundle mainBundle] pathForResource:pdfFileName ofType:@"pdf"]];
    
    if (!self.docInteractionController) {
        self.docInteractionController = [UIDocumentInteractionController interactionControllerWithURL:pagesURL];
        self.docInteractionController.delegate = self;
    } else {
        self.docInteractionController.URL = pagesURL;
    }
    [self.docInteractionController presentOptionsMenuFromBarButtonItem:self.menuButton animated:YES];
}

// Called when action has been taken, including when QuickLook has been selected.
- (void)documentInteractionControllerDidDismissOptionsMenu:(UIDocumentInteractionController *)controller
{
     self.docInteractionController = nil;
}

- (void)documentInteractionController:(UIDocumentInteractionController *)controller willBeginSendingToApplication:(NSString *)application
{
    
}

- (void)documentInteractionController:(UIDocumentInteractionController *)controller didEndSendingToApplication:(NSString *)application
{
    
}

- (UIViewController *)documentInteractionControllerViewControllerForPreview:(UIDocumentInteractionController *)controller
{
    return (self);
}

- (UIView *)documentInteractionControllerViewForPreview:(UIDocumentInteractionController *)controller
{
    return (self.view);
}

// Called when user chooses "Done" from QuickLook.
- (void)documentInteractionControllerDidEndPreview:(UIDocumentInteractionController *)controller
{
    
}

+ (void)displayToastWithMessage:(NSString *)toastMessage
{
    [[NSOperationQueue mainQueue] addOperationWithBlock:^ {
        UIWindow * keyWindow = [[UIApplication sharedApplication] keyWindow];
        UILabel *toastView = [[UILabel alloc] init];
        toastView.text = toastMessage;
        toastView.font = [UIFont fontWithName:@"Helvetica" size:14.0f];
        toastView.textColor = [UIColor whiteColor];
        toastView.backgroundColor = [[UIColor blackColor] colorWithAlphaComponent:0.9];
        toastView.textAlignment = NSTextAlignmentCenter;
        toastView.frame = CGRectMake(0.0f, 0.0f, keyWindow.frame.size.width/2.0f, 28.0f);
        toastView.layer.cornerRadius = 7.0f;
        toastView.layer.masksToBounds = YES;
        //toastView.center = keyWindow.center;
        toastView.center = CGPointMake(keyWindow.center.x, keyWindow.frame.size.height*0.85f);
        
        [keyWindow addSubview:toastView];
        
        [UIView animateWithDuration: 3.0f
                              delay: 0.0
                            options: UIViewAnimationOptionCurveEaseOut
                         animations: ^{
                             toastView.alpha = 0.0;
                         }
                         completion: ^(BOOL finished) {
                             [toastView removeFromSuperview];
                         }
         ];
    }];
}

@end

// Save parameters file and index file with info about it, then signal thread that it's ready for upload.
static void saveParam(const ARParam *param, ARdouble err_min, ARdouble err_avg, ARdouble err_max, void *userdata)
{
    if (userdata) {
        ARViewController *vc = (__bridge ARViewController *)userdata;
        [vc saveParam2:param err_min:err_min err_avg:err_avg err_max:err_max];
    }
}
