using System.Collections.Generic;
using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class VkSettings : ScriptableObject 
{
    private const string VkSettingsAssetName = "VkSettings";
    private const string VkSettingsPath = "Scripts/SocialNetworks/Resources";
    private const string VkSettingsAssetExtension = ".asset";

    private static VkSettings instance;
    private static VkSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load(VkSettingsAssetName) as VkSettings;
                if (instance == null)
                {
                    instance = ScriptableObject.CreateInstance<VkSettings>();
#if UNITY_EDITOR
                    string properPath = Path.Combine(Application.dataPath, VkSettingsPath);
                    if (!Directory.Exists(properPath))
                    {
                        Directory.CreateDirectory(properPath);
                    }

                    string fullPath = Path.Combine(
                        Path.Combine("Assets", VkSettingsPath),
                        VkSettingsAssetName + VkSettingsAssetExtension);
                    AssetDatabase.CreateAsset(instance, fullPath);
#endif
                }
            }

            return instance;
        }
    }

    [SerializeField]
    private string mobileAppId = "0";
    [SerializeField]
    private string[] levelImgIds = new string[]{};
    [SerializeField]
    private List<AchievementsIds.Id> achievementImgIdsKeys = new List<AchievementsIds.Id> { };
    [SerializeField]
    private List<string> achievementImgIdsValues = new List<string> { };
    [SerializeField]
    private string gameGroupId = "0";
    [SerializeField]
    private string vkGamingGroupId = "78616012";
    [SerializeField]
    private string winPhoneProductId = "1adee049-b6d7-4d12-967d-3774b810ab3f";
    private Dictionary<AchievementsIds.Id,string> achievementImgIds = new Dictionary<AchievementsIds.Id, string>();
    public static string MobileAppId
    {
        get
        {
            return Instance.mobileAppId;
        }
        set
        {
            if (Instance.mobileAppId != value)
            {
                Instance.mobileAppId = value;
                DirtyEditor();
            }
        }
    }
    public static string[] LevelImgIds
    {
        get
        {
            return Instance.levelImgIds;
        }
        set
        {
            if (Instance.levelImgIds != value)
            {
                Instance.levelImgIds = value;
                DirtyEditor();
            }
        }
    }
    public static Dictionary<AchievementsIds.Id, string> AchievementImgIds
    {
        get
        {
            if (Instance.achievementImgIds.Count == 0)
            {
                for (int i = 0; i < Instance.achievementImgIdsKeys.Count; ++i)
                {
                    Instance.achievementImgIds.Add(Instance.achievementImgIdsKeys[i],Instance.achievementImgIdsValues[i]);
                }
            }
            return Instance.achievementImgIds;
        }
        set
        {
            if (Instance.achievementImgIds != value)
            {
                Instance.achievementImgIds = value;
                Instance.achievementImgIdsKeys = Instance.achievementImgIds.Keys.ToList();
                Instance.achievementImgIdsValues = Instance.achievementImgIds.Values.ToList();
                DirtyEditor();
            }
        }
    }
    public static string GameGroupId
    {
        get
        {
            return Instance.gameGroupId;
        }
        set
        {
            if (Instance.gameGroupId != value)
            {
                Instance.gameGroupId = value;
                DirtyEditor();
            }
        }
    }
    public static string WinPhoneProductId
    {
        get
        {
            return Instance.winPhoneProductId;
        }
        set
        {
            if (Instance.winPhoneProductId != value)
            {
                Instance.winPhoneProductId = value;
                DirtyEditor();
            }
        }
    }
    public static string VkGamingGroupId
    {
        get
        {
            return Instance.vkGamingGroupId;
        }
        set
        {
            if (Instance.vkGamingGroupId != value)
            {
                Instance.vkGamingGroupId = value;
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
