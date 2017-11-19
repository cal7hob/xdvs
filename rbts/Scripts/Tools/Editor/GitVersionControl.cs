using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class GitVersionControl : EditorWindow
{
//=============================================================================================================================
#region Git Window 
//=============================================================================================================================
    private static GitVersionControl window;

    [MenuItem("Tools/Git/Version Control %0")]
    public static void VersionControl()
    {
        //UnityEngine.Debug.Log(GitAPI.count);
        UnityEngine.Debug.Log(GitAPI.Status());
        //GitAPI.Clear();
    }

    [MenuItem("Tools/Git/History %h")]
    public static void History()
    {
        //EditorUtility.ClearProgressBar();
        UnityEngine.Debug.Log(GitAPI.History(3));
        //UnityEngine.Debug.Log(GitAPI.count);
        //EditorApplication.UnlockReloadAssemblies();
    }

    [MenuItem("Tools/Git/Update %u")]
    public static void Update()
    {
        GitAPI.Update();
    }

    /*[MenuItem("Tools/Git/Fix LFS")]
    public static void FixLFS()
    {
        GitAPI.Update();
    }*/

    //[MenuItem("Tools/Git/Test _F10")]
    static void Test()
    {
        GitAPI.UpdateLFS();
    }

    /*public static void Init()
    {
        window = GetWindow<GitVersionControl>("Git Version Control");
        window.minSize = new Vector2(400, 400); //width height
        window.Load();
    }

    public void Load()
    {

    }

    void OnGUI()
    {
        if (window == null) Init();
        //AERectPosition rect = new AERectPosition((int)window.position.width);
    }*/
    #endregion

    //=============================================================================================================================
    #region Git API
    //=============================================================================================================================
    public class GitAPI
    {
        public class ProgressBar
        {
            protected string title;
            public int percent = 0;
            protected string message_;
            public string message
            {
                get
                {
                    return message_;
                }

                set
                {
                    message_ = value.Length > 47 ? value.Substring(value.Length - 45, 45) : value; //47
                    Update();
                }
            }

            public void MessageSetFull(string message)
            {
                message_ = message;
            }

            public void Update()
            {
                EditorApplication.update += UpdateProgressBarHandler;
            }

            private void UpdateProgressBarHandler()
            {
                EditorApplication.update -= UpdateProgressBarHandler;
                EditorUtility.DisplayProgressBar(title, ToString(), percent * 0.01f);
                //Log("ProgressBarPaint " + progress.ToString());
            }

            public void ClearProgressBar()
            {
                EditorApplication.update += ClearProgressBarHandler;
            }

            private void ClearProgressBarHandler()
            {
                EditorApplication.update -= ClearProgressBarHandler;
                EditorUtility.ClearProgressBar();
            }

            public override string ToString()
            {
                return percent + "% " + message_;
            }
        }

        private const string gitPath = @"D:\ProgS\Git\bin\git.exe";//D:\ProgS\Git\mingw64\libexec\git-core //D:\ProgS\Git\mingw64\bin

        private static string projectPath_;
        public static string projectPath { get { return projectPath_ ?? (projectPath_ = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/'))); } }

        public static string History(int counts_)
        {
            string out_;
            Command(out out_, "log --pretty=format:\"%cd|%s|%an|%h\" --date=format:\"%H:%M %d.%M.%Y\" -" + counts_);
            return out_;
        }

        public static string Status()
        {
            string out_;
            Command(out out_, "status -s");
            return counts + "\n" + out_;
        }
        
        public static int counts
        {
            get
            {
                string out_ = "";
                //if (Command(out out_, "rev-list HEAD.. --count")) return int.Parse(out_);
                //if (Command(out out_, "rev-list --count ..HEAD")) return int.Parse(out_);
                //if (Command(out out_, "rev-list --count ..develop")) return int.Parse(out_);
                //if (Command(out out_, "rev-list --count develop..develop")) return int.Parse(out_);
                if (Command(out out_, "rev-list HEAD --count")) return int.Parse(out_);
                return -1;
            }
        }

        /*public static int GetCountToHash(string hash)
        {
            string out_ = "";
            Log("rev-list HEAD --count " + hash + "..");
            if (Command(out out_, "rev-list HEAD --count " + hash + ".."))
            {
                Log(out_);
                return int.Parse(out_);
            }
            return -1;
        }*/

        public static void Update()
        {
            /*string check = @"";
            string check1 = @"Downloading Assets/Armada/Models/Maps/Armada_map_01/Textures/armada_atlas_lod1_01_v2.psd (582.55 KB)";
            UpdateCommandHandler updateCommandHandler = new UpdateCommandHandler("pull origin HEAD");
            updateCommandHandler.HandlerError(check1);
            updateCommandHandler.ClearProgressBar();
            return;*/
            Clear();
            Thread.Sleep(1000);
            ProcessAPI.StartAsynch(new UpdateCommandHandler("pull origin HEAD", UpdateLFS)); //pull origin develop
        }

        public class UpdateCommandHandler : CommandHandler
        {
            public UpdateCommandHandler(string command, Action<Action> callback = null) : base(command)
            {
                progress.startCommit = counts;
                progress.endCommit = progress.startCommit;
                this.callback = callback;
                Log("Update, count commit: " + progress.startCommit);
                //EditorApplication.LockReloadAssemblies();
                //WaitUnlockReloadAssemblies(); //AssetDatabase.StopAssetEditing();
            }

            public class Progress : ProgressBar
            {
                public Progress()
                {
                    title = "Git update progress";
                }

                public enum Status
                {
                    OnStart,
                    OnUpdate,
                    OnMerge,

                    FullUpdate,
                    NotUpdate,

                    NotUpdateCommitChange,
                    NotConnect,
                    NotValidRepository,
                    UnableToAccess
                }

                public Status status = Status.OnStart;
                public int startCommit;
                public int endCommit;
                public int countCommit = 0;

                public int countFiles = 0;
                public int curretFiles = 0;

                public override string ToString()
                {
                    return percent + "% " + curretFiles + "/" + countFiles + " " + message_;
                }
            }

            public Progress progress = new Progress();
            private Action<Action> callback;

            public override void Handler(object sendingProcess, DataReceivedEventArgs outLine)
            {
                string line = outLine.Data;

                if (progress.status == Progress.Status.OnMerge)
                {
                    if (line.Contains(" files changed, "))//25 files changed, 90 insertions(+), 50 deletions(-)
                    {
                        progress.endCommit = counts;
                        progress.countCommit = progress.endCommit - progress.startCommit;
                        progress.MessageSetFull(line);
                        progress.status = Progress.Status.FullUpdate;
                        Stop();
                    }
                    progress.message = line;
                    return;
                }

                if (progress.status == Progress.Status.OnStart)
                {
                    if (line.StartsWith("Updating"))//Updating ba9f413..4bf702a
                    {
                        progress.status = Progress.Status.OnUpdate;
                        return;
                    }

                    if (line.StartsWith("Already up-to-date"))
                    {
                        progress.status = Progress.Status.NotUpdate;
                        Stop();
                        return;
                    }
                }

                if (progress.status == Progress.Status.OnUpdate)
                {
                    if (line.StartsWith("Fast-forward")) // on fast merge
                    {
                        progress.status = Progress.Status.OnMerge;
                        return;
                    }
                }

                base.Handler(sendingProcess, outLine);
            }

            /*public override void HandlerError(object sendingProcess, DataReceivedEventArgs outLine)
            {
                HandlerError(outLine.Data);
            }*/

            public override void HandlerError(object sendingProcess, DataReceivedEventArgs outLine)
            {
                string line = outLine.Data;
                if (line.StartsWith("Checking out files:")) //Checking out files:   9% (31/317)   /nDownloading Assets/Armada/Models/Maps/Armada_map_02/Textures/tex_roads_noSnow_track.psd(299.02 MB)
                {
                    string out_;
                    int sIndex = line.Length;
                    if (GetParamEnd(line, out out_, ref sIndex, "Checking out files:  ", "%"))
                    {
                        progress.percent = int.Parse(out_);
                        if (GetParam(line, out out_, ref sIndex, "(", "/"))
                        {
                            progress.curretFiles = int.Parse(out_);
                            if (progress.countFiles == 0)
                            {
                                sIndex--;
                                if (GetParam(line, out out_, ref sIndex, "/", ")"))
                                {
                                    progress.countFiles = int.Parse(out_);
                                }
                            }
                        }

                        if (GetParam(line, out out_, ref sIndex, "Downloading "))
                        {
                            progress.message = out_;
                        }
                    }
                    return;
                }

                if (line.StartsWith("Downloading"))
                {
                    string out_;
                    int sIndex = 0;
                    if (GetParam(line, out out_, ref sIndex, "Downloading "))
                    {
                        progress.message = out_;
                    }
                    return;
                }

                if (line.StartsWith("From ")) return; //From http://git.scifi-tanks.com/Games/battle-vehicles
                if (line.StartsWith("Please move or remove them before you merge.")
                    || line.StartsWith("Please commit your changes or stash them before you merge.")) // Please move or remove them before you merge.
                {
                    progress.status = Progress.Status.NotUpdateCommitChange;;
                    return;
                }
                if (line.StartsWith("Aborting")) Stop();
                if (line.Contains("* branch")) return; //* branch            HEAD       -> FETCH_HEAD

                if (line.StartsWith("fatal:"))
                {
                    if (line.Contains("Couldn't resolve host "))//fatal: unable to access 'http://git.scifi-tanks.com/Games/battle-vehicles.git/': Couldn't resolve host 'git.scifi-tanks.com'
                    {
                        progress.status = Progress.Status.NotConnect;
                    }
                    else
                    {
                        if(line.Contains("not valid: is this a git repository"))//not valid: is this a git repository
                        {
                            progress.status = Progress.Status.NotValidRepository;
                        }
                        else
                        {
                            if (line.Contains("unable to access"))//fatal: unable to access 'http://git.scifi-tanks.com/Games/battle-vehicles.git/': Failed to connect to git.scifi-tanks.com port 80: Timed out
                            {
                                progress.status = Progress.Status.UnableToAccess;
                            }
                            else
                            {
                                base.HandlerError(sendingProcess, outLine);
                            }
                        }
                    }
                    Stop();
                }

                if (line.StartsWith("error: Your local changes to the following files would be overwritten by merge:")) return;
                if (line.StartsWith("	Assets"))
                {
                    Log("merge:" + line);
                    return;
                }

                base.HandlerError(sendingProcess, outLine);
            }

            public override void Stop()
            {
                if (progress.status == Progress.Status.OnMerge) //Status.OnUpdate
                {
                    progress.status = Progress.Status.FullUpdate;
                    Log("Fix FullUpdate");
                }

                progress.ClearProgressBar();
                Thread.Sleep(300);
                if (progress.status == Progress.Status.FullUpdate)
                {
                    Log("Status: " + progress.status + ", " + progress.countCommit + " commits, " + progress.message);
                    if (callback == null)
                    {
                        RefreshAssetDatabase();
                    }
                    else
                    {
                        callback(RefreshAssetDatabase);
                    }
                }
                else
                {
                    Log("Status: " + progress.status);
                }
                base.Stop();
            }

            private void RefreshAssetDatabase()
            {
                EditorApplication.update += RefreshAssetDatabaseHandler;
            }

            private void RefreshAssetDatabaseHandler()
            {
                EditorApplication.update -= RefreshAssetDatabaseHandler;
                AssetDatabase.Refresh();
            }
            
            private static void WaitUnlockReloadAssemblies()
            {
                Thread thread = new Thread(UnlockReloadAssemblies);
                thread.Name = "WaitUnlockReloadAssemblies";
                thread.Start();
            }

            private static void UnlockReloadAssemblies()
            {
                Thread.Sleep(3000); //Log("UnlockReloadAssemblies");
                EditorApplication.update += UnlockReloadAssemblies_;
            }

            private static void UnlockReloadAssemblies_()
            {
                EditorApplication.update -= UnlockReloadAssemblies_;
                EditorApplication.UnlockReloadAssemblies(); //EditorApplication.delayCall += AssetDatabase.StartAssetEditing;
            }
        }

        public static void UpdateLFS(Action callback = null)
        {
            ProcessAPI.StartAsynch(new UpdateLFSCommandHandler("lfs pull origin HEAD", callback));
        }

        public class UpdateLFSCommandHandler : CommandHandler
        {
            public UpdateLFSCommandHandler(string command, Action callback) : base(command)
            {
                Log(command);
                this.callback = callback;
            }

            public class Progress : ProgressBar
            {
                public Progress()
                {
                    title = "Git LFS update progress";
                }

                public enum Status
                {
                    OnStart,
                    OnUpdate,
                    FullUpdate,
                    NotUpdate
                }

                public Status status = Status.OnStart;
                public int countFiles = 0;
                public int curretFiles = 0;
                public float size = 0;
                public float loadSize = 0;

                public override string ToString()
                {
                    return percent + "% " + "(" + curretFiles + " of " + curretFiles + " files)" + loadSize + " / " + size + " " + message_;
                }
            }

            public Progress progress = new Progress();
            private Action callback;

            public override void HandlerError(object sendingProcess, DataReceivedEventArgs outLine)
            {
                string line = outLine.Data;
                if (line.StartsWith("Git LFS:")) //Git LFS: (0 of 21 files) 85.54 MB / 820.34 MB
                {
                    Log(line);
                    progress.percent = 100;
                    /*string out_;
                    int sIndex = 0;
                    if (GetParam(line, out out_, ref sIndex, "Git LFS: (", " of"))
                    {
                        progress.curretFiles = int.Parse(out_);
                        if (GetParam(line, out out_, ref sIndex, " ", " files"))
                        {
                            progress.countFiles = int.Parse(out_);
                            if (GetParam(line, out out_, ref sIndex, ") ", " MB "))
                            {
                                progress.loadSize = float.Parse(out_);
                                if (GetParam(line, out out_, ref sIndex, "/ ", " MB"))
                                {
                                    progress.size = float.Parse(out_);
                                    progress.percent = (int)(progress.loadSize / progress.size * 100);
                                    if (progress.status == Progress.Status.OnStart)
                                    {
                                        progress.status = Progress.Status.OnUpdate;
                                        progress.message = "Mb";
                                    }
                                    else
                                    {
                                        progress.Update();
                                    }
                                }
                            }
                        }
                    }*/
                    return;
                }
                base.HandlerError(sendingProcess, outLine);
            }

            public override void Stop()
            {
                if (progress.percent > 0)
                {
                    progress.status = Progress.Status.FullUpdate;
                    //progress.ClearProgressBar();
                    Log("LFS Status: " + progress.status + " " + progress);
                }
                else
                {
                    progress.status = Progress.Status.NotUpdate;
                    Log("LFS Status: " + progress.status);
                }
                callback();
                base.Stop();
            }
        }

        public static void Clear()
        {
            ProcessAPI.StartAsynch(new ClearCommandHandler("status -s"));
        }

        public class ClearCommandHandler : CommandHandler
        {
            public ClearCommandHandler(string command) : base(command) { }

            private string patch;

            public override void Handler(object sendingProcess, DataReceivedEventArgs outLine)
            {
                string line = outLine.Data;
                if (line.EndsWith("\"")) line = line.Replace("\"", "");
                if (line.EndsWith(".meta")) //line.StartsWith(" M") && 
                {
                    Delete(line);
                    return;
                }

                if (line.StartsWith(" M")) //if (line.StartsWith("??"))
                {
                    if (line.Contains("ProjectSettings")) //ProjectSettings/ProjectSettings.asset
                    {
                        Delete(line);
                        return;
                    }
                }

                if (line.EndsWith(".mat"))
                {
                    DeletePlusMeta(line);
                    return;
                }
            }

            private void Delete(string line)
            {
                string patch = line.Substring(3);
                //Log("Delete " + patch);
                if (File.Exists(patch)) File.Delete(patch);
            }

            private void DeletePlusMeta(string line)
            {
                string patch = line.Substring(3);
                if (File.Exists(patch))
                {
                    File.Delete(patch);
                    patch += ".meta";
                    if (File.Exists(patch)) File.Delete(patch);
                }
            }
        }
        
        public static bool Command(out string out_, string command)
        {
            return ProcessAPI.Start(out out_, projectPath, gitPath, command);
        }

        public class CommandHandler : ProcessAPI.CommandHandler
        {
            public CommandHandler(string command) : base(projectPath, gitPath, command) { }
        }

        private static void Log(string message)
        {
            UnityEngine.Debug.Log("[" + System.DateTime.Now.ToString("HH:mm:ss") + "] " + message);
        }

        private static bool GetParam(string source, out string output, ref int startIndex, string start, string end)
        {
            startIndex = source.IndexOf(start, startIndex);
            if (startIndex != -1)
            {
                startIndex = startIndex + start.Length;
                int startEnd = source.IndexOf(end, startIndex);
                if (startEnd != -1)
                {
                    output = source.Substring(startIndex, startEnd - startIndex);
                    startIndex = startEnd + end.Length;
                    return true;
                }
            }
            output = null;
            return false;
        }

        private static bool GetParam(string source, out string output, ref int startIndex, string start)
        {
            startIndex = source.IndexOf(start, startIndex);
            if (startIndex != -1)
            {
                startIndex = startIndex + start.Length;
                output = source.Substring(startIndex);
                startIndex = source.Length;
                return true;
            }
            output = null;
            return false;
        }

        private static bool GetParamEnd(string source, out string output, ref int startIndex, string start, string end)
        {
            startIndex = source.LastIndexOf(start, startIndex);
            if (startIndex != -1)
            {
                startIndex = startIndex + start.Length;
                int startEnd = source.IndexOf(end, startIndex);
                if (startEnd != -1)
                {
                    output = source.Substring(startIndex, startEnd - startIndex);
                    startIndex = startEnd + end.Length;
                    return true;
                }
            }
            output = null;
            return false;
        }
    }
#endregion

//=============================================================================================================================
#region Process API
//=============================================================================================================================
    public class ProcessAPI
    {
        public class CommandHandler
        {
            public CommandHandler(string workingPath, string path, string command)
            {
                this.workingPath = workingPath;
                this.path = path;
                this.command = command;
            }

            public string workingPath;
            public string path;
            public string command;
            public Process process;

            public virtual void Handler(object sendingProcess, DataReceivedEventArgs outLine)
            {
                Log("_ [" + outLine.Data.Length + "] " + outLine.Data);
            }

            public virtual void HandlerError(object sendingProcess, DataReceivedEventArgs outLine)
            {
                if (outLine.Data == string.Empty) return;
                Log(command + ", [" + outLine.Data.Length + "] " + outLine.Data);
            }

            public virtual void Stop()
            {
                process.Close();
                Thread.CurrentThread.Abort();
            }
        }

        private static Process Create(string workingPath, string path, string command)
        {
            return new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,

                    FileName = path,
                    WorkingDirectory = workingPath,
                    Arguments = command,

                    StandardOutputEncoding = Encoding.UTF8, //Encoding.ASCII //Encoding.Unicode, //System.Text.UTF8Encoding
                    StandardErrorEncoding = Encoding.UTF8
                }
            };
        }

        public static bool Start(out string out_, string workingPath, string path, string command)
        {
            Process process = Create(workingPath, path, command);
            process.Start();
            string error = process.StandardError.ReadToEnd();
            out_ = process.StandardOutput.ReadToEnd(); //gitProcess.WaitForExit(); gitProcess.StandardInput.WriteLine("");
            process.Close();

            if (out_ == string.Empty && error != string.Empty)
            {
                Log(command + ", " + error);
                return false;
            }
            //Log("git " + command + ": " + out_);
            return true;
        }

        public static void StartAsynch(CommandHandler commandHandler)
        {
            Thread thread = new Thread(Command_);
            thread.Name = "ProcessAPIStartAsynch";
            thread.Start(commandHandler);
        }

        private static void Command_(object command_) //Handler handler, string command, string path
        {
            try
            {
                CommandHandler command = (CommandHandler)command_;
                command.process = Create(command.workingPath, command.path, command.command); //command.process.StandardOutput.BaseStream.ReadTimeout = 60000;
                //Log(command.process.StartInfo.StandardOutputEncoding);
                //Log(command.process.StartInfo.StandardErrorEncoding);
                command.process.OutputDataReceived += new DataReceivedEventHandler(command.Handler);
                command.process.ErrorDataReceived += new DataReceivedEventHandler(command.HandlerError);
                command.process.Start();
                command.process.BeginOutputReadLine();
                command.process.BeginErrorReadLine();
                command.process.WaitForExit();
                Log(command.command + " WaitForExit EndCurrentThread");
                command.Stop();
            }
            catch (System.Exception e)
            {
                Log(e.ToString()); //Log(e.GetType() + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        private static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }
    }
#endregion
}
