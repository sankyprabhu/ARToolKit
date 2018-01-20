/*
 *  PluginFunctions.cs
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
using System.Runtime.InteropServices;
using UnityEngine;

public static class PluginFunctions
{
	[NonSerialized]
	public static bool inited = false;

	// Delegate type declaration.
	public delegate void LogCallback([MarshalAs(UnmanagedType.LPStr)] string msg);

	// Delegate instance.
	private static LogCallback logCallback = null;
	private static GCHandle logCallbackGCH;

	public static void arwRegisterLogCallback(LogCallback lcb)
	{
        logCallback = lcb;
		if (lcb != null) {
			logCallbackGCH = GCHandle.Alloc(logCallback); // Does not need to be pinned, see http://stackoverflow.com/a/19866119/316487 
		}
		if (Application.platform == RuntimePlatform.IPhonePlayer) ARNativePluginStatic.arwRegisterLogCallback(logCallback);
		else ARNativePlugin.arwRegisterLogCallback(logCallback);
		if (lcb == null) {
			logCallbackGCH.Free();
		}
	}

	public static void arwSetLogLevel(int logLevel)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) ARNativePluginStatic.arwSetLogLevel(logLevel);
		else ARNativePlugin.arwSetLogLevel(logLevel);
	}

	public static bool arwInitialiseAR(int pattSize = 16, int pattCountMax = 25)
	{
		bool ok;
		if (Application.platform == RuntimePlatform.IPhonePlayer) ok = ARNativePluginStatic.arwInitialiseARWithOptions(pattSize, pattCountMax);
		else ok = ARNativePlugin.arwInitialiseARWithOptions(pattSize, pattCountMax);
		if (ok) PluginFunctions.inited = true;
		return ok;
	}
	
	public static string arwGetARToolKitVersion()
	{
		StringBuilder sb = new StringBuilder(128);
		bool ok;
		if (Application.platform == RuntimePlatform.IPhonePlayer) ok = ARNativePluginStatic.arwGetARToolKitVersion(sb, sb.Capacity);
		else ok = ARNativePlugin.arwGetARToolKitVersion(sb, sb.Capacity);
		if (ok) return sb.ToString();
		else return "unknown";
	}

	public static int arwGetError()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetError();
		else return ARNativePlugin.arwGetError();
	}

    public static bool arwShutdownAR()
	{
		bool ok;
		if (Application.platform == RuntimePlatform.IPhonePlayer) ok = ARNativePluginStatic.arwShutdownAR();
		else ok = ARNativePlugin.arwShutdownAR();
		if (ok) PluginFunctions.inited = false;
		return ok;
	}
	
	public static bool arwStartRunningB(string vconf, byte[] cparaBuff, int cparaBuffLen, float nearPlane, float farPlane)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwStartRunningB(vconf, cparaBuff, cparaBuffLen, nearPlane, farPlane);
		else return ARNativePlugin.arwStartRunningB(vconf, cparaBuff, cparaBuffLen, nearPlane, farPlane);
	}
	
	public static bool arwStartRunningStereoB(string vconfL, byte[] cparaBuffL, int cparaBuffLenL, string vconfR, byte[] cparaBuffR, int cparaBuffLenR, byte[] transL2RBuff, int transL2RBuffLen, float nearPlane, float farPlane)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwStartRunningStereoB(vconfL, cparaBuffL, cparaBuffLenL, vconfR, cparaBuffR, cparaBuffLenR, transL2RBuff, transL2RBuffLen, nearPlane, farPlane);
		else return ARNativePlugin.arwStartRunningStereoB(vconfL, cparaBuffL, cparaBuffLenL, vconfR, cparaBuffR, cparaBuffLenR, transL2RBuff, transL2RBuffLen, nearPlane, farPlane);
	}

	public static bool arwIsRunning()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwIsRunning();
		else return ARNativePlugin.arwIsRunning();
	}

	public static bool arwStopRunning()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwStopRunning();
		else return ARNativePlugin.arwStopRunning();
	}

	public static bool arwGetProjectionMatrix(float[] matrix)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetProjectionMatrix(matrix);
		else return ARNativePlugin.arwGetProjectionMatrix(matrix);
	}

	public static bool arwGetProjectionMatrixStereo(float[] matrixL, float[] matrixR)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetProjectionMatrixStereo(matrixL, matrixR);
		else return ARNativePlugin.arwGetProjectionMatrixStereo(matrixL, matrixR);
	}

	public static bool arwGetVideoParams(out int width, out int height, out int pixelSize, out String pixelFormatString)
	{
		StringBuilder sb = new StringBuilder(128);
		bool ok;
		if (Application.platform == RuntimePlatform.IPhonePlayer) ok = ARNativePluginStatic.arwGetVideoParams(out width, out height, out pixelSize, sb, sb.Capacity);
		else ok = ARNativePlugin.arwGetVideoParams(out width, out height, out pixelSize, sb, sb.Capacity);
		if (!ok) pixelFormatString = "";
		else pixelFormatString = sb.ToString();
		return ok;
	}

	public static bool arwGetVideoParamsStereo(out int widthL, out int heightL, out int pixelSizeL, out String pixelFormatL, out int widthR, out int heightR, out int pixelSizeR, out String pixelFormatR)
	{
		StringBuilder sbL = new StringBuilder(128);
		StringBuilder sbR = new StringBuilder(128);
		bool ok;
		if (Application.platform == RuntimePlatform.IPhonePlayer) ok = ARNativePluginStatic.arwGetVideoParamsStereo(out widthL, out heightL, out pixelSizeL, sbL, sbL.Capacity, out widthR, out heightR, out pixelSizeR, sbR, sbR.Capacity);
		else ok = ARNativePlugin.arwGetVideoParamsStereo(out widthL, out heightL, out pixelSizeL, sbL, sbL.Capacity, out widthR, out heightR, out pixelSizeR, sbR, sbR.Capacity);
		if (!ok) {
			pixelFormatL = "";
			pixelFormatR = "";
		} else {
			pixelFormatL = sbL.ToString();
			pixelFormatR = sbR.ToString();
		}
		return ok;
	}

	public static bool arwCapture()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwCapture();
		else return ARNativePlugin.arwCapture();
	}

	public static bool arwUpdateAR()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwUpdateAR();
		else return ARNativePlugin.arwUpdateAR();
	}
	
	public static bool arwUpdateTexture32([In, Out]Color32[] colors32)
	{
		bool ok;
		GCHandle handle = GCHandle.Alloc(colors32, GCHandleType.Pinned);
		IntPtr address = handle.AddrOfPinnedObject();
		if (Application.platform == RuntimePlatform.IPhonePlayer) ok = ARNativePluginStatic.arwUpdateTexture32(address);
		else ok = ARNativePlugin.arwUpdateTexture32(address);
		handle.Free();
		return ok;
	}
	
	public static bool arwUpdateTexture32Stereo([In, Out]Color32[] colors32L, [In, Out]Color32[] colors32R)
	{
		bool ok;
		GCHandle handle0 = GCHandle.Alloc(colors32L, GCHandleType.Pinned);
		GCHandle handle1 = GCHandle.Alloc(colors32R, GCHandleType.Pinned);
		IntPtr address0 = handle0.AddrOfPinnedObject();
		IntPtr address1 = handle1.AddrOfPinnedObject();
		if (Application.platform == RuntimePlatform.IPhonePlayer) ok = ARNativePluginStatic.arwUpdateTexture32Stereo(address0, address1);
		else ok = ARNativePlugin.arwUpdateTexture32Stereo(address0, address1);
		handle0.Free();
		handle1.Free();
		return ok;
	}
	
    public static int arwGetTrackableAppearanceCount(int markerID)
	{
        if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetTrackableAppearanceCount(markerID);
        else return ARNativePlugin.arwGetTrackableAppearanceCount(markerID);
	}

	public static bool arwGetTrackableAppearanceConfig(int markerID, int patternID, float[] matrix, out float width, out float height, out int imageSizeX, out int imageSizeY)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetTrackableAppearanceConfig(markerID, patternID, matrix, out width, out height, out imageSizeX, out imageSizeY);
		else return ARNativePlugin.arwGetTrackableAppearanceConfig(markerID, patternID, matrix, out width, out height, out imageSizeX, out imageSizeY);
	}
	
	public static bool arwGetTrackableAppearanceImage(int markerID, int patternID, [In, Out]Color32[] colors32)
	{
        bool ok;
        GCHandle handle = GCHandle.Alloc(colors32, GCHandleType.Pinned);
        IntPtr address = handle.AddrOfPinnedObject();
		if (Application.platform == RuntimePlatform.IPhonePlayer) ok = ARNativePluginStatic.arwGetTrackableAppearanceImage(markerID, patternID, address);
		else ok = ARNativePlugin.arwGetTrackableAppearanceImage(markerID, patternID, address);
        handle.Free();
        return ok;
    }
	
	public static bool arwGetTrackableOptionBool(int markerID, int option)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetTrackableOptionBool(markerID, option);
		else return ARNativePlugin.arwGetTrackableOptionBool(markerID, option);
	}
	
	public static void arwSetTrackableOptionBool(int markerID, int option, bool value)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) ARNativePluginStatic.arwSetTrackableOptionBool(markerID, option, value);
		else ARNativePlugin.arwSetTrackableOptionBool(markerID, option, value);
	}

	public static int arwGetTrackableOptionInt(int markerID, int option)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetTrackableOptionInt(markerID, option);
		else return ARNativePlugin.arwGetTrackableOptionInt(markerID, option);
	}
	
	public static void arwSetTrackableOptionInt(int markerID, int option, int value)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) ARNativePluginStatic.arwSetTrackableOptionInt(markerID, option, value);
		else ARNativePlugin.arwSetTrackableOptionInt(markerID, option, value);
	}

	public static float arwGetTrackableOptionFloat(int markerID, int option)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetTrackableOptionFloat(markerID, option);
		else return ARNativePlugin.arwGetTrackableOptionFloat(markerID, option);
	}
	
	public static void arwSetTrackableOptionFloat(int markerID, int option, float value)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) ARNativePluginStatic.arwSetTrackableOptionFloat(markerID, option, value);
		else ARNativePlugin.arwSetTrackableOptionFloat(markerID, option, value);
	}

    //
    // Constants for use with tracker option setters/getters.
    //
    public enum ARW_TRACKER_OPTION {
        ARW_TRACKER_OPTION_2D_MAX_IMAGES = 0,                          ///< int.
        ARW_TRACKER_OPTION_SQUARE_THRESHOLD = 1,                       ///< Threshold value used for image binarization. int in range [0-255].
        ARW_TRACKER_OPTION_SQUARE_THRESHOLD_MODE = 2,                  ///< Threshold mode used for image binarization. int.
        ARW_TRACKER_OPTION_SQUARE_LABELING_MODE = 3,                   ///< int.
        ARW_TRACKER_OPTION_SQUARE_PATTERN_DETECTION_MODE = 4,          ///< int.
        ARW_TRACKER_OPTION_SQUARE_BORDER_SIZE = 5,                     ///< float in range (0-0.5).
        ARW_TRACKER_OPTION_SQUARE_MATRIX_CODE_TYPE = 6,                ///< int.
        ARW_TRACKER_OPTION_SQUARE_IMAGE_PROC_MODE = 7,                 ///< int.
        ARW_TRACKER_OPTION_SQUARE_DEBUG_MODE = 8,                      ///< Enables or disable state of debug mode in the tracker. When enabled, a black and white debug image is generated during marker detection. The debug image is useful for visualising the binarization process and choosing a threshold value. bool.
    };

	public static bool arwGetTrackerOptionBool(int option)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetTrackerOptionBool(option);
		else return ARNativePlugin.arwGetTrackerOptionBool(option);
	}

	public static void arwSetTrackerOptionBool(int option, bool value)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) ARNativePluginStatic.arwSetTrackerOptionBool(option, value);
		else ARNativePlugin.arwSetTrackerOptionBool(option, value);
	}

	public static int arwGetTrackerOptionInt(int option)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetTrackerOptionInt(option);
		else return ARNativePlugin.arwGetTrackerOptionInt(option);
	}

	public static void arwSetTrackerOptionInt(int option, int value)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) ARNativePluginStatic.arwSetTrackerOptionInt(option, value);
		else ARNativePlugin.arwSetTrackerOptionInt(option, value);
	}

	public static float arwGetTrackerOptionFloat(int option)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwGetTrackerOptionFloat(option);
		else return ARNativePlugin.arwGetTrackerOptionFloat(option);
	}

	public static void arwSetTrackerOptionFloat(int option, float value)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) ARNativePluginStatic.arwSetTrackerOptionFloat(option, value);
		else ARNativePlugin.arwSetTrackerOptionFloat(option, value);
	}

	public static int arwAddMarker(string cfg)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwAddMarker(cfg);
		else return ARNativePlugin.arwAddMarker(cfg);
	}
	
	public static bool arwRemoveMarker(int markerID)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwRemoveMarker(markerID);
		else return ARNativePlugin.arwRemoveMarker(markerID);
	}

	public static int arwRemoveAllMarkers()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwRemoveAllMarkers();
		else return ARNativePlugin.arwRemoveAllMarkers();
	}


	public static bool arwQueryMarkerVisibility(int markerID)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwQueryMarkerVisibility(markerID);
		else return ARNativePlugin.arwQueryMarkerVisibility(markerID);
	}

	public static bool arwQueryMarkerTransformation(int markerID, float[] matrix)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwQueryMarkerTransformation(markerID, matrix);
		else return ARNativePlugin.arwQueryMarkerTransformation(markerID, matrix);
	}

	public static bool arwQueryMarkerTransformationStereo(int markerID, float[] matrixL, float[] matrixR)
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer) return ARNativePluginStatic.arwQueryMarkerTransformationStereo(markerID, matrixL, matrixR);
		else return ARNativePlugin.arwQueryMarkerTransformationStereo(markerID, matrixL, matrixR);
	}
	
}
