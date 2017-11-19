using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(BonusPoints))]
public class BonusPointsEditor : Editor
{
	void OnEnable()
	{
		BonusPoints bonusPoints = (BonusPoints)target;
		bonusPoints.CollectPoints();
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		BonusPoints bonusPoints = (BonusPoints)target;
		if (GUILayout.Button("Recollect points"))
		{
			bonusPoints.CollectPoints();
		}
		if (GUILayout.Button("Выровнять по рельефу"))
		{
			bonusPoints.Align();
		}
	}
}