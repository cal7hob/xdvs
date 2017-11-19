using System.Collections;
using System.Collections.Generic;
using AssetBundles;
using UnityEngine;

public class ShellPool
{
    private Shell shellPrefab;
    private readonly Queue<Shell> shells;
    private readonly string shellName;
    
    public ShellPool(string shellName)
    {
        this.shellName = shellName;
        shells = new Queue<Shell>(10);
        BattleController.Instance.StartCoroutine(PrefabLoading());
    }

    public Shell FromPool()
    {
        return shells.Count == 0 ? AddShell() : shells.Dequeue();
    }

    public void Return(Shell shell)
    {
        if (shell == null) return;
        shells.Enqueue(shell);
    }

    private Shell AddShell()
    {
        if (shellPrefab == null)
        {
            Debug.LogError("shellPrefab == null, shellName = " + shellName);
            return null;
        }

        Shell shell = Object.Instantiate(shellPrefab);
        shell.shellName = shellName;

        return shell;
    }

    private IEnumerator PrefabLoading()
    {
        string bundle = string.Format("{0}/shells", GameManager.CurrentResourcesFolder).ToLower();
        string prefabName = shellName + QualityManager.QualitySuffix;

        AssetBundleLoadAssetOperation request = AssetBundleManager.LoadAssetAsync(bundle, prefabName, typeof(GameObject));

        if (request == null)
            yield break;

        yield return request;

        shellPrefab = request.GetAsset<GameObject>().GetComponent<Shell>();
    }
}