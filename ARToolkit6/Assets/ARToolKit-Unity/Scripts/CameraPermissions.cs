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
 *  Author(s):  Daqri LLC
 *
 */
namespace Artoolkit
{

    using UnityEngine;
    using System.Runtime.InteropServices;

    public class CameraPermissions : MonoBehaviour
    {
        #if UNITY_ANDROID
        //
        //Android Plugin
        //
        private AndroidJavaObject androidPlugin = null;
        #endif

        private const string LogTag = "CameraPermissions: ";


        void Start()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
                if(androidPlugin == null)
                    connectToAndroidPlugin();
            #endif

            ConfirmCameraPermissions();

        }

        #if UNITY_ANDROID && !UNITY_EDITOR
        private void connectToAndroidPlugin(){
            ARController.Log (LogTag + "About to initialize the Android Plugin");
            using( AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer")){
                if (jc != null) {
                    using (AndroidJavaObject activity = jc.GetStatic<AndroidJavaObject> ("currentActivity")) {
                        androidPlugin = activity.Call<AndroidJavaObject> ("getARToolKitPlugin");
                        if (null == androidPlugin) {
                            ARController.Log (LogTag + "ERROR: Could not connect to ARToolKit-Android plugin! Are we missing ar6jUnity.jar?");
                        } else { 
                            androidPlugin.Call ("unityIsUp", new object[]{ true });
                        }
                    }
                }
            }
        }
        #endif

        /// <summary>
        /// Called when the user grants camera permissions in response to CheckCameraPermissions() or RequestCameraPermissions().
        /// </summary>
        /// <param name="message">(unused)</param>
        void OnCameraPermissionGranted(string message)
        {

            Debug.Log("=====>> Camera permissions granted.");

        }

        /// <summary>
        /// Called when the user denies camera permissions in response to CheckCameraPermissions() or RequestCameraPermissions().
        /// </summary>
        /// <param name="message">(unused)</param>
        void OnCameraPermissionDenied(string message)
        {

            Debug.LogWarning("=====>> Camera permissions denied.");

            if ( ShouldDisplayCameraPermissionsRationale() )
                DisplayCameraPermissionsRationale();

        }

        /// <summary>
        /// Confirms that the application has camera permissions, requesting them if necessary.
        /// </summary>
        public void ConfirmCameraPermissions()
        {

#if USAGE_EXAMPLE
            if ( HasCameraPermissions() )
                Debug.Log("ARToolKit has camera permissions.");
            else
            Debug.LogWarning("ARToolKit does not have camera permissions!");
#endif

#if !UNITY_EDITOR && UNITY_ANDROID
            if ( CheckCameraPermissions(gameObject.name) )
            Debug.Log("ARToolKit already has camera permissions.");
            else
            Debug.Log("ARToolKit is requesting camera permissions.");
#endif

#if USAGE_EXAMPLE
            RequestCameraPermissions(gameObject.name);
#endif

            if ( ShouldDisplayCameraPermissionsRationale() )
                Debug.Log("ARToolKit should explain camera permission rationale.");
            else
                Debug.Log("ARToolKit does not need to explain camera permission rationale.");

        }

        /// <summary>
        /// Determines whether the application should display camera permissions rationale.
        /// </summary>
        /// <returns><c>true</c> if camera permissions rationale should be displayed, <c>false</c> otherwise.</returns>
        public bool ShouldDisplayCameraPermissionsRationale()
        {

#if !UNITY_EDITOR && UNITY_ANDROID
            return ShouldExplainCameraPermissions();
#else
            return false;
#endif

        }

        public void DisplayCameraPermissionsRationale()
        {

            Debug.Log("Displaying camera permissions rationale view.");
            //TODO: implement view

        }

        void OnApplicationFocus(bool focus)
        {

            if ( !focus )
                return;

            Debug.Log("ARToolKit has just regained focus.");
            #if UNITY_ANDROID && !UNITY_EDITOR
                if(androidPlugin == null)
                    connectToAndroidPlugin ();
            #endif

            if ( ShouldDisplayCameraPermissionsRationale() )
                DisplayCameraPermissionsRationale();

        }

#if UNITY_IOS
        /// <summary>
        /// Determines if application has camera permissions.
        /// Note: This method may prompt a request for camera permissions; use CheckCameraPermissions() for initial access.
        /// </summary>
        /// <returns><c>true</c> if camera access is allowed; otherwise, <c>false</c>.</returns>
        //[DllImport ("__Internal")]
        //private static extern bool HasCameraPermissions();

        /// <summary>
        /// Checks the camera permissions, and requests access (via RequestCameraPermissions) if not already allowed.
        /// </summary>
        /// <returns><c>true</c> if camera access was previously granted, <c>false</c> otherwise.</returns>
        /// <param name="gameObject">Name of game object to receive permission notifications.</param>
        //[DllImport ("__Internal")]
        //private static extern bool CheckCameraPermissions(string gameObject);

        /// <summary>
        /// Explicitly requests camera permissions for application; calls OnCameraPermissionGranted() or OnCameraPermissionDenied().
        /// </summary>
        /// <param name="gameObject">Name of game object to receive permission notifications.</param>
        //[DllImport ("__Internal")]
        //private static extern void RequestCameraPermissions(string gameObject);

        /// <summary>
        /// Determines whether application should explain the need for camera permissions.
        /// </summary>
        /// <returns><c>true</c> if camera permissions have been denied and should be explained, <c>false</c> otherwise.</returns>
        //[DllImport ("__Internal")]
        //private static extern bool ShouldExplainCameraPermissions();
#endif

#if UNITY_ANDROID
        private const int AndroidMarshmallow = 23;  // SDK version for Android 6.0 (Android M / Marshmallow)

        private bool HasCameraPermissions()
        {

            if ( !isAndroidMarshmallow() )
                return true;

            return ( this.androidPlugin.Call<bool>("hasCameraPermissions") );

        }

        private bool CheckCameraPermissions(string gameObject)
        {

            if ( !isAndroidMarshmallow() )
                return true;

            ARController.Log (LogTag + "CheckCameraPermissions called.");
            return ( this.androidPlugin.Call<bool>("checkCameraPermissions", gameObject) );

        }

        private void RequestCameraPermissions(string gameObject)
        {

            if ( !isAndroidMarshmallow() )
                return;

            this.androidPlugin.Call("requestCameraPermissions", gameObject);

        }

        private bool ShouldExplainCameraPermissions()
        {

            if ( !isAndroidMarshmallow() )
                return false;

            return ( this.androidPlugin.Call<bool>("shouldExplainCameraPermissions") );

        }

        private bool isAndroidMarshmallow()
        {

#if !UNITY_EDITOR
            int version = this.androidPlugin.Call<int>("getAndroidVersion");
            ARController.Log(LogTag+"Android SDK version: " +version);

            return ( version >= AndroidMarshmallow );
#else
            return false;
#endif

        }
#endif

    }

}
