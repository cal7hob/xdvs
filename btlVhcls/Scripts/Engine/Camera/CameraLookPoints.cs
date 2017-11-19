using UnityEngine;

public class CameraLookPoints : MonoBehaviour
{
    [SerializeField] CamLookTransform engine;
    [SerializeField] CamLookTransform cannon;
    [SerializeField] CamLookTransform reloader;
    [SerializeField] CamLookTransform armor;
    [SerializeField] CamLookTransform tracks;

    public CamLookTransform Engine { get { return engine; } }
    public CamLookTransform Cannon { get { return cannon; } }
    public CamLookTransform Reloader { get { return reloader; } }
    public CamLookTransform Armor { get { return armor; } }
    public CamLookTransform Tracks { get { return tracks; } }
}
