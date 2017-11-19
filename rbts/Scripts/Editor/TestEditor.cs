#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Test))]
public class TestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        Test test = (Test) target;
        DrawDefaultInspector();

        if (GUILayout.Button("Test"))
        {
            test.TestFromEditor();
        }
    }
}
#endif
