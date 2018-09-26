/*
 *  ARStaticCamera.cs
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

using UnityEngine;
using System.Collections;
using System.Linq;
public class ARStaticCamera : MonoBehaviour {
	public  enum  ViewEye {
		Left, Right
	}
	
	private const float  NEAR_PLANE = 0.01f; // Default as defined in ARController.cpp
	private const float  FAR_PLANE  = 10.0f; // Default as defined in ARController.cpp
	private const string LOG_TAG           = "ARStaticCamera: ";
	private const string OPTICAL_LOG       = LOG_TAG + "Optical parameters: fovy={0}, aspect={1}, camera position (m)=({2}, {3}, {4})";
	private const string LEFT_EYE_NAME     = "ARCamera Left Eye";
	private const string RIGHT_EYE_NAME    = "ARCamera Right Eye";
	private const float  NO_LATERAL_OFFSET = 0.0f;

	private static ARStaticCamera instance = null;
	public  static ARStaticCamera Instance {
		get {
			if (null == instance) {
				instance = GameObject.FindObjectOfType<ARStaticCamera>();
			}
			return instance;
		}
	}

	#region Editor
	// UnityEditor doesn't serialize properties.
	// In order to keep track of what we're using, we serialize their properties here,
	// rather than using some ugly ID association with EditorPrefs.
	// These are not #if'd out because that would change the serialization layout of the class.
	// TODO: Remove this by dynamic lookup of these values based on actually used
	// serialized information.
	public  int       EditorOpticalIndexL     = 0;
	public  string    EditorOpticalNameL      = null;
	public  int       EditorOpticalIndexR     = 0;
	public  string    EditorOpticalNameR      = null;
	#endregion
	public  int       ContentLayer            = 0;
	public  float     NearPlane               = NEAR_PLANE;
	public  float     FarPlane                = FAR_PLANE;

	public  bool      Stereo				  = false;
	public  bool      Optical                 = false;

	public  byte[]    OpticalParametersL      = null;
	public  byte[]    OpticalParametersR      = null;
	// Average of male/female IPD from https://en.wikipedia.org/wiki/Interpupillary_distance
	public  float     OpticalEyeLateralOffset = 63.5f;
	
	private Matrix4x4 opticalViewMatrixL      = Matrix4x4.identity;
	private Matrix4x4 opticalViewMatrixR      = Matrix4x4.identity;
	
	private Camera leftCamera = null;
	private Camera LeftCamera {
		get {
			if (null == leftCamera) {
				leftCamera = MakeCamera(LEFT_EYE_NAME);
			}
			return leftCamera;
		}
	}
	
	private Camera rightCamera = null;
	private Camera RightCamera {
		get {
			if (null == rightCamera) {
				rightCamera = MakeCamera(RIGHT_EYE_NAME);
			}
			return rightCamera;
		}
	}

	private bool useLateralOffset {
		get {
			return Optical && !OpticalParametersL.SequenceEqual(OpticalParametersR);
		}
	}

	private void Awake() {
		if (null == instance) {
			instance = this;
		} else {
			Debug.LogError("ERROR: MORE THAN ONE ARSTATICCAMERA IN SCENE!");
		}
	}

	public void ConfigureViewports(Rect pixelRectL, Rect pixelRectR) {
		LeftCamera.pixelRect = pixelRectL;
		if (Stereo) {
			RightCamera.pixelRect = pixelRectR;
		}
	}

	public bool SetupCamera(Matrix4x4 projectionMatrixL, Matrix4x4 projectionMatrixR, out bool opticalOut) {
		opticalOut = Optical;

		bool success = SetupCamera(projectionMatrixL, LeftCamera, Optical ? OpticalParametersL : null, NO_LATERAL_OFFSET, ref opticalViewMatrixL);
		if (Stereo) {
			success = success && SetupCamera(projectionMatrixR, RightCamera, Optical ? OpticalParametersR : null, useLateralOffset ? OpticalEyeLateralOffset : NO_LATERAL_OFFSET, ref opticalViewMatrixR);
		}
		return success;
	}

	private bool SetupCamera(Matrix4x4 projectionMatrix, Camera referencedCamera, byte[] opticalParameters, float lateralOffset, ref Matrix4x4 opticalViewMatrix) {
		// A perspective projection matrix from the tracker
		referencedCamera.orthographic = false;
		
//		if (null == opticalParameters) {
			referencedCamera.projectionMatrix = projectionMatrix;
//		} else {
//			float fovy ;
//			float aspect;
//			float[] m = new float[16];
//			float[] p = new float[16];
//			bool opticalSetupOK = PluginFunctions.arwLoadOpticalParams(null, opticalParameters, opticalParameters.Length, out fovy, out aspect, m, p);
//			if (!opticalSetupOK) {
//				ARController.Log(LOG_TAG + "Error loading optical parameters.");
//				return false;
//			}
//			m[12] *= ARTrackable.UNITY_TO_ARTOOLKIT;
//			m[13] *= ARTrackable.UNITY_TO_ARTOOLKIT;
//			m[14] *= ARTrackable.UNITY_TO_ARTOOLKIT;
//			ARController.Log(string.Format(OPTICAL_LOG, fovy, aspect, m[12].ToString("F3"), m[13].ToString("F3"), m[14].ToString("F3")));
//			
//			referencedCamera.projectionMatrix = ARUtilityFunctions.MatrixFromFloatArray(p);
//			
//			opticalViewMatrix = ARUtilityFunctions.MatrixFromFloatArray(m);
//			if (lateralOffset != NO_LATERAL_OFFSET) {
//				opticalViewMatrix = Matrix4x4.TRS(new Vector3(-lateralOffset, 0.0f, 0.0f), Quaternion.identity, Vector3.one) * opticalViewMatrix; 
//			}
//			// Convert to left-hand matrix.
//			opticalViewMatrix = ARUtilityFunctions.LHMatrixFromRHMatrix(opticalViewMatrix);
//			
//			referencedCamera.transform.localPosition = ARUtilityFunctions.PositionFromMatrix(opticalViewMatrix);
//			referencedCamera.transform.localRotation = ARUtilityFunctions.RotationFromMatrix(opticalViewMatrix);
//		}
		
		// Don't clear anything or else we interfere with other foreground cameras
		referencedCamera.clearFlags = CameraClearFlags.Nothing;

		// Renders after the clear and background cameras
		referencedCamera.depth = ARController.BACKGROUND_CAMERA_DEPTH + 1;
		
		// Ensure background camera isn't rendered in ARCamera.
		referencedCamera.cullingMask = 1 << ContentLayer;
		if (ARController.Instance.VideoIsStereo) {
			referencedCamera.cullingMask = 1 << ContentLayer;
		}

        // Set clipping planes.
        referencedCamera.farClipPlane = FarPlane;
        referencedCamera.nearClipPlane = NearPlane;

		return true;
	}

	private Camera MakeCamera(string cameraName) {
		GameObject cameraObject = new GameObject(cameraName);
		cameraObject.transform.parent        = gameObject.transform;
		cameraObject.transform.localPosition = Vector3.zero;
		cameraObject.transform.localRotation = Quaternion.identity;
		cameraObject.transform.localScale    = Vector3.zero;
		return cameraObject.AddComponent<Camera>();
	}
}
