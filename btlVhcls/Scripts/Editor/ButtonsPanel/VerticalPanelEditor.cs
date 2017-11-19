using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace XDevs.ButtonsPanel
{
    [CustomEditor(typeof(VerticalPanel), true)]
    public class VerticalPanelEditor : Editor
    {

        ReorderableList list;

        private void OnEnable()
        {
            list = new ReorderableList(serializedObject, serializedObject.FindProperty("buttons"), true, true, true, true);
            list.drawHeaderCallback += rect => GUI.Label(rect, "Buttons");
            list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = list.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("startYPos"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("spaceBetweenButtons"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("alignBy"));

            list.DoLayoutList();
            serializedObject.ApplyModifiedProperties();

            var target = (VerticalPanel)base.target;

            if (GUILayout.Button("Align", GUILayout.Width(100)))
            {
                target.Align();
            }
        }
    }
}