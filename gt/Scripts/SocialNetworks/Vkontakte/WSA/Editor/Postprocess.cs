using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

namespace UnityEditor.VKEditor
{
    public static class VkWSAPostProcess
    {
        [PostProcessBuild(300)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.WSAPlayer)
                return;

            if ( (EditorUserBuildSettings.wsaSDK != WSASDK.UniversalSDK81) && (EditorUserBuildSettings.wsaSDK != WSASDK.PhoneSDK81) ) {
                return;
            }

            string fromDestination = Application.dataPath + "/Scripts/SocialNetworks/Vkontakte/WSA/Editor/Resources";
            string toDestination = path + "/" + Application.productName;

            if (EditorUserBuildSettings.wsaSDK == WSASDK.UniversalSDK81)
            {
                DirectoryCopy(fromDestination, toDestination + "/" + Application.productName + ".WindowsPhone", true);
                AddProtocol(toDestination + "/" + Application.productName + ".WindowsPhone");
                AddVkProtocolHandler(toDestination + "/" + Application.productName + ".Shared");
                ChangePhoneProductId(toDestination + "/" + Application.productName + ".WindowsPhone");
                AddVkConfig(toDestination + "/" + Application.productName + ".WindowsPhone");
            }
            else if (EditorUserBuildSettings.wsaSDK == WSASDK.PhoneSDK81)
            {
                DirectoryCopy(fromDestination, toDestination, true);
                AddProtocol(toDestination);
                AddVkProtocolHandler(toDestination);
                ChangePhoneProductId(toDestination);
                AddVkConfig(toDestination);
            }
        }

        private static void AddVkConfig(string path)
        {
            string vkConfigPath = path + "/VKConfig.xml";

            var doc = new System.Xml.XmlDocument();
            doc.Load(vkConfigPath);
            var protoName = doc.SelectSingleNode("/Extensions/Protocol/@Name");
            protoName.Value = "vk" + VkSettings.MobileAppId;
            doc.Save(vkConfigPath);

            string csprojFile = path + "/" + Application.productName;
            if (EditorUserBuildSettings.wsaSDK == WSASDK.UniversalSDK81)
            {
                csprojFile = csprojFile + ".WindowsPhone.csproj";
            }
            else if (EditorUserBuildSettings.wsaSDK == WSASDK.PhoneSDK81)
            {
                csprojFile = csprojFile + ".csproj";
            }
            doc.Load(csprojFile);
            var nsmgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("m", "http://schemas.microsoft.com/developer/msbuild/2003");

            if (doc.SelectSingleNode("/m:Project/m:ItemGroup/m:Content[@Include='VKConfig.xml']", nsmgr) != null) return;

            var node = doc.SelectSingleNode("/m:Project/m:ItemGroup",nsmgr);
            var content = doc.CreateElement(node.Prefix, "Content", node.NamespaceURI);
            {
                var a = doc.CreateAttribute("Include");
                a.Value = "VKConfig.xml";
                content.Attributes.Append(a);
                node.AppendChild(content);
            }
            doc.Save(csprojFile);
        }

        private static void ChangePhoneProductId(string path)
        {
            if (string.IsNullOrEmpty(VkSettings.WinPhoneProductId)) return;
            string fullPath = path + "/Package.appxmanifest";
            var doc = new System.Xml.XmlDocument();
            doc.Load(fullPath);
            var nsmgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("m2", "http://schemas.microsoft.com/appx/2010/manifest");
            nsmgr.AddNamespace("mp", "http://schemas.microsoft.com/appx/2014/phone/manifest");
            //var node = doc.SelectSingleNode("/m2:Package/m2:Identity/@Name", nsmgr);
            //node.Value = "";
            var node = doc.SelectSingleNode("/m2:Package/mp:PhoneIdentity/@PhoneProductId", nsmgr);
            if (node != null) node.Value = VkSettings.WinPhoneProductId;
            doc.Save(fullPath);
        }

        private static void AddVkProtocolHandler(string path)
        {
            string fullPath = path + "/App.xaml.cs";
            string data = File.ReadAllText(fullPath, Encoding.UTF8);
            data = Regex.Replace(data, @"ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;", "ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;\n\tVK.WindowsPhone.SDK_XAML.VKProtocolActivationHelper.HandleProtocolLaunch(eventArgs);");
            File.WriteAllText(fullPath, data, Encoding.UTF8);
        }

        private static void AddProtocol(string manifestPath)
        {
            var doc = new System.Xml.XmlDocument();
            doc.Load(manifestPath + "/Package.appxmanifest");
            var nsmgr = new System.Xml.XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("m2", "http://schemas.microsoft.com/appx/2010/manifest");
            
            var node = doc.SelectSingleNode("/m2:Package/m2:Applications/m2:Application/m2:Extensions/m2:Extension[@Category='windows.protocol']/m2:Protocol[@Name='vk" + VkSettings.MobileAppId + "']", nsmgr);
            if (node != null)
            {
                return;
            }

            var app = doc.SelectSingleNode("/m2:Package/m2:Applications/m2:Application", nsmgr);
            if (app == null)
            {
                throw new System.Exception("path /Package/Applications/Application not found in file " + manifestPath+ "/Package.appxmanifest");
            }

            var exts = app.SelectSingleNode("m2:Extensions", nsmgr);
            if (exts == null)
            {
                exts = doc.CreateElement(app.Prefix, "m2:Extensions", app.NamespaceURI);
                app.AppendChild(exts);
            }
            
            var extc = doc.CreateElement(app.Prefix, "Extension", app.NamespaceURI);
            {
                var a = doc.CreateAttribute("Category");
                a.Value = "windows.protocol";
                extc.Attributes.Append(a);
                exts.AppendChild(extc);
            }

            var prt = doc.CreateElement(app.Prefix, "Protocol", app.NamespaceURI);
            prt.SetAttribute("Name", "vk" + VkSettings.MobileAppId);
            var attribute = doc.CreateAttribute("m2", "DesiredView", "http://schemas.microsoft.com/appx/2013/manifest");
            attribute.Value = "useLess";
            prt.Attributes.Append(attribute);
            extc.AppendChild(prt);

            doc.Save(manifestPath + "/Package.appxmanifest");
        }

        public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                if (file.Name.EndsWith("meta")) continue;
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

    }
}
