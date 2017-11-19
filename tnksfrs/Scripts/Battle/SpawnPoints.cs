using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using XDevs.LiteralKeys;

public class SpawnPoints : MonoBehaviour
{
    public static SpawnPoints instance;

    public bool drawGizmos = true;

    private const float CHECK_SPHERE_RADIUS = 4.0f;

    private bool didSpawnForTutorial;
    private int currentDebugPointIndex;

    private static List<List<Transform>> points = null;

    public static List<List<Transform>> Points
    {
        get
        {
            if (instance != null && points == null)
            {
                instance.CollectPoints();
            }

            return points;
        }

        set
        {
            //Debug.LogError("Set points as " + (value == null ? "Null" : value.Count.ToString()));
            points = value;
        }
    }

    /* UNITY SECTION */

	void Awake()
	{
		instance = this;

        gameObject.SetActive((GameData.IsTeamMode && name.EndsWith("_T")) || (!GameData.IsTeamMode && name.EndsWith("_D")));

        if(gameObject.activeSelf)
            CollectPoints();
	}

    void OnDestroy()
    {
        Points = null;
        instance = null;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!drawGizmos || Points == null)
            return;

        for (int i = 0; i < Points.Count; i++)
        {
            for (int j = 0; j < Points[i].Count; j++)
            {
                Gizmos.color = i < 1 ? Color.red : Color.blue;
                Gizmos.matrix = Points[i][j].localToWorldMatrix;
                Gizmos.DrawCube(Vector3.zero, Vector3.one);
            }
        }
    }
    #endif

    /* PUBLIC SECTION */

	public void CollectPoints()
	{
		Points = new List<List<Transform>>();

		Transform team0Parent = transform.Find("0");
		Transform team1Parent = transform.Find("1");

        if (team0Parent == null || (GameData.IsTeamMode && team1Parent == null))
		{
			DT.LogError("SpawnPoints Parent not found! The object SpawnPoints must contains object <0> in both modes and object <1> only in team mode.");
			return;
		}
		
		Points.Add(new List<Transform>());

		foreach (Transform child in team0Parent)
			Points[0].Add(child);

        if (GameData.IsTeamMode || team1Parent != null)
		{
			Points.Add(new List<Transform>(team1Parent.childCount)); // Team 1.

			foreach (Transform child in team1Parent)
				Points[1].Add(child);
		}
	}

	public void Align()
	{
		CollectPoints();

        int terrainMask = MiscTools.GetLayerMask(Layer.Key.Terrain);

		foreach (var transformList in Points)
		{
			foreach (Transform trans in transformList)
			{
				RaycastHit hit;

				if (Physics.Raycast(trans.position, Vector3.down, out hit, 300, terrainMask))
					trans.position = hit.point + Vector3.up * 2;
			}
		}
	}

    public Transform GetRandomPoint(int teamId = 0, bool forcedRespawn = false)
	{
        if (!XD.StaticContainer.Profile.BattleTutorialCompleted && !didSpawnForTutorial)
        {
            didSpawnForTutorial = true;
            return Points[0][0];
        }

        #if UNITY_EDITOR
        if (forcedRespawn)
        {
            List<Transform> debugSpawnPoints = Points.SelectMany(teamPoints => teamPoints).ToList();

			while (true)
			{
				if (currentDebugPointIndex < debugSpawnPoints.Count)
				{
					if (CheckPointAccessibility(debugSpawnPoints[currentDebugPointIndex].position) && debugSpawnPoints[currentDebugPointIndex].gameObject.activeSelf)
					{
						DT.Log("Respawned at point {0} / {1}.", currentDebugPointIndex + 1, debugSpawnPoints.Count);
						return debugSpawnPoints[currentDebugPointIndex++];
					}
					currentDebugPointIndex++;
				}
				else
				{
					currentDebugPointIndex = 0;
					return debugSpawnPoints[currentDebugPointIndex];
				}
			}
        }
        #endif
        /*
        if (teamId < 0)
        {
            Debug.LogError("Points.Count: " + Points.Count + ", try get " + teamId, gameObject);
            teamId = 1;
        }*/
        
        int randIndex = MiscTools.random.Next(0, Points[teamId].Count);
        int index = randIndex;

        do
        {
            if (CheckPointAccessibility(Points[teamId][index].position) && Points[teamId][index].gameObject.activeSelf)
            {
                return Points[teamId][index];
            }

            index = (int)Mathf.Repeat(index + 1, Points[teamId].Count);
        }
        while (index != randIndex);
        Debug.LogError("There are no available spawn points!");

        return Points[teamId][index];
	}

    public Transform GetRandomPoint(VehicleController vehicle, int teamId = 0, bool forcedRespawn = false)
    {
        Transform randomPoint = GetRandomPoint(teamId, forcedRespawn);

        randomPoint.position = GetCorrectPosition(vehicle, randomPoint);

        return randomPoint;
    }

    public Vector3 GetCorrectPosition(VehicleController vehicle, Transform targetTransform = null)
    {
        targetTransform = targetTransform ?? vehicle.transform;

        if (vehicle is FlightController)
            return targetTransform.position;

		float lowerYBound
            = SafeLinq.Min(
                vehicle
                    .GetComponentsInChildren<Collider>()
                    .Select(children => children.GetComponent<Collider>().bounds.min.y));

		float deltaHeight = Mathf.Abs(vehicle.transform.position.y - lowerYBound);

        // Немного приподнимаем танк.
        deltaHeight += 0.05f;

        RaycastHit hit;

        if (!Physics.Raycast(
                /* origin:      */  targetTransform.position,
                /* direction:   */  Vector3.down,
                /* hitInfo:     */  out hit,
                /* maxDistance: */  100,
                /* layerMask:   */  BattleController.TerrainLayerMask))
        {
            return targetTransform.position;
        }

        Vector3 newPosition
            = new Vector3(
                x:  targetTransform.position.x,
                y:  hit.point.y + deltaHeight,
                z:  targetTransform.position.z);

        return newPosition;
    }

	public static bool CheckPointAccessibility(Vector3 point)
	{
	    int layerMask = GameData.IsTeamMode ?
            MiscTools.GetLayerMask(Layer.Items[Layer.Key.Friend], Layer.Items[Layer.Key.Enemy], Layer.Items[Layer.Key.Player]) | BotDispatcher.BotsCommonMask :
            MiscTools.GetLayerMask(Layer.Items[Layer.Key.Enemy], Layer.Items[Layer.Key.Player]) | BotDispatcher.BotsCommonMask;        

	    return !Physics.CheckSphere(
	        position: point,
	        radius: CHECK_SPHERE_RADIUS,
	        layerMask: layerMask);
	}
}
