/*
 *  ARControllerEditor.cs
 *  ARToolKit for Unity
 *
 *  This file is part of ARToolKit for Unity.
 *
 *  ARToolKit for Unity is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ARToolKit for Unity is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with ARToolKit for Unity.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  As a special exception, the copyright holders of this library give you
 *  permission to link this library with independent modules to produce an
 *  executable, regardless of the license terms of these independent modules, and to
 *  copy and distribute the resulting executable under terms of your choice,
 *  provided that you also meet, for each linked independent module, the terms and
 *  conditions of the license of that module. An independent module is a module
 *  which is neither derived from nor based on this library. If you modify this
 *  library, you may extend this exception to your version of the library, but you
 *  are not obligated to do so. If you do not wish to do so, delete this exception
 *  statement from your version.
 *
 *  Copyright 2015-2016 Daqri, LLC.
 *  Copyright 2010-2015 ARToolworks, Inc.
 *
 *  Author(s): Philip Lamb, Julian Looser, Wally Young
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ARController))]
public class ARControllerEditor : Editor {
	private bool     showVideoConfiguration    = false;
	private bool     showVideoOptions          = false;
    private bool     showSquareTrackingOptions = false;
	private bool     showTwoDTrackingOptions   = false;
	private bool     showApplicationOptions    = false;
	private string[] cameras                   = null;
    private int      cameraCount               = 0;

    public override void OnInspectorGUI() {
        ARController arController = (ARController)target;
        if (arController == null) {
			return;
		}

        EditorGUILayout.LabelField("Version", "ARToolKit " + arController.Version);

        EditorGUILayout.Separator();
        if (cameras == null) cameras = ARToolKitAssetManager.GetCameras();
        cameraCount = (cameras == null ? 0 : cameras.Length);
            
		if (!arController.VideoIsStereo) {
            if (cameraCount <= 0) arController.videoCparamOverride0 = false;
            EditorGUI.BeginDisabledGroup(cameraCount <= 0);
            arController.videoCparamOverride0 = !EditorGUILayout.Toggle("Use automatic camera parameters", !arController.videoCparamOverride0);
            EditorGUI.EndDisabledGroup();
            if (!arController.videoCparamOverride0) {
                arController.videoCParamName0 = "";
            } else {
                arController.EditorCameraIndex = EditorGUILayout.Popup("Camera Parameter", Math.Min(cameraCount - 1, arController.EditorCameraIndex), cameras);
                if (string.Compare(cameras[arController.EditorCameraIndex], arController.videoCParamName0, StringComparison.Ordinal) != 0) {
                    arController.videoCParamName0 = cameras[arController.EditorCameraIndex];
                }
            }
            if (cameraCount <= 0) EditorGUILayout.HelpBox("No camera parameters found.", MessageType.Info);
		} else {
            if (cameraCount <= 0) arController.videoCparamOverride0 = false;
            EditorGUI.BeginDisabledGroup(cameraCount <= 0);
            arController.videoCparamOverride0 = !EditorGUILayout.Toggle("Use automatic camera parameters (L)", !arController.videoCparamOverride0);
            EditorGUI.EndDisabledGroup();
            if (!arController.videoCparamOverride0) {
                arController.videoCParamName0 = "";
            } else {
                arController.EditorCameraIndex = EditorGUILayout.Popup("Camera Parameter (L)", Math.Min(cameraCount - 1, arController.EditorCameraIndex), cameras);
                if (string.Compare(cameras[arController.EditorCameraIndex], arController.videoCParamName0, StringComparison.Ordinal) != 0) {
                    arController.videoCParamName0 = cameras[arController.EditorCameraIndex];
                }
            }
            if (cameraCount <= 0) arController.videoCparamOverride1 = false;
            EditorGUI.BeginDisabledGroup(cameraCount <= 0);
            arController.videoCparamOverride1 = !EditorGUILayout.Toggle("Use automatic camera parameters (R)", !arController.videoCparamOverride1);
            EditorGUI.EndDisabledGroup();
            if (!arController.videoCparamOverride1) {
                arController.videoCParamName1 = "";
            } else {
                arController.EditorCameraIndexR = EditorGUILayout.Popup("Camera Parameter (R)", Math.Max(cameraCount - 1, arController.EditorCameraIndexR), cameras);
                if (string.Compare(cameras[arController.EditorCameraIndexR], arController.videoCParamName1, StringComparison.Ordinal) != 0) {
                    arController.videoCParamName1 = cameras[arController.EditorCameraIndexR];
                }
            }
            if (cameraCount <= 0) EditorGUILayout.HelpBox("No camera parameters found.", MessageType.Info);
		}

		showVideoConfiguration = EditorGUILayout.Foldout(showVideoConfiguration, "Video Configuration");
		if (showVideoConfiguration) {
			if (!arController.VideoIsStereo) {
				arController.videoConfigurationAndroid0      = EditorGUILayout.TextField("Android",           arController.videoConfigurationAndroid0);
				arController.videoConfigurationiOS0          = EditorGUILayout.TextField("iOS",               arController.videoConfigurationiOS0);
				arController.videoConfigurationLinux0        = EditorGUILayout.TextField("Linux",             arController.videoConfigurationLinux0);
				arController.videoConfigurationMacOSX0       = EditorGUILayout.TextField("Mac OS X",          arController.videoConfigurationMacOSX0);
				arController.videoConfigurationWindows0      = EditorGUILayout.TextField("Windows",           arController.videoConfigurationWindows0);
				arController.videoConfigurationWindowsStore0 = EditorGUILayout.TextField("Windows Store",     arController.videoConfigurationWindowsStore0);
			} else {
				arController.videoConfigurationAndroid0      = EditorGUILayout.TextField("Android (L)",       arController.videoConfigurationAndroid0);
				arController.videoConfigurationAndroid1      = EditorGUILayout.TextField("Android (R)",       arController.videoConfigurationAndroid1);
				arController.videoConfigurationiOS0          = EditorGUILayout.TextField("iOS (L)",           arController.videoConfigurationiOS0);
				arController.videoConfigurationiOS1          = EditorGUILayout.TextField("iOS (R)",           arController.videoConfigurationiOS1);
				arController.videoConfigurationLinux0        = EditorGUILayout.TextField("Linux (L)",         arController.videoConfigurationLinux0);
				arController.videoConfigurationLinux1        = EditorGUILayout.TextField("Linux (R)",         arController.videoConfigurationLinux1);
				arController.videoConfigurationMacOSX0       = EditorGUILayout.TextField("OS X (L)",          arController.videoConfigurationMacOSX0);
				arController.videoConfigurationMacOSX1       = EditorGUILayout.TextField("OS X (R)",          arController.videoConfigurationMacOSX1);
				arController.videoConfigurationWindows0      = EditorGUILayout.TextField("Windows (L)",       arController.videoConfigurationWindows0);
				arController.videoConfigurationWindows1      = EditorGUILayout.TextField("Windows (R)",       arController.videoConfigurationWindows1);
				arController.videoConfigurationWindowsStore0 = EditorGUILayout.TextField("Windows Store (L)", arController.videoConfigurationWindowsStore0);
				arController.videoConfigurationWindowsStore1 = EditorGUILayout.TextField("Windows Store (R)", arController.videoConfigurationWindowsStore1);
			}
            arController.VideoIsStereo = EditorGUILayout.Toggle("Stereo Video Input", arController.VideoIsStereo);
            EditorGUILayout.HelpBox("Check this option if you plan to use two cameras to track the environment. This is not stereoscopic rendering. Note: You will have to configure both left (L) and right (R) cameras separately.", MessageType.Info);
		}

		showVideoOptions = EditorGUILayout.Foldout(showVideoOptions, "Video Background");
		if (showVideoOptions) {
			arController.BackgroundLayer0 = EditorGUILayout.LayerField("Background Layer", arController.BackgroundLayer0);

			ContentMode currentContentMode = arController.ContentMode;
			ContentMode newContentMode = (ContentMode)EditorGUILayout.EnumPopup("Content Mode", currentContentMode);
			if (newContentMode != currentContentMode) {
				arController.ContentMode = newContentMode;
			}
			arController.ContentRotate90 = EditorGUILayout.Toggle("Rotate 90° Clockwise", arController.ContentRotate90);
			arController.ContentFlipV    = EditorGUILayout.Toggle("Flip Vertically",      arController.ContentFlipV);
			arController.ContentFlipH    = EditorGUILayout.Toggle("Flip Horizontally",    arController.ContentFlipH);
		}

		showTwoDTrackingOptions = EditorGUILayout.Foldout(showTwoDTrackingOptions, "2D Tracking Options");
		if (showTwoDTrackingOptions) {
            int[] values = {1, 2, 3, 4, 5, 6, 7, 8};
            String[] names = {"1", "2", "3", "4", "5", "6", "7", "8"};
            arController.TwoDTrackerMaxMarkerCount = EditorGUILayout.IntPopup("Maximum simultaneous trackers:", arController.TwoDTrackerMaxMarkerCount, names, values);
		}

        showSquareTrackingOptions = EditorGUILayout.Foldout(showSquareTrackingOptions, "Square Tracking Options");
        if (showSquareTrackingOptions) {
            // Threshold mode selection
            ARController.ARToolKitThresholdMode currentThreshMode = arController.VideoThresholdMode;
            ARController.ARToolKitThresholdMode newThreshMode = (ARController.ARToolKitThresholdMode)EditorGUILayout.EnumPopup("Mode", currentThreshMode);
            if (newThreshMode != currentThreshMode) {
                arController.VideoThresholdMode = newThreshMode;
            }
            EditorGUILayout.HelpBox(ARController.ThresholdModeDescriptions[newThreshMode], MessageType.Info);

            // Show threshold slider only in manual or bracketing modes.
            if (newThreshMode == ARController.ARToolKitThresholdMode.Manual || newThreshMode == ARController.ARToolKitThresholdMode.Bracketing) {
                int currentThreshold = arController.VideoThreshold;
                int newThreshold = EditorGUILayout.IntSlider("Threshold", currentThreshold, 0, 255);
                if (newThreshold != currentThreshold) {
                    arController.VideoThreshold = newThreshold;
                }
            }
        }

		showApplicationOptions = EditorGUILayout.Foldout(showApplicationOptions, "Additional Options");
		if (showApplicationOptions) {
			arController.AutoStartAR = EditorGUILayout.Toggle("Auto-Start AR.", arController.AutoStartAR);
			if (arController.AutoStartAR) {
				EditorGUILayout.HelpBox("ARController.StartAR() will be called during MonoBehavior.Start().", MessageType.Info);
			} else {
				EditorGUILayout.HelpBox("ARController.StartAR() will not be called during MonoBehavior.Start(); you must call it yourself.", MessageType.Warning);
			}

			arController.QuitOnEscOrBack = EditorGUILayout.Toggle("Quit on [Esc].", arController.QuitOnEscOrBack);
			if (arController.QuitOnEscOrBack) {
				EditorGUILayout.HelpBox("The [esc] key (Windows, OS X) or the [Back] button (Android) will quit the app.", MessageType.Info);
			} else {
				EditorGUILayout.HelpBox("The [esc] key (Windows, OS X) or the [Back] button (Android) will be ignored.", MessageType.Warning);
			}

			arController.LogLevel = (ARController.AR_LOG_LEVEL)EditorGUILayout.EnumPopup("Native Log Level", arController.LogLevel);
		}
    }
}