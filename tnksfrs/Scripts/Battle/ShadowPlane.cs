using UnityEngine;
using XD;

public class ShadowPlane : MonoBehaviour, ISubscriber
{
    [SerializeField]
	private float           correctHeight = 0.05f;
    [SerializeField]
    private LayerMask       shadowLayer = 0;
	
	private Vector3         start = new Vector3();
	private Vector3         end = new Vector3();
    private Vector3         point = new Vector3();

    private new Transform   transform = null;
    private Transform       parent = null;
    private RaycastHit      hit = new RaycastHit();


    #region ISubscriber
    public string Description
    {
        get
        {
            return name;
        }

        set
        {
            name = value;
        }
    }

    public void Reaction(Message message, params object[] parameters)
    {
        switch (message)
        {
            case Message.SettingsChanged:
                switch (parameters.Get<GameSettingsParameter>())
                {
                    case GameSettingsParameter.Shadows:
                        gameObject.SetActive(!parameters.Get<bool>());
                        break;
                }
                break;
        }
    }
    #endregion

    private void Awake()
    {
        StaticType.Options.AddSubscriber(this);
        transform = GetComponent<Transform>();
        parent = transform.parent;
        shadowLayer = 1 << LayerMask.NameToLayer("Terrain");        

        gameObject.SetActive(!StaticType.Options.Instance<IOptions>().Shadows);
    }

    private void OnDestroy()
    {
        StaticType.Options.RemoveSubscriber(this);
    }

    private int frame = 3;
    private void FixedUpdate()
    {
        frame++;
        if (frame % 5 != 0)
        {
            return;
        }

        Vector3 position = parent.position;
		start.Set(position.x, position.y + 1, position.z);
		end.Set(position.x, position.y - 10, position.z);

		if(Physics.Linecast(start, end, out hit, shadowLayer))
		{
            point.Set(hit.point.x, hit.point.y + correctHeight, hit.point.z);
			transform.position = point;
		}	
	}
}