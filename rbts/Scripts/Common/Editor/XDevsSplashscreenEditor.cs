using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XDevs;

[CustomEditor(typeof(XdevsSplashScreen))]
public class XdevsSplashScreenEditor : Editor
{
    private XdevsSplashScreen master;
    private string splashesPath;
    private string path;

    private void OnEnable()
    {
        master = (XdevsSplashScreen)target;

        path = new string[]
                {
                    Application.dataPath,
                    "Resources",
                    ServiceSettings.Services[ServiceSettingsKeys.Service.PlayerSettingsOptions][ServiceSettingsKeys.Field.SplashscreenPath]
                }
                .Aggregate((x, y) => Path.Combine(x, y));
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        if (GUILayout.Button("Получить имена файлов по пути " + path, new GUIStyle(GUI.skin.button) { wordWrap = true }))
        {
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);

            master.splashscreens =
                files
                    .Where(file => !file.EndsWith(".meta", ignoreCase: true, culture: CultureInfo.InvariantCulture))
                    .Select(file => Path.GetFileNameWithoutExtension(file))
                    .ToArray();
        }
    }
}
