using UnityEngine;
using UnityEditor;
using System.Collections;

namespace XDevs.ButtonsPanel
{
    [CustomEditor(typeof(PanelButton))]
    public class PanelButtonEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var button = (PanelButton)target;

            if (GUILayout.Button("Calc height", GUILayout.Width(100)))
            {
                button.CalculateHeight();
            }
        }
    }
}