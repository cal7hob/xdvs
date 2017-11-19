using UnityEngine;
using UnityEditor;
using System.Collections;
using System;

[CustomPropertyDrawer(typeof(QualityNameAttribute))]
public class QualityNamePropertyDrawer : PropertyDrawer {

    string[] names;

    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

        if (names == null) {
            names = new string[QualitySettings.names.Length+1];
            names[0] = " ";
            int i = 1;
            foreach (var s in QualitySettings.names) {
                names[i] = s;
                i++;
            }
        }

        // Now draw the property as a Slider or an IntSlider based on whether it's a float or integer.
        if (property.propertyType == SerializedPropertyType.String) {
            int ind = Array.IndexOf(names, property.stringValue);
            if (ind < 0) ind = 0;

            int val = EditorGUI.Popup(position, label.text, ind, names);

            if (ind != val) {
                property.stringValue = names[val];
            }
        }
        else
            EditorGUI.LabelField(position, label.text, "Use QualityName with string.");
    }
}
