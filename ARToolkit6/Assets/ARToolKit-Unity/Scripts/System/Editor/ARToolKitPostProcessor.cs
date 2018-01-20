/*
 *  ARToolKitPostProcessor.cs
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
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class ARToolKitPostProcessor {
#if UNITY_STANDALONE_WIN
	private const  string   EXE           = ".exe";
	private const  string   RELATIVE_PATH = "{0}_Data/Plugins/";
	private static string[] REDIST_FILES  = { "ARvideo.dll", "DSVL.dll", "pthreadVC2.dll", "vcredist.exe" };
	private const string FILE_NAME_STATUS = "ARToolKit Post Process Build Player: Operating of file {0}.";
	[PostProcessBuild(int.MaxValue)]
    public static void OnPostProcessBuild(BuildTarget target, string appPath) {
		string[] pathSplit     = appPath.Split('/');
		string   fileName      = pathSplit[pathSplit.Length - 1];
		string   pathDirectory = appPath.TrimEnd(fileName.ToCharArray());
		Debug.Log(string.Format(FILE_NAME_STATUS, fileName));
		fileName = fileName.Trim(EXE.ToCharArray());
		
		string fromPath = Path.Combine(pathDirectory, string.Format(RELATIVE_PATH, fileName));
		if (Directory.Exists(string.Format(RELATIVE_PATH, fileName))) {
			Debug.LogError("ARTOOLKIT BUILD ERROR: Couldn't data directory!");
			Debug.LogError("Please move DLLs from [appname]_data/Plugins to the same directory as the exe!");
			return;
		}

		// Error when copying to remote drives.
		if (fromPath.StartsWith ("//")) {
			fromPath = fromPath.Remove(0, 1);
		}

		foreach (string redistFile in REDIST_FILES) {
			File.Move(Path.Combine(fromPath, redistFile), Path.Combine(pathDirectory, redistFile));
		}
	}
#elif UNITY_IPHONE
    private class IosFramework {
        public  string Name, Id, RefId, LastKnownFileType, FormattedName, Path, SourceTree;

        public IosFramework(string name, string id, string refId, string lastKnownFileType, string formattedName, string path, string sourceTree) {
            Name              = name;
            Id                = id;
            RefId             = refId;
            LastKnownFileType = lastKnownFileType;
            Path              = path;
            SourceTree        = sourceTree;
            FormattedName     = formattedName;
        }
    }

    private delegate void ProcessTask(ref string source);

    private const string LOGFILE_NAME                          = "postprocess.log";
    private const string PBXJPROJ_FILE_PATH                    = "Unity-iPhone.xcodeproj/project.pbxproj";

    private const string PBXBUILDFILE_SECTION_END              = "/* End PBXBuildFile section */";
    private const string PBXBUILDFILE_STRING_FORMAT            = "\t\t{0} /* {1} in Frameworks */ = {{isa = PBXBuildFile; fileRef = {2} /* {1} */; }};\n";

    private const string PBXFILEREFERENCE_SECTION_END          = "/* End PBXFileReference section */";
    private const string PBXFILEREFERENCE_STRING_FORMAT        = "\t\t{0} /* {1} */ = {{isa = PBXFileReference; lastKnownFileType = {2}; name = {3}; path = {4}; sourceTree = {5}; }};\n";
    
    private const string PBXFRAMEWORKSBUILDPHASE_SECTION_BEGIN = "/* Begin PBXFrameworksBuildPhase section */";
    private const string PBXFRAMEWORKSBUILDPHASE_SUBSET        = "files = (";
    private const string PBXFRAMEWORKSBUILDPHASE_STRING_FORMAT = "\n\t\t\t\t{0} /* {1} in Frameworks */,";

    private const string PBXGROUP_SECTION_BEGIN                = "/* Begin PBXGroup section */";
    private const string PBXGROUP_SUBSET_1                     = "/* Frameworks */ = {";
    private const string PBXGROUP_SUBSET_2                     = "children = (";
    private const string PBXGROUP_STRING_FORMAT                = "\n\t\t\t\t{0} /* {1} */,";

    private const string ENABLE_BITCODE                        = "ENABLE_BITCODE = YES";
    private const string DISABLE_BITCODE                       = "ENABLE_BITCODE = NO";

    private static IosFramework[] iosFrameworks = {
        new IosFramework("libstdc++.6.dylib",    "E0005ED91B047A0C00FEB577", "E0005ED81B047A0C00FEB577", "\"compiled.mach-o.dylib\"",
                         "\"libstdc++.6.dylib\"", "\"usr/lib/libstdc++.6.dylib\"",                  "SDKROOT"),
        new IosFramework("Accelerate.framework", "E0005ED51B04798800FEB577", "E0005ED41B04798800FEB577", "wrapper.framework",
                         "Accelerate.framework", "System/Library/Frameworks/Accelerate.framework", "SDKROOT"),
        new IosFramework("libsqlite3.dylib",     "E0005ED91B047FF800FEB577", "E0005ED81B047FF800FEB577", "\"compiled.mach-o.dylib\"",
                         "\"libsqlite3.dylib\"", "\"usr/lib/libsqlite3.dylib\"", "SDKROOT"),
        new IosFramework("libz.dylib",           "4A38B1721E4BE21000C2919E", "4A38B1711E4BE21000C2919E", "\"compiled.mach-o.dylib\"",
                         "\"libz.dylib\"",       "\"usr/lib/libz.dylib\"", "SDKROOT"),
        new IosFramework("Security.framework",   "4A38B1701E4BE20900C2919E", "4A38B16F1E4BE20900C2919E", "wrapper.framework",
                         "Security.framework",   "System/Library/Frameworks/Security.framework", "SDKROOT")
    };

    private static StreamWriter streamWriter = null;

    [PostProcessBuildAttribute(int.MaxValue)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject) {
        string logPath = Path.Combine(pathToBuiltProject, LOGFILE_NAME);
#if UNITY_4_5 || UNITY_4_6
        if (target != BuildTarget.iPhone) {
#else
        if (target != BuildTarget.iOS) {
#endif
            Debug.LogError("ARToolKitPostProcessor::OnIosPostProcess - Called on non iOS build target!");
            return;
        } else if (File.Exists(logPath)) {
			streamWriter = new StreamWriter(logPath, true);
			streamWriter.WriteLine("OnIosPostProcess - Beginning iOS post-processing.");
			streamWriter.WriteLine("OnIosPostProcess - WARNING - Attempting to process directory that has already been processed. Skipping.");
			streamWriter.WriteLine("OnIosPostProcess - Aborted iOS post-processing.");
			streamWriter.Close();
			streamWriter = null;
        } else {
            streamWriter = new StreamWriter(logPath);
            streamWriter.WriteLine("OnIosPostProcess - Beginning iOS post-processing.");

            try {
                string pbxprojPath = Path.Combine(pathToBuiltProject, PBXJPROJ_FILE_PATH);
                if (File.Exists(pbxprojPath)) {
                    string pbxproj = File.ReadAllText(pbxprojPath);
                      
                    streamWriter.WriteLine("OnIosPostProcess - Modifying file at " + pbxprojPath);
                      
                    string pbxBuildFile            = string.Empty;
                    string pbxFileReference        = string.Empty;
                    string pbxFrameworksBuildPhase = string.Empty;
                    string pbxGroup                = string.Empty;
                    for (int i = 0; i < iosFrameworks.Length; ++i) {
                        if (pbxproj.Contains(iosFrameworks[i].Path)) {
                            streamWriter.WriteLine("OnIosPostProcess - Project already contains reference to " + iosFrameworks[i].Name + " - skipping.");
                            continue;
                        }
                        pbxBuildFile            += string.Format(PBXBUILDFILE_STRING_FORMAT,            new object[] { iosFrameworks[i].Id,            iosFrameworks[i].Name, iosFrameworks[i].RefId });
                        pbxFileReference        += string.Format(PBXFILEREFERENCE_STRING_FORMAT,        new object[] { iosFrameworks[i].RefId,         iosFrameworks[i].Name, iosFrameworks[i].LastKnownFileType,
                                                                                                                       iosFrameworks[i].FormattedName, iosFrameworks[i].Path, iosFrameworks[i].SourceTree });
                        pbxFrameworksBuildPhase += string.Format(PBXFRAMEWORKSBUILDPHASE_STRING_FORMAT, new object[] { iosFrameworks[i].Id,            iosFrameworks[i].Name });
                        pbxGroup                += string.Format(PBXGROUP_STRING_FORMAT,                new object[] { iosFrameworks[i].RefId,         iosFrameworks[i].Name });
                        streamWriter.WriteLine("OnPostProcessBuild - Processed " + iosFrameworks[i].Name);
                    }

                    int index = pbxproj.IndexOf(PBXBUILDFILE_SECTION_END);
                    pbxproj = pbxproj.Insert(index, pbxBuildFile);
                    streamWriter.WriteLine("OnPostProcessBuild - Injected PBXBUILDFILE");

                    index = pbxproj.IndexOf(PBXFILEREFERENCE_SECTION_END);
                    pbxproj = pbxproj.Insert(index, pbxFileReference);
                    streamWriter.WriteLine("OnPostProcessBuild - Injected PBXFILEREFERENCE");

                    index = pbxproj.IndexOf(PBXFRAMEWORKSBUILDPHASE_SECTION_BEGIN);
                    index = pbxproj.IndexOf(PBXFRAMEWORKSBUILDPHASE_SUBSET, index) + PBXFRAMEWORKSBUILDPHASE_SUBSET.Length;
                    pbxproj = pbxproj.Insert(index, pbxFrameworksBuildPhase);
                    streamWriter.WriteLine("OnPostProcessBuild - Injected PBXFRAMEWORKSBUILDPHASE");

                    index = pbxproj.IndexOf(PBXGROUP_SECTION_BEGIN);
                    index = pbxproj.IndexOf(PBXGROUP_SUBSET_1, index);
                    index = pbxproj.IndexOf(PBXGROUP_SUBSET_2, index) + PBXGROUP_SUBSET_2.Length;
                    pbxproj = pbxproj.Insert(index, pbxGroup);
                    streamWriter.WriteLine("OnPostProcessBuild - Injected PBXGROUP");

                    pbxproj = pbxproj.Replace(ENABLE_BITCODE, DISABLE_BITCODE);
                    streamWriter.WriteLine("OnPostProcessBuild - Disabled Bitcode");

                    File.Delete(pbxprojPath);
                    File.WriteAllText(pbxprojPath, pbxproj);

                    streamWriter.WriteLine("OnIosPostProcess - Ending iOS post-processing successfully.");
                } else {
                    streamWriter.WriteLine("OnIosPostProcess - ERROR - File " + pbxprojPath + " does not exist!");
                }
            } catch (System.Exception e) {
                streamWriter.WriteLine("ProcessSection - ERROR - " + e.Message + " : " + e.StackTrace);

            } finally {
                streamWriter.Close();
                streamWriter = null;
            }
        }
    }

#endif
}
