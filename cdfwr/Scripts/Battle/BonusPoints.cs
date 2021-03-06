using System;
using System.Collections.Generic;
using UnityEngine;
using XDevs.LiteralKeys;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BonusPoints : MonoBehaviour
{
	public bool drawGizmos;

	private static BonusPoints instance;
	private static Dictionary<int, Transform> points;
	private LinkedList<int> occupied;
	
	void Awake()
	{
		instance = this;
		CollectPoints();
		occupied = new LinkedList<int>();
	}

	void OnDestroy()
	{
		instance = null;
		points = null;
	}

	/*	PUBLIC SECTION	*/
	public static Vector3 GetRandomPoint(out int pointIndex)
	{
        int index = 0;
        do
        {
            index = MiscTools.random.Next(0, points.Count);
        }
        while (instance.occupied.Contains(index));

        pointIndex = index;
        return points[index].position;
    }

	public static void UnlockPoint(int index)
	{
		if (!instance)
			return;

		LinkedListNode<int> node = instance.occupied.Find(index);
		if (node != null)
		    instance.occupied.Remove(node);
	}

	public static void LockPoint(int index)
	{
		if (!instance.occupied.Contains(index))
			instance.occupied.AddLast(index);
	}

	/* PRIVATE SECTION */
	public void CollectPoints()
	{
        points = new Dictionary<int, Transform>(transform.childCount);
	    Char[] numbers = {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};
	    int pointsProcessed = 0;
        foreach (Transform child in transform)
        {
            int number = 0;
            int.TryParse(child.name.Substring(child.name.IndexOfAny(numbers)), out number);
            if (points.ContainsKey(number))
            {
                DT.LogError(gameObject, "Invalid bonus point number ({0})", number);
                continue;
            }
            pointsProcessed++;
            points.Add(number, child);
	    }
//        DT.Log("Points found: {0}, points processed: {1}", transform.childCount, pointsProcessed);
	}

#if UNITY_EDITOR
    [MenuItem("HelpTools/Bonus points names")]
    public static void RenameBonusPoints()
    {
        GameObject go = GameObject.Find("BonusPoints");
        if (!go)
        {
            Debug.LogError("BonusPoints gameobject not found!");
            return;
        }

        BonusPoints bp = go.GetComponent<BonusPoints>();
        int id = 0;
        foreach (Transform child in bp.transform)
        {
            child.name = string.Format("BonusPoint{0}", id++) ;
            EditorUtility.SetDirty(child.gameObject);
        }

        bp.CollectPoints();
    }

	public void Align()
	{
		CollectPoints();
        int terrainMask = MiscTools.GetLayerMask(Layer.Key.Terrain);
		foreach (var trans in points.Values)
		{
			RaycastHit hit;
			if (Physics.Raycast(trans.position, Vector3.down, out hit, 300, terrainMask))
				trans.position = hit.point + Vector3.up * 2;
		}
	}

	void OnDrawGizmos()
	{
		if (!drawGizmos || points == null)
			return;

		Gizmos.color = Color.green;
		for (int i = 0; i < points.Count; i++)
		{
			Gizmos.DrawCube(points[i].position, new Vector3(1f, 1f, 1f));
		}
	}
#endif
}
