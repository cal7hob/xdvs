using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using PlistCS;

namespace UnityEditor.VKEditor
{
	public class FixupFiles
	{
		protected static string Load(string fullPath)
		{
			string data;
			FileInfo projectFileInfo = new FileInfo( fullPath );
			StreamReader fs = projectFileInfo.OpenText();
			data = fs.ReadToEnd();
			fs.Close();
			
			return data;
		}
		
		protected static void Save(string fullPath, string data)
		{
			System.IO.StreamWriter writer = new System.IO.StreamWriter(fullPath, false);
			writer.Write(data);
			writer.Close();
		}
		public static void AddIdCapWebBrowserComponent(string path)
		{
			string[] files = Directory.GetFiles( path, "WMAppManifest.xml", SearchOption.AllDirectories );
			if (files.Length > 1) {
				Debug.LogError("More than one WMAppManifest.xml file in the project");
				return;
			}
			
			string fullPath = files [0];
			string data = Load (fullPath);
			
			if (string.IsNullOrEmpty (data))
				return;
			
			if (data.Contains("<Capability Name=\"ID_CAP_WEBBROWSERCOMPONENT\" />"))
			{
				Console.WriteLine("Already exists");
				return;
				
			}
			
			data = Regex.Replace(data, @"<Capabilities>", "<Capabilities>\n\t\t\t<Capability Name=\"ID_CAP_WEBBROWSERCOMPONENT\" />");
			Save(fullPath,data);
		}
		
		
		public static void FixSimulator(string path)
		{
			string fullPath = Path.Combine(path, Path.Combine("Libraries", "RegisterMonoModules.cpp"));
			string data = Load (fullPath);
			
			
			data = Regex.Replace(data, @"\s+void\s+mono_dl_register_symbol\s+\(const\s+char\*\s+name,\s+void\s+\*addr\);", "");
			data = Regex.Replace(data, "typedef int gboolean;", "typedef int gboolean;\n\tvoid mono_dl_register_symbol (const char* name, void *addr);");
			
			//this only need to be done for unity 4, unity 5 declares user functions correctly
			if (GetUnityVersionNumber () < 500) {
				data = Regex.Replace(data,
				                     @"#endif\s+//\s*!\s*\(\s*TARGET_IPHONE_SIMULATOR\s*\)\s*}\s*void RegisterAllStrippedInternalCalls\s*\(\s*\)",
				                     "}\n\nvoid RegisterAllStrippedInternalCalls()");
				data = Regex.Replace(data,
				                     @"mono_aot_register_module\(mono_aot_module_mscorlib_info\);",
				                     "mono_aot_register_module(mono_aot_module_mscorlib_info);\n#endif // !(TARGET_IPHONE_SIMULATOR)");
			}
			Save (fullPath, data);
			fullPath = Path.Combine(path, Path.Combine("Classes", "UnityAppController.mm"));
			data = Load (fullPath);
			
			data = Regex.Replace (data, @"AppController_SendNotificationWithArg\(kUnityOnOpenURL, notifData\);\n",
			                      " AppController_SendNotificationWithArg(kUnityOnOpenURL, notifData);\n\t[VKSdk processOpenURL:url fromApplication:sourceApplication];\n");
			
			data = Regex.Replace (data, @"#include\s"+"\"PluginBase/AppDelegateListener.h\"",
			                      " #include \"PluginBase/AppDelegateListener.h\"\n#include \"VKSdk.h\"");
			
			Save (fullPath, data);
			fullPath = Path.Combine(path,  "Info.plist");
			Dictionary<string, object> dict = (Dictionary<string, object>)Plist.readPlist(fullPath);
			
			
			var urlType = new Dictionary<string,object> ();
			urlType.Add ("CFBundleTypeRole", "Editor");
			urlType.Add ("CFBundleURLName", "vk"+VkSettings.MobileAppId);
			urlType.Add ("CFBundleURLSchemes", new List<object>{"vk"+VkSettings.MobileAppId});

            var customScheme = new Dictionary<string, object>();
            customScheme.Add("CFBundleURLName", GameData.androidBundleIds[GameData.InterfaceToGame(GameData.CurInterface)]);
            customScheme.Add("CFBundleURLSchemes", new List<object> { GameData.androidBundleIds[GameData.InterfaceToGame(GameData.CurInterface)] });

            var listOfUrlTypes = new List<object>{urlType, customScheme};
            object cfBundleURLTypes;
            if (dict.TryGetValue ("CFBundleURLTypes", out cfBundleURLTypes)) {
                var urlTypes = (List<object>)cfBundleURLTypes;
                urlTypes.AddRange (listOfUrlTypes);
			} else {
                dict.Add ("CFBundleURLTypes", listOfUrlTypes);
			}
            object lsApplicationQueriesSchemes;
            var listOfSchemes = new List<object>{"vk","vk-share","vkauthorize"};
            if (dict.TryGetValue ("LSApplicationQueriesSchemes", out lsApplicationQueriesSchemes)) {
                var schemes = (List<object>)lsApplicationQueriesSchemes;
                schemes.AddRange (listOfSchemes);
            } else {
                dict.Add ("LSApplicationQueriesSchemes", listOfSchemes);
            }
			Plist.writeXml(dict, fullPath);
			
		}
		
		
		
		private static int GetUnityVersionNumber()
		{
			string version = Application.unityVersion;
			string[] versionComponents = version.Split('.');
			
			int majorVersion = 0;
			int minorVersion = 0;
			
			try
			{
				if (versionComponents != null && versionComponents.Length > 0 && versionComponents[0] != null)
					majorVersion = Convert.ToInt32(versionComponents[0]);
				if (versionComponents != null && versionComponents.Length > 1 && versionComponents[1] != null)
					minorVersion = Convert.ToInt32(versionComponents[1]);
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.Message);
			}
			
			return ((majorVersion * 100) + (minorVersion * 10));
		}
		
		
	}
}
