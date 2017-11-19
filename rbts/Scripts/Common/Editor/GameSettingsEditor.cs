using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GameSettings))]
public class GameSettingsEditor : Editor
{
    private bool showShellInfos;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Separator();

        showShellInfos = EditorGUILayout.Foldout(showShellInfos, "Настройки снарядов");
        if (showShellInfos)
        {
            ShowShellInfos();
        }
    }

    private void ShowShellInfos()
    {
        SerializedProperty shellIds = serializedObject.FindProperty("shellIds");
        SerializedProperty shellPrefabs = serializedObject.FindProperty("shellFxInfos");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("ID снаряда", GUILayout.Width(80f));
        EditorGUILayout.LabelField("FXInfo");
        EditorGUILayout.EndHorizontal();
        for (int i = 0; i < shellIds.arraySize; i++)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("-", GUILayout.Width(20f)))
            {
                RemoveShellAtInd(shellIds, shellPrefabs, i);
                return;
            }
            EditorGUILayout.PropertyField(shellIds.GetArrayElementAtIndex(i), GUIContent.none, true, GUILayout.Width(30f));
            EditorGUILayout.PropertyField(shellPrefabs.GetArrayElementAtIndex(i), GUIContent.none, true, GUILayout.Width(260f));
            EditorGUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Добавить"))
        {
            AddNewShell(shellIds, shellPrefabs);
        }
    }

    private void AddNewShell(SerializedProperty shellIds, SerializedProperty shellPrefabs)
    {
        try
        {
            shellIds.InsertArrayElementAtIndex(shellIds.arraySize);
            shellPrefabs.InsertArrayElementAtIndex(shellPrefabs.arraySize);
            serializedObject.ApplyModifiedProperties();
        }
        catch (ArgumentException)
        {
            return;
        }
    }

    private void RemoveShellAtInd(SerializedProperty shellIds, SerializedProperty shellPrefabs, int index)
    {
        shellIds.DeleteArrayElementAtIndex(index);
        shellPrefabs.DeleteArrayElementAtIndex(index);
        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("HelpTools/GameSettings")]
    public static void ShowSettingsObj()
    {
        GameSettings asset = AssetDatabase.LoadAssetAtPath<GameSettings>(string.Format("Assets/Resources/{0}/GameSettings.asset", GameManager.CurrentResourcesFolder));
        if (asset == null)
            return;

        Selection.activeObject = asset;
    }
}
