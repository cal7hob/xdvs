using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AppBuild;
using System.Linq;

#if !UNITY_WSA
using TapjoyUnity.Internal;
#endif
//using CyclicBuildState = BuildGameSwitcher.CyclicBuildState;

public class JenkinsBuilder
{
    public enum CyclicBuildState
    {
        WaitSwichPlatform,
        WaitSwichGame,
        SwichGame,
        WaitRefresh,
        WaitForCompile,
        IsBuilding,
        Finished
    }

    private class Services : Dictionary<string, AppBuild.Service>
    {
        public Services()
        {
            this["BuildSettings"] = new AppBuild.BuildSettingsOptions();
            this["PlayerSettings"] = new AppBuild.PlayerSettingsOptions();
#if UNITY_ANDROID
            this["PlayServices"] = new AppBuild.PlayServicesOptions ();
#endif
            this["UnityPurchasing"] = new AppBuild.UnityPurchasingOptions();
            this["Facebook"] = new AppBuild.FacebookOptions();
            this["PushWoosh"] = new AppBuild.PushwooshOptions();
            this["AndroidManifest"] = new AppBuild.AndroidManifestOptions();
            this["GoogleAnalytics"] = new AppBuild.GoogleAnalyticsOptions();
#if UNITY_ANDROID || UNITY_IOS
            this["UnityAds"] = new AppBuild.UnityAdsOptions();
            this["Chartboost"] = new AppBuild.ChartboostOptions();
#endif

            GameSetup gameSetup = new AppBuild.GameSetup();
            BuildGameSwitcher.Version = gameSetup.GameDataScript.GetPlayerBundleVersion(); //fix
            this["GameSetup"] = gameSetup;
            this["AppsFlyerOptions"] = new AppBuild.AppsFlyerOptions();
            this["VkontakteOptions"] = new AppBuild.VkontakteOptions();
#if UNITY_ANDROID || UNITY_IOS
            this["OdnoklassnikiOptions"] = new AppBuild.OdnoklassnikiOptions();
#endif
            // Запускать перемещение ресурсов последним
            this["ResourcesSwitcher"] = new AppBuild.ResourcesSwitcher ();
            UpdateData ();
        }

        public void UpdateData()
        {
            foreach (KeyValuePair<string, Service> service in this)
            {
                try
                {
                    service.Value.collectData();
                    service.Value.testCollectedData();
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Ошибка сбора данных билда:\n{0}\n\nСервис:\n{1}\n\nВызовы:\n{2}", e.Message, service, e.StackTrace);
                    break;
                }
            }
        }

        public void Switch(Game _game)
        {
            foreach (KeyValuePair<string, Service> service in this)
            {
                try
                {
                    service.Value.SwitchTo(_game);
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Ошибка переключения билда:\n{0}\n\nСервис:\n{1}\n\nВызовы:\n{2}", e.Message, service, e.StackTrace);
                    break;
                }
            }
        }
    }

    private class CommandLineArgs
    {
        private static Dictionary<string, string> args_;
        public string buildPathFile;
        public Game buildGame = Game.Undefined;
        public BuildTargetGroup buildGroup = BuildTargetGroup.Standalone;
        public BuildTarget buildPlatform = BuildTarget.StandaloneWindows;
        public static Dictionary<string, string> args
        {
            get
            {
                if (args_ == null) Init();
                return args_;
            }
        }

        private static void Init()
        {
            string key, value;
            string[] argsArr = Environment.GetCommandLineArgs();
            //string[] argsArr = @"C:\Program Files\Unity\Editor\Unity.exe -projectPath F:\Projects\BattleVehiclesAndroid -logFile F:\Projects\BattleVehiclesAndroid\unity3d_editor.log -batchmode -executeMethod BuildJenkins.Build -buildPathFile D:\build\IronTanksBat.apk -buildPlatform Android -buildGame IronTanks".Split(' '); //Armada SpaceJet FutureTanks -notQuit
            args_ = new Dictionary<string, string>();
            int count = argsArr.Length - 1;
            for (int i = 1; i < count;) //Array.FindIndex<string>(args, arg => arg.StartsWith("-"))
            {
                if (!(key = argsArr[i++]).StartsWith("-")) continue;
                if ((value = argsArr[i]).StartsWith("-"))
                {
                    value = "1";
                }
                else
                {
                    i++;
                }
                args_.Add(key, value);
                //Log(key + ", " + value);
            }
        }

        private static string GetCommandLineArg(string name)
        {
            if (args_ == null) Init();

            if (args_.ContainsKey(name)) return args_[name];
            return null;
        }

        public static CommandLineArgs ReadCommandLineArgs()
        {
            string arg;
            CommandLineArgs result = new CommandLineArgs();
            if ((result.buildPathFile = GetCommandLineArg("-buildPathFile")) == null) throw new Exception(@"Not selected assembly path (-buildPathFile D:\build\Test.exe)");
            result.buildPathFile = result.buildPathFile.Replace("\"", "");

            if ((arg = GetCommandLineArg("-buildGame")) == null) throw new Exception("Not selected build game (-buildGame SpaceJet)");
            if (!Enum.IsDefined(typeof(Game), arg)) throw new Exception("Not support game " + arg);
            result.buildGame = (Game)Enum.Parse(typeof(Game), arg);

            if ((arg = GetCommandLineArg ("-buildGroup")) == null) throw new Exception ("Not selected build group (-buildGroup Standalone)");
            if (!Enum.IsDefined (typeof (BuildTargetGroup), arg)) throw new Exception ("Not support build group: " + arg);
            result.buildGroup = (BuildTargetGroup)Enum.Parse (typeof (BuildTargetGroup), arg);

            if ((arg = GetCommandLineArg("-buildPlatform")) == null) throw new Exception("Not selected build platform (-buildPlatform StandaloneWindows)");
            if (!Enum.IsDefined(typeof(BuildTarget), arg)) throw new Exception("Not support platform: " + arg);
            result.buildPlatform = (BuildTarget)Enum.Parse(typeof(BuildTarget), arg);

            return result;
        }
    }

    //private const string menuPath = "Tools/Jenkins/";
    private const string REG_KEY_CYCLIC_BUILD_STATE = "BuildState";
    private const string REG_KEY_CYCLIC_COMMAND_LINE = "CommandLine";
    private static JenkinsBuilder instance_;
    private static JenkinsBuilder Instance { get { return instance_ ?? (instance_ = new JenkinsBuilder()); } }
    public static Game currentGame { get { return BuildGameSwitcher.currentGame; } }
    private Services services;
    private string state = "";
    private DateTime startTime;
    private static bool isStateControl = false;
    private string exception = null;

    //private static string projectPath_;
    //public static string projectPath { get { return projectPath_ ?? (projectPath_ = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf(Path.DirectorySeparatorChar))); } }

    //[MenuItem(menuPath + "Build")] //_F9
    public static void Build()
    {
        CommandLineArgs commandLineArgs = CommandLineArgs.ReadCommandLineArgs();
        Log("Start Build " + commandLineArgs.buildGame + " to platform " + commandLineArgs.buildPlatform + " in " + commandLineArgs.buildPathFile);
        EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.WaitSwichPlatform.ToString());
        if (!isStateControl) Init();
    }

    private static string[] GetEnabledScenes(Game game)
    {
        return (BuildSettingsOptions.scenes[game]).Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
    }

    /*private static string[] GetEnabledScenes()
    {
        List<string> EditorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled) continue;
            EditorScenes.Add(scene.path);
        }
        return EditorScenes.ToArray();
    }*/

    private static void GenericBuild(string[] scenes, string target_dir, BuildTarget build_target, BuildOptions build_options)
    {
        string path = target_dir.Substring(0, target_dir.LastIndexOf(Path.DirectorySeparatorChar));
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        //EditorUserBuildSettings.SwitchActiveBuildTarget(build_target);
        string res = BuildPipeline.BuildPlayer(scenes, target_dir, build_target, build_options);
        if (res.Length > 0)
        {
            throw new Exception("BuildPlayer failure: " + res);
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    static void Init()
    {
        if (!EditorPrefs.HasKey(REG_KEY_CYCLIC_BUILD_STATE)) return;
        if (EditorPrefs.GetString(REG_KEY_CYCLIC_BUILD_STATE) != CyclicBuildState.Finished.ToString())
        {
            if (!CommandLineArgs.args.ContainsKey("-buildPathFile"))
            {
                Log("fix finished state (start not buildPathFile): " + EditorPrefs.GetString(REG_KEY_CYCLIC_BUILD_STATE));
                EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.Finished.ToString());
            }

            Log("Init state: " + EditorPrefs.GetString(REG_KEY_CYCLIC_BUILD_STATE));
            Instance.InitServices();
            Instance.startTime = DateTime.Now;
            EditorApplication.update += Instance.StateControl;
            isStateControl = true;
        }
    }

    private void InitServices()
    {
        if (services == null) services = new Services();
    }

    public static void SwitchToGame(Game game)
    {
        EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.SwichGame.ToString());
        Instance.InitServices();
        /*if (Instance.services.ContainsKey("ResourcesSwitcher"))
        {
            Instance.services["ResourcesSwitcher"].SetParameters(false);
            Log("ResourcesSwitcher false");
        }*/
        if (Instance.services.ContainsKey("ResourcesSwitcher")) Instance.services["ResourcesSwitcher"].SetParameters(true);
        Instance.services.Switch(game);
        Instance.services.UpdateData();
        EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.WaitRefresh.ToString());
    }

    private void ResourcesSwitcher()
    {
        if (services.ContainsKey("ResourcesSwitcher"))
            ((ResourcesSwitcher)services["ResourcesSwitcher"]).MovedRes2Res();
    }

    //Запусается после компиляции скриптов
    private void StateControl()
    {
        if (EditorPrefs.HasKey(REG_KEY_CYCLIC_BUILD_STATE))
        {
            state = EditorPrefs.GetString(REG_KEY_CYCLIC_BUILD_STATE);
        }
        else
        {
            EditorApplication.update -= StateControl;
            return;
        }

        if (state == CyclicBuildState.WaitSwichPlatform.ToString()) //for (int i = 0; i < 200; i++)
        {
            if ((DateTime.Now - startTime).TotalSeconds < 10) return;
            if (CheckIsCompiling()) return;
            CommandLineArgs commandLineArgs = CommandLineArgs.ReadCommandLineArgs();
            EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.WaitSwichGame.ToString());
            if (EditorUserBuildSettings.activeBuildTarget != commandLineArgs.buildPlatform)
            {
                Log("SwitchBuildPlatform " + EditorUserBuildSettings.activeBuildTarget + " > " + commandLineArgs.buildPlatform);
                EditorUserBuildSettings.SwitchActiveBuildTarget(commandLineArgs.buildGroup, commandLineArgs.buildPlatform);
                startTime = DateTime.Now;
            }
            return;
        }

        if (state == CyclicBuildState.WaitSwichGame.ToString()) //for (int i = 0; i < 200; i++)
        {
            if ((DateTime.Now - startTime).TotalSeconds < 10) return;
            if (CheckIsCompiling()) return;
            CommandLineArgs commandLineArgs = CommandLineArgs.ReadCommandLineArgs();
            EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.WaitForCompile.ToString());
            if (commandLineArgs.buildGame != currentGame)
            {
                Log("SwitchGame " + currentGame + " > " + commandLineArgs.buildGame);
                SwitchToGame(commandLineArgs.buildGame);
                startTime = DateTime.Now;
            }
            return;
        }

        //Даем скриптам докомпилироваться еще чуть чуть после окончания компилирования :-)     чтобы не было ошибок OnGUI
        if (state == CyclicBuildState.WaitRefresh.ToString()) //for (int i = 0; i < 200; i++)
        {
            if ((DateTime.Now - startTime).TotalSeconds < 10) return;
            if (CheckIsCompiling()) return;
            Log("Refresh");
            EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.WaitForCompile.ToString());
            AssetDatabase.Refresh();
            services.UpdateData();
            startTime = DateTime.Now;
            return;
        }
        
        if (state == CyclicBuildState.WaitForCompile.ToString())
        {
            if ((DateTime.Now - startTime).TotalSeconds < 10) return; //for (int i = 0; i < 1000; i++)
            if (CheckIsCompiling()) return;
            EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.IsBuilding.ToString());
            CommandLineArgs commandLineArgs = CommandLineArgs.ReadCommandLineArgs();
            Log("Build game " + commandLineArgs.buildGame + " to platform " + commandLineArgs.buildPlatform + " in " + commandLineArgs.buildPathFile);

            try
            {
                if (commandLineArgs.buildPlatform == BuildTarget.Android) {
                    PlayerSettings.Android.keystoreName = System.IO.Path.GetFullPath (Application.dataPath + "/../debug.keystore");
                    PlayerSettings.Android.keystorePass = "xdevsdebug";
                    PlayerSettings.Android.keyaliasName = "xdevsdebug";
                    PlayerSettings.Android.keyaliasPass = "xdevsdebug";
                }
                GenericBuild (GetEnabledScenes(commandLineArgs.buildGame), commandLineArgs.buildPathFile, commandLineArgs.buildPlatform, BuildOptions.None);
            }
            catch (Exception e)
            {
                exception = e.Message + " " + e.StackTrace;
                Log("error " + exception);
            }

            Log("Build_end");
            EditorPrefs.SetString(REG_KEY_CYCLIC_BUILD_STATE, CyclicBuildState.Finished.ToString());
            return;
        }

        if (state == CyclicBuildState.Finished.ToString())
        {
            Log("ResourcesSwitcher");
            ResourcesSwitcher();
            EditorApplication.update -= StateControl;
            Log("End");
            //if (CommandLineArgs.args.ContainsKey("-notQuit")) return;
            if (exception != null)
            {
                if (exception.Contains("Facebook"))
                {
                    Log("errorInfo fix Facebook" + exception);
                    EditorApplication.Exit(0);
                }
                Log("errorInfo " + exception);
                EditorApplication.Exit(1);
            }
            EditorApplication.Exit(0);
            return;
        }

    }

    public bool CheckIsCompiling()
    {
        if (EditorApplication.isCompiling)
        {
            Log("Add time for " + EditorPrefs.GetString(REG_KEY_CYCLIC_BUILD_STATE));
            startTime = DateTime.Now;
            return true;
        }
        return false;
    }

    private static void Log(object message)
    {
        Debug.Log("[" + DateTime.Now.ToString("HH:mm:ss") + "] !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! " + message);
    }

    /*private static void DebugPrint(string[] str)
    {
        foreach (string item in str) Log(item);
    }*/

    /*public static void CopyProject(string projectName)
    {

        List<string> link = new List<string>() { "Assets", "ProjectSettings", "SpriteCollections" };
        DirectoryInfo directoryProject = new DirectoryInfo(projectPath);

        string target = directoryProject.Parent.FullName + Path.DirectorySeparatorChar + projectName;  //directoryProject.FullName.Substring(0, directoryProject.FullName.LastIndexOf(Path.DirectorySeparatorChar)) + Path.DirectorySeparatorChar + projectName;
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);
        Log(target);

        foreach (DirectoryInfo directory in directoryProject.GetDirectories())
        {
            //Log(directory.Name);
            if (link.Contains(directory.Name))
            {
                Log(directory.FullName + " > " + target + Path.DirectorySeparatorChar + directory.Name);
                //string out_;
                //GitVersionControl.ProcessAPI.Start(out out_, Environment.SystemDirectory, "cmd.exe", "/c mklink.exe /j " + target + Path.DirectorySeparatorChar + directory.Name + " " + directory.FullName);
                //Log("_ " + out_);
                //CreateSymbolicLink(directory.FullName, target + Path.DirectorySeparatorChar + directory.Name, SymbolicLink.Directory);
                //if (!CreateSymbolicLinkW(directory.FullName, target + Path.DirectorySeparatorChar + directory.Name, SymbolicLink.Directory)) Log("error " + GetLastError());
                continue;
            }

            if (directory.Name.StartsWith(".")) continue;
            //Copy(directory.FullName, target);
        }

        foreach (FileInfo fi in directoryProject.GetFiles())
        {
            fi.CopyTo(target + Path.DirectorySeparatorChar + fi.Name, true);
        }
    }

    public static void Copy(string sourcePath, string destinationPath)
    {
        foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)) Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
        foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)) File.Copy(filePath, filePath.Replace(sourcePath, destinationPath), true);
    }

    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    [DllImport("kernel32.dll")]
    static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

    [DllImport("kernel32.dll")]
    static extern bool CreateSymbolicLinkW(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

    enum SymbolicLink
    {
        File = 0,
        Directory = 1
    }*/
}
