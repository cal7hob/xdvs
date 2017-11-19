using UnityEditor;
using UnityEngine;
using System.Collections;

[CustomEditor(typeof(SpawnPoints))]
public class SpawnPointsEditor : Editor
{
	void OnEnable()
	{
		SpawnPoints spawnPoints = (SpawnPoints)target;
		spawnPoints.CollectPoints();
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
		SpawnPoints spawnPoints = (SpawnPoints)target;
		if (GUILayout.Button("Recollect points"))
		{
			spawnPoints.CollectPoints();
		}
		if (GUILayout.Button("Выровнять по рельефу"))
		{
			spawnPoints.Align();
		}
	}
}