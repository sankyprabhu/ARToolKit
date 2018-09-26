/*
 *  ARPattern.cs
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
 *  Author(s): Julian Looser, Philip Lamb
 *
 */

using UnityEngine;

public class ARPattern {
    public Texture2D texture    = null;
    public Matrix4x4 matrix     = Matrix4x4.identity;
    public float     width      = 0.0f;
	public float     height     = 0.0f;
	public int       imageSizeX = 0;
	public int       imageSizeY = 0;

    public ARPattern(int markerID, int patternID) {
		float[] matrixRawArray = new float[16];
		float widthRaw = 0.0f;
		float heightRaw = 0.0f;

		// Get the pattern local transformation and size.
		if (!PluginFunctions.arwGetTrackableAppearanceConfig(markerID, patternID, matrixRawArray, out widthRaw, out heightRaw, out imageSizeX, out imageSizeY)) {
			throw new System.ArgumentException("Invalid argument", "markerID,patternID");
		}

		width  = widthRaw  * ARTrackable.UNITY_TO_ARTOOLKIT;
		height = heightRaw * ARTrackable.UNITY_TO_ARTOOLKIT;
		// Scale the position from ARToolKit units (mm) into Unity units (m).
		matrixRawArray[12] *= ARTrackable.UNITY_TO_ARTOOLKIT;
		matrixRawArray[13] *= ARTrackable.UNITY_TO_ARTOOLKIT;
		matrixRawArray[14] *= ARTrackable.UNITY_TO_ARTOOLKIT;

		Matrix4x4 matrixRaw = ARUtilityFunctions.MatrixFromFloatArray(matrixRawArray);

		// ARToolKit uses right-hand coordinate system where the marker lies in x-y plane with right in direction of +x,
		// up in direction of +y, and forward (towards viewer) in direction of +z.
		// Need to convert to Unity's left-hand coordinate system where marker lies in x-y plane with right in direction of +x,
		// up in direction of +y, and forward (towards viewer) in direction of -z.
		matrix = ARUtilityFunctions.LHMatrixFromRHMatrix(matrixRaw);

		// Handle pattern image.
		if (imageSizeX > 0 && imageSizeY > 0) {
			// Allocate a new texture for the pattern image
			texture = new Texture2D(imageSizeX, imageSizeY, TextureFormat.RGBA32, false);
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Clamp;
			texture.anisoLevel = 0;
			
			// Get the pattern image data and load it into the texture
			Color32[] colors32 = new Color32[imageSizeX * imageSizeY];
			if (PluginFunctions.arwGetTrackableAppearanceImage(markerID, patternID, colors32)) {
				texture.SetPixels32(colors32);
				texture.Apply();
			}
		}
    }
}
