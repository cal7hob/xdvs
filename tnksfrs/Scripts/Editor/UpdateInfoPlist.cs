//author: KamorinIlya

#if UNITY_IOS || UNITY_STANDALONE_OSX
using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.FacebookEditor;
using AppBuild;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEditor.iOS.Xcode;
using System.Security.AccessControl;


public static class UpdateInfoPlist
{
    private static string fileName = "";
    private static string fullPath = "";

#if UNITY_STANDALONE_OSX
    private static Dictionary<Interface, string> macAppCategory = new Dictionary<Interface, string>()
    {
		{Interface.Armada2, "public.app-category.sports-games"},
    };
#endif

    [PostProcessBuild(200)]
	public static void OnPostProcessBuild(BuildTarget target, string path)
	{
        fileName = "Info.plist";
#if UNITY_STANDALONE_OSX
        fileName = Path.Combine("Contents", fileName);
#endif
        fullPath = Path.Combine(path, fileName);
        //DT.LogError ("UpdateInfoPlist. fullPath = {0}", fullPath);~~
        var fbParser = new PListParser(fullPath);

#if UNITY_IOS

        //!!!!! Modify project.pbxproj !!!!!
        string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
        PBXProject proj = new PBXProject();
        proj.ReadFromString(File.ReadAllText(projPath));

        string targetGuid = proj.TargetGuidByName("Unity-iPhone");
		string debugConfig = proj.BuildConfigByName(targetGuid, "Debug");
		string releaseConfig = proj.BuildConfigByName(targetGuid, "Release");

        //Disable BITCODE
        //proj.SetBuildProperty(targetGuid, "ENABLE_BITCODE", "false");
        proj.SetBuildPropertyForConfig(debugConfig, "ENABLE_BITCODE", "false");
        proj.SetBuildPropertyForConfig(releaseConfig, "ENABLE_BITCODE", "false");
        
        //Disable DSYM generation - to speed up release building
        proj.SetBuildPropertyForConfig(releaseConfig, "DEBUG_INFORMATION_FORMAT", "dwarf");

		proj.AddFileToBuild(targetGuid, proj.AddFile("usr/lib/libsqlite3.dylib", "Frameworks/libsqlite3.dylib", PBXSourceTree.Sdk));
		proj.AddFileToBuild(targetGuid, proj.AddFile("usr/lib/libz.dylib", "Frameworks/libz.dylib", PBXSourceTree.Sdk));

/* For example...
        //pbxProj.SetBuildProperty(target, "FRAMEWORK_SEARCH_PATHS", "$(SRCROOT)/Frameworks");
        //pbxProj.AddBuildProperty(target, "FRAMEWORK_SEARCH_PATHS", "$(inherited)");

        //pbxProj.AddBuildProperty(target, "OTHER_LDFLAGS", "-ObjC");
*/

        File.WriteAllText(projPath, proj.WriteToString());


        Game game = (Game)Enum.Parse (typeof(Game), GameData.CurInterface.ToString ());//СurInterface используется потому что он работает в редакторе, даже если не isPlaying

        DT.Log("Add Facebook Scheme Suffix <{0}>", GameData.InterfaceShortName.ToLower());
        fbParser.XMLDict["FacebookUrlSchemeSuffix"] = GameData.InterfaceShortName.ToLower();

        List<object> capabilities = (List<object>)fbParser.XMLDict["UIRequiredDeviceCapabilities"];
        capabilities.Add("gamekit");
        fbParser.XMLDict["UIRequiredDeviceCapabilities"] = capabilities.ToArray();

        // Наполняем белый список разрешенными для запуска схемами
        var fbOpts = new FacebookOptions ();
        var lst = new List<object> ();
        //    "fbapi",
        //    "fbapi20130214","fbapi20130410","fbapi20130702","fbapi20131010","fbapi20131219","fbapi20140410","fbapi20140116","fbapi20150313","fbapi20150629",
        //    "fbauth",
        //    "fbauth2",
        //    "fb-messenger-api20140430","fb-messenger-api",
        //    "fbshareextension"
        foreach (var g in fbOpts.GetFields["IosURLSuffix"])
        {
            if (string.IsNullOrEmpty(g.Value)) {
                continue;
            }
            if (g.Value == Facebook.Unity.FacebookSettings.IosURLSuffix) {
                continue;
            }

            lst.Add ("fb" + fbOpts.GetFields["AppId"][g.Key] + g.Value);
        }

        foreach (var androidBundleId in GameData.androidBundleIds)
        {
            if(androidBundleId.Key != game)
                lst.Add(androidBundleId.Value);
        }

        List<object> CFBundleURLTypes;
        if (fbParser.XMLDict.ContainsKey("LSApplicationQueriesSchemes")) {
            CFBundleURLTypes = fbParser.XMLDict["LSApplicationQueriesSchemes"] as List<object>;
        }
        else {
            CFBundleURLTypes = new List<object>();
            fbParser.XMLDict["LSApplicationQueriesSchemes"] = CFBundleURLTypes;
        }

        if(!fbParser.XMLDict.ContainsKey("NSCalendarsUsageDescription"))
            fbParser.XMLDict["NSCalendarsUsageDescription"] = "This app requires access to the calendar.";
        if(!fbParser.XMLDict.ContainsKey("NSPhotoLibraryUsageDescription"))
            fbParser.XMLDict["NSPhotoLibraryUsageDescription"] = "This app requires access to the photo library.";
//        if(!fbParser.XMLDict.ContainsKey("NSCameraUsageDescription"))		//Setted in PlayerSettings
//            fbParser.XMLDict["NSCameraUsageDescription"] = "This app requires access to the camera.";
//        if(!fbParser.XMLDict.ContainsKey("NSMicrophoneUsageDescription"))
//            fbParser.XMLDict["NSMicrophoneUsageDescription"] = "This app requires access to the microphone.";

        CFBundleURLTypes.AddRange (lst);
#elif UNITY_STANDALONE_OSX
        if(macAppCategory.ContainsKey(GameData.CurInterface))
            fbParser.XMLDict["LSApplicationCategoryType"] =  macAppCategory[GameData.CurInterface];
        else
        {
            DT.LogError("Cant find game category for game {0}! Are you forgot to add it to the UpdateInfoPlist.macAppCategory dictionary?", GameData.CurInterface);
            fbParser.XMLDict["LSApplicationCategoryType"] =  macAppCategory[Interface.IronTanks];
        }

		fbParser.XMLDict["CFBundleExecutable"] = "TankForce";//GameData.CurInterface.ToString();
        fbParser.XMLDict["CFBundleIdentifier"] = GetMacBundleId();
		fbParser.XMLDict["CFBundleName"] = "Tank Force";//GameData.CurInterface.ToString();
        fbParser.XMLDict["LSMinimumSystemVersion"] = "10.11.6";

        GameObject gameDataPrefab = (GameObject)AssetDatabase.LoadAssetAtPath("Assets/Prefabs/GameData.prefab", typeof(GameObject));
        GameData gameDataScript = gameDataPrefab.GetComponent<GameData>();
        fbParser.XMLDict["CFBundleShortVersionString"] = gameDataScript.GetPlayerBundleVersion();
        fbParser.XMLDict["CFBundleVersion"] = gameDataScript.GetPlayerBundleVersion() + (gameDataScript.GetBuildNumber() == "0" ? "" : "." + gameDataScript.GetBuildNumber());
        if(gameDataScript.GetPlayerBundleVersion().Length == 0)
            DT.LogError("Invalid PlayerBundleVersion! '{0}'", gameDataScript.GetPlayerBundleVersion());
        if(gameDataScript.GetBuildNumber().Length == 0)
            DT.LogError("Invalid BuildNumber! '{0}'", gameDataScript.GetBuildNumber());

#endif

#region NSAppTransportSequrity
        PListDict ATSdic = fbParser.XMLDict.ContainsKey("NSAppTransportSecurity") ? (PListDict)fbParser.XMLDict["NSAppTransportSecurity"] : new PListDict();
        ATSdic["NSAllowsArbitraryLoads"] = true;
        
/*if(EditorUserBuildSettings.development)//Подрубаем дебаг сервер в инфо плист
        {
            PListDict domains = ATSdic.ContainsKey("NSExceptionDomains") ? (PListDict)ATSdic["NSExceptionDomains"] : new PListDict();
            PListDict gameAppTransportSequrityDict = new PListDict();
            gameAppTransportSequrityDict.Add("NSExceptionAllowsInsecureHTTPLoads", true);
            gameAppTransportSequrityDict.Add("NSIncludesSubdomains", true);
            domains.Add(Http.Manager.GetServer(GameData.CurInterface, WorldRegion.Debug).domain, gameAppTransportSequrityDict);
            ATSdic["NSExceptionDomains"] = domains;
        }*/
        
        fbParser.XMLDict["NSAppTransportSecurity"] =  ATSdic;
#endregion

        fbParser.WriteToFile();

#if UNITY_STANDALONE_OSX
        ModifyUnibillosxBundlePlist (path);
        ExecuteScript (path);
#else//iOS
        ModifyIosIcons.ModifyIcons (path);
#endif

	}


