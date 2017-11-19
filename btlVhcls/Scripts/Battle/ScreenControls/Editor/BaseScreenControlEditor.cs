using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace XDevs.ScreenControls
{
    [CustomEditor(typeof(BaseScreenControl))]
    public class BaseScreenControlEditor : Editor
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
        }
    }

}
