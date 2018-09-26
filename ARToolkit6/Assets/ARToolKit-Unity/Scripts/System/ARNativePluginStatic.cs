/*
 *  ARNativePluginStatic.cs
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
 *  Copyright 2015 Daqri, LLC.
 *  Copyright 2010-2015 ARToolworks, Inc.
 *
 *  Author(s): Philip Lamb
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

public static class ARNativePluginStatic
{
	
	#if UNITY_IOS
	[DllImport("__Internal")]
	public static extern void aruRequestCamera();
	#endif

	[DllImport("__Internal")]
	public static extern void arwRegisterLogCallback(PluginFunctions.LogCallback callback);

	[DllImport("__Internal")]
	public static extern void arwSetLogLevel(int logLevel);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwInitialiseAR();
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwInitialiseARWithOptions(int pattSize, int pattCountMax);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwGetARToolKitVersion([MarshalAs(UnmanagedType.LPStr)]StringBuilder buffer, int length);
	
	[DllImport("__Internal")]
	public static extern int arwGetError();
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwShutdownAR();
	
	[DllImport("__Internal", CharSet = CharSet.Ansi)]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwStartRunningB(string vconf, byte[] cparaBuff, int cparaBuffLen, float nearPlane, float farPlane);
	
	[DllImport("__Internal", CharSet = CharSet.Ansi)]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwStartRunningStereoB(string vconfL, byte[] cparaBuffL, int cparaBuffLenL, string vconfR, byte[] cparaBuffR, int cparaBuffLenR, byte[] transL2RBuff, int transL2RBuffLen, float nearPlane, float farPlane);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwIsRunning();
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwStopRunning();
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwGetProjectionMatrix([Out][MarshalAs(UnmanagedType.LPArray, SizeConst=16)] float[] matrix);

	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwGetProjectionMatrixStereo([Out][MarshalAs(UnmanagedType.LPArray, SizeConst=16)] float[] matrixL, [Out][MarshalAs(UnmanagedType.LPArray, SizeConst=16)] float[] matrixR);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwGetVideoParams(out int width, out int height, out int pixelSize, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pixelFormatBuffer, int pixelFormatBufferLen);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwGetVideoParamsStereo(out int widthL, out int heightL, out int pixelSizeL, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pixelFormatBufferL, int pixelFormatBufferLenL, out int widthR, out int heightR, out int pixelSizeR, [MarshalAs(UnmanagedType.LPStr)]StringBuilder pixelFormatBufferR, int pixelFormatBufferLenR);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwCapture();
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwUpdateAR();
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	//public static extern bool arwUpdateTexture32([In, Out]Color32[] colors32);
	public static extern bool arwUpdateTexture32(IntPtr colors32);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	//public static extern bool arwUpdateTexture32Stereo([In, Out]Color32[] colors32L, [In, Out]Color32[] colors32R);
	public static extern bool arwUpdateTexture32Stereo(IntPtr colors32L, IntPtr colors32R);
	
	[DllImport("__Internal")]
    public static extern int arwGetTrackableAppearanceCount(int markerID);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwGetTrackableAppearanceConfig(int markerID, int patternID, [Out][MarshalAs(UnmanagedType.LPArray, SizeConst=16)] float[] matrix, out float width, out float height, out int imageSizeX, out int imageSizeY);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	//public static extern bool arwGetTrackableAppearanceImage(int markerID, int patternID, [In, Out]Color32[] colors32);
	public static extern bool arwGetTrackableAppearanceImage(int markerID, int patternID, IntPtr colors32);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwGetTrackableOptionBool(int markerID, int option);
	
	[DllImport("__Internal")]
	public static extern void arwSetTrackableOptionBool(int markerID, int option, bool value);
	
	[DllImport("__Internal")]
	public static extern int arwGetTrackableOptionInt(int markerID, int option);
	
	[DllImport("__Internal")]
	public static extern void arwSetTrackableOptionInt(int markerID, int option, int value);
	
	[DllImport("__Internal")]
	public static extern float arwGetTrackableOptionFloat(int markerID, int option);
	
	[DllImport("__Internal")]
	public static extern void arwSetTrackableOptionFloat(int markerID, int option, float value);
	
    [DllImport("__Internal")]
    [return: MarshalAsAttribute(UnmanagedType.I1)]
    public static extern bool arwGetTrackerOptionBool(int option);

    [DllImport("__Internal")]
    public static extern void arwSetTrackerOptionBool(int option, bool value);

    [DllImport("__Internal")]
    public static extern int arwGetTrackerOptionInt(int option);

    [DllImport("__Internal")]
    public static extern void arwSetTrackerOptionInt(int option, int value);

    [DllImport("__Internal")]
    public static extern float arwGetTrackerOptionFloat(int option);

    [DllImport("__Internal")]
    public static extern void arwSetTrackerOptionFloat(int option, float value);

	[DllImport("__Internal", CharSet = CharSet.Ansi)]
	public static extern int arwAddMarker(string cfg);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwRemoveMarker(int markerID);
	
	[DllImport("__Internal")]
	public static extern int arwRemoveAllMarkers();
	
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwQueryMarkerVisibility(int markerID);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwQueryMarkerTransformation(int markerID, [Out][MarshalAs(UnmanagedType.LPArray, SizeConst=16)] float[] matrix);
	
	[DllImport("__Internal")]
	[return: MarshalAsAttribute(UnmanagedType.I1)]
	public static extern bool arwQueryMarkerTransformationStereo(int markerID, [Out][MarshalAs(UnmanagedType.LPArray, SizeConst=16)] float[] matrixL, [Out][MarshalAs(UnmanagedType.LPArray, SizeConst=16)] float[] matrixR);	
}

