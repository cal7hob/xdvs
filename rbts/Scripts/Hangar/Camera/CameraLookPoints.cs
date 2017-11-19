using UnityEngine;

public class CameraLookPoints : MonoBehaviour
{
    [SerializeField] CamLookPoint engine;
    [SerializeField] CamLookPoint cannon;
    [SerializeField] CamLookPoint reloader;
    [SerializeField] CamLookPoint armor;
    [SerializeField] CamLookPoint tracks;

    public CamLookPoint Engine { get { return engine; } }
    public CamLookPoint Cannon { get { return cannon; } }
    public CamLookPoint Reloader { get { return reloader; } }
    public CamLookPoint Armor { get { return armor; } }
    public CamLookPoint Tracks { get { return tracks; } }
}
