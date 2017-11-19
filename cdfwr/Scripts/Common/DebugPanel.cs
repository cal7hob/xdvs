using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DebugPanel : MonoBehaviour 
{
    public GameObject wrapper;

    public static DebugPanel Instance{ get; private set;}
    private Vector2 scroolPos;
    List<string>listLog = new List<string>();

    private void Awake()
    {
        if (Instance != null)
        {
            return;
        }

        Instance = this;
        wrapper.SetActive (DT.useDebugPanelForLogging);
    }

//    private void OnDestroy()
//    {
//        Instance = null;
//    }

    public static void Log(string message)
    {
        if (Instance == null)
        {
            Debug.LogError("Instance = NULL. Cant Log to DebugPanel!");
            return;
        }
        Instance.listLog.Add(message);
    }

    public void Clear()
    {
        listLog.Clear ();
    }
	
    public void Toggle()
    {
        wrapper.SetActive (!wrapper.activeSelf);
    }

    private void OnGUI()
    {
        if (GUILayout.Button ("Show/Hide",GUILayout.MinWidth(300),GUILayout.ExpandWidth(false)))
            Toggle ();
        if (!wrapper.activeSelf)
            return;
        scroolPos = GUILayout.BeginScrollView(scroolPos);
        for(int i = 0; i < listLog.Count; i++)
        {
            GUILayout.BeginHorizontal();
            
            GUILayout.Label(listLog[i],GUILayout.MinWidth(Screen.width)/*,GUILayout.ExpandWidth(true)*/);

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button ("Clear",GUILayout.MinWidth(300),GUILayout.ExpandWidth(false)))
            listLog.Clear ();

        GUILayout.EndScrollView();

    }
}
