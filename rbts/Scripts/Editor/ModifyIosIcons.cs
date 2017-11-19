//author: KamorinIlya


using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.FacebookEditor;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static class ModifyIosIcons
{
	public static void ModifyIcons(string path)
    {
        string destPngDir = "Unity-iPhone/Images.xcassets/AppIcon.appiconset";
        string fullDestDir = string.Format ("{0}/{1}",path, destPngDir);

        //DirectoryInfo directoryInfo = new DirectoryInfo (path);
        string fileName = string.Format ("{0}/Contents.json",fullDestDir);

        if (!File.Exists (fileName)) 
        {
            DT.LogError("Cant modify iOS icons! File <{0}> not found!", fileName);
            return;
        }


        //string fileName = "/Iron_Tanks_5/Build_IT/Unity-iPhone/Images.xcassets/AppIcon.appiconset/Contents.json";
        IconsJsonObject jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<IconsJsonObject> (File.ReadAllText (fileName));
        if (jsonObject == null) 
        {
            Debug.LogError("jsonObject == null!");
            return;
        }

        //Delete all Png
//        string[] files = Directory.GetFiles (fullDestDir);
//        foreach(string p in files)
//            if(p.ToLower().Contains(".png"))
//                File.Delete (p);

        //Copy needed icons
        //string iconFileName = "icon_256x256@2x.png";
        File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "29.png"), string.Format("{0}/{1}",fullDestDir,"29.png"));
        File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "58.png"), string.Format("{0}/{1}",fullDestDir,"58.png"));
        File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "87.png"), string.Format("{0}/{1}",fullDestDir,"87.png"));
        File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "80.png"), string.Format("{0}/{1}",fullDestDir,"80.png"));
        //File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "120.png"), string.Format("{0}/{1}",fullDestDir,"120.png"));
        File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "40.png"), string.Format("{0}/{1}",fullDestDir,"40.png"));
        File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "50.png"), string.Format("{0}/{1}",fullDestDir,"50.png"));
        File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "100.png"), string.Format("{0}/{1}",fullDestDir,"100.png"));
        //File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "180.png"), string.Format("{0}/{1}",fullDestDir,"180.png"));
        File.Copy ( string.Format("{0}/Game_Source/Icons/{1}/{2}",Application.dataPath, GameData.CurInterface, "167.png"), string.Format("{0}/{1}",fullDestDir,"167.png"));

        //Set it to all slots
//        for(int i = 0; i < jsonObject.images.Count; i++)
//            jsonObject.images[i].filename = iconFileName;

        //Add absebtee icons
        jsonObject.images.Add (new IconJsonObject("29x29", "iphone", "29.png", "1x"));
        jsonObject.images.Add (new IconJsonObject("29x29", "iphone", "58.png", "2x"));
        jsonObject.images.Add (new IconJsonObject("29x29", "iphone", "87.png", "3x"));
        jsonObject.images.Add (new IconJsonObject("40x40", "iphone", "80.png", "2x"));
        jsonObject.images.Add (new IconJsonObject("40x40", "iphone", "Icon-120.png", "3x"));

        jsonObject.images.Add (new IconJsonObject("29x29", "ipad", "29.png", "1x"));
        jsonObject.images.Add (new IconJsonObject("29x29", "ipad", "58.png", "2x"));
        jsonObject.images.Add (new IconJsonObject("40x40", "ipad", "40.png", "1x"));
        jsonObject.images.Add (new IconJsonObject("40x40", "ipad", "80.png", "2x"));
        jsonObject.images.Add (new IconJsonObject("50x50", "ipad", "50.png", "1x"));
        jsonObject.images.Add (new IconJsonObject("50x50", "ipad", "100.png", "2x"));
        jsonObject.images.Add (new IconJsonObject("83.5x83.5", "ipad", "167.png", "2x"));

        File.WriteAllText (fileName, Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject));
        //Debug.LogError(Newtonsoft.Json.JsonConvert.SerializeObject(jsonObject));
    }



}



[Serializable]
public class IconsJsonObject
{
    public List<IconJsonObject> images;
    public IconJsonObjectInfo info;
}

[Serializable]
public class IconJsonObject
{
    public string size = "";
    public string idiom = "";
    public string filename = "";
    public string scale = "";

    public IconJsonObject(){}

    public IconJsonObject(string _size, string _idiom, string _filename, string _scale)
    {
        size = _size;
        idiom = _idiom;
        filename = _filename;
        scale = _scale;
    }

    public override string ToString ()
    {

        string temp = string.Format ("'size' : '{0}', 'idiom' : '{1}', 'filename' : '{2}', 'scale' : '{3}'", size, idiom, filename, scale);
        Debug.LogError (temp);
        return "{" + temp + "}";
    }
}

[Serializable]
public class IconJsonObjectInfo
{
    public int version = 0;
    public string author = "";
}