using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

static public class AssetBundleManager
{
    public static AssetBundle getAssetBundle(string url, int version)
    {
        string keyName = url + version;
        AssetBundleRef abRef;
        if (dictAssetBundleRefs.TryGetValue(keyName, out abRef))
            return abRef.assetBundle;
        return null;
    }

    public static IEnumerator downloadAssetBundle(string url, int version)
    {
        string keyName = url + version;
        if (dictAssetBundleRefs.ContainsKey(keyName))
            yield return null;
        else
        {
            while (!Caching.ready)
                yield return null;

            using (WWW www = WWW.LoadFromCacheOrDownload(url, version))
            {
                yield return www;
                if (www.error == null)
                {
                    AssetBundleRef abRef = new AssetBundleRef(url, version);
                    abRef.assetBundle = www.assetBundle;
                    dictAssetBundleRefs.Add(keyName, abRef);
                }
                else Debug.Log("WWW download:" + www.error);
            }
        }
    }

    public static void Unload(string url, int version, bool allObjects)
    {
        string keyName = url + version;
        AssetBundleRef abRef;
        if (dictAssetBundleRefs.TryGetValue(keyName, out abRef))
        {
            abRef.assetBundle.Unload(allObjects);
            abRef.assetBundle = null;
            dictAssetBundleRefs.Remove(keyName);
        }
    }

    private static Dictionary<string, AssetBundleRef> dictAssetBundleRefs = new Dictionary<String,AssetBundleRef>();

    private class AssetBundleRef
    {
        public AssetBundle assetBundle = null;
        public int version;
        public string url;
        public AssetBundleRef(string strUrlIn, int intVersionIn)
        {
            url = strUrlIn;
            version = intVersionIn;
        }
    };
}
