using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

[Serializable]
public class ResourceLink : IDisposable
{
    [SerializeField]
    private string path;
    private Object resource;
    private ResourceRequest request;
    private List<Action<Object>> loadCallbacks = new List<Action<Object>>();
    private HashSet<object> users = new HashSet<object>();

    public void RegisterUserObject(object userObject)
    {
        users.Add(userObject);
    }

    public void UnregisterUserObject(object userObject)
    {
        if (users.Count == 0)
            return;

        users.Remove(userObject);
        if (users.Count == 0)
        {
            Dispose();
        }
    }

    public T GetObject<T>() where T : Object
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Trying to load resource from empty ResourceLink");
            return null;
        }

        if (resource == null)
        {
            resource = Resources.Load<T>(string.Format("{0}/{1}", GameManager.CurrentResourcesFolder, path));
        }

        return (T)resource;
    }

    public void GetObjectAsync(Action<Object> loadCallback)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogError("Trying to load resource from empty ResourceLink");
            return;
        }

        if (resource != null)
        {
            if (loadCallback != null)
            {
                loadCallback(resource);
            }
            return;
        }

        loadCallbacks.Add(loadCallback);
        AsyncOperationsChecker.Instance.CheckOperation(Resources.LoadAsync(string.Format("{0}/{1}", GameManager.CurrentResourcesFolder, path)), DoneCallback);
    }

    private void DoneCallback(AsyncOperation operation)
    {
        ResourceRequest request = operation as ResourceRequest;
        resource = request.asset;
        for (int i = 0; i < loadCallbacks.Count; ++i)
        {
            loadCallbacks[i](resource);
        }
        loadCallbacks.Clear();
    }

    public void Dispose()
    {
        if (resource == null || resource is GameObject || resource is MonoBehaviour)
            return;

        Resources.UnloadAsset(resource);
        loadCallbacks.Clear();
        resource = null;
    }
}
