using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(FXInfo))]
public class FXInfoEditor : PropertyDrawer
{
    private readonly string projectResPath;
    private bool expanded;
    private bool locked;

    public FXInfoEditor()
    {
        projectResPath = string.Format("Assets/Resources/{0}/", GameManager.CurrentResourcesFolder);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        expanded = EditorGUI.Foldout(new Rect(position.x, position.y, 10f, 20f), expanded, "");
        position = new Rect(position.x, position.y + 25f, position.width, position.height);
        if (!expanded)
            return;
        SerializedProperty fxResHigh = property.FindPropertyRelative("fxResourcesHigh");
        SerializedProperty fxResLow = property.FindPropertyRelative("fxResourcesLow");

        Rect headerPosition = new Rect(position.x + 140f, position.y, 140f, position.height);
        EditorGUI.LabelField(headerPosition, "HighDetailed");
        headerPosition = new Rect(headerPosition.x + 155f, headerPosition.y, 140f, headerPosition.height);
        EditorGUI.LabelField(headerPosition, "LowDetailed");

        int i;
        int arraySize = Mathf.Min(fxResHigh.arraySize, (int)GraphicsLevel.GraphicsLevelCount);
        Rect newRect = position;

        for (i = 0; i < arraySize; i++)
        {
            newRect = new Rect(position.x + 15f, position.y + (i + 1) * 20f, 100f, 20f);
            EditorGUI.LabelField(newRect, ((GraphicsLevel)i).ToString());
            newRect = new Rect(newRect.x + 105f, newRect.y, 110f, newRect.height);
            if (DrawResSelector(fxResHigh.GetArrayElementAtIndex(i), newRect, property))
            {
                FillEmptyLines(fxResHigh, i);
            }

            newRect.x += 155f;
            if (DrawResSelector(fxResLow.GetArrayElementAtIndex(i), newRect, property))
            {
                FillEmptyLines(fxResLow, i);
            }
        }

        if (locked && GUI.Button(new Rect(position.x, newRect.y + 15, 120f, 20f), "Разблокировать"))
        {
            SetLocked(false, property.serializedObject.targetObject);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!expanded)
            return 20f;

        SerializedProperty fxRes = property.FindPropertyRelative("fxResourcesHigh");
        int arraySize = Mathf.Min(fxRes.arraySize, (int)GraphicsLevel.GraphicsLevelCount);
        return 60f + (locked ? 20f : 0f) + 20f * arraySize;
    }

    private bool DrawResSelector(SerializedProperty property, Rect rect, SerializedProperty rootProperty)
    {
        if (string.IsNullOrEmpty(property.stringValue))
        {
            // Draw res selector
            GameObject go = EditorGUI.ObjectField(rect, GUIContent.none, null, typeof(GameObject), false) as GameObject;
            if (go == null)
                return false;

            Object prefab = PrefabUtility.GetPrefabObject(go);
            if (prefab == null)
                return false;

            string stringValue = AssetDatabase.GetAssetPath(prefab);
            if (!stringValue.Contains(projectResPath))
            {
                Debug.LogError("Указанный объект не находится в папке ресурсов текущего проекта и не может быть выбран!");
                return false;
            }
            property.stringValue = stringValue.Replace(projectResPath, "").Replace(".prefab", "");

            return true;
        }

        // Draw property field
        EditorGUI.LabelField(rect, property.stringValue);

        rect.Set(rect.x + 110f, rect.y, 18f, rect.size.y);

        if (GUI.Button(rect, "<"))
        {
            string resPath = string.Format("{0}{1}.prefab", projectResPath, property.stringValue);

            GameObject asset =
                AssetDatabase.LoadAssetAtPath<GameObject>(resPath);

            if (asset == null)
                return false;

            SetLocked(true, rootProperty.serializedObject.targetObject);
            Selection.activeObject = asset;
            return false;
        }

        rect.Set(rect.x + 18f, rect.y, 18f, rect.size.y);

        if (GUI.Button(rect, "X"))
        {
            property.stringValue = null;
        }

        return false;
    }

    private void FillEmptyLines(SerializedProperty array, int index)
    {
        // Раскомментировать, если понадобиться автоматическое заполнение
        
        /*        string filledValue = array.GetArrayElementAtIndex(index).stringValue;
        int i;
        for (i = index - 1; i >= 0; i--)
        {
            SerializedProperty currentElement = array.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(currentElement.stringValue))
                break;

            currentElement.stringValue = filledValue;
        }
        for (i = index + 1; i < array.arraySize; i++)
        {
            SerializedProperty currentElement = array.GetArrayElementAtIndex(i);
            if (!string.IsNullOrEmpty(currentElement.stringValue))
                break;

            currentElement.stringValue = filledValue;
        }
        
        array.serializedObject.ApplyModifiedPropertiesWithoutUndo();*/
    }

    private void SetLocked(bool value, Object thisGO)
    {
        if (locked == value)
            return;

        if (!value)
        {
            Selection.activeObject = thisGO;
        }

        ActiveEditorTracker.sharedTracker.isLocked = locked = value;
    }
}