    private static void ModifyUnibillosxBundlePlist(string path)
    {
        fileName = "Contents/Plugins/unitypurchasing.bundle/Contents/Info.plist";
        fullPath = Path.Combine(path, fileName);
        //DT.LogError ("UpdateInfoPlist.ModifyUnibillosxBundlePlist fullPath = {0}", fullPath);
        var fbParser = new PListParser(fullPath);

        fbParser.XMLDict["CFBundleIdentifier"] = GetMacBundleId();

        fbParser.WriteToFile();
    }


    private static void ExecuteScript(string path)
    {
		UnityEngine.Debug.Log("TF: UpdateInfoPlist.ExecuteScript: " + path);
        ProcessStartInfo psi = new ProcessStartInfo();
		string projectFolder = new DirectoryInfo (Application.dataPath).Parent.FullName;

		psi.WorkingDirectory = Path.Combine (projectFolder, "Assets/Editor");
		psi.FileName = Path.Combine (psi.WorkingDirectory, "StartMacBuildInPerl.sh");
#region File.GetAccessControl
		//Хотел сделать чтобы если у файла не установлен флаг на выполнение - выдавать варнинг, но видимо это еще не реализовано для мака
		//При вызове метода File.GetAccessControl происходит NotImplementedException: The requested feature is not implemented.
//		FileSecurity fSecurity = File.GetAccessControl(Path.Combine(psi.WorkingDirectory, psi.FileName));
//		foreach (FileSystemAccessRule fsar in fSecurity.GetAccessRules(true, true, typeof(System.Security.Principal.NTAccount)))
//		{
//			string userName = fsar.IdentityReference.Value;
//			string userRights = fsar.FileSystemRights.ToString();
//			string userAccessType = fsar.AccessControlType.ToString();
//			UnityEngine.Debug.LogError(userName + " : " + userAccessType + " : " + userRights);
//		}
//		FileSystemAccessRule accessRule = new FileSystemAccessRule ("", FileSystemRights.ExecuteFile, AccessControlType.Allow);
//		fSecurity.AddAccessRule(accessRule);
#endregion
        psi.UseShellExecute = false;
		psi.CreateNoWindow = false; //bilo true
        psi.RedirectStandardOutput = true;
		psi.RedirectStandardError = true;
		//DT.Log (string.Format("{0} {1} {2}", path, "standaloneOSXUniversal", UnityEditor.PlayerSettings.useMacAppStoreValidation.ToString().ToLower()));
        psi.Arguments = string.Format("{0} {1} {2}", path, "standaloneOSXUniversal", UnityEditor.PlayerSettings.useMacAppStoreValidation.ToString().ToLower());

		System.Diagnostics.Process p = System.Diagnostics.Process.Start(psi);
        string strOutput = p.StandardOutput.ReadToEnd();
		string strError = p.StandardError.ReadToEnd ();
        p.WaitForExit();
		if(!string.IsNullOrEmpty(strOutput))
        	UnityEngine.Debug.Log(strOutput);
		if(!string.IsNullOrEmpty(strError))
			UnityEngine.Debug.LogError(strError);
    }

