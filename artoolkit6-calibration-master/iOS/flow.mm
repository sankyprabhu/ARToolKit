/*
 *  flow.mm
 *  ARToolKit6
 *
 *  This file is part of ARToolKit.
 *
 *  Copyright 2015-2017 Daqri LLC. All Rights Reserved.
 *  Copyright 2013-2015 ARToolworks, Inc. All Rights Reserved.
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

#define _GNU_SOURCE // asprintf()

#include "flow.hpp"

#include <stdio.h> // asprintf()
#include <pthread.h>
#include <Eden/EdenMessage.h>
#include <AR6/AR/ar.h>

#import <Foundation/Foundation.h>

//
// Globals.
//

static bool gInited = false;
static FLOW_STATE gState = FLOW_STATE_NOT_INITED;
static pthread_mutex_t gStateLock;
static pthread_mutex_t gEventLock;
static pthread_cond_t gEventCond;
static EVENT_t gEvent = EVENT_NONE;
static EVENT_t gEventMask = EVENT_NONE;
static pthread_t gThread;
static int gThreadExitStatus;
static bool gStop;

// Completion callback.
static FLOW_CALLBACK_t gCallback = NULL;
static void *gCallbackUserdata = NULL;

// Logging macros
#define  LOG_TAG    "flow"

// Status bar.
#define STATUS_BAR_MESSAGE_BUFFER_LEN 128
unsigned char statusBarMessage[STATUS_BAR_MESSAGE_BUFFER_LEN] = "";

// Calibration inputs.
static Calibration *gFlowCalib = nullptr;


//
// Function prototypes.
//

static void *flowThread(void *arg);
static void flowSetEventMask(const EVENT_t eventMask);

//
// Functions.
//

bool flowInitAndStart(Calibration *calib, FLOW_CALLBACK_t callback, void *callback_userdata)
{
    pthread_mutex_init(&gStateLock, NULL);
    pthread_mutex_init(&gEventLock, NULL);
    pthread_cond_init(&gEventCond, NULL);

    // Calibration inputs.
    gFlowCalib = calib;

    // Completion callback.
    gCallback = callback;
    gCallbackUserdata = callback_userdata;

    gStop = false;
    pthread_create(&gThread, NULL, flowThread, NULL);

    gInited = true;

    return (true);
}

bool flowStopAndFinal()
{
	void *exit_status_p;		 // Pointer to return value from thread, will be filled in by pthread_join().

	if (!gInited) return (false);

	// Request stop and wait for join.
	gStop = true;
#ifndef ANDROID
	pthread_cancel(gThread); // Not implemented on Android.
#endif
#ifdef DEBUG
	ARLOGi("flowStopAndFinal(): Waiting for flowThread() to exit...\n");
#endif
	pthread_join(gThread, &exit_status_p);
#ifdef DEBUG
#  ifndef ANDROID
	ARLOGi("  done. Exit status was %d.\n",((exit_status_p == PTHREAD_CANCELED) ? 0 : *(int *)(exit_status_p))); // Contents of gThreadExitStatus.
#  else
	ARLOGi("  done. Exit status was %d.\n", *(int *)(exit_status_p)); // Contents of gThreadExitStatus.
#  endif
#endif
    
    gFlowCalib = nullptr;

	// Clean up.
	pthread_mutex_destroy(&gStateLock);
	pthread_mutex_destroy(&gEventLock);
	pthread_cond_destroy(&gEventCond);
	gState = FLOW_STATE_NOT_INITED;
	gInited = false;

	return true;
}

FLOW_STATE flowStateGet()
{
	FLOW_STATE ret;

	if (!gInited) return (FLOW_STATE_NOT_INITED);

	pthread_mutex_lock(&gStateLock);
	ret = gState;
	pthread_mutex_unlock(&gStateLock);
	return (ret);
}

static void flowStateSet(FLOW_STATE state)
{
	if (!gInited) return;

	pthread_mutex_lock(&gStateLock);
	gState = state;
	pthread_mutex_unlock(&gStateLock);
}

static void flowSetEventMask(const EVENT_t eventMask)
{
	pthread_mutex_lock(&gEventLock);
	gEventMask = eventMask;
	pthread_mutex_unlock(&gEventLock);
}

bool flowHandleEvent(const EVENT_t event)
{
	bool ret;

	if (!gInited) return false;

	pthread_mutex_lock(&gEventLock);
	if ((event & gEventMask) == EVENT_NONE) {
		ret = false; // not handled (discarded).
	} else {
		gEvent = event;
		pthread_cond_signal(&gEventCond);
		ret = true;
	}
	pthread_mutex_unlock(&gEventLock);

	return (ret);
}

static EVENT_t flowWaitForEvent(void)
{
	EVENT_t ret;

	pthread_mutex_lock(&gEventLock);
	while (gEvent == EVENT_NONE && !gStop) {
#ifdef ANDROID
        // Android "Bionic" libc doesn't implement cancelation, so need to let wait expire somewhat regularly.
        const struct timespec twoSeconds = {2, 0};
        pthread_cond_timedwait_relative_np(&gEventCond, &gEventLock, &twoSeconds);
#else
		pthread_cond_wait(&gEventCond, &gEventLock);
#endif
	}
	ret = gEvent;
	gEvent = EVENT_NONE; // Clear wait state.
	pthread_mutex_unlock(&gEventLock);

	return (ret);
}

static void flowThreadCleanup(void *arg)
{
	pthread_mutex_unlock(&gStateLock);
    // Clear status bar.
    statusBarMessage[0] = '\0';
}

static void *flowThread(void *arg)
{
	bool captureDoneSinceBackButtonLastPressed;
	EVENT_t event;
	// TYPE* TYPE_INSTANCE = (TYPE *)arg; // Cast the thread start arg to the correct type.

    ARLOGi("Start flow thread.\n");

    // Register our cleanup function, with no arg.
	pthread_cleanup_push(flowThreadCleanup, NULL);

	// Welcome.
	flowStateSet(FLOW_STATE_WELCOME);

	while (!gStop) {

		if (flowStateGet() == FLOW_STATE_WELCOME) {
			EdenMessageShow((const unsigned char *)NSLocalizedString(@"Intro",@"Welcome message for first run").UTF8String);
		} else {
			EdenMessageShow((const unsigned char *)NSLocalizedString(@"Reintro",@"Welcome message for subsequent runs").UTF8String);
		}
		flowSetEventMask((EVENT_t)(EVENT_TOUCH | EVENT_MODAL));
		event = flowWaitForEvent();
		if (gStop) break;
        
        if (event == EVENT_MODAL) {
            flowSetEventMask(EVENT_MODAL);
            event = flowWaitForEvent();
            continue;
        } else {
            EdenMessageHide();
        }

		// Start capturing.
		captureDoneSinceBackButtonLastPressed = false;
		flowStateSet(FLOW_STATE_CAPTURING);
		flowSetEventMask((EVENT_t)(EVENT_TOUCH|EVENT_BACK_BUTTON));

		do {
			snprintf((char *)statusBarMessage, STATUS_BAR_MESSAGE_BUFFER_LEN, NSLocalizedString(@"CalibCapturing",@"Message during image capture").UTF8String, gFlowCalib->calibImageCount() + 1, gFlowCalib->calibImageCountMax());
			event = flowWaitForEvent();
			if (gStop) break;
			if (event == EVENT_TOUCH) {

				if (gFlowCalib->capture()) {
			    	captureDoneSinceBackButtonLastPressed = true;
				}

			} else if (event == EVENT_BACK_BUTTON) {

				if (!captureDoneSinceBackButtonLastPressed) {
                    gFlowCalib->uncaptureAll();
                    break;
				} else {
					gFlowCalib->uncapture();
				}
				captureDoneSinceBackButtonLastPressed = false;
			}

		} while (gFlowCalib->calibImageCount() < gFlowCalib->calibImageCountMax());

		// Clear status bar.
		statusBarMessage[0] = '\0';

		if (gFlowCalib->calibImageCount() < gFlowCalib->calibImageCountMax()) {

			flowSetEventMask(EVENT_TOUCH);
            flowStateSet(FLOW_STATE_DONE);
			EdenMessageShow((const unsigned char *)NSLocalizedString(@"CalibCanceled",@"Message when user cancels a calibration run.").UTF8String);
			flowWaitForEvent();
			if (gStop) break;
			EdenMessageHide();

		} else {
			ARParam param;
			ARdouble err_min, err_avg, err_max;

			flowSetEventMask(EVENT_NONE);
			flowStateSet(FLOW_STATE_CALIBRATING);
			EdenMessageShow((const unsigned char *)NSLocalizedString(@"CalibCalculating",@"Message during calibration calculation.").UTF8String);
			gFlowCalib->calib(&param, &err_min, &err_avg, &err_max);
    		EdenMessageHide();

            if (gCallback) (*gCallback)(&param, err_min, err_avg, err_max, gCallbackUserdata);
            gFlowCalib->uncaptureAll(); // prepare for next run.

			// Calibration complete. Post results as status.
			flowSetEventMask(EVENT_TOUCH);
			flowStateSet(FLOW_STATE_DONE);
			unsigned char *buf;
			asprintf((char **)&buf, NSLocalizedString(@"CalibResults",@"Message when user completes a calibration run.").UTF8String, err_min, err_avg, err_max);
			EdenMessageShow(buf);
			free(buf);
			flowWaitForEvent();
			if (gStop) break;
			EdenMessageHide();

		}

		//pthread_testcancel(); // Not implemented on Android.
	} // while (!gStop);
    
	pthread_cleanup_pop(1); // Unlocks gStateLock.

    ARLOGi("End flow thread.\n");

	gThreadExitStatus = 1; // Put the exit status into a global
	return (&gThreadExitStatus); // Pass a pointer to the global as our exit status.
}


