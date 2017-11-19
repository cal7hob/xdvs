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

            return int.Parse(idString);
        }
    }

    public string TechnicalName
    {
        get
        {
            if (GameData.IsGame(Game.WWT2))
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
