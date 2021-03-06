using System;
using System.IO;
using System.Text;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AssetBundles;

public class Localizer : MonoBehaviour
{
    public enum LocalizationLanguage
    {
        Undefined	= 0,
        Russian		= 1,
        English		= 2,
        German		= 3,
        French		= 4,
        Chinese		= 5,
        Italian		= 6,
        Spanish		= 7,
        Korean      = 8,
        Vietnamese  = 9,
        Japanese    = 10,
    }

    [SerializeField]
    private LocalizationLanguage language;

    private static Localizer instance;
    private static Dictionary<string, string> locDictionary;
    private static List<LabelLocalizationAgent> agents = new List<LabelLocalizationAgent>(100);
    private static List<LocalizationFontAgent> fontAgents = new List<LocalizationFontAgent>(100);
    private static bool loaded = false;
    private static tk2dFontData currentFontData;
    private static string fontPath;

    public static tk2dFontData CurrentFontData
    {
        get { return currentFontData; }
    }

    public static bool Loaded
    {
        get { return loaded; }
    }

    /*   UNITY SECTION   */
    void Awake()
    {
        agents.Clear();
        if (instance != null)
        {
            //DT.LogError("There is more than one Localizer on the scene");
            Destroy(this);
            return;
        }

        instance = this;
        if (!loaded)
        {
            language = GetDefaultLanguage();
            StartCoroutine(LoadDictionary());
        }
    }


    /*   PUBLIC SECTION   */
    public static LocalizationLanguage Language
    {
        get { return instance.language; }
        set
        {
            if (instance.language == value)
                return;
            instance.language = value;
            if (value == LocalizationLanguage.Undefined) {
                instance.language = GetDefaultLanguage ();
            }
            instance.StartCoroutine(instance.LoadDictionary());
        }
    }

    public static LocalizationLanguage GetDefaultLanguage()
    {

#if UNITY_EDITOR
        return LocalizationLanguage.English;
#else

    #if LANG_CHINESE_ONLY
        return (int)Localizer.LocalizationLanguage.Chinese;
    #endif

    #if UNITY_ANDROID && !UNITY_EDITOR
        try 
        {
            var locale = new AndroidJavaClass("java.util.Locale");
            var localeInst = locale.CallStatic<AndroidJavaObject>("getDefault");
            var name = localeInst.Call<string>("getISO3Language");

            var rusLocales = new List<string>()
            {
                "kaz","ukr","bel","uzb","bak","tat","aze","tgk","kir","tuk","arm","hye","mon","rus"
            };      

            if (rusLocales.Contains(name))
            {
                return LocalizationLanguage.Russian;
            }
            else
            {
                switch (name)
                {
                    case "ita":
                        return LocalizationLanguage.Italian;
                    case "fra":
                        return LocalizationLanguage.French;
                    case "fre":
                        return LocalizationLanguage.French;
                    case "deu":
                        return LocalizationLanguage.German;
                    case "ger":
                        return LocalizationLanguage.German;
                    case "esl":
                        return LocalizationLanguage.Spanish;
                    case "spa":
                        return LocalizationLanguage.Spanish;
                    case "chi":
                        return LocalizationLanguage.Chinese;
                    case "zho":
                        return LocalizationLanguage.Chinese;
                    case "kor":
                        return LocalizationLanguage.Korean;
                    case "vie":
                        return LocalizationLanguage.Vietnamese;
                    case "jpn":
                        return LocalizationLanguage.Japanese;
                }
            }   
        }
        catch (System.Exception e)
        {
            Debug.Log("Can`t get default language: " + e);
        }
    #endif
        if (SocialSettings.PlatformOdnoklassniki || SocialSettings.PlatformVkontakte)
            return LocalizationLanguage.Russian;
        foreach (var i in Enum.GetValues(typeof(LocalizationLanguage)))
        {
            if (Application.systemLanguage.ToString() == i.ToString())
            {
                return (LocalizationLanguage)i;
            }
        }

        return LocalizationLanguage.English;
#endif
    }


    public static string GetText(string key, params object[] parameters)
    {
        if (!loaded)
        {
            DT.LogError("Localization is not loaded. Trying to get value for key '{0}'", key);
            return "";
        }

        string result;
        if (key == null)
            return "";

        locDictionary.TryGetValue(key, out result);
        if (string.IsNullOrEmpty(result))
        {
            DT.LogError("Localization: key '{0}' not found.", key);
            return "";
        }

        if (parameters.Length > 0 && parameters[0] != null)
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                result = result.Replace("%%" + i, parameters[i].ToString());
            }
        }

        return result;
    }

    public static void AddLabelAgent(LabelLocalizationAgent agent)
    {
        agents.Add(agent);
        if (loaded)
            agent.LocalizeLabel();
    }

    public static void AddFontAgent(LocalizationFontAgent agent)
    {
        fontAgents.Add(agent);
        if (loaded)
        {
            agent.SetFontData(currentFontData);
        }
    }

    public static void RemoveLabelAgent(LabelLocalizationAgent agent)
    {
        if (agents.Contains(agent))
            agents.Remove(agent);
    }

    public static void RemoveFontAgent(LocalizationFontAgent agent)
    {
        if (fontAgents.Contains(agent))
            fontAgents.Remove(agent);
    }

    /*   PRIVATE SECTION   */

    private IEnumerator LoadDictionary()
    {
        //string locFileName = Path.Combine(Application.streamingAssetsPath, language + ".txt");
        string[] lines;

        var op = AssetBundleManager.LoadAssetAsync ("common/localization", language.ToString(), typeof(TextAsset));
        yield return op;

        var asset = op.GetAsset<TextAsset>();
        lines = asset.text.Split(new[] { '\n' }, StringSplitOptions.None);

//#if (UNITY_ANDROID || UNITY_WEBPLAYER || UNITY_WP8 || UNITY_WEBGL) && !UNITY_EDITOR
//#if UNITY_WEBPLAYER || UNITY_WEBGL
//        locFileName = Application.dataPath + @"/StreamingAssets/" + instance.language + @".txt";
//#endif
//        WWW www = new WWW(locFileName);
//        while (!www.isDone)
//            yield return null;
//        lines = Encoding.UTF8.GetString(www.bytes, 0, www.bytes.Length).Split(new[]{'\n'}, StringSplitOptions.None);
//#elif (UNITY_WEBPLAYER || UNITY_WEBGL) && UNITY_EDITOR
//        lines = File.ReadAllLines(locFileName);
//#else
//        lines = File.ReadAllLines(locFileName, Encoding.UTF8);
//#endif
        var l_locDictionary = new Dictionary<string, string>(lines.Length);
        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.Length < 2 || line[0] == '#' || line[1] == '#')
                continue;
            string[] pair = line.Split('=');
            if (pair.Length < 2)
            {
                DT.Log("Localization loading error ({0}): line = {1}", language.ToString(), line);
                continue;
            }

            pair[1] = pair[1].Replace("\\", Environment.NewLine);

            var key = pair[0].Trim();
            var value = pair[1].Trim();

            if (l_locDictionary.ContainsKey(pair[0].Trim()))
            {
                Debug.LogError("Localization key " + key + " already exists in file " + language + ".txt");
            }
            else
            {
                l_locDictionary.Add(key, value);
            }	
        }

        // Load remote localization
        yield return LoadServerLocalization ();

        locDictionary = l_locDictionary;
        loaded = true;
        yield return null;


        LoadFont();
        InformAgents();

        Dispatcher.Send(EventId.OnLanguageChange, new EventInfo_SimpleEvent());
    }

    private static void LoadFont()
    {
        string fontSuffix = "fnt";
        bool isCommonFont = false;//Если эта переменная установлена в true - то шрифт ищется в папке Resources/Common/Fonts/

        if(IsAsianFont(Language))
        {
            fontSuffix += Language.ToString();
            isCommonFont = true;
        }
        else
            fontSuffix += "Share";

        if (fontPath == fontSuffix)
            return;

        currentFontData = Resources.Load<tk2dFontData>((isCommonFont ? "Common"  : GameManager.CurrentResourcesFolder) + "/Fonts/" + fontSuffix);
        InformFontAgents();
        Resources.UnloadUnusedAssets();

        fontPath = fontSuffix;
    }

    public static IEnumerator LoadServerLocalization () {
        if (!string.IsNullOrEmpty (GameData.localizationStamp)) {
            var langForLoad = instance.language;
            string path = LangToRemotePath (instance.language);
            var req = Http.Manager.Instance ().CreateRequest (path);
            WWW www = new WWW (req.Url);
            yield return www;
            if (!string.IsNullOrEmpty (www.error)) {
                Debug.LogErrorFormat ("Can't load server localization, error: {0}", www.error);
                yield break;
            }

            try {
                // Если во время загрузки локи быстро сменили на другой язык, то пропускаем подгрузку в словарь
                if (instance.language == langForLoad) {
                    Debug.LogFormat ("Load localization for {0}", langForLoad);
                    Dictionary<string, object> data = MiniJSON.Json.Deserialize (www.text) as Dictionary<string, object>;
                    foreach (var kvp in data) {
                        string val = kvp.Value.ToString ().Replace("\\", Environment.NewLine).Trim ();
                        locDictionary[kvp.Key] = val;
                    }
                }
                else {
                    Debug.LogFormat ("Skip load localization for {0} becase lang was changed to {1}", langForLoad, instance.language);
                }
            }
            catch (System.Exception e) {
                Debug.LogErrorFormat ("Can't decode server localization, error: {0}", e.Message);
            }
        }
    }
    public static bool IsAsianFont(LocalizationLanguage lang)
    {
        switch (lang)
        {
            case LocalizationLanguage.Chinese:
            case LocalizationLanguage.Korean:
            case LocalizationLanguage.Vietnamese:
            case LocalizationLanguage.Japanese:
                return true;
            default:
                return false;
        }
    }

    private static void InformAgents()
    {
        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].LocalizeLabel();
        }
    }

    private static void InformFontAgents()
    {
        foreach (var agent in fontAgents)
        {
            agent.SetFontData(currentFontData);
        }
    }

    public static bool ContainsKey(string key)
    {
        if (instance == null || key == null || key.Length == 0 || locDictionary == null || !loaded)
            return false;
        return locDictionary.ContainsKey(key);
    }

    private static string LangToRemotePath (LocalizationLanguage lang) {
        string path = "/translations/" + GameData.localizationStamp + "/client.";
        switch (lang) {
            case LocalizationLanguage.English:
                path += "en";
                break;
            case LocalizationLanguage.Chinese:
                path += "cn";
                break;
            case LocalizationLanguage.French:
                path += "fr";
                break;
            case LocalizationLanguage.German:
                path += "de";
                break;
            case LocalizationLanguage.Italian:
                path += "it";
                break;
            case LocalizationLanguage.Japanese:
                path += "jp";
                break;
            case LocalizationLanguage.Korean:
                path += "kr";
                break;
            case LocalizationLanguage.Russian:
                path += "ru";
                break;
            case LocalizationLanguage.Spanish:
                path += "es";
                break;
            case LocalizationLanguage.Vietnamese:
                path += "vn";
                break;
        }
        path += ".json";
        return path;
    }
}
