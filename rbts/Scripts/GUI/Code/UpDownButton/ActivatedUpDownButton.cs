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
			uiItem.enabled = alwaysActiveUIItem ? true : isActivated;

        if (upStateGO && GameData.IsGame(Game.IronTanks | Game.ToonWars))
            upStateGO.SetActive(isActivated);
            
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
                HelpTools.SetAlphaForAllWidgets(objectsToChangeAlpha[i], isActivated ? activeAlpha : inactiveAlpha);

        if (objectsToHide != null)
            for (int i = 0; i < objectsToHide.Length; i++)
                HelpTools.SetAlphaForAllWidgets(objectsToHide[i], isActivated ? activeAlpha : 0);
    }
}
