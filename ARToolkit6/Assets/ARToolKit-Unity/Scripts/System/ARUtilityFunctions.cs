/*
 *  ARUtilityFunctions.cs
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
 *  Author(s): Julian Looser, Philip Lamb, Wally Young
 *
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ARUtilityFunctions
{
	private const string LOG_TAG = "ARUtilityFunctions: ";

	/// <summary>
	/// Returns the named camera or null if not found.
	/// </summary>
	/// <param name="name">Camera name to search for.</param>
	/// <returns>The named <see cref="Camera"/> or null if not found.</returns>
	public static Camera FindCameraByName(string name)
	{
	    foreach (Camera c in Camera.allCameras)
	    {
	        if (c.gameObject.name == name) return c;
	    }

	    return null;
	}


	/// <summary>
	/// Creates a Unity matrix from an array of floats.
	/// </summary>
	/// <param name="values">Array of 16 floats to populate the matrix.</param>
	/// <returns>A new <see cref="Matrix4x4"/> with the given values.</returns>
	public static Matrix4x4 MatrixFromFloatArray(float[] values)
	{
	    if (values == null || values.Length < 16) throw new ArgumentException("Expected 16 elements in values array", "values");

	    Matrix4x4 mat = new Matrix4x4();
	    for (int i = 0; i < 16; i++) mat[i] = values[i];
	    return mat;
	}

	public static Quaternion RotationFromMatrix(Matrix4x4 m) {
		// Trap the case where the matrix passed in has an invalid rotation submatrix.
		if (m.GetColumn(2) == Vector4.zero) {
			ARController.Log("QuaternionFromMatrix got zero matrix.");
			return Quaternion.identity;
		}
		return Quaternion.LookRotation(m.GetColumn(2), m.GetColumn(1));
	}

	public static Vector3 PositionFromMatrix(Matrix4x4 m)
	{
	    return m.GetColumn(3);
	}

	// Convert from right-hand coordinate system with <normal vector> in direction of +x,
	// <orthorgonal vector> in direction of +y, and <approach vector> in direction of +z,
	// to Unity's left-hand coordinate system with <normal vector> in direction of +x,
	// <orthorgonal vector> in direction of +y, and <approach vector> in direction of +z.
	// This is equivalent to negating row 2, and then negating column 2.
	public static Matrix4x4 LHMatrixFromRHMatrix(Matrix4x4 rhm)
	{
		Matrix4x4 lhm = new Matrix4x4();

		// Column 0.
		lhm[0, 0] =  rhm[0, 0];
		lhm[1, 0] =  rhm[1, 0];
		lhm[2, 0] = -rhm[2, 0];
		lhm[3, 0] =  rhm[3, 0];
		
		// Column 1.
		lhm[0, 1] =  rhm[0, 1];
		lhm[1, 1] =  rhm[1, 1];
		lhm[2, 1] = -rhm[2, 1];
		lhm[3, 1] =  rhm[3, 1];
		
		// Column 2.
		lhm[0, 2] = -rhm[0, 2];
		lhm[1, 2] = -rhm[1, 2];
		lhm[2, 2] =  rhm[2, 2];
		lhm[3, 2] = -rhm[3, 2];
		
		// Column 3.
		lhm[0, 3] =  rhm[0, 3];
		lhm[1, 3] =  rhm[1, 3];
		lhm[2, 3] = -rhm[2, 3];
		lhm[3, 3] =  rhm[3, 3];

		return lhm;
	}

	private const string UNPACK_ERROR = LOG_TAG + "Error unpacking '{0}' to '{1}'"; 

	public static bool GetFileFromStreamingAssets(string relative, out string desination) {
		desination = Path.Combine(Application.streamingAssetsPath, relative);
        ARController.Log ("GetFileFromStreamingAssets(): destination: " + desination);
#if !UNITY_METRO
		// On Android, we need to unpack the StreamingAssets from the .jar file in which
		// they're archived into the native file system.
		// URIs are valid whether we're using an absolute path or not.
		// Check specifically for URL-like scheme.
		if (desination.Contains("://")) {
			// E.g. "jar:file://" + Application.dataPath + "!/assets/" + basename;
			string source = desination;
			desination = Path.Combine(Application.temporaryCachePath, relative);
			// File has already been unpacked. Skip.
			// TODO: Add some integrity checking that it's the right file.
            if (File.Exists(desination)) {
                ARController.Log ("File already exists at destination: " + desination);
				return true;
			}
			WWW www = new WWW(source);
			// This will block in the webplayer.
		    // TODO: switch to co-routine.
			while (!www.isDone);
			if (!string.IsNullOrEmpty(www.error)) {
				ARController.Log(string.Format(UNPACK_ERROR, source, desination));
				desination = string.Empty;
				return false;
			}
			// Note: 64MB limit on File.WriteAllBytes.
			// TODO: Verify limit.
            Directory.CreateDirectory(desination.Substring(0,desination.LastIndexOf('/')));
			File.WriteAllBytes(desination, www.bytes);
		}
#endif
		return true;
	}


    #if UNITY_EDITOR
    private static BuildTarget[] SupportedPlatforms = {BuildTarget.Android,BuildTarget.iOS,BuildTarget.StandaloneLinux64,BuildTarget.StandaloneOSXIntel64};

    public static bool isTargetPlatformSupported(){

        for (int i = 0; i < SupportedPlatforms.Length; i++) {
            if (SupportedPlatforms [i] == EditorUserBuildSettings.activeBuildTarget)
                return true;
        }
        return false;
    }
    #endif

}
