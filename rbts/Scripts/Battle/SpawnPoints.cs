using System.Linq;
using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif
using XDevs.LiteralKeys;

public class SpawnPoints : MonoBehaviour
{
    public struct SpawnData
    {
        public Vector3 position;
        public Quaternion rotation;

        public SpawnData(Transform dataSource)
        {
            position = dataSource.position;
            rotation = dataSource.rotation;
        }

    }

    public static SpawnPoints instance;

    public bool drawGizmos = true;
    public bool drawSpheres = true;

    public const float CHECK_SPHERE_RADIUS = 4.0f;

    private bool didSpawnForTutorial;
    private int currentDebugPointIndex;
    private static int checkLayerMask;

    public static List<List<Transform>> Points { get; private set; }

    /* UNITY SECTION */

	void Awake()
	{
		instance = this;
        checkLayerMask = MiscTools.GetLayerMask(Layer.Items[Layer.Key.Friend], Layer.Items[Layer.Key.Enemy],
                Layer.Items[Layer.Key.Player]) | BotDispatcher.BotsCommonMask;

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
                Transform point = Points[i][j];
                Gizmos.DrawCube(point.position, Vector3.one);
                /*if (drawSpheres)
                    Gizmos.DrawSphere(point.position, CHECK_SPHERE_RADIUS);*/
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
	    {
	        if (child.gameObject.activeInHierarchy)
	        {
	            Points[0].Add(child);
	        }
	    }

	    if (GameData.IsTeamMode || team1Parent != null)
		{
			Points.Add(new List<Transform>(team1Parent.childCount)); // Team 1.

		    foreach (Transform child in team1Parent)
		    {
		        if (child.gameObject.activeInHierarchy)
		        {
		            Points[1].Add(child);
		        }
		    }
		}
	}

#if UNITY_EDITOR
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
			    {
			        trans.position = hit.point + Vector3.up * 2;
                    EditorUtility.SetDirty(trans);
			    }
			}
		}

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
	}
#endif

    public SpawnData GetRandomPoint(int teamId = 0, bool forcedRespawn = false)
	{
        if (ProfileInfo.IsBattleTutorial && !didSpawnForTutorial)
        {
            didSpawnForTutorial = true;
            return new SpawnData(Points[0][0]);
        }

        #if UNITY_EDITOR
        if (forcedRespawn)
        {
            List<Transform> debugSpawnPoints = Points.SelectMany(teamPoints => teamPoints).ToList();

			while (true)
			{
				if (currentDebugPointIndex < debugSpawnPoints.Count)
				{
					if (CheckPointAccessibility(debugSpawnPoints[currentDebugPointIndex].position))
					{
					    Transform spawnPoint = debugSpawnPoints[currentDebugPointIndex++];
                        Debug.LogFormat(spawnPoint.gameObject, "Respawned at point '{2}'({0}/{1}).", currentDebugPointIndex, debugSpawnPoints.Count, spawnPoint.name);
						return new SpawnData(spawnPoint);
					}
					currentDebugPointIndex++;
				}
				else
				{
					currentDebugPointIndex = 0;
					return new SpawnData(debugSpawnPoints[currentDebugPointIndex]);
				}
			}
        }
        #endif

        int randIndex = MiscTools.random.Next(0, Points[teamId].Count);
        int index = randIndex;

        do
        {
            if (CheckPointAccessibility(Points[teamId][index].position))
                return new SpawnData(Points[teamId][index]);

            index = (index + 1) % Points[teamId].Count;
        }
        while (index != randIndex);
        Debug.LogError("There are no available spawn points!");

        return new SpawnData(Points[teamId][index]);
	}

    public SpawnData GetRandomPoint(VehicleController vehicle, int teamId = 0, bool forcedRespawn = false)
    {
        SpawnData randomPoint = GetRandomPoint(teamId, forcedRespawn);
        randomPoint.position = CheckSpawnPosition(vehicle, randomPoint.position);

        return randomPoint;
    }

    public Vector3 CheckSpawnPosition(VehicleController vehicle, Vector3 position)
    {
		float lowerYBound = vehicle.EntireBounds.min.y;
		float deltaHeight = Mathf.Abs(vehicle.transform.position.y - lowerYBound);

        // Немного приподнимаем танк.
        deltaHeight += 0.05f;

        RaycastHit hit;

        if (!Physics.Raycast(
                /* origin:      */  position,
                /* direction:   */  Vector3.down,
                /* hitInfo:     */  out hit,
                /* maxDistance: */  100f,
                /* layerMask:   */  BattleController.TerrainLayerMask))
        {
            return position;
        }

        Vector3 newPosition
            = new Vector3(
                x:  position.x,
                y:  hit.point.y + deltaHeight,
                z:  position.z);

        return newPosition;
    }

	public static bool CheckPointAccessibility(Vector3 point)
	{
	    return !Physics.CheckSphere(
	        position: point,
	        radius: CHECK_SPHERE_RADIUS,
	        layerMask: checkLayerMask);
	}
}
