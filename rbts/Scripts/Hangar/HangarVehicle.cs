using System.Linq;
using UnityEngine;

public class HangarVehicle : MonoBehaviour
{
    [SerializeField]
    private VehicleInfo info;

    public VehicleInfo Info { get { return info; } }

    public int IdFromObjectName
    {
        get
        {
            string[] nameParts = name.Split('_');

            string idString = nameParts[0];

            if (GameData.IsGame(Game.SpaceJet))
                idString = nameParts[1];

            return int.Parse(idString);
        }
    }

    public string TechnicalName
    {
        get
        {
            if (GameData.IsGame(Game.FutureTanks | Game.FTRobotsInvasion))
            {
                char separator = '_';

                string[] parts = name.Split(separator);

                if (parts.Length < 3)
                {
                    DT.LogError("Wrong template name <{0}>! The name must have 3 groups, separated by '_' ", name);
                    return string.Empty;
                }

                string technicalName = string.Join(separator.ToString(), parts.Skip(2).ToArray());

                return technicalName;
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

            if (GameData.IsGame(Game.ToonWars | Game.BattleOfWarplanes | Game.BattleOfHelicopters | Game.Armada | Game.WWR))
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
            return transform.Find("Body").gameObject ?? transform.Find("Mesh").gameObject;
        }
    }

    public Renderer BodyRenderer
    {
        get { return Body.GetComponent<Renderer>(); }
    }

    public Mesh BodyMesh
    {
        get { return Body.GetComponent<MeshFilter>().sharedMesh; }
    }
}