    private static string GetMacBundleId()
    {
        return GameData.androidBundleIds [GameData.InterfaceToGame (GameData.CurInterface)] + "mac";
    }

    static void FillATS (PListDict root)
    {
        PListDict NSAppTransportSecurity;
        if (!root.ContainsKey("NSAppTransportSecurity")) {
            NSAppTransportSecurity = new PListDict();
            root["NSAppTransportSecurity"] = NSAppTransportSecurity;
        }
        else {
            NSAppTransportSecurity = root["NSAppTransportSecurity"] as PListDict;
        }


        PListDict NSExceptionDomains;
        if (!NSAppTransportSecurity.ContainsKey("NSExceptionDomains"))
        {
            NSExceptionDomains = new PListDict();
            root["NSExceptionDomains"] = NSExceptionDomains;
        }
        else
        {
            NSExceptionDomains = root["NSExceptionDomains"] as PListDict;
        }

        var rec = new PListDict();
        rec["NSIncludesSubdomains"] = true;
        rec["NSThirdPartyExceptionRequiresForwardSecrecy"] = false;

        NSExceptionDomains["facebook.com"] = rec;
        NSExceptionDomains["fbcdn.net"] = rec;
        NSExceptionDomains["akamaihd.net"] = rec;
    }
}
#endif
