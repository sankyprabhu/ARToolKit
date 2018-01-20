/*
 *  ARStaticCameraEditor.cs
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
 *
 *  Author(s): Wally Young
 *
 */

using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ARStaticCamera))] 
public class ARStaticCameraEditor : Editor {
	private static string path = string.Empty;
	private static string BasePath {
		get {
			if (string.IsNullOrEmpty(path)) {
				path = Path.Combine(Application.streamingAssetsPath, ARToolKitAssetManager.OPTICAL_DIRECTORY_NAME);
			}
			return path;
		}
	}
	private const string OPTICAL_EXT = ".dat";

	public override void OnInspectorGUI() {
		ARStaticCamera arStaticCamera = (ARStaticCamera)target;
		if (null == arStaticCamera) {
			return;
		}
		
		arStaticCamera.ContentLayer = EditorGUILayout.LayerField("AR Content Layer", arStaticCamera.ContentLayer);
		arStaticCamera.NearPlane   = EditorGUILayout.FloatField("Near Plane",       arStaticCamera.NearPlane);
		arStaticCamera.FarPlane    = EditorGUILayout.FloatField("Far Plane",        arStaticCamera.FarPlane);
		EditorGUILayout.Separator();

		arStaticCamera.Stereo  = EditorGUILayout.Toggle("Stereo Rendering",            arStaticCamera.Stereo);

        if (ARToolKitAssetManager.OpticalCalibrations.Length <= 0) {
            arStaticCamera.Optical = false;
        }
        EditorGUI.BeginDisabledGroup(ARToolKitAssetManager.OpticalCalibrations.Length <= 0);
        arStaticCamera.Optical = EditorGUILayout.Toggle("Optical See-Through Display", arStaticCamera.Optical);
        EditorGUI.EndDisabledGroup();
        if (ARToolKitAssetManager.OpticalCalibrations.Length <= 0) {
            EditorGUILayout.HelpBox("No optical calibration parameters found.", MessageType.Info);
        }
        if (arStaticCamera.Optical && ARToolKitAssetManager.OpticalCalibrations.Length <= 0) {
			Debug.LogError("ARStaticCamera - Attempted to enable optical see-through mode with no optical configurations in StreamingAssets/ARToolKit5/Optical!");
			arStaticCamera.Optical = false;
		}

		if (!arStaticCamera.Optical) {
			// Reset to defaults in ARStaticCamera.cs
			arStaticCamera.EditorOpticalIndexL     = 0;
			arStaticCamera.EditorOpticalNameL      = null;
			arStaticCamera.OpticalParametersL      = null;
			arStaticCamera.EditorOpticalIndexR     = 0;
			arStaticCamera.EditorOpticalNameR      = null;
			arStaticCamera.OpticalParametersR      = null;
			arStaticCamera.OpticalEyeLateralOffset = 63.5f;
			return;
		}

		int newIndexL = EditorGUILayout.Popup("Left Eye Configuration",  arStaticCamera.EditorOpticalIndexL, ARToolKitAssetManager.OpticalCalibrations);
		int newIndexR = EditorGUILayout.Popup("Right Eye Configuration", arStaticCamera.EditorOpticalIndexR, ARToolKitAssetManager.OpticalCalibrations);
		if ((string.CompareOrdinal(ARToolKitAssetManager.OpticalCalibrations[newIndexL], arStaticCamera.EditorOpticalNameL) != 0) ||
			(string.CompareOrdinal(ARToolKitAssetManager.OpticalCalibrations[newIndexR], arStaticCamera.EditorOpticalNameR) != 0)) {
			arStaticCamera.EditorOpticalIndexL = newIndexL;
			arStaticCamera.EditorOpticalNameL = ARToolKitAssetManager.OpticalCalibrations[newIndexL];
			arStaticCamera.OpticalParametersL = ReadOpticalParamsByName(arStaticCamera.EditorOpticalNameL);
			arStaticCamera.EditorOpticalIndexR = newIndexR;
			arStaticCamera.EditorOpticalNameR = ARToolKitAssetManager.OpticalCalibrations[newIndexR];
			arStaticCamera.OpticalParametersR = ReadOpticalParamsByName(arStaticCamera.EditorOpticalNameR);
			arStaticCamera.Optical = true;
		}


		if (newIndexL == newIndexR) {
			// Sane defaults derived from https://en.wikipedia.org/wiki/Interpupillary_distance
			arStaticCamera.OpticalEyeLateralOffset = EditorGUILayout.Slider("Inter-Pupular Distance (mm)", arStaticCamera.OpticalEyeLateralOffset, 0.50f, 0.80f);
			EditorGUILayout.HelpBox("If you are using the same optical configuration for both the left and right eyes, you must supply an IPD (inter-pupular" +
			                        "distance) value. This is the spacing from one pupil to the other, in milimeters.", MessageType.Warning);
		}

		EditorGUILayout.Separator();
		EditorGUILayout.HelpBox("Do not create a second camera object for the right eye! ARToolKit will do this for you.", MessageType.Info);
	}

	private static byte[] ReadOpticalParamsByName(string name) {
		return File.ReadAllBytes(Path.Combine(BasePath, name + OPTICAL_EXT));
	}

}
