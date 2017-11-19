using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using Rewired;
using System.Linq;

[CustomPropertyDrawer(typeof(RewiredCustomButtonAttribute))]
public class RewiredCustomButtonPropertyDrawer : PropertyDrawer
{

    class Opts
    {
        public InputManager im;
        public int[] ids;
        public string[] names;
    }

    Dictionary<string, Opts> optsCache = new Dictionary<string, Opts>();

    string error = "";

    Opts GetOptions (RewiredCustomButtonAttribute attr)
    {
        string key = attr.rewiredPrefab + attr.controller;
        if (optsCache.ContainsKey(key)) {
            return optsCache[key];
        }

        Opts opts = new Opts();

        var im = (InputManager)AssetDatabase.LoadAssetAtPath(attr.rewiredPrefab, typeof(InputManager));
        if (im == null) {
            error = "Can't find object Rewired.InputManager in prefab " + attr.rewiredPrefab;
            return null;
        }
        opts.im = im; 

        var controller = im.userData.GetCustomController(attr.controller);
        if (controller == null)
        {
            error = "Can't find controller '"+attr.controller+"' in prefab " + attr.rewiredPrefab;
            return null;
        }

        opts.names = controller.buttons.Select(b => b.name).ToArray();
        opts.ids = controller.buttons.Select(b => b.elementIdentifierId).ToArray();

        optsCache[key] = opts;

        return opts;
    }

    override public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        Opts opts = GetOptions(attribute as RewiredCustomButtonAttribute);
        if (opts == null) {
            EditorGUI.LabelField(position, label.text, error);
            return;
        }


        // Now draw the property as a Slider or an IntSlider based on whether it's a float or integer.
        if (property.propertyType == SerializedPropertyType.Integer)
        {
            int ind = Array.IndexOf(opts.ids, property.intValue);
            if (ind < 0) ind = 0;

            int val = EditorGUI.Popup(position, label.text, ind, opts.names);

            if (ind != val)
            {
                property.intValue = opts.ids[val];
            }
        }
        else
            EditorGUI.LabelField(position, label.text, "Use RewiredCustomButton with int.");
    }
}
