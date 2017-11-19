using System;
using System.Linq;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;
using System.Collections.Generic;
using XDevs.LiteralKeys;

using JSONObject = System.Collections.Generic.Dictionary<string, object>;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif

public static class HelpTools
{
    public interface IDictTransformable
    {
        Dictionary<string, object> ToDictionary();
        void LoadFromDictionary(Dictionary<string, object> dict);
    }


#if UNITY_EDITOR
    [MenuItem ("Window/Asset Server %0")]
    private static void AltZeroVersionControl ()
    {
        EditorApplication.ExecuteMenuItem ("Window/Version Control");
    }

	public static void CleanSceneFromTextures() {
		GameObject o;
		while ( (o = GameObject.Find ("tk2dSpriteFromTexture - ")) != null) {
			Object.DestroyImmediate (o);
		}
		while ( (o = GameObject.Find ("tk2dSpriteFromTexture - unknown_user")) != null) {
			Object.DestroyImmediate (o);
		}
	}

    [MenuItem("HelpTools/For artists/Find scaled objects")]
    private static void FindScaledObjects()
    {
        Transform[] objects = Selection.activeTransform.GetComponentsInChildren<Transform>(true);
        Debug.Log("Find scaled objects report:");
        foreach (Transform transform in objects)
        {
            if (transform.lossyScale != Vector3.one)
            {
                Debug.LogFormat(transform.gameObject, "Object {0} has non-one-unit scale", transform.name);
            }
        }
    }

    [MenuItem("HelpTools/FontNormalizer/IT")]
    public static void FontNormalizerIT() { FontNormalizer(Interface.IronTanks); }

    [MenuItem("HelpTools/FontNormalizer/FT")]
    public static void FontNormalizerFT() { FontNormalizer(Interface.FutureTanks); }

    [MenuItem("HelpTools/FontNormalizer/TW")]
    public static void FontNormalizerTW() { FontNormalizer(Interface.ToonWars); }

    [MenuItem("HelpTools/FontNormalizer/SJ")]
    public static void FontNormalizerSJ() { FontNormalizer(Interface.SpaceJet); }

    [MenuItem("HelpTools/FontNormalizer/AC")]
    public static void FontNormalizerAC() { FontNormalizer(Interface.ApocalypticCars); }

    [MenuItem("HelpTools/FontNormalizer/BW")]
    public static void FontNormalizerBW() { FontNormalizer(Interface.BattleOfWarplanes); }

    [MenuItem("HelpTools/FontNormalizer/BH")]
    public static void FontNormalizerBH() { FontNormalizer(Interface.BattleOfHelicopters); }

    [MenuItem("HelpTools/FontNormalizer/AR")]
    public static void FontNormalizerAR() { FontNormalizer(Interface.Armada); }

    [MenuItem("HelpTools/PowerA test")]
    public static void PowerAtest() {
        for (float v=0f; v <= 1.1f; v += 0.1f)
        {
            Debug.LogFormat("v:{0,5:N3}, Mathf.pow: {1,5:N3}, PowerA: {2,5:N3}", v, Mathf.Pow(v, 3), PowerA(v, 3));
        }
        for (float v = 0f; v <= 1.1f; v += 0.1f)
        {
            Debug.LogFormat("v:{0,5:N3}, Mathf.pow: {1,5:N3}, PowerA: {2,5:N3}", v, Mathf.Pow(v, 1f/3f), PowerA(v, 1f/3f));
        }
    }

    /*
    [MenuItem("HelpTools/Export Vehicles DB")]
    public static void ExportLocalDB()
    {
        Dictionary<string, object> result = new Dictionary<string, object>(4);
        CollectComponentsForExport<VehicleInfo>(result, "VehicleShopWindow", "vehicles");
        CollectComponentsForExport<VehicleModuleInfos>(result, "VehicleModuleInfos", "modules");
        CollectComponentsForExport<BodykitInEditor>(result, "PatternShopWindow", "patterns");
        CollectComponentsForExport<BodykitInEditor>(result, "DecalShopWindow", "decals");
        string data = Facebook.MiniJSON.Json.Serialize(result);

        var file = EditorUtility.SaveFilePanel(
                    "Save VehiclesDB as JSON",
                    "",
                    "VehiclesDB.json",
                    "json");
#if !UNITY_WEBPLAYER
        var f = File.CreateText (file);
        f.WriteLine (data);
        f.Close ();
#endif
    }
    */

    private static void CollectComponentsForExport<T>(JSONObject destination, string gameObjectName, string key) where T: MonoBehaviour, IDictTransformable
	{
		GameObject examinedGO = GameObject.Find(gameObjectName);
		if (!examinedGO)
		{
			DT.LogError("GameObject '{0} not found", gameObjectName);
			return;
		}

		T[] components = examinedGO.GetComponentsInChildren<T>(true);
		if (components == null)
		{
			DT.LogError("There is no '{0}' components in '{1}' hierarchy", typeof(T).Name, gameObjectName);
			return;
		}

		List<JSONObject> list = new List<JSONObject>(components.Length);
		foreach (var comp in components)
		{
			list.Add(comp.ToDictionary());
		}
		destination.Add(key, list);
	}

    private static void FontNormalizer(Interface iface)
    {
        tk2dFontData font = Resources.Load<tk2dFontData>(iface.ToString() + "/Fonts/fntShare");
        if (!font)
        {
            Debug.LogError("Font data not found in resources");
            return;
        }

        tk2dTextMesh[] labels = Resources.FindObjectsOfTypeAll<tk2dTextMesh>();
        DT.Log("Found {0} objects", labels.Length);
        foreach (var label in labels)
        {
            /*            if (!label.font.name.Contains("75"))
                            label.scale *= 0.73f;*/
            label.font = font;
            var agents = label.GetComponents<LocalizationFontAgent>();
            for (int i = 0; i < agents.Length - 1; i++)
            {
                Object.DestroyImmediate(agents[i], true);
            }

            if (label.gameObject.GetComponent<LocalizationFontAgent>() == null)
                label.gameObject.AddComponent<LocalizationFontAgent>();

            EditorUtility.SetDirty(label.gameObject);
        }
    }


    [MenuItem("HelpTools/Erase ALL application data")]
    private static void EraseAll() { ObscuredPrefs.DeleteAll(); }

    //[MenuItem("HelpTools/Total Cheat")]
    //private static void TotalCheat()
    //{
    //    ObscuredValuesInit();
    //    ObscuredPrefs.SetInt("Experience", 1000000);
    //    ObscuredPrefs.SetInt ("Gold", GameData.CHEAT_THRESHOLD - 100);
    //    ObscuredPrefs.SetInt("Silver", 9000000);
    //    ObscuredPrefs.SetLong("lastProfileSaveTimestamp", ObscuredPrefs.GetLong("lastProfileSaveTimestamp") + 1);
    //    ObscuredPrefs.Save();
    //}

    [MenuItem("HelpTools/Dump modules from VehicleInfo[]/at VehicleShopWindow")]
    private static void DumpPresettedModulesOnScene()
    {
        if (HangarController.Instance == null)
        {
            DT.LogError("Hangar Scene must be run!");
            return;
        }

        DumpPresettedModules(VehiclePool.Instance.Items.Select(info => info).ToArray());
    }

    [MenuItem("HelpTools/Dump modules from VehicleInfo[]/at prefabs")]
    private static void DumpPresettedModulesInPrefabs()
    {
        HangarVehicle[] hangarVehicles = Resources.LoadAll<HangarVehicle>(String.Empty);
        DumpPresettedModules(hangarVehicles.Select(hangarVehicle => hangarVehicle.Info).ToArray());
    }

    private static void DumpPresettedModules(VehicleInfo[] vehicleInfos)
    {
        string modulesSummary = String.Empty;

        foreach (VehicleInfo vehicleInfo in vehicleInfos)
            modulesSummary
                += string.Format(
                    "Vehicle: {2} (id: {13}){0}{0}{1}Cannon:{0}{1}{1}Primary gain: {3}{0}{1}{1}Secondary gain: {4}{0}"
                        + "{1}Reloader:{0}{1}{1}Primary gain: {5}{0}{1}{1}Secondary gain: {6}{0}"
                        + "{1}Armor:{0}{1}{1}Primary gain: {7}{0}{1}{1}Secondary gain: {8}{0}"
                        + "{1}Engine:{0}{1}{1}Primary gain: {9}{0}{1}{1}Secondary gain: {10}{0}"
                        + "{1}Tracks:{0}{1}{1}Primary gain: {11}{0}{1}{1}Secondary gain: {12}{0}{0}",
                    Environment.NewLine,
                    "\t",
                    vehicleInfo.vehicleName,
                    vehicleInfo.cannonUpgrades.Sum(upgrade => upgrade.primaryGain),
                    vehicleInfo.cannonUpgrades.Sum(upgrade => upgrade.secondaryGain),
                    vehicleInfo.reloaderUpgrades.Sum(upgrade => upgrade.primaryGain),
                    vehicleInfo.reloaderUpgrades.Sum(upgrade => upgrade.secondaryGain),
                    vehicleInfo.armorUpgrades.Sum(upgrade => upgrade.primaryGain),
                    vehicleInfo.armorUpgrades.Sum(upgrade => upgrade.secondaryGain),
                    vehicleInfo.engineUpgrades.Sum(upgrade => upgrade.primaryGain),
                    vehicleInfo.engineUpgrades.Sum(upgrade => upgrade.secondaryGain),
                    vehicleInfo.tracksUpgrades.Sum(upgrade => upgrade.primaryGain),
                    vehicleInfo.tracksUpgrades.Sum(upgrade => upgrade.secondaryGain),
                    vehicleInfo.id);

        modulesSummary = modulesSummary.Trim();

#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
        var path = EditorUtility.SaveFilePanel(
                    "Save presetted modules dump",
                    "",
                    "vehicleInfoModules.txt",
                    "txt");

        var file = File.CreateText(path);

        file.WriteLine(modulesSummary);

        file.Close();
#else
        DT.Log(modulesSummary);
#endif
    }



    public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs, string fileMask = "" )
    {
#if !(UNITY_WEBPLAYER || UNITY_WEBGL)
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();
        // If the destination directory doesn't exist, create it.
        if (!Directory.Exists(destDirName))
        {
            Directory.CreateDirectory(destDirName);
        }

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files;
        if(string.IsNullOrEmpty(fileMask))
            files = dir.GetFiles();
        else
            files = dir.GetFiles(fileMask);

        //DT.LogError ("Copy {0} atlas textures", files.Length);

        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.CopyTo(temppath, false);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath, copySubDirs, fileMask);
            }
        }
#endif
    }


    [MenuItem ("HelpTools/2D/Fix MeshRenderers")]
    public static void FixMeshrenderersFor2dLayer () {
        int layer = LayerMask.NameToLayer ("2D");
        var arr = Resources.FindObjectsOfTypeAll<MeshRenderer> ();
        List<MeshRenderer> lst = new List<MeshRenderer> ();
        foreach (var item in arr) {
            if (item.gameObject.layer == layer) {
                lst.Add (item);
            }
        }

        Undo.RecordObjects (lst.ToArray (), "Change MeshRenderer properties fot 2D layer");
        foreach (var r in lst) {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
    }

#endif

    //	private static void ImportComponents<T>(string gameObjectName, JSONObject data, string key)
    //		where T : MonoBehaviour, IDictTransformable
    //	{
    //		JsonPrefs jsonData = new JsonPrefs(data);
    //		T[] components = GameObject.Find(gameObjectName).GetComponentsInChildren<T>(true);
    //		Dictionary<int, T> componentsDict = new Dictionary<int, T>(components.Length);
    //		int id;
    //		foreach (T component in components)
    //		{
    //			int ind = component.gameObject.name.IndexOf('_');
    //			if (ind == -1)
    //			{
    //				DT.LogWarning(component.gameObject, "No id specified in game object name ({0})", component.gameObject.name);
    //				componentsDict.Add(0, component);
    //				continue;
    //			}
    //			if (!int.TryParse(component.gameObject.name.Substring(0, ind), out id) &&
    //				!int.TryParse(component.gameObject.name.Substring(ind + 1), out id))
    //				continue;
    //
    //			componentsDict.Add(id, component);
    //		}
    //
    //        //Debug.Log (key + " -> " + MiniJSON.Json.Serialize (jsonData.ValueObjectList (key)));
    //		List<JSONObject> list = (jsonData.ValueObjectList(key, new List<object>()).Select(x => (JSONObject)x)).ToList();
    //		foreach (JSONObject curData in list)
    //		{
    //			jsonData = new JsonPrefs(curData);
    //			int objId = jsonData.ValueInt("id");
    //			componentsDict[objId].LoadFromDictionary(curData);
    //		}
    //	}

    public static void ImportComponentsVehicleInfo(HangarVehicle[] hangarVehicles, JSONObject data, string key)
    {
        JsonPrefs jsonData = new JsonPrefs(data);

        Dictionary<int, VehicleInfo> componentsDict = new Dictionary<int, VehicleInfo>(hangarVehicles.Length);

        foreach (HangarVehicle vehicle in hangarVehicles)
            componentsDict.Add(vehicle.IdFromObjectName, vehicle.Info);

        //Debug.Log (key + " -> " + MiniJSON.Json.Serialize (jsonData.ValueObjectList (key)));
        List<JSONObject> list = (jsonData.ValueObjectList(key, new List<object>()).Select(x => (JSONObject)x)).ToList();

        foreach (JSONObject curData in list)
        {
            jsonData = new JsonPrefs(curData);

            int objId = jsonData.ValueInt("id");

            if (!componentsDict.ContainsKey(objId))
            {
                DT.LogWarning("No gameobject on the scene for loading VehicleInfo (id={0})", objId);
                continue;
            }

            componentsDict[objId].LoadFromDictionary(curData);
        }
    }

    public static void ImportComponentsBodykitInEditor(BodykitInEditor[] bodykitsInEditor, JSONObject data, string key)
    {
        Debug.Log("ImportComponentsBodykitInEditor");

        JsonPrefs jsonData = new JsonPrefs(data);

        Dictionary<int, BodykitInEditor> componentsDict = new Dictionary<int, BodykitInEditor>(bodykitsInEditor.Length);

        foreach (BodykitInEditor component in bodykitsInEditor)
        {
            int ind = component.gameObject.name.IndexOf('_');

            if (ind == -1)
            {
                DT.LogWarning(component.gameObject, "No id specified in game object name ({0})", component.gameObject.name);
                componentsDict[0] = component;
                continue;
            }

            int id;

            if (!int.TryParse(component.gameObject.name.Substring(0, ind), out id) &&
                !int.TryParse(component.gameObject.name.Substring(ind + 1), out id))
            {
                continue;
            }

            componentsDict.Add(id, component);
        }

        //Debug.Log (key + " -> " + MiniJSON.Json.Serialize (jsonData.ValueObjectList (key)));
        //List<JSONObject> list = (jsonData.ValueObjectList(key, new List<object>()).Select(x => (JSONObject)x)).ToList();
        var list = jsonData.ValueObjectList(key, null);

        if (list == null)
        {
            Debug.LogError("GameData.vehiclesDataStorage is corrupted in key '" + key + "'!!!");
            Debug.Log("GameData.vehiclesDataStorage[" + key + "] -> " + MiniJSON.Json.Serialize(jsonData.ValueObjectList(key)));
            Debug.Log("GameData.vehiclesDataStorage -> " + MiniJSON.Json.Serialize(jsonData));
            return;
        }

        foreach (JSONObject curData in list)
        {
            jsonData = new JsonPrefs(curData);

            int objId = jsonData.ValueInt("id");

            if (!componentsDict.ContainsKey(objId))
            {
                DT.LogWarning("No gameobject on the scene for loading BodyKit (id={0})", objId);
                continue;
            }

            componentsDict[objId].LoadFromDictionary(curData);
        }
    }

    public static void ImportComponentsModuleInfos(TankModuleInfos modulesInfos, JSONObject data, string key)
    {
        JsonPrefs jsonData = new JsonPrefs(data);

        //Debug.Log (key + " -> " + MiniJSON.Json.Serialize (jsonData.ValueObjectList (key)));
        List<JSONObject> list = (jsonData.ValueObjectList(key, new List<object>()).Select(x => (JSONObject)x)).ToList();

        modulesInfos.LoadFromDictionary(list[0]);
    }

    public static bool Approximately(float a, float b, float tolerance = 0.00001f)
    {
        return (Mathf.Abs(a - b) < tolerance);
    }

    /// <summary>
    /// Применение чувствительности к значению управляющей оси
    /// </summary>
    /// <param name="value">Значение оси управления</param>
    /// <param name="sensitivity">
    ///     Выбранная игроком чувствительность [0;1]. 
    ///     0 - минимальная чувствительность
    ///     0.5 - не применять изменение чувствительности
    ///     1 - максимальная чувствительность
    /// </param>
    /// <returns></returns>
    public static float ApplySensitivity (float value, float sensitivity)
    {
        if (Approximately(sensitivity, .5f)) return value;
        var sens = Mathf.Abs(1 - 2 * sensitivity);
        if (sensitivity < .5f) {
            return (1f - sens) * value + sens * value * value * value;
        }
        else {
            var v = Mathf.Abs(value);
            return ((1f - sens) * v + sens * (float)PowerA(v, 1f/3f)) *Mathf.Sign(value);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static double PowerA(double a, double b)
    {
        //int tmp = (int)(BitConverter.DoubleToInt64Bits(a) >> 32);
        //int tmp2 = (int)(b * (tmp - 1072632447) + 1072632447);
        //return BitConverter.Int64BitsToDouble(((long)tmp2) << 32);

        long tmp = BitConverter.DoubleToInt64Bits(a);
        long tmp2 = (long)(b * (tmp - 4606921280493453312L) + 4606921280493453312L);
        return BitConverter.Int64BitsToDouble(tmp2);
    }

    public static float ClampAngle(float value, float min, float max)
    {
        if (Mathf.DeltaAngle(value, min) > 0)
            return min;

        if (Mathf.DeltaAngle(value, max) < 0)
            return max;

        return value;
    }

    public static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
    {
        return Mathf.Atan2(
            y: Vector3.Dot(axis, Vector3.Cross(from, to)),
            x: Vector3.Dot(from, to)) * Mathf.Rad2Deg;
    }


    public static void ClampLabelText(tk2dTextMesh lbl, float width)
    {
        if (lbl == null)
        {
            Debug.LogError("ClampLabelText. lbl is NULL!");
            return;
        }

        string s = lbl.text;

        bool changed = false;

        while (lbl.GetEstimatedMeshBoundsForString(s).size.x > width && s.Length > 0)
        {
            s = s.Substring(0, s.Length - 1);
            changed = true;
        }

        if (changed)//Чтобы лишний раз не вызывалось событие OnChange в tk2dTextMesh (нами добавленное событие)
            lbl.text = s;
    }

    public static string To2DToolKitColorFormatString(this Color color, string toolkitFormatStringPrefix = "^C")
    {
        string r = ((int)(color.r * 255)).ToString("X2");
        string g = ((int)(color.g * 255)).ToString("X2");
        string b = ((int)(color.b * 255)).ToString("X2");
        string a = ((int)(color.a * 255)).ToString("X2");

        return string.Format("{0}{1}{2}{3}{4}", toolkitFormatStringPrefix, r, g, b, a);
    }

    public static void SetAlphaForAllWidgets(GameObject go, float alpha)
    {
        tk2dBaseSprite sprite = go.GetComponent<tk2dBaseSprite>();

        if (sprite)
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, alpha);

        tk2dTextMesh lbl = go.GetComponent<tk2dTextMesh>();

        if (lbl)
            lbl.color = new Color(lbl.color.r, lbl.color.g, lbl.color.b, alpha);
    }

    public static void SetTextToAllLabelsInCollection(IEnumerable<tk2dTextMesh> collection, string text)
    {
        if (collection != null && text != null)
            foreach (var label in collection)
                if (label)
                    label.text = text;
    }

    public static void SetSpriteToAllSpritesInCollection(IEnumerable<tk2dBaseSprite> collection, string sprName)
    {
        if (collection != null && sprName != null)
        {
            foreach (var spr in collection)
            {
                if (spr)
                    spr.SetSprite(sprName);
            }
        }
    }

    public static void SetColorToAllSpritesInCollection(IEnumerable<tk2dBaseSprite> collection, Color c)
    {
        if (collection != null)
            foreach (var spr in collection)
                if (spr)
                    spr.color = c;
    }

    public static void SetScaleToAllSpritesInCollection(IEnumerable<tk2dBaseSprite> collection, Vector3 scale)
    {
        if (collection != null)
            foreach (var spr in collection)
                if (spr)
                    spr.scale = scale;
    }

    public static void SetScaleToAllSpritesInCollection(IEnumerable<tk2dBaseSprite> collection, float x, float y)
    {
        if (collection != null)
            foreach (var spr in collection)
                if (spr)
                    spr.scale = new Vector3(x, y, spr.scale.z);
    }

    public static void SetColorToAllInterfaceElementsInCollection(IEnumerable<InterfaceElementBase> collection, Color c)
    {
        if (collection != null)
            foreach (var ie in collection)
                if (ie)
                    ie.SetColor(c);
    }

    public static void SetAlphaToAllInterfaceElementsInCollection(IEnumerable<InterfaceElementBase> collection, float alpha)
    {
        Color c;
        if (collection != null)
            foreach (var ie in collection)
                if (ie)
                {
                    c = ie.GetColor();
                    ie.SetColor(new Color(c.r, c.g, c.b, alpha));
                }
    }

    public static bool IsEmptyCollection(IEnumerable<object> collection)
    {
        return collection == null || collection.Count() == 0;
    }

    /// <summary>
	/// Parses string to TEnum without try/catch and .NET 4.5 TryParse()
	/// </summary>
	public static bool TryParseToEnum<TEnum>(string probablyEnumAsString_, out TEnum enumValue_, TEnum defaultEnumValue_, bool showWarning = false, bool ignoreCase = false) where TEnum : struct
    {
        enumValue_ = defaultEnumValue_;

        // Enum.IsDefined не имеет параметра ignoreCase
        if ((ignoreCase && !Enum.GetNames(typeof(TEnum)).Any(x => string.Equals(x, probablyEnumAsString_, StringComparison.OrdinalIgnoreCase)))
            || (!ignoreCase && !Enum.IsDefined(typeof(TEnum), probablyEnumAsString_)))
        {
            if (showWarning)
                DT.LogError("Can't parse value {0} to enum of type {1}", probablyEnumAsString_, enumValue_.GetType());
            return false;
        }
        enumValue_ = (TEnum)Enum.Parse(typeof(TEnum), probablyEnumAsString_, ignoreCase);
        return true;
    }

    /// <summary>
    /// Parses string to TEnum without try/catch and .NET 4.5 TryParse(). Short ver.
    /// </summary>
    public static bool TryParseToEnum<TEnum>(string probablyEnumAsString_, out TEnum enumValue_, bool ignoreCase = false) where TEnum : struct
    {
        TEnum en = (TEnum)Enum.GetValues(typeof(TEnum)).GetValue(0);
        return TryParseToEnum<TEnum>(probablyEnumAsString_, out enumValue_, en, false, ignoreCase);
    }

    public static TEnum GetParsedEnumValue<TEnum>(string probablyEnumAsString_, TEnum defaultEnumValue, bool showWarning = false, bool ignoreCase = false) where TEnum : struct
    {
        TEnum enumValue = defaultEnumValue;
        TryParseToEnum(probablyEnumAsString_, out enumValue, defaultEnumValue, showWarning, ignoreCase);
        return enumValue;
    }

    public static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

    public static string MakeFirstLetterBigAndSmallAnother(this string text)
    {
        if (text == null || text.Length == 0)
            return text;
        string lower = text.ToLower();
        return char.ToUpper(lower[0]) + lower.Substring(1);
    }

    public static string GetSignString(float val)
    {
        return val < 0 ? "-" : "+";
    }
}

/// <summary>
/// Создано из-за неудобства использования Vector2 - приходится постоянно преобразовывать флоат в инт
/// </summary>
public struct IntPair
{
    public int x;
    public int y;

    public IntPair(int _x = 0, int _y = 0)
    {
        x = _x;
        y = _y;
    }
}
