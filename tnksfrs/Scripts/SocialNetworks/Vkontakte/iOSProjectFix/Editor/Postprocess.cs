using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.VkXCodeEditor;

namespace UnityEditor.VKEditor
{
	public static class XCodePostProcess
	{
		[PostProcessBuild(300)]
		public static void OnPostProcessBuild(BuildTarget target, string path)
		{
			
			if (target.ToString() == "iOS" || target.ToString() == "iPhone")
			{
				XCProject project = new XCProject( path );
				string[] files = Directory.GetFiles( Application.dataPath, "*.vkprojmods", SearchOption.AllDirectories );
				foreach( string file in files ) {
					UnityEngine.Debug.Log("ProjMod File: "+file);
					project.ApplyMod( file );
				}
				
				project.Save();
				
				FixupFiles.FixSimulator(path);
				
			}
			//if (target.ToString () == "WP8Player") {
			//	FixupFiles.AddIdCapWebBrowserComponent(path);
			//}
			
			
		}
		
		
	}
}
