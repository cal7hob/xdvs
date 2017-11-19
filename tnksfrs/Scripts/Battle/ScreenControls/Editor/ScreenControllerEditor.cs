using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace XDevs.ScreenControls
{
    [CustomEditor(typeof(ScreenController))]
    public class ScreenControllerEditor : Editor
    {
        ReorderableList list;

        void OnEnable()
        {
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("buttons"), true, true, true, true);
            list.drawHeaderCallback += rect => GUI.Label(rect, "Buttons");
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, 120, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("rewiredButtonId"), GUIContent.none);
                EditorGUI.PropertyField(
                    new Rect(rect.x + 120, rect.y, rect.width - 120, EditorGUIUtility.singleLineHeight),
                    element.FindPropertyRelative("guiButton"), GUIContent.none);
            };
        }

        override public void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }
    }
}