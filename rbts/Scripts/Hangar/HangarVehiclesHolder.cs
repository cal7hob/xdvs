using System.Collections.Generic;
using UnityEngine;

public class HangarVehiclesHolder : MonoBehaviour
{
    private static HangarVehiclesHolder instance;

    private readonly Dictionary<int, HangarVehicle> hangarVehiclesDict = new Dictionary<int,HangarVehicle>();

    private HangarVehicle[] hangarVehicles;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        hangarVehicles = GetComponentsInChildren<HangarVehicle>(true);

        foreach (HangarVehicle hangarVehicle in hangarVehicles)
            hangarVehiclesDict[hangarVehicle.IdFromObjectName] = hangarVehicle;

        instance = this;
    }

    void OnDestroy()
    {
        instance = null;
    }

    public static HangarVehicle[] HangarVehicles
    {
        get
        {
            if (instance == null)
                return null;

            return instance.hangarVehicles;
        }
    }

    public static HangarVehicle GetByIdOrDefault(int id)
    {
        if (instance == null)
            return null;

        HangarVehicle hangarVehicle;

        instance.hangarVehiclesDict.TryGetValue(id, out hangarVehicle);

        return hangarVehicle;
    }
}
