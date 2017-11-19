using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

[CustomPropertyDrawer(typeof(ShowInLabelAttribute))]
public class ShowInLabelPropertyDrawer : PropertyDrawer {

    string[] names;

    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        switch (property.propertyType) {
            case SerializedPropertyType.String:
                EditorGUI.LabelField(position, label.text, property.stringValue);
                break;
            case SerializedPropertyType.Float:
                EditorGUI.LabelField(position, label.text, property.floatValue.ToString());
                break;
            case SerializedPropertyType.Integer:
                EditorGUI.LabelField(position, label.text, property.intValue.ToString());
                break;
            case SerializedPropertyType.Boolean:
                EditorGUI.LabelField(position, label.text, property.boolValue ? "true" : "false");
                break;
            default:
                EditorGUI.LabelField(position, label.text, "Unsupported type " + property.propertyType);
                break;
        }
    }
}
