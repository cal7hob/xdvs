using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(DeltaProgressBar))]
public class DeltaProgressBarEditor : Editor
{
	//void OnEnable()
	//{
	//	DeltaProgressBar deltaProgressBar = (DeltaProgressBar)target;
	//	deltaProgressBar.mainBar.Init();
	//	deltaProgressBar.Init();
	//}
	
	//public override void OnInspectorGUI()
	//{
	//	base.OnInspectorGUI();
	//	DeltaProgressBar deltaProgressBar = (DeltaProgressBar)target;
	//	float val = EditorGUILayout.FloatField("Max", deltaProgressBar.Max);
	//	if (!Mathf.Approximately(val, deltaProgressBar.Max))
	//		deltaProgressBar.Max = val;
	//	val = EditorGUILayout.FloatField("Primary value", deltaProgressBar.PrimaryValue);
	//	if (!Mathf.Approximately(val, deltaProgressBar.PrimaryValue))
	//		deltaProgressBar.PrimaryValue = val;
	//	val = EditorGUILayout.FloatField("Secondary value", deltaProgressBar.SecondaryValue);
	//	if (!Mathf.Approximately(val, deltaProgressBar.SecondaryValue))
	//		deltaProgressBar.SecondaryValue = val;
	//	GUILayout.BeginHorizontal();
	//	EditorGUILayout.PrefixLabel("Title");
	//	string txt = EditorGUILayout.TextArea(deltaProgressBar.Title);
	//	if (txt != deltaProgressBar.Title)
	//		deltaProgressBar.Title = txt;
	//	GUILayout.EndHorizontal();
	//	GUILayout.BeginHorizontal();
	//	EditorGUILayout.PrefixLabel("Value suffix");
	//	txt = EditorGUILayout.TextArea(deltaProgressBar.ValueSuffix);
	//	if (txt != deltaProgressBar.ValueSuffix)
	//		deltaProgressBar.ValueSuffix = txt;
	//	GUILayout.EndHorizontal();
	//}

}
