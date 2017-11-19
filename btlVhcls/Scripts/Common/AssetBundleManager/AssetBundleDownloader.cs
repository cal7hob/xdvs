using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.IO;

using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AssetBundles
{
    public abstract class AssetBundleDownloaderAbstract : MonoBehaviour
    {
        public bool IsDone { get { return m_isDone; } }
        public bool IsLoading { get { return m_isLoading; } }
        public long LoadingBytesFull { get { return m_loadingFullBytes; } }
        public long LoadingBytes { get { return m_progressBytes + m_currentProgressBytes; } }

        protected List<Bundleinfo> m_remoteBundles = new List<Bundleinfo>();
        protected bool m_isRemoteManifestLoaded = false;

        protected string m_baseRemoteUri;
        protected bool m_isDone = false;
        protected bool m_initDone = false;
        protected bool m_isLoading = false;
        protected long m_loadingFullBytes = 0L;
        protected long m_progressBytes = 0L;
        protected long m_currentProgressBytes = 0L;

        protected int m_maxConcurentDownloads = 3;
        protected Queue<DownloadTaskAbstract> m_tasksQueue = new Queue<DownloadTaskAbstract>();
        protected List<DownloadTaskAbstract> m_workingTasks = new List<DownloadTaskAbstract>();
        protected List<DownloadTaskAbstract> m_waitingTasks = new List<DownloadTaskAbstract>();

        protected virtual void Awake()
        {
            m_baseRemoteUri = AssetBundleManager.BaseDownloadingURL;
            if (!m_baseRemoteUri.EndsWith("/")) {
                m_baseRemoteUri += "/";
            }
            UberDebug.LogChannel("AssetBundleDownloader", "baseRemoteUrl: {0}", m_baseRemoteUri);
        }


        protected abstract IEnumerator Start();

        protected virtual IEnumerator LoadRemoteManifest()
        {
            bool isDone = false;
            int tries = 0;
            AssetBundle bundle = null;
            AssetBundleManifest remoteAssetBundleManifest = null;
            while (!isDone)
            {
                tries++;
                var remoteRequest = UnityWebRequest.GetAssetBundle(m_baseRemoteUri + Utility.GetPlatformName());
                yield return remoteRequest.Send();

                if (remoteRequest.isNetworkError)
                {
                    if (tries < 4 && CanTryRedownload(remoteRequest))
                    {
                        UberDebug.LogWarningChannel("AssetBundleDownloader", "Error while loading {0}: {1}", remoteRequest.url, remoteRequest.error);
                        UberDebug.LogWarningChannel("AssetBundleDownloader", "Try to redownload...");
                        yield return new WaitForSecondsRealtime(1f);
                        continue;
                    }
                    else
                    {
                        UberDebug.LogErrorChannel("AssetBundleDownloader", "Can't load {0}: {1}", remoteRequest.url, remoteRequest.error);
                        yield break;
                    }
                }

                bundle = (remoteRequest.downloadHandler as DownloadHandlerAssetBundle).assetBundle;
                if (bundle == null)
                {
                    UberDebug.LogErrorChannel("AssetBundleDownloader", "{0} is not a valid asset bundle.", remoteRequest.url);
                    yield break;
                }

                var opLoadAssetRemote = bundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
                yield return opLoadAssetRemote;
                remoteAssetBundleManifest = (AssetBundleManifest)opLoadAssetRemote.asset;
                if (remoteAssetBundleManifest == null)
                {
                    bundle.Unload(true);
                    yield break;
                }

                isDone = true;
            }
            m_isRemoteManifestLoaded = true;

            // Loading remote bundles sizes
            isDone = false;
            tries = 0;
            Dictionary<string, JsonPrefs> json = new Dictionary<string, JsonPrefs>();
            while (!isDone)
            {
                tries++;
                var remoteJsonRequest = UnityWebRequest.Get(m_baseRemoteUri + Utility.GetPlatformName() + ".json");
                yield return remoteJsonRequest.Send();

                if (remoteJsonRequest.isNetworkError)
                {
                    if (tries < 4 && CanTryRedownload(remoteJsonRequest))
                    {
                        UberDebug.LogWarningChannel("AssetBundleDownloader", "Error while loading {0}: {1}", remoteJsonRequest.url, remoteJsonRequest.error);
                        UberDebug.LogWarningChannel("AssetBundleDownloader", "Try to redownload...");
                        continue;
                    }
                    else
                    {
                        UberDebug.LogErrorChannel("AssetBundleDownloader", "Can't load {0}: {1}", remoteJsonRequest.url, remoteJsonRequest.error);
                        break;
                    }
                }

                json = (MiniJSON.Json.Deserialize(remoteJsonRequest.downloadHandler.text) as Dictionary<string, object>)
                    .ToDictionary(key => key.Key, val => new JsonPrefs(val.Value));
                isDone = true;
            }

            yield return null;
            foreach (string bundleName in remoteAssetBundleManifest.GetAllAssetBundles())
            {
                string uri = m_baseRemoteUri + bundleName;
                m_remoteBundles.Add(Bundleinfo.FromRemotePath(
                    bundleName, uri,
                    remoteAssetBundleManifest.GetAssetBundleHash(bundleName),
                    json.ContainsKey(bundleName) ? json[bundleName].ValueLong("size") : 0
                ));
            }

            yield return null;
            bundle.Unload(true);
        }

        protected virtual void Update()
        {
            if (!m_initDone) return;

            for (int i = m_waitingTasks.Count - 1; i >= 0; i--) {
                var task = m_waitingTasks[i];
                task.WaitForSeconds -= Time.unscaledDeltaTime;
                if (task.WaitForSeconds <= 0f) {
                    m_waitingTasks.RemoveAt(i);
                    m_tasksQueue.Enqueue(task);
                    UberDebug.LogChannel("AssetBundleDownloader", "Enqueue waiting task {0}", task.Uri);
                }
            }

            // Check for available job slots
            if ((m_tasksQueue.Count > 0) && (m_workingTasks.Count < m_maxConcurentDownloads)) {
                while ((m_tasksQueue.Count > 0) && (m_workingTasks.Count < m_maxConcurentDownloads)) {
                    var task = m_tasksQueue.Dequeue();
                    if (task.Start()) {
                        m_workingTasks.Add(task);
                    }
                    else {
                        UberDebug.LogWarningChannel("AssetBundleDownloader", "Start failed, schedule to redownload, ", task.Uri);
                        task.WaitForSeconds = 1f;
                        m_waitingTasks.Add(task);
                    }
                }
            }

            // Update current tasks
            m_currentProgressBytes = 0L;
            for (int i = m_workingTasks.Count - 1; i >= 0; i--)
            {
                var task = m_workingTasks[i];
                if (!task.Update()) {
                    m_workingTasks.RemoveAt(i);
                    ProcessFinishedOperation(task);
                }
                else if (task.Size > 0) {
                    m_currentProgressBytes += task.Downloaded;
                }

            }

            m_isDone = m_tasksQueue.Count == 0 && m_workingTasks.Count == 0 && m_waitingTasks.Count == 0;
        }

        protected void ProcessFinishedOperation(DownloadTaskAbstract task)
        {
            if (task.Request.isNetworkError) {
                TaskLoadError(task);
                return;
            }

            UberDebug.LogChannel("AssetBundleDownloader", "Finish task : {0}", task.Uri);

            // Check loaded file for open
            if (task.IsBundle && !IsBundleOk(task)) {
                TaskLoadError(task);
                return;

            }

            if (task.Size > 0) {
                m_progressBytes += task.Size;
            }
            task.End();
        }

        protected abstract bool IsBundleOk(DownloadTaskAbstract task);

        private void TaskLoadError(DownloadTaskAbstract task)
        {
            UberDebug.LogWarningChannel("AssetBundleDownloader", "Error loading bundle '{0}': {1}", task.Uri, task.Request.error);
            UberDebug.LogWarningChannel("AssetBundleDownloader", "Schedule to redownload, try #{0}...", task.Tries);
            task.WaitForSeconds = 1f;
            task.End();
            m_waitingTasks.Add(task);
        }


        protected bool CanTryRedownload(UnityWebRequest request)
        {
            return request.responseCode == -1 || request.responseCode >= 500;
        }
    }

#if !(UNITY_WSA && !UNITY_WSA_10_0)
    public class AssetBundleDownloader : AssetBundleDownloaderAbstract
    {
        public string AssetsPath { get { return m_assetsPath; } }

        public const string kAssets = @"assets";

        protected string m_assetsPath;
        protected string m_basePath;

        protected bool m_isLocalAssetsGood = false;

        protected List<Bundleinfo> m_localBundles = new List<Bundleinfo>();

        override protected void Awake()
        {
            base.Awake();
            m_assetsPath = Path.Combine(Application.persistentDataPath, kAssets);
            m_basePath = Path.Combine (m_assetsPath, Utility.GetPlatformName());

            if (!Directory.Exists (m_basePath)) {
                Directory.CreateDirectory(m_basePath);
            }

            UberDebug.LogChannel("AssetBundleDownloader", "basePath: {0}", m_basePath);
        }

        override protected IEnumerator Start()
        {
            // Loading local manifest
            string localManifest = Path.Combine(m_basePath, Utility.GetPlatformName());
            yield return LoadLocalManifest(localManifest);

            // Loading remote manifest
            yield return LoadRemoteManifest();

            // Find expired bundles and remove them
            if (m_isRemoteManifestLoaded) {
                foreach (var localBundle in m_localBundles) {
                    if (m_remoteBundles.Find(b => b.hash == localBundle.hash) == null) {
                        UberDebug.LogChannel("AssetBundleDownloader", "Delete expired bundle {0}", localBundle.pathOrUri);
                        File.Delete(localBundle.pathOrUri);
                    }
                }
            }

            yield return null;

            // Add new manifest to download first
            string remoteManifest = m_baseRemoteUri + Utility.GetPlatformName();
            m_tasksQueue.Enqueue(new DownloadFileTask(remoteManifest, localManifest, -1L, true));
            m_tasksQueue.Enqueue(new DownloadFileTask(remoteManifest + ".json", localManifest + ".json", -1L, false));
            UberDebug.LogChannel("AssetBundleDownloader", "Download new manifest file: {0}", remoteManifest);

            // Find new bundles and queue them for downloading
            bool haveNewBundles = false;
            foreach (var remoteBundle in m_remoteBundles) {
                if (m_localBundles.Find(b => b.hash == remoteBundle.hash) == null) {
                    m_loadingFullBytes += remoteBundle.size;
                    haveNewBundles = true;
                    m_tasksQueue.Enqueue(new DownloadFileTask (remoteBundle, Path.Combine (m_basePath, remoteBundle.bundleName)));
                    UberDebug.LogChannel("AssetBundleDownloader", "Added to download: {0}", remoteBundle.bundleName);
                }
            }

            if (!haveNewBundles) {
                m_tasksQueue.Clear();
            }

            m_isLoading = m_tasksQueue.Count > 0;

            if (!m_isLoading && !m_isLocalAssetsGood) {
                MessageBox.Show(MessageBox.Type.Critical, "Can't download game assets.", (m) => Application.Quit());
                yield break;
            }

            m_isDone = m_isLocalAssetsGood && !m_isLoading;
            m_initDone = true;
            UberDebug.LogChannel("AssetBundleDownloader", "isDone={0}, isLoading={1}, initDone={2}, isLocalAssetsGood={3}", m_isDone, m_isLoading, m_initDone, m_isLocalAssetsGood);
        }

        IEnumerator LoadLocalManifest(string localManifest)
        {
            if (!File.Exists(localManifest)) {
                yield break;
            }

            var opLoadLocal = AssetBundle.LoadFromFileAsync(localManifest);
            yield return opLoadLocal;

            if (opLoadLocal.assetBundle == null) {
                yield break;
            }

            var opLoadAssetLocal = opLoadLocal.assetBundle.LoadAssetAsync<AssetBundleManifest>("AssetBundleManifest");
            yield return opLoadAssetLocal;
            AssetBundleManifest localAssetBundleManifest = (AssetBundleManifest)opLoadAssetLocal.asset;

            if (localAssetBundleManifest == null) {
                opLoadLocal.assetBundle.Unload(true);
                yield break;
            }

            string jsonLocalPath = localManifest + ".json";
            Dictionary<string, JsonPrefs> jsonLocal = new Dictionary<string, JsonPrefs>();
            if (File.Exists(jsonLocalPath)) {
                jsonLocal = (MiniJSON.Json.Deserialize(File.ReadAllText(jsonLocalPath)) as Dictionary<string, object>)
                    .ToDictionary(key => key.Key, val => new JsonPrefs(val.Value));
            }
            else {
                Debug.LogWarning("JSON manifest does not exist. Skipping bundles size checking.");
            }

            bool allBundlesOk = true;
            foreach (string bundleName in localAssetBundleManifest.GetAllAssetBundles()) {
                string fileName = Path.Combine(m_basePath, bundleName);
                if (!File.Exists(fileName)) {
                    allBundlesOk = false;
                    Debug.LogWarningFormat("Local bundle does not exist: '{0}'", bundleName);
                    continue;
                }

                long requiredSize = jsonLocal.ContainsKey(bundleName) ? jsonLocal[bundleName].ValueLong("size") : -1;
                if (requiredSize > 0) {
                    var fi = new FileInfo(fileName);
                    if (fi.Length != requiredSize) {
                        allBundlesOk = false;
                        Debug.LogWarningFormat("Loaded bundle size not equal required in JSON manifest: '{0}' ({1}), requred size: {2}", bundleName, fi.Length, requiredSize);
                        continue;
                    }
                }

                var testBundle = AssetBundle.LoadFromFileAsync(fileName);
                yield return testBundle;
                if (testBundle.assetBundle == null) {
                    allBundlesOk = false;
                    Debug.LogWarningFormat("Test load local bundle failed! '{0}'", bundleName);
                    continue;
                }
                testBundle.assetBundle.Unload(true);

                m_localBundles.Add(Bundleinfo.FromLocalPath(bundleName, fileName, localAssetBundleManifest.GetAssetBundleHash(bundleName)));
            }
            m_isLocalAssetsGood = allBundlesOk;
            opLoadLocal.assetBundle.Unload(true);
        }

        protected override bool IsBundleOk(DownloadTaskAbstract task)
        {
            var bundle = AssetBundle.LoadFromFile(((DownloadFileTask)task).FileName);
            if (bundle == null) {
                return false;
            }
            bundle.Unload(true);
            return true;
        }
    }
#endif
    public class AssetBundleCacher : AssetBundleDownloaderAbstract
    {
        override protected IEnumerator Start()
        {
            // Loading remote manifest
            yield return LoadRemoteManifest();

            // Find not cached bundles and queue them for downloading
            foreach (var remoteBundle in m_remoteBundles) {
                if (!Caching.IsVersionCached(remoteBundle.pathOrUri, remoteBundle.hash)) {
                    m_loadingFullBytes += remoteBundle.size;
                    m_tasksQueue.Enqueue(new CacheBundleTask(remoteBundle));
                    UberDebug.LogChannel ("AssetBundleDownloader", "Added to download: {0}", remoteBundle.bundleName);
                }
            }

            m_isLoading = m_tasksQueue.Count > 0;

            if (!m_isLoading && !m_isRemoteManifestLoaded)
            {
                MessageBox.Show(MessageBox.Type.Critical, "Can't download game assets.", (m) => Application.Quit());
                yield break;
            }

            m_isDone = !m_isLoading;
            m_initDone = true;
            UberDebug.LogChannel("AssetBundleDownloader", "isDone={0}, isLoading={1}, initDone={2}", m_isDone, m_isLoading, m_initDone);
        }

        protected override bool IsBundleOk(DownloadTaskAbstract task)
        {
            //var bundle = AssetBundle.LoadFromFile(((DownloadFileTask)task).FileName);
            //if (bundle == null)
            //{
            //    return false;
            //}
            //bundle.Unload(true);
            Bundleinfo bundle = ((CacheBundleTask)task).Bundle;
            return Caching.IsVersionCached(bundle.pathOrUri, bundle.hash);
        }
    }

    public class Bundleinfo
    {
        public string bundleName;
        public string pathOrUri;
        public Hash128 hash;
        public long size;

#if !(UNITY_WSA && !UNITY_WSA_10_0)
        public static Bundleinfo FromLocalPath (string bundleName, string fileName, Hash128 hash)
        {
            return new Bundleinfo {
                bundleName = bundleName,
                pathOrUri = fileName,
                hash = hash,
                size = new FileInfo(fileName).Length
            };
        }
#endif

        public static Bundleinfo FromRemotePath (string bundleName, string uri, Hash128 hash, long size)
        {
            return new Bundleinfo {
                bundleName = bundleName,
                pathOrUri = uri,
                hash = hash,
                size = size
            };
        }
    }

    public abstract class DownloadTaskAbstract
    {
        public string Uri { get { return m_uri; } }
        public bool IsBundle { get { return m_isBundle; } }
        public long Size { get { return m_sizeToDownload; } }
        public long Downloaded { get { return m_request == null ? 0L : (long)m_request.downloadedBytes; } }
        public UnityWebRequest Request { get { return m_request; } }
        public bool IsDone {
            get { return m_request == null || m_request.isDone; }
        }
        public int Tries { get { return m_tries; } }
        public float WaitForSeconds { get { return m_waitForSeconds; } set { m_waitForSeconds = value; } }

        protected string m_uri;
        protected bool m_isBundle = false;
        protected long m_sizeToDownload;
        protected int m_tries = 0;
        protected float m_waitForSeconds = 0f;

        protected UnityWebRequest m_request = null;
        protected AsyncOperation m_operation = null;

        public DownloadTaskAbstract (string uri, long size, bool isBundle = false)
        {
            m_uri = uri;
            m_sizeToDownload = size;
            m_isBundle = isBundle;
        }

        public bool Start()
        {
            End();
            m_request = CreateRequest();
            if (m_request == null)
            {
                return false;
            }
            m_operation = m_request.Send();
            m_tries++;
            return true;
        }

        public void End()
        {
            if (m_request != null)
            {
                m_request.Dispose();
                m_request = null;
            }
        }

        public bool Update()
        {
            if (m_operation == null)
            {
                return false;
            }

            if (m_operation.isDone)
            {
                m_operation = null;
                return false;
            }
            return true;
        }

        protected abstract UnityWebRequest CreateRequest();
    }

    class DownloadFileTask : DownloadTaskAbstract
    {
        public string FileName { get { return m_fileName; } }
        protected string m_fileName;


        public DownloadFileTask (string uri, string fileName, long size, bool isBundle = false) : base (uri, size, isBundle)
        {
            m_fileName = fileName;
        }

        public DownloadFileTask(Bundleinfo bundle, string fileName) : base(bundle.pathOrUri, bundle.size, true)
        {
            m_fileName = fileName;
        }

        protected override UnityWebRequest CreateRequest()
        {
            try
            {
                var wr = new UnityWebRequest(m_uri, "GET")
                {
                    downloadHandler = new DownloadHandlerFile(m_fileName)
                };
                return wr;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return null;
            }
        }
    }

    class CacheBundleTask : DownloadTaskAbstract
    {
        public Bundleinfo Bundle { get { return m_bundleInfo; } }

        protected Bundleinfo m_bundleInfo;

        public CacheBundleTask(Bundleinfo bundle) : base(bundle.pathOrUri, bundle.size, true)
        {
            m_bundleInfo = bundle;
        }

        protected override UnityWebRequest CreateRequest()
        {
            try {
                return UnityWebRequest.GetAssetBundle(m_bundleInfo.pathOrUri, m_bundleInfo.hash, 0);
            }
            catch (Exception e) {
                Debug.LogException(e);
                return null;
            }
        }
    }

    public class DownloadHandlerFile : DownloadHandlerScript
    {
        FileStream m_file;

        int m_contentLength = -1;
        int m_progress = 0;

        public DownloadHandlerFile (string file) : base()
        {
            string path = Path.GetDirectoryName(file);
            if (!Directory.Exists (path)) {
                Directory.CreateDirectory(path);
            }
            m_file = File.Create(file);
        }

        ~DownloadHandlerFile()
        {
            if (m_file != null) {
                m_file.Dispose();
                m_file = null;
            }
        }

        protected override void ReceiveContentLength(int contentLength)
        {
            m_contentLength = contentLength;
            base.ReceiveContentLength(contentLength);
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            m_file.Write(data, 0, dataLength);
            m_progress += dataLength;
            return true;
        }

        protected override float GetProgress()
        {
            return Mathf.Clamp01(m_progress / m_contentLength);
        }

        protected override void CompleteContent()
        {
            m_file.Dispose();
            m_file = null;
            base.CompleteContent();
        }


        protected override byte[] GetData()
        {
            throw new NotSupportedException();
        }

        protected override string GetText()
        {
            throw new NotSupportedException();
        }

    }
}
