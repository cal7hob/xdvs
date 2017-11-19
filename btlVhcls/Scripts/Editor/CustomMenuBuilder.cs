using UnityEditor;
using UnityEngine;
using ContextMenu = Tanks.ContextMenu;

[CustomEditor(typeof(ContextMenu), true)]
public class ContextMenuBuilder : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var contextMenuScript = (ContextMenu)target;

        var addMenuItemButton = GUILayout.Button("+", GUILayout.Width(40));

        if (addMenuItemButton)
        {
            contextMenuScript.AddMenuItem();
        }
    }
}
