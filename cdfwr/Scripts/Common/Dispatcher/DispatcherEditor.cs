#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(Dispatcher))]
public class DispatcherEditor : Editor
{
    private EventId eventId;

    public override void OnInspectorGUI()
    {
        Dispatcher dispatcher = (Dispatcher)target;
        bool oldShowSubscr = dispatcher.showSubscriptions;
        dispatcher.showSubscriptions = EditorGUILayout.Toggle("Show subscriptions after scene unload", dispatcher.showSubscriptions);
        if (dispatcher.showSubscriptions != oldShowSubscr)
        {
            EditorUtility.SetDirty(dispatcher);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        string oldCheckSceneName = dispatcher.checkSceneName;
        if (dispatcher.showSubscriptions)
        {
            dispatcher.checkSceneName = EditorGUILayout.TextField("Check scene name", dispatcher.checkSceneName);
            if (dispatcher.checkSceneName != oldCheckSceneName)
            {
                EditorUtility.SetDirty(dispatcher);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
        dispatcher.showEventSent = EditorGUILayout.Toggle("Show event sent", dispatcher.showEventSent);
        if (dispatcher.showEventSent)
        {
            EventId newEventId = (EventId) EditorGUILayout.EnumPopup("Log event", eventId);
            if (newEventId != eventId)
            {
                eventId = newEventId;
                if (!dispatcher.eventsToLog.Contains(eventId))
                    dispatcher.eventsToLog.Add(eventId);
                EditorUtility.SetDirty(dispatcher.gameObject);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            for(int i = 0; i < dispatcher.eventsToLog.Count; i++)
            {
                
            }
        }
    }
}

#endif