/*
 *  ARTrackable.cs
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
 *  Author(s):  Philip Lamb, Julian Looser, Wally Young
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;

/// <summary>
/// ARTrackable objects represent an ARToolKit trackable, even when ARToolKit is not
/// initialised.
/// To find trackables from elsewhere in the Unity environment:
///   ARTrackable[] trackables = FindObjectsOfType{ARTrackable}(); // (or FindObjectsOfType(typeof(ARTrackable)) as ARTrackable[]);
/// </summary>
///
[ExecuteInEditMode]
public class ARTrackable : MonoBehaviour {

    public enum TrackableType {
        TwoD,               // 2D image target
        Square,             // Square template (pattern) marker
        SquareBarcode,      // Square matrix (barcode) marker.
        Multimarker         // Multiple Square markers treated as a single target
    }

	public enum ARWTrackableOption : int {
		ARW_TRACKABLE_OPTION_FILTERED                        = 1,
		ARW_TRACKABLE_OPTION_FILTER_SAMPLE_RATE              = 2,
		ARW_TRACKABLE_OPTION_FILTER_CUTOFF_FREQ              = 3,
		ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION = 4,
		ARW_TRACKABLE_OPTION_SQUARE_CONFIDENCE               = 5,
		ARW_TRACKABLE_OPTION_SQUARE_CONFIDENCE_CUTOFF        = 6,
		ARW_TRACKABLE_OPTION_IMAGE_HEIGHT                    = 7
	}

    public readonly static Dictionary<TrackableType, string> TrackableTypeNames = new Dictionary<TrackableType, string> {
		{TrackableType.TwoD,          "2D image"},
		{TrackableType.Square,        "Square pictorial marker"},
		{TrackableType.SquareBarcode, "Square barcode marker"},
    	{TrackableType.Multimarker,   "MultiSquare configuration"},
    };

    private const string LOG_TAG = "ARTrackable: ";

	public const int    NO_ID   = -1;

	private const string TWOD_FORMAT           = "ARToolKit/Images";
	private const string MULTI_FORMAT		   = "ARToolKit";

	private const string TWOD_CONFIG           = "2d;{0};{1}";
	private const string SINGLE_BUFFER_CONFIG  = "square_buffer;{0};buffer={1}";
	private const string SINGLE_BARCODE_CONFIG = "square_barcode;{0};{1}";
	private const string MULTI_CONFIG          = "multisquare;{0}";
	private const string MULTI_EXT             = ".dat";
	public const float ARTOOLKIT_TO_UNITY      = 1000.0f;
	public const float UNITY_TO_ARTOOLKIT      = 1.0f / ARTOOLKIT_TO_UNITY;

	private const string LOAD_FAILURE          = LOG_TAG + "Failed to load {0}. Quitting.";

	#region Editor
	// UnityEditor doesn't serialize properties.
	// In order to keep track of what we're using, we serialize their properties here,
	// rather than using some ugly ID association with EditorPrefs.
	// These are not #if'd out because that would change the serialization layout of the class.
	// TODO: Remove this by dynamic lookup of these values based on actually used
	// serialized information.
	public        int    EditorMarkerIndex     = 0;
	public        string EditorMarkerName      = string.Empty;
	#endregion

	// Current Unique Identifier (UID) assigned to this marker.
	// UID is not serialized because its value is only meaningful during a specific run.
    public int UID {
		get {
			return uid;
		}
	}

    // Public members get serialized
	public TrackableType Type {
		get {
			return type;
		}
		set {
			if (value != type) {
				Unload();
				type = value;
				Load();
			}
		}
	}

	public string PatternContents {
		get {
			return patternContents;
		}
		set {
			if (string.CompareOrdinal(value, patternContents) != 0) {
				Unload();
				patternContents = value;
				Load();
			}
		}
	}

	public float PatternWidth {
		get {
			return patternWidth;
		}
		set {
			if (value != patternWidth) {
				Unload();
				patternWidth = value;
				Load();
			}
		}
	}

	// Barcode markers have a user-selected ID.
	public int BarcodeID {
		get {
			return barcodeID;
		}
		set {
			if (value != barcodeID) {
				Unload();
				barcodeID = value;
				Load();
			}
		}
	}

    // If the marker is multi, it just has a config filename
	public string MultiConfigFile {
		get {
			return multiConfigFile;
		}
		set {
			if (string.CompareOrdinal(value, multiConfigFile) != 0) {
				Unload();
				multiConfigFile = value;
				Load();
			}
		}
	}

	// 2D images have an image name (including the extension).
	public string TwoDImageName {
		get {
			return twoDImageName;
		}
		set {
			if (string.CompareOrdinal(value, twoDImageName) != 0) {
				Unload();
				twoDImageName = value;
				Load();
			}
		}
	}

	public Matrix4x4 TransformationMatrix {
		get {
			return transformationMatrix;
		}
	}

	public bool Visible {
		get {
			return visible;
		}
	}

	public ARPattern[] Patterns {
		get {
			return patterns;
		}
	}

	public bool Filtered {
		get {
			return currentFiltered;
		}
		set {
			if (value == currentFiltered) {
				return;
			}
			currentFiltered = value;
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetTrackableOptionBool(UID, (int)ARWTrackableOption.ARW_TRACKABLE_OPTION_FILTERED, value);
			}
		}
	}

	public float FilterSampleRate {
		get {
			return currentFilterSampleRate;
		}
		set {
			if (value == currentFilterSampleRate) {
				return;
			}
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetTrackableOptionFloat(UID, (int)ARWTrackableOption.ARW_TRACKABLE_OPTION_FILTER_SAMPLE_RATE, value);
			}
		}
	}

	public float FilterCutoffFreq {
		get {
			return currentFilterCutoffFreq;
		}
		set {
			if (value == currentFilterCutoffFreq) {
				return;
			}
			currentFilterCutoffFreq = value;
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetTrackableOptionFloat(UID, (int)ARWTrackableOption.ARW_TRACKABLE_OPTION_FILTER_CUTOFF_FREQ, value);
			}
		}
	}

	public bool UseContPoseEstimation {
		get {
			return currentUseContPoseEstimation;
		}
		set {
			currentUseContPoseEstimation = value;
			if (Type != TrackableType.Square && Type != TrackableType.SquareBarcode) {
				return;
			}
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetTrackableOptionBool(UID, (int)ARWTrackableOption.ARW_TRACKABLE_OPTION_SQUARE_USE_CONT_POSE_ESTIMATION, value);
			}
		}
	}

	public float TwoDImageHeight {
		get {
			return currentTwoDImageHeight;
		}
		set {
			if (value == currentTwoDImageHeight) {
				return;
			}
			currentTwoDImageHeight = value;
			if (Type != TrackableType.TwoD) {
				return;
			}
			lock (loadLock) {
				if (UID == NO_ID) {
					return;
				}
				PluginFunctions.arwSetTrackableOptionFloat(UID, (int)ARWTrackableOption.ARW_TRACKABLE_OPTION_IMAGE_HEIGHT, value);
			}
		}
	}

	public List<AAREventReceiver> eventReceivers = new List<AAREventReceiver>();

	[NonSerialized] private int         uid                  = NO_ID;
	[NonSerialized] public float        TwoDImageWidth       = 1.0f;               // Once marker is loaded, this holds the width of the marker in Unity units.
	[NonSerialized] private ARPattern[] patterns             = null;               // Single markers have a single pattern, multi markers have one or more, NFT have none.
	[NonSerialized] protected bool      visible              = false;              // Marker is visible or not.
	[NonSerialized] protected Matrix4x4 transformationMatrix = Matrix4x4.identity; // Full transformation matrix as a Unity matrix.

	// Private fields with accessors.
	// Marker configuration options.
	// Normally set through Inspector Editor script.
	[SerializeField] private TrackableType type                         = TrackableType.TwoD;
	[SerializeField] private bool          currentFiltered              = false;
	[SerializeField] private float         currentFilterSampleRate      = 30.0f;
	[SerializeField] private float         currentFilterCutoffFreq      = 15.0f;
	// NFT Marker Only
	[SerializeField] private string        twoDImageName                = string.Empty;
	[SerializeField] private float         currentTwoDImageHeight       = 1.0f;      // 2D image only; Height of image.
	// Single Marker Only
	[SerializeField] private float         patternWidth                 = 0.08f;     // Square marker only; Width of pattern in meters.
	[SerializeField] private bool          currentUseContPoseEstimation = false;     // Square marker only; Whether continuous pose estimation should be used.
	// Single Non-Barcode Marker Only
	[SerializeField] private string        patternContents              = string.Empty;
	// Single Barcode Marker Only
	[SerializeField] private int           barcodeID                    = 0;
	// Multimarker Only
	[SerializeField] private string        multiConfigFile              = string.Empty;

	private object loadLock = new object();

	// Load the underlying ARToolKit marker structure(s) and set the UID.
	public void Load() {
		lock (loadLock) {
			if (UID != NO_ID) {
				return;
			}
            if (!PluginFunctions.inited) {
                // If arwInitialiseAR() has not yet been called, we can't load the native trackable yet.
                // ARController.InitialiseAR() will call this again when arwInitialiseAR() has been called.
                return;
            }
			// Work out the configuration string to pass to ARToolKit.
			string assetDirectory = Application.streamingAssetsPath;
			string configuration  = string.Empty;
            string path           = string.Empty;

			switch (Type) {

				case TrackableType.TwoD:
					if (string.IsNullOrEmpty(TwoDImageName)) {
						ARController.Log(string.Format(LOAD_FAILURE, "2D image trackable due to no TwoDImageName"));
						return;
					}
                    path = Path.Combine(TWOD_FORMAT, TwoDImageName);
                    if (!ARUtilityFunctions.GetFileFromStreamingAssets(path, out assetDirectory)) {
						ARController.Log(string.Format(LOAD_FAILURE, TwoDImageName));
						return;
					}
					if (!string.IsNullOrEmpty(assetDirectory)) {
                        configuration = string.Format(TWOD_CONFIG, assetDirectory, TwoDImageHeight * ARTOOLKIT_TO_UNITY);
				    }
				    break;
				case TrackableType.Square:
					// Multiply width by 1000 to convert from metres to ARToolKit's millimetres.
					configuration = string.Format(SINGLE_BUFFER_CONFIG, PatternWidth * ARTOOLKIT_TO_UNITY, PatternContents);
					break;
				case TrackableType.SquareBarcode:
					// Multiply width by 1000 to convert from metres to ARToolKit's millimetres.
					configuration = string.Format(SINGLE_BARCODE_CONFIG, BarcodeID, PatternWidth * ARTOOLKIT_TO_UNITY);
					break;
				case TrackableType.Multimarker:
					if (string.IsNullOrEmpty(MultiConfigFile)) {
						ARController.Log(string.Format(LOAD_FAILURE, "multimarker due to no MultiConfigFile"));
						return;
					}
					path = Path.Combine(MULTI_FORMAT, MultiConfigFile + MULTI_EXT);
					ARUtilityFunctions.GetFileFromStreamingAssets(path, out assetDirectory);
					if (!string.IsNullOrEmpty(assetDirectory)) {
						configuration = string.Format(MULTI_CONFIG, assetDirectory);
					}
					break;
				default:
					// Unknown marker type?
					ARController.Log(string.Format(LOAD_FAILURE, "due to unknown marker"));
					return;
			}

			// If a valid config. could be assembled, get ARToolKit to process it, and assign the resulting ARMarker UID.
			if (string.IsNullOrEmpty(configuration)) {
				ARController.Log(LOG_TAG + "trackable configuration is null or empty.");
				return;
			}

			uid = PluginFunctions.arwAddMarker(configuration);
			if (UID == NO_ID) {
				ARController.Log(LOG_TAG + "Error loading trackable.");
				return;
			}

			// Trackable loaded. Do any additional configuration.
			if (Type == TrackableType.Square || Type == TrackableType.SquareBarcode) {
				UseContPoseEstimation = currentUseContPoseEstimation;
			}

			Filtered         = currentFiltered;
			FilterSampleRate = currentFilterSampleRate;
			FilterCutoffFreq = currentFilterCutoffFreq;

			// Retrieve any required information from the configured ARTrackable.
			if (Type == TrackableType.TwoD) {
				int dummyImageSizeX, dummyImageSizeY;
				float dummyTwoDImageHeight;
				PluginFunctions.arwGetTrackableAppearanceConfig(UID, 0, null, out TwoDImageWidth, out dummyTwoDImageHeight, out dummyImageSizeX, out dummyImageSizeY);
				TwoDImageWidth *= UNITY_TO_ARTOOLKIT;
			} else {
				// Create array of patterns. A single marker will have array length 1.
				int numPatterns = PluginFunctions.arwGetTrackableAppearanceCount(UID);
				if (numPatterns > 0) {
					patterns = new ARPattern[numPatterns];
					for (int i = 0; i < numPatterns; ++i) {
						patterns[i] = new ARPattern(UID, i);
					}
				}
			}
		}
	}

	// Unload any underlying ARToolKit structures, and clear the UID.
	public void Unload() {
		lock (loadLock) {
			if (UID == NO_ID) {
				return;
			}
            // Remove the native trackable, unless arwShutdownAR() has already been called (as it will already have been removed.)
			if (PluginFunctions.inited) {
				PluginFunctions.arwRemoveMarker(UID);
			}
			uid = NO_ID;
			patterns = null; // Delete the patterns too.
		}
	}

	private void Start() {
		if (Application.isPlaying) {
			for (int i = 0; i < transform.childCount; ++i) {
				this.transform.GetChild(i).gameObject.SetActive(false);
			}
		}
	}

	private void OnEnable() {
		Load();
	}

	private void OnDisable() {
		Unload();
	}

	// 1 - Query for visibility.
	// 2 - Determine if visibility state is new.
	// 3 - If visible, calculate marker pose.
	// 4 - If visible, set marker pose.
	// 5 - If visibility state is new, notify event receivers via "OnMarkerFound" or "OnMarkerLost".
	// 6 - If visibility state is new, set appropriate active state for marker children.
	// 7 - If visible, notify event receivers that the marker's pose has been updated via "OnMarkerTracked".
	protected virtual void LateUpdate() {
		if (!Application.isPlaying) {
			return;
		}

		float[] matrixRawArray = new float[16];
		lock(loadLock) {
			if (UID == NO_ID || !PluginFunctions.inited) {
				visible = false;
				return;
			}

			Vector3 storedScale  = transform.localScale;
			transform.localScale = Vector3.one;

			// 1 - Query for visibility.
			bool nowVisible = PluginFunctions.arwQueryMarkerTransformation(UID, matrixRawArray);

			// 2 - Determine if visibility state is new.
			bool notify = (nowVisible != visible);
			visible = nowVisible;

			// 3 - If visible, calculate marker pose.
			if (visible) {
				// Scale the position from ARToolKit units (mm) into Unity units (m).
				matrixRawArray[12] *= UNITY_TO_ARTOOLKIT;
				matrixRawArray[13] *= UNITY_TO_ARTOOLKIT;
				matrixRawArray[14] *= UNITY_TO_ARTOOLKIT;

				Matrix4x4 matrixRaw = ARUtilityFunctions.MatrixFromFloatArray(matrixRawArray);
				// ARToolKit uses right-hand coordinate system where the marker lies in x-y plane with right in direction of +x,
				// up in direction of +y, and forward (towards viewer) in direction of +z.
				// Need to convert to Unity's left-hand coordinate system where marker lies in x-y plane with right in direction of +x,
				// up in direction of +y, and forward (towards viewer) in direction of -z.
				transformationMatrix = ARUtilityFunctions.LHMatrixFromRHMatrix(matrixRaw);

				// 4 - If visible, set marker pose.
				Matrix4x4 pose = ARStaticCamera.Instance.transform.localToWorldMatrix * transformationMatrix;
				transform.position   = ARUtilityFunctions.PositionFromMatrix(pose);
				transform.rotation   = ARUtilityFunctions.RotationFromMatrix(pose);
				transform.localScale = storedScale;
			}

			// 5 - If visibility state is new, notify event receivers via "OnMarkerFound" or "OnMarkerLost".
			if (notify) {
				if (null != eventReceivers && eventReceivers.Count > 0) {
					if (visible) {
						eventReceivers.ForEach(x => x.OnMarkerFound(this));
					} else {
						eventReceivers.ForEach(x => x.OnMarkerLost(this));
					}
				}
				// 6 - If visibility state is new, set appropriate active state for marker children.
				for (int i = 0; i < transform.childCount; ++i) {
					transform.GetChild(i).gameObject.SetActive(visible);
				}
			}

            if (visible) {
                // 7 - If visible, notify event receivers that the marker's pose has been updated via "OnMarkerTracked".
                if (null != eventReceivers && eventReceivers.Count > 0) {
                    eventReceivers.ForEach (x => x.OnMarkerTracked (this));
                }
            }
		}
	}

	public void ClearUnusedValues() {
		patterns = null;
		if (type != TrackableType.Multimarker) {
			multiConfigFile = string.Empty;
		}
		if (type != TrackableType.TwoD) {
			twoDImageName = string.Empty;
			TwoDImageWidth = 0.0f;
			currentTwoDImageHeight = 1.0f;
		}
		if (type != TrackableType.Square) {
			patternContents = string.Empty;
		}
		if (type != TrackableType.SquareBarcode) {
			barcodeID = 0;
		}
        if (type != TrackableType.Square && type != TrackableType.SquareBarcode) {
			patternWidth = 0.08f;
			UseContPoseEstimation = false;
		}
	}
}