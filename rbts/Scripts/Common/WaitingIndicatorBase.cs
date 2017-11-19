using UnityEngine;
using System.Collections.Generic;

public class WaitingIndicatorBase : MonoBehaviour
{
    [SerializeField] protected GameObject wrapper;
    [SerializeField] protected tk2dSpriteFromTexture circle;
    [SerializeField] protected float circleSpeed = -1.5f;
    [SerializeField] protected tk2dCameraAnchor[] anchors;
    [SerializeField] protected List<Parent> parents;//родители на все случаи жизни - заполнять енам ParentType, накидывать в префаб

    protected Transform DefaultParent { get { return GetParent(ParentType.Default); } }

    public enum ParentType
    {
        Default,
        MapSelection,
        Kits,
    }

    [System.Serializable]
    public class Parent
    {
        public ParentType type;
        public Transform transform;
    }

    protected virtual void Start()
    {
        Camera cam = XdevsSplashScreen.Instance.GetComponent<Camera>();
        if (anchors != null)
            for (int i = 0; i < anchors.Length; i++)
                if (anchors[i] != null)
                    anchors[i].AnchorCamera = cam;
        string path = string.Format("{0}/Textures/WaitingIndicator", GameData.CurInterface);
        Texture2D texToReplace = (Texture2D)Resources.Load(path);
        //DT.LogWarning("SpriteFromRes. Set texture {0}", "Assets/Resources/" + path);
        if (texToReplace == null)
        {
            DT.LogError("WaitingIndicator. Cant find texture {0}", "Assets/Resources/" + path);
            return;
        }
        circle.texture = texToReplace;
        circle.ForceBuild();
    }

    protected virtual void FixedUpdate()
	{
		if (wrapper == null || !wrapper.activeSelf || circle == null)
            return;
        circle.transform.Rotate(0, 0, circleSpeed);
    }

	public void Show(Transform parent = null, Vector3? position = null)
	{
        if(wrapper == null)
        {
            Debug.LogError("WaitingIndicatorBase.wrapper == null!");
            return;
        }
        wrapper.transform.SetParent(parent ? parent : DefaultParent, false);
        //Debug.LogErrorFormat("parent = {0}", wrapper.transform.parent == null ? "null" : wrapper.transform.parent.name);
        if (position != null)
            wrapper.transform.localPosition = (Vector3)position;
        
        wrapper.SetActive(true);
	}

	public void Hide()
	{
		if(wrapper == null)
		{
			Debug.LogError("WaitingIndicatorBase.Hide(). wrapper == null!");
			return;
		}
        wrapper.transform.SetParent(DefaultParent, false);
		wrapper.SetActive(false);
	}

    public void SetActive(bool en)
    {
        if (en)
            Show();
        else
            Hide();
    }

    public bool IsShowed{get{return wrapper.GetActive();}}

    public Transform GetParent(ParentType parentType)
    {
        if (parents == null || parents.Count == 0)
            return null;
        for (int i = 0; i < parents.Count; i++)
            if (parents[i].type == parentType)
                return parents[i].transform;
        return null;
    }
         
}
