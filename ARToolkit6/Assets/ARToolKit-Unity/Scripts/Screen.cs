/*
 *  CameraPermissions.cs
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
 *  Author(s):  Philip Lamb, Thorsten Bux, 
 *
 */
#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// <para>Used to debug changing the orientation of the device in editor mode.</para>
/// <para>
/// <c>If you notice any missing members, add them here.</c>
/// </para>
/// </summary>
public static class Screen
{
    private static ScreenOrientation editorScreenOrientation = ScreenOrientation.Portrait;

    /// <summary>Specifies logical <see cref="Screen.orientation"/> of the screen.</summary>
    public static ScreenOrientation orientation
    {
        get { return editorScreenOrientation; }

        set { editorScreenOrientation = value; }
    }

    /// <summary>
    /// The current <see cref="Screen.width"/> of the screen window in pixels (Read
    /// Only).
    /// </summary>
    public static int width
    {
        get { return UnityEngine.Screen.width; }
    }

    /// <summary>
    /// The current <see cref="Screen.height"/> of the screen window in pixels (Read
    /// Only).
    /// </summary>
    public static int height
    {
        get { return UnityEngine.Screen.height; }
    }

    /// <summary>The current screen resolution (Read Only).</summary>
    public static Resolution currentResolution
    {
        get { return UnityEngine.Screen.currentResolution; }
    }
}
#endif
