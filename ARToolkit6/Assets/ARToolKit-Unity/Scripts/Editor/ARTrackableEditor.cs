/*
 *  ARTrackableEditor.cs
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
 *
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.IO;

[CustomEditor(typeof(ARTrackable))]
public class ARTrackableEditor : Editor {
	private bool showGlobalSquareOptions = false;
//	private Guid   cacheGuid               = Guid.Empty;
//	// When indexes update, there are three likely scenarios:
//	//     1) ID has not changed.
//	//     2) ID has shifted by a few values due to sorting.
//	//     3) Content has been removed, and therefore there is no ID.
//	private int ReassociateContentID(int index, TrackableType markerType, string content) {
//		if (string.CompareOrdinal(ARToolKitAssetManager.AllMarkers[index], content) == 0) {
//			return index;
//		} else {
//			for (int i = 0; i < ARToolKitAssetManager.
//		}
//	}

	public override void OnInspectorGUI() {
		// Get the ARMarker that this panel will edit.
		ARTrackable arMarker = (ARTrackable)target;
        if (null == arMarker) {
			return;
		}
		// Attempt to load. Might not work out if e.g. for a single marker, pattern hasn't been
		// assigned yet, or for an NFT marker, dataset hasn't been specified.
        if (arMarker.UID == ARTrackable.NO_ID) {
            PluginFunctions.arwInitialiseAR();
            Debug.Log("ARTrackableEditor:Editor::OnInspectorGUI(): calling Load()");
			arMarker.Load();
            Debug.Log("ARTrackableEditor:Editor::OnInspectorGUI(): arMarker.UID == " + arMarker.UID);
		}

        //Check if a new image was dropped into the Project directory
        string path = Application.streamingAssetsPath + "/" + ARToolKitAssetManager.IMAGES_DIRECTORY_NAME;
        DirectoryInfo dir = new DirectoryInfo (path);
        FileInfo[] imageFileList = dir.GetFiles("*.jpg").Union(dir.GetFiles("*.jpeg")).ToArray();
        if (imageFileList != null && ARToolKitAssetManager.Images != null && imageFileList.Length != ARToolKitAssetManager.Images.Length) {

            if (imageFileList.Length < ARToolKitAssetManager.Images.Length) {
                //An image was deleted from the file system so we might have an empty ARTrackable now
                Debug.unityLogger.Log (LogType.Warning, "<color=red>Trackable image removed. Please check all ARTrackables and make sure that they have an image assigned.</color>");
            }
            //We found a new trackable in the file system or a trackable was removed lets reload the trackables.
            ARToolKitAssetManager.Reload ();
        }

        //Draw the drag n drop area
        DropAreaGUI ();
            
        int selectedMarker = ArrayUtility.IndexOf (ARToolKitAssetManager.AllMarkers, arMarker.EditorMarkerName);
		arMarker.EditorMarkerIndex = EditorGUILayout.Popup("Marker", selectedMarker, ARToolKitAssetManager.AllMarkers);

        bool newSelection = false;
        if (arMarker.EditorMarkerIndex < 0) {
            //An image was deleted from the file system so we have an empty ARTrackable now
            Debug.unityLogger.Log (LogType.Warning, "<color=red>Trackable image removed. Please check the ARTrackable and make sure that is has an image assigned.</color>");
            return;
        }
        else{
            if (string.CompareOrdinal(arMarker.EditorMarkerName, ARToolKitAssetManager.AllMarkers[arMarker.EditorMarkerIndex]) != 0) {
                newSelection = true;
                arMarker.EditorMarkerName = ARToolKitAssetManager.AllMarkers[arMarker.EditorMarkerIndex];
            }
        }

        ARTrackable.TrackableType markerType = DetermineTrackableType(arMarker.EditorMarkerIndex);
		if (arMarker.Type != markerType) {
			arMarker.ClearUnusedValues();
			arMarker.Type = markerType;
			UpdatePatternDetectionMode();
		}

        EditorGUILayout.LabelField("Type ", ARTrackable.TrackableTypeNames[arMarker.Type]);

        EditorGUILayout.LabelField("Unique ID", (arMarker.UID == ARTrackable.NO_ID ? "Not Loaded": arMarker.UID.ToString()));
		
		EditorGUILayout.BeginHorizontal();
		// Draw all the marker images
		if (arMarker.Patterns != null) {
			for (int i = 0; i < arMarker.Patterns.Length; ++i) {
				EditorGUILayout.Separator();
				GUILayout.Label(new GUIContent(string.Format("Pattern {0}, {1}m", i, arMarker.Patterns[i].width.ToString("n3")), arMarker.Patterns[i].texture), GUILayout.ExpandWidth(false)); // n3 -> 3 decimal places.
			}
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Separator();

        switch (arMarker.Type) {
            case ARTrackable.TrackableType.TwoD:
                if (newSelection) {
                    arMarker.TwoDImageName = ARToolKitAssetManager.AllMarkers[arMarker.EditorMarkerIndex];
                }
                float twoDImageHeight = EditorGUILayout.FloatField("Image height", arMarker.TwoDImageHeight);
                if (twoDImageHeight != arMarker.TwoDImageHeight) {
                    EditorUtility.SetDirty(arMarker);
                    arMarker.TwoDImageHeight = twoDImageHeight;
                }
                		
                float width = 0.0f, height = 0.0f;
                int imageWidth = 0, imageHeight = 0;
                float[] transformation = new float[16];

                Debug.Log("ARTrackableEditor:Editor::OnInspectorGUI(): calling PluginFunctions.arwGetTrackableAppearanceConfig(arMarker.UID == " + arMarker.UID + ")");
                if (PluginFunctions.arwGetTrackableAppearanceConfig(arMarker.UID, 0, transformation, out width, out height, out imageWidth, out imageHeight)) {
                    Color32[] imagePixels = new Color32[imageWidth * imageHeight];
                    if (PluginFunctions.arwGetTrackableAppearanceImage(arMarker.UID, 0, imagePixels)) {
                        //Set the texture with the trackable appearance.
                        Texture2D texture = new Texture2D(imageWidth, imageHeight, TextureFormat.RGBA32,true);
                        texture.SetPixels32(imagePixels);
                        texture.Apply();

                        //Display label and texture to the user
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Label("Trackable Appearance");

                        //Resize texture for viewport with max with and height
                        GUILayout.Label(ARTrackableAppearanceScale.BilinearWithMaxSize(texture, 200, 200));
                        EditorGUILayout.EndHorizontal();
                    }
                }
                break;
            case ARTrackable.TrackableType.Square:
				if (newSelection) {
					arMarker.PatternContents   = GetPatternContents(ARToolKitAssetManager.AllMarkers[arMarker.EditorMarkerIndex]);
				}
				arMarker.PatternWidth          = EditorGUILayout.FloatField("Pattern Width (m)",         arMarker.PatternWidth);
				arMarker.UseContPoseEstimation = EditorGUILayout.Toggle(    "Contstant Pose Estimation", arMarker.UseContPoseEstimation);
				break;
            case ARTrackable.TrackableType.SquareBarcode:
				if (newSelection) {
					string[] idArray               = ARToolKitAssetManager.AllMarkers[arMarker.EditorMarkerIndex].Split(' ');
					arMarker.BarcodeID             = int.Parse(idArray[idArray.Length - 1]);
				}
				arMarker.PatternWidth          = EditorGUILayout.FloatField("Pattern Width (m)",         arMarker.PatternWidth);
				arMarker.UseContPoseEstimation = EditorGUILayout.Toggle(    "Contstant Pose Estimation", arMarker.UseContPoseEstimation);
				break;
            case ARTrackable.TrackableType.Multimarker:
				if (newSelection) {
					arMarker.MultiConfigFile = ARToolKitAssetManager.AllMarkers[arMarker.EditorMarkerIndex];
				}
        	    break;
        }

        EditorGUILayout.Separator();
		
		arMarker.Filtered = EditorGUILayout.Toggle("Filter Pose", arMarker.Filtered);
		if (arMarker.Filtered) {
			arMarker.FilterSampleRate = EditorGUILayout.Slider("Sample Rate", arMarker.FilterSampleRate, 1.0f, 30.0f);
			arMarker.FilterCutoffFreq = EditorGUILayout.Slider("Cutoff Frequency", arMarker.FilterCutoffFreq, 1.0f, 30.0f);
		}

        if (arMarker.Type == ARTrackable.TrackableType.Square || arMarker.Type == ARTrackable.TrackableType.SquareBarcode || arMarker.Type == ARTrackable.TrackableType.Multimarker) {
			showGlobalSquareOptions = EditorGUILayout.Foldout(showGlobalSquareOptions, "Global Square Tracking Options");
			if (showGlobalSquareOptions) {
				ARController.Instance.TemplateSize = EditorGUILayout.IntSlider("Template Size (bits)", ARController.Instance.TemplateSize, 16, 64);
				
				int currentTemplateCountMax = ARController.Instance.TemplateCountMax;
				int newTemplateCountMax = EditorGUILayout.IntField("Maximum Template Count", currentTemplateCountMax);
				if (newTemplateCountMax != currentTemplateCountMax && newTemplateCountMax > 0) {
					ARController.Instance.TemplateCountMax = newTemplateCountMax;
				}
				
				bool trackInColor = EditorGUILayout.Toggle("Track Templates in Color", ARController.Instance.trackTemplatesInColor);
				if (trackInColor != ARController.Instance.trackTemplatesInColor) {
					ARController.Instance.trackTemplatesInColor = trackInColor;
					UpdatePatternDetectionMode();
				}
				
				ARController.Instance.BorderSize    = UnityEngine.Mathf.Clamp(EditorGUILayout.FloatField("Border Size (%)", ARController.Instance.BorderSize), 0.0f, 0.5f);
				ARController.Instance.LabelingMode  = (ARController.ARToolKitLabelingMode)EditorGUILayout.EnumPopup("Marker Border Color", ARController.Instance.LabelingMode);
				ARController.Instance.ImageProcMode = (ARController.ARToolKitImageProcMode)EditorGUILayout.EnumPopup("Image Processing Mode", ARController.Instance.ImageProcMode); 
			}
		}

		var obj = new SerializedObject(arMarker);
		var prop = obj.FindProperty("eventReceivers");
		EditorGUILayout.PropertyField(prop, new GUIContent("Event Receivers"), true);
		obj.ApplyModifiedProperties();
	}

	private static void UpdatePatternDetectionMode() {
		ARTrackable[] markers = FindObjectsOfType<ARTrackable>();
		
		bool trackColor = ARController.Instance.trackTemplatesInColor;
		bool templateMarkers = false;
		bool matrixMarkers   = false;
		foreach (ARTrackable marker in markers) {
			switch (marker.Type) {
            case ARTrackable.TrackableType.Multimarker:
				// Dumb default, pending introspection into dat file.
				templateMarkers = true;
				matrixMarkers = true;
				break;
            case ARTrackable.TrackableType.Square:
				templateMarkers = true;
				break;
            case ARTrackable.TrackableType.SquareBarcode:
				matrixMarkers = true;
				break;
			}
		}
		
		var mode = ARController.ARToolKitPatternDetectionMode.AR_MATRIX_CODE_DETECTION;
		if (templateMarkers && matrixMarkers) {
			if (trackColor) {
				mode = ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_COLOR_AND_MATRIX;
			} else {
				mode = ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_MONO_AND_MATRIX;
			}
		} else if (templateMarkers && !matrixMarkers) {
			if (trackColor) {
				mode = ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_COLOR;
			} else {
				mode = ARController.ARToolKitPatternDetectionMode.AR_TEMPLATE_MATCHING_MONO;
			}
		}
		
		ARController.Instance.PatternDetectionMode = mode;
	}

	
    private static ARTrackable.TrackableType DetermineTrackableType(int markerIndex) {
		int start = ARToolKitAssetManager.Images.Length;
		if (markerIndex < start) {
            return ARTrackable.TrackableType.TwoD;
		}
		start += ARToolKitAssetManager.PatternMarkers.Length;
		if (markerIndex < start) {
            return ARTrackable.TrackableType.Square;
		}
		start += ARToolKitAssetManager.Multimarkers.Length;
		if (markerIndex < start) {
            return ARTrackable.TrackableType.Multimarker;
		}
		ARController arController = ARController.Instance;
		start += ARToolKitAssetManager.GetBarcodeList(arController.MatrixCodeType).Length;
		if (markerIndex < start) {
            return ARTrackable.TrackableType.SquareBarcode;
		}
		// Default. Harmless out of range.
        return ARTrackable.TrackableType.Square;
	}

	private const string PATTERN_EXT = ".patt";
	private static string GetPatternContents(string markerName) {
		string path = Path.Combine(Application.streamingAssetsPath, ARToolKitAssetManager.PATTERN_DIRECTORY_NAME);
		path = Path.Combine(path, markerName + PATTERN_EXT);
		return File.ReadAllText(path);
	}

    //With inspiraton from here: https://gist.github.com/bzgeb/3800350
    //and here: https://forum.unity3d.com/threads/drag-and-drop-in-the-editor-explanation.223242/
    private void DropAreaGUI ()
    {
        Event currentEvent = Event.current;
        EventType currentEventType = currentEvent.type;
        Rect dropArea = GUILayoutUtility.GetRect (0.0f, 50.0f, GUILayout.ExpandWidth (true));
        GUI.Box (dropArea, "Choose your image from the box below or drop a new image here. (Only '.jpg' and '.jpeg')");

        // The DragExited event does not have the same mouse position data as the other events,
        // so it must be checked now:
        if ( currentEventType == EventType.DragExited ) DragAndDrop.PrepareStartDrag();// Clear generic data when user pressed escape. (Unfortunately, DragExited is also called when the mouse leaves the drag area)

        //No need to go further if the mouse is outside of the drop area
        if (!dropArea.Contains(currentEvent.mousePosition)) return;

        switch (currentEventType) {
        case EventType.DragUpdated:
        case EventType.DragPerform:
            if (!dropArea.Contains (currentEvent.mousePosition)) {
                Debug.unityLogger.Log (LogType.Warning, "Drop released outside the area" + currentEvent.mousePosition);
                return;
            }
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (currentEventType == EventType.DragPerform) {
                DragAndDrop.AcceptDrag ();

                //Here we look how many images were dropped into the drop area
                foreach (String path in DragAndDrop.paths) {
                    //Copy the image over into the StreamingAssets directory
                    copyToStreamingAssets(path);
                }
            }
            break;
        }
    }

    private void copyToStreamingAssets(String path){
        String fileName = path.Substring (path.LastIndexOf ('/')+1);
        String destination = Application.streamingAssetsPath + "/"+ ARToolKitAssetManager.IMAGES_DIRECTORY_NAME +"/"+ fileName;
        //Check type of file to be .jpg or .jpeg
        if (fileName.Contains (".jpg") || fileName.Contains (".jpeg")) {
            Debug.Log ("Copy image from: " + path + " to: " + destination);
            if (!File.Exists (destination)) {
                FileUtil.CopyFileOrDirectory (path, destination);
                AssetDatabase.Refresh ();
                ARToolKitAssetManager.Reload ();
            } else {
                Debug.unityLogger.Log (LogType.Error, "File with name: " + fileName + " already exists at destination: " + destination);
            }
        } else {
            //Dropped file is not a valid format print a warning\
            Debug.unityLogger.Log (LogType.Warning, "<color=red>Item with invalid extension dropped. Only .jpg and .jpeg allowed.</color>");
        }
    }
}