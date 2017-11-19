using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace SocialNetworks.Avatars
{
    public class Registry : MonoBehaviour
    {

        public int AvatarSize {
            get { return avatarSize; }
            //set { avatarSize = value;}
        }

        public int AtlasSize {
            get { return atlasSize; }
            //set { atlasSize = value;}
        }

        private static Registry m_instance;
        public static Registry Instance {
            get {
                if (null == m_instance)
                {
                    GameObject o = new GameObject ();
                    o.name = "AvatarsRegistry";
                    o.transform.position = Vector3.zero;

                    o.AddComponent<Registry> ();
                }
                return m_instance;
            }
        }


        protected void Awake ()
        {
            if (m_instance != null) {
                Destroy (gameObject);
                return;
            }

            DontDestroyOnLoad (gameObject);
            m_instance = this;
            int sz = atlasSize / avatarSize;
            int count = sz * sz;

            atlas = new Texture2D(atlasSize, atlasSize, TextureFormat.RGB24, false, true);
            atlas.Apply();

            atlasIndex = new List<RegistryRecord>(count);

            var names = new string[count];
            var regions = new Rect[count];
            var anchors = new Vector2[count];


            int i = 0;
            for (int x = 0; x < sz; ++x)
            {
                for (int y = 0; y < sz; ++y)
                {
                    names[i] = (x + 1) + "_" + (y + 1);
                    regions[i] = new Rect(x * avatarSize, y * avatarSize, avatarSize, avatarSize);

                    var rec = new RegistryRecord();
                    rec.index = i;
                    rec.name = names[i];
                    rec.x = x;
                    rec.y = y;
                    atlasIndex.Add(rec);

                    i++;
                }
            }

            //var scs = tk2dSpriteCollectionSize.Default();
            /*if (tk2dCamera.Instance != null)
            {
                scs = tk2dSpriteCollectionSize.ForTk2dCamera(tk2dCamera.Instance);
            }*/

            m_sheduler = gameObject.GetComponent<Sheduler.TasksSheduler>();
            if (m_sheduler == null)
            {
                m_sheduler = gameObject.AddComponent<Sheduler.TasksSheduler>();
                m_sheduler.maxParallelTasks = 2;
            }
        }

        protected Texture2D atlas;
        protected int avatarSize = 50;
        protected int atlasSize = 512;
        List<RegistryRecord> atlasIndex;

        protected Sheduler.TasksSheduler m_sheduler;


        public void GetAvatar(SocialPlatform platform, string uid, Action<bool, IAvatarRecord> callback)
        {
            int idx = FindAvatar (platform, uid);

            if (idx >= 0) {
                if (atlasIndex[idx].loaded) {
                    callback(true, atlasIndex[idx].GetAvatarRecord());
                    return;
                }

                atlasIndex[idx].subscribers.Enqueue (callback);
                return;
            }

            idx = FindFreePlace ();

            if (idx < 0) {
                Debug.LogWarning("Avatars atlas overload");
                callback(false, null);
                return;
            }

            atlasIndex[idx].uid = uid;
            atlasIndex[idx].platform = platform;
            atlasIndex[idx].loaded = false;

            var task = new DownloadTask(atlasIndex[idx], OnAvatarLoaded);
            m_sheduler.Shedule(task);

            atlasIndex[idx].subscribers.Enqueue(callback);
        }

        protected int FindAvatar (SocialPlatform platform, string uid)
        {
            var cnt = atlasIndex.Count;
            for (var i = 0; i < cnt; ++i) {
                if ((atlasIndex[i].platform == platform) && (atlasIndex[i].uid == uid)) {
                    return i;
                }
            }
            return -1;
        }

        protected int FindFreePlace ()
        {
            for (var i = atlasIndex.Count - 1; i >= 0; --i) {
                if (!atlasIndex[i].IsUsed ()) {
                    return i;
                }
            }
            return -1;
        }

        private void OnAvatarLoaded(DownloadTask task, Texture2D texture)
        {
            if (texture == null || MiscTools.isErrorImage (texture)) {
                Notify (task, false);
                return;
            }

            if ( (texture.width != avatarSize) || (texture.height != avatarSize) ) {
                if (!texture.Resize (avatarSize, avatarSize)) {
                    Debug.LogError ("Can't resize avatar texture");
                    Notify (task, false);
                    return;
                }
            }

            int idx = task.Index;

            int x = atlasIndex[idx].x * avatarSize;
            int y = atlasIndex[idx].y * avatarSize;

            y = atlasSize - y - avatarSize;

            atlasIndex[idx].loaded = true;

            atlas.SetPixels (x, y, avatarSize, avatarSize, texture.GetPixels ());
            atlas.Apply ();

            Notify (task, true);
        }

        private void Notify (DownloadTask task, bool success = true)
        {
            int idx = task.Index;

            if (!success) {
                atlasIndex[idx].platform = SocialPlatform.Undefined;
                atlasIndex[idx].uid = null;
            }

            try {
                while (atlasIndex[idx].subscribers.Count > 0)
                {
                    atlasIndex[idx].subscribers.Dequeue()(success, atlasIndex[idx].GetAvatarRecord());
                }
            }
            catch {}
            atlasIndex[idx].subscribers.Clear();
        }

    }
}
