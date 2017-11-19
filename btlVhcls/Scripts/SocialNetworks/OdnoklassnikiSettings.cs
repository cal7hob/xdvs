using UnityEngine;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SocialNetworks
{

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
    public class OdnoklassnikiSettings : ScriptableObject
    {
        private const string OkSettingsAssetName = "OkSettings";
        private const string OkSettingsPath = "Scripts/SocialNetworks/Resources";
        private const string OkSettingsAssetExtension = ".asset";

        private static OdnoklassnikiSettings instance;

        private static OdnoklassnikiSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load(OkSettingsAssetName) as OdnoklassnikiSettings;
                    if (instance == null)
                    {
                        instance = ScriptableObject.CreateInstance<OdnoklassnikiSettings>();
#if UNITY_EDITOR
                    string properPath = Path.Combine(Application.dataPath, OkSettingsPath);
                    if (!Directory.Exists(properPath))
                    {
                        Directory.CreateDirectory(properPath);
                    }

                    string fullPath = Path.Combine(
                        Path.Combine("Assets", OkSettingsPath),
                        OkSettingsAssetName + OkSettingsAssetExtension);
                    AssetDatabase.CreateAsset(instance, fullPath);
#endif
                    }
                }

                return instance;
            }
        }

        [SerializeField]
        private string gameGroupId = "0";
        [SerializeField]
        private string baseUrlForPostImages = "0";
        
        
        public static string GameGroupId
        {
            get { return Instance.gameGroupId; }
            set
            {
                if (Instance.gameGroupId != value)
                {
                    Instance.gameGroupId = value;
                    DirtyEditor();
                }
            }
        }

        public static string BaseUrlForPostImages
        {
            get { return Instance.baseUrlForPostImages; }
            set
            {
                if (Instance.baseUrlForPostImages != value)
                {
                    Instance.baseUrlForPostImages = value;
                    DirtyEditor();
                }
            }
        }

        private static void DirtyEditor()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(Instance);
#endif
        }
    }
}