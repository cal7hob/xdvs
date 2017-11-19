using UnityEngine;


public class ActivatedUpDownButton : tk2dUIUpDownButton
{
    public GameObject activatedGO;
    public GameObject[] activatedObjects;//Пришлось так сделать, чтобы не слетели ссылки на всех префабах
    public GameObject disactivatedGO;
    public GameObject[] disactivatedObjects;//Пришлось так сделать, чтобы не слетели ссылки на всех префабах
    public GameObject[] objectsToChangeAlpha;
    public GameObject[] objectsToHide;
    public float activeAlpha = 1f;
    public float inactiveAlpha = 0.6f;
    public bool alwaysActiveUIItem = false;
    private bool isInited = false;
    protected bool isActivated = true;

    void Awake()
    {
        isInited = true;
        UseActivation(fromAwake: true); 
    }


    public bool Activated
    {
        get { return isActivated; }
        set
        {
            if (isInited && isActivated == value)
                return;

            isActivated = value;
            if (isInited)
                UseActivation();
        }
    }

    protected virtual void UseActivation(bool fromAwake = false)
    {
		if (uiItem)
			uiItem.enabled = alwaysActiveUIItem || isActivated;
            
        if (downStateGO)
            downStateGO.SetActive(false);

        if (activatedGO)
            activatedGO.SetActive(isActivated);
        
        if (disactivatedGO)
            disactivatedGO.SetActive(!isActivated);

        MiscTools.SetObjectsActivity(activatedObjects, isActivated);
        MiscTools.SetObjectsActivity(disactivatedObjects, !isActivated);
        //DT3.LogError("UseActivation {0}, fromAwake = {1}, object = {2}, parent = {3}, parent.parent.name = {4}",
        //    isActivated, fromAwake, transform.name, transform.parent ? transform.parent.name : "null", transform.parent && transform.parent.parent ? transform.parent.parent.name : "null");
        if (objectsToChangeAlpha != null)
            for (int i = 0; i < objectsToChangeAlpha.Length; i++)
                SetAlphaForAllWidgets(objectsToChangeAlpha[i], isActivated ? activeAlpha : inactiveAlpha);

        if (objectsToHide != null)
            for (int i = 0; i < objectsToHide.Length; i++)
                SetAlphaForAllWidgets(objectsToHide[i], isActivated ? activeAlpha : 0);
    }

    private static void SetAlphaForAllWidgets(GameObject go, float alpha)
    {
        if(go == null) return;
        tk2dBaseSprite sprite = go.GetComponent<tk2dBaseSprite>();

        if (sprite)
            sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, alpha);

        tk2dTextMesh lbl = go.GetComponent<tk2dTextMesh>();

        if (lbl)
            lbl.color = new Color(lbl.color.r, lbl.color.g, lbl.color.b, alpha);
    }
}
