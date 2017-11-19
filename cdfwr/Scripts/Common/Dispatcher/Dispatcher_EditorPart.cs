#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.SceneManagement;

public partial class Dispatcher
{
    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != checkSceneName)
            return;

        foreach (var oneEventHandlers in handlers)
        {
            bool any = false;
            foreach (var x in oneEventHandlers.Value)
            {
                if (x != null)
                {
                    any = true;
                    break;
                }
            }
            if (!any)
                continue;

            Debug.LogFormat("Subscriptions for event {0}:", oneEventHandlers.Key);
            int i = 1;
            foreach (var callback in oneEventHandlers.Value)
            {
                if (callback == null)
                    continue;

                foreach (var simpleCallback in callback)
                {
                    Debug.LogFormat("{0}) {1}.{2}", i++, simpleCallback.Target != null ? simpleCallback.Target.GetType().Name : "static", simpleCallback.Method.Name);
                }
                i = 1;
            }
        }
    }
}

#endif