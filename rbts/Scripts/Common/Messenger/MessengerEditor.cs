#if UNITY_EDITOR

using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(Messenger))]
public class DispatcherEditor : Editor
{
    private EventId eventId;

    public override void OnInspectorGUI()
    {
        Messenger messenger = (Messenger)target;
        bool oldShowSubscr = messenger.showSubscriptions;
        messenger.showSubscriptions = EditorGUILayout.Toggle("Show subscriptions after scene unload", messenger.showSubscriptions);
        if (messenger.showSubscriptions != oldShowSubscr)
        {
            EditorUtility.SetDirty(messenger);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        string oldCheckSceneName = messenger.checkSceneName;
        if (messenger.showSubscriptions)
        {
            messenger.checkSceneName = EditorGUILayout.TextField("Check scene name", messenger.checkSceneName);
            if (messenger.checkSceneName != oldCheckSceneName)
            {
                EditorUtility.SetDirty(messenger);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
        messenger.showEventSent = EditorGUILayout.Toggle("Show event sent", messenger.showEventSent);
        if (messenger.showEventSent)
        {
            EventId newEventId = (EventId) EditorGUILayout.EnumPopup("Log event", eventId);
            if (newEventId != eventId)
            {
                eventId = newEventId;
                if (!messenger.eventsToLog.Contains(eventId))
                    messenger.eventsToLog.Add(eventId);
                EditorUtility.SetDirty(messenger.gameObject);
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            for(int i = 0; i < messenger.eventsToLog.Count; i++)
            {
                
            }
        }
    }
}

#endif