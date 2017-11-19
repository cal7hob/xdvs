using System;
using System.Collections.Generic;

using UnityEngine;

public class HangarVehicle : MonoBehaviour
{
    [SerializeField]
    private VehicleInfo info;

    private GameObject bodyMesh;

    public VehicleInfo Info
    {
        get { return info; }
    }

    public int IdFromObjectName
    {
        get
        {
            int vehicleId;

            if (!TryParseIdFromObjectName(name, out vehicleId))
                throw new FormatException();

            return vehicleId;
        }
    }

    public string TechnicalName
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks))
            {
                string[] parts = name.Split('_');

                if (parts.Length != 4)
                {
                    DT.LogError("Wrong template name <{0}>! The name must have 4 groups, separated by '_' ", name);
                    return string.Empty;
                }

                return parts[2] + "_" + parts[3];
            }

            if (GameData.IsGame(Game.SpaceJet))
            {
                string[] parts = name.Split('_');

                if (parts.Length != 4)
                {
                    DT.LogError("Wrong template name <{0}>! The name must have 4 groups, separated by '_' ", name);
                    return string.Empty;
                }

                return parts[3];
            }

            if (GameData.IsGame(Game.ToonWars | Game.BattleOfWarplanes | Game.WingsOfWar | Game.BattleOfHelicopters | Game.Armada | Game.MetalForce))
            {
                string[] parts = name.Split('_');

                if (parts.Length != 3)
                {
                    DT.LogError("Wrong template name <{0}>! The name must have 3 groups, separated by '_' ", name);
                    return string.Empty;
                }

                return parts[2];
            }

            return string.Empty;
        }
    }

    public GameObject Body
    {
        get
        {
            if (bodyMesh != null)
                return bodyMesh;

            List<Transform> allChildren = transform.GetAllChildrenRecursively();

            foreach (Transform child in allChildren)
            {
                if (CheckBodyMesh(child))
                    return bodyMesh = child.gameObject;
            }

            return null;
        }
    }

    public Renderer BodyRenderer
    {
        get { return Body.GetComponent<Renderer>(); }
    }

    public Mesh BodyMeshFilter
    {
        get { return Body.GetComponent<MeshFilter>().sharedMesh; }
    }

    public static bool TryParseIdFromObjectName(string name, out int vehicleId)
    {
        string[] nameParts = name.Split('_');

        string idString = nameParts[0];

        if (nameParts[0].Contains(GameData.GetInterfaceShortName(Interface.SpaceJet)))
            idString = nameParts[1];

        return int.TryParse(idString, out vehicleId);
    }

    private bool CheckBodyMesh(Transform transform)
    {
        return (transform.name == "Body" || (transform.parent != null && transform.parent.name == "Mesh_Body")) &&
               transform.GetComponent<Renderer>() != null;
    }
}
