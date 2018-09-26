//using UnityEngine;
//using System.Collections;
//
//public abstract AARConfiguration : MonoBehaviour {
//	private const    string      LOAD_FAILURE = LOG_TAG + "Failed to load {0}. Quitting.";
//	public  abstract string      GetConfigString();
//	public  abstract ARPattern[] GetPatterns();
//	public  abstract void        ApplySettings();
//	public  static   ARPattern   GetPatterns(int uid) {
//		int numPatterns = PluginFunctions.arwGetMarkerPatternCount(uid);
//		if (numPatterns <= 0) {
//			return new ARPattern[0];
//		}
//		ARPattern[] patterns = new ARPattern[numPatterns];
//		for (int i = 0; i < numPatterns; ++i) {
//			patterns[i] = new ARPattern(uid, i);
//		}
//		return patterns;
//	}
//}
//
//public class PatternMarkerConfiguration : AARConfiguration {
//	private const string SINGLE_BUFFER_CONFIG = "square_buffer;{0};buffer={1}";
//	public        bool   ContinuousPoseEstimation;
//	public        float  PatternWidthMM;
//	public        byte[] PatternContents;
//
//	public string GetConfigString() {
//		return string.Format(SINGLE_BUFFER_CONFIG, PatternWidthMM * ARTrackable.ARTOOLKIT_TO_UNITY, PatternContents);
//	}
////		UseContPoseEstimation = ContinuousPoseEstimation;
//}
//
//public class BarcodeMarkerConfiguration : AARConfiguration {
//	private const string SINGLE_BARCODE_CONFIG = "square_barcode;{0};{1}";
//	public        bool  ContinuousPoseEstimation;
//	public        float PatternWidthMM;
//	public string GetConfigString() {
//		return string.Format(SINGLE_BARCODE_CONFIG, BarcodeID, PatternWidth * ARTrackable.ARTOOLKIT_TO_UNITY);
//	}
////		UseContPoseEstimation = ContinuousPoseEstimation;
//}
//
//public class NFTMarkerConfiguration : AARConfiguration {
//	private const    string   NFT_CONFIG = "2d;{0};{1}";
//	public           string   NFTDataName;
//	public           float    TwoDImageHeight;
//	public string GetConfigString() {
//		// Work out the configuration string to pass to ARToolKit.
//		string assetDirectory = Application.streamingAssetsPath;
//		string configuration  = string.Empty;
//
//		if (string.IsNullOrEmpty(NFTDataName)) {
//			ARController.Log(string.Format(LOAD_FAILURE, "NFT marker due to no NFTDataName"));
//			return;
//		}
//		string relative = string.Format(TWOD_FORMAT, NFTDataName);
//		foreach (string ext in NFTDataExts) {
//			assetDirectory = string.Empty;
//			string temp = relative + ext;
//			if (!ARUtilityFunctions.GetFileFromStreamingAssets(temp, out assetDirectory)) {
//				ARController.Log(string.Format(LOAD_FAILURE, relative));
//				return;
//			}
//		}
//
//		return string.Format(NFT_CONFIG, assetDirectory.Split('.')[0]);
//	}
//	//		TwoDImageHeight = currentTwoDImageHeight;
//	//		int imageSizeX, imageSizeY;
//	//		PluginFunctions.arwGetMarkerPatternConfig(UID, 0, null, out nftWidth, out nftHeight, out imageSizeX, out imageSizeY);
//	//		nftWidth  *= UNITY_TO_ARTOOLKIT;
//	//		nftHeight *= UNITY_TO_ARTOOLKIT;}	
//}
//
//public class MultiMarkerConfiguration : AARConfiguration {
//	private const string MULTI_CONFIG = "multisquare;{0}";
//	public        string MultiConfigFile;
//	public string GetConfigString() {
//		if (string.IsNullOrEmpty(MultiConfigFile)) {
//			ARController.Log(string.Format(LOAD_FAILURE, "multimarker due to no MultiConfigFile"));
//			return string.Empty;
//		}
//		string path = Path.Combine(MULTI_FORMAT, MultiConfigFile);
//		ARUtilityFunctions.GetFileFromStreamingAssets(path, out assetDirectory);
//		if (string.IsNullOrEmpty(assetDirectory)) {
//			ARController.Log(string.Format(LOAD_FAILURE, "multimarker due to asset extraction"));
//			return string.Empty;
//		}
//		return string.Format(MULTI_CONFIG, assetDirectory);;
//	}
////		uid = PluginFunctions.arwAddMarker(configuration);
//}