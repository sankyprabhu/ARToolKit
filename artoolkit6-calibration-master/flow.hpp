/*
 *  flow.hpp
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

#pragma once

#include "Calibration.hpp"

extern unsigned char statusBarMessage[];

// Called when the flow has completed and generated a calibration.
typedef void (*FLOW_CALLBACK_t)(const ARParam *param, ARdouble err_min, ARdouble err_avg, ARdouble err_max, void *userdata);

typedef enum {
	FLOW_STATE_NOT_INITED = 0,
	FLOW_STATE_WELCOME,
	FLOW_STATE_CAPTURING,
	FLOW_STATE_CALIBRATING,
	FLOW_STATE_DONE
} FLOW_STATE;

typedef enum {
	EVENT_NONE = 0,
	EVENT_TOUCH = 1,
	EVENT_BACK_BUTTON = 2,
    EVENT_MODAL = 4
} EVENT_t;

bool flowInitAndStart(Calibration *calib, FLOW_CALLBACK_t callback, void *callback_userdata);

FLOW_STATE flowStateGet();

bool flowHandleEvent(const EVENT_t event);

bool flowStopAndFinal();
