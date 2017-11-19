using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class BonusNotify
{
    public tk2dTextMesh topText;
    public tk2dTextMesh bottomText;
    public tk2dBaseSprite bottomSprite;
    public GameObject instance;

    public float disappearanceTime = 2;

    public void SetDescription(string text)
    {
        topText.text = text;
    }

    public void SetCount(string text)
    {
        bottomText.text = text;
    }

    public void SetSprite(string picture)
    {
        bottomSprite.SetSprite(picture);
    }
}

public class Notifier: MonoBehaviour
{
    [System.Serializable]
    public class BonusData
    {
        public string name = "";
        public string locKey = "";
        public string sprite = "";
        public Vector3 spriteScale = new Vector3(2,2,1);
        public GameObject[] objectsToActivate;//When showing
    }

    [SerializeField] private BonusNotify bonusNotify;
    [SerializeField] private tk2dTextMesh outOfMapNotify;
    [SerializeField] private List<BonusData> bonusesData;

    private Dictionary<string, BonusData> bonusesDataDic = new Dictionary<string, BonusData>();
    private IEnumerator outOfMapRoutine;
    private Coroutine disappearCoroutine = null;

    public static Notifier Instance { get; private set; } 
	
	void Awake()
	{
		Instance = this;

        if (bonusesData != null)
            for (int i = 0; i < bonusesData.Count; i++)
                if (bonusesData[i] != null)
                    bonusesDataDic[bonusesData[i].name] = bonusesData[i];

        SetBonusVisible(false);
        RefreshOutOfMapRoutine();
        Dispatcher.Subscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Subscribe(EventId.OnBattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
    }

	void OnDestroy()
	{
        Dispatcher.Unsubscribe(EventId.OnStatTableChangeVisibility, OnStatTableChangeVisibility);
        Dispatcher.Unsubscribe(EventId.OnBattleSettingsChangeVisibility, OnBattleSettingsChangeVisibility);
        Instance = null;
	}

	/*	PRIVATE SECTION	*/

    private void OnStatTableChangeVisibility(EventId id, EventInfo ei)
    {
        //При открытии таблицы - убираем нотифаер
        if (((EventInfo_B)ei).bool1)
            SetBonusVisible(false);
    }

    private void OnBattleSettingsChangeVisibility(EventId id, EventInfo ei)
    {
        //При открытии настроек - убираем нотифаер
        if (((EventInfo_B)ei).bool1)
            SetBonusVisible(false);
    }

    private void SetBonusVisible(bool visible)
	{
        bonusNotify.instance.SetActive(visible);
        if(!visible)
            HideAdditionalBonusObjects();
        Dispatcher.Send(EventId.OnNotifierChangeVisibility, new EventInfo_B(visible));
    }

	private IEnumerator Disappearance()
	{
		yield return new WaitForSeconds(2);
		SetBonusVisible(false);
	}

    private void RefreshOutOfMapRoutine()
    {
        outOfMapRoutine = MiscTools.FadingRoutine(Instance.outOfMapNotify, 2, 0);
    }

    #region PUBLIC SECTION

    /// <summary>
    /// 
    /// </summary>
    /// <param name="bonusName">Имя прописано в списке bonusesData</param>
    public static void ShowBonus(string bonusName, string sprite, string topText, int amount, AudioClip audioClip = null)
    {
        if (!Instance.bonusesDataDic.ContainsKey(bonusName))
            return;

        if (audioClip != null)
            AudioDispatcher.PlayClipAtPosition(audioClip, BattleController.MyVehicle.transform);

        if (StatTable.OnScreen)
            return;

        //Instance.HideAdditionalBonusObjects();
        // Show additional objects for current bonus.
        //MiscTools.SetObjectsActivity(Instance.bonusesDataDic[bonusName].objectsToActivate, true);

        if (Instance.disappearCoroutine != null)
            Instance.StopCoroutine(Instance.disappearCoroutine); // Выключаем действующую корутину.

        Instance.bonusNotify.SetDescription(topText);
        Instance.bonusNotify.SetSprite(sprite);
        Instance.bonusNotify.SetCount(amount > 0 ? MiscTools.GetCultureSpecificFormatOfNumber(amount) : "");
        Instance.SetBonusVisible(true);

        Instance.disappearCoroutine = Instance.StartCoroutine(Instance.Disappearance());
    }
    
    public static void ShowBonus(BonusItem.BonusType bonusType, int amount)
	{
        string bonusName = bonusType.ToString();
        if (!Instance.bonusesDataDic.ContainsKey(bonusName))
        {
            Debug.LogError("Cant show Notifier for bonus " + bonusName);
            return;
        }
        ShowBonus(bonusName, Instance.bonusesDataDic[bonusName].sprite, Localizer.GetText(Instance.bonusesDataDic[bonusName].locKey), amount, BattleController.BonusGetSound);
	}

    private void HideAdditionalBonusObjects()
    {
        foreach (var bonusDataPair in Instance.bonusesDataDic)
            MiscTools.SetObjectsActivity(bonusDataPair.Value.objectsToActivate, false);
    }

    public void ShowOutOfMapNotify() 
    {
        Instance.outOfMapNotify.gameObject.SetActive(true);
        Instance.StartCoroutine(outOfMapRoutine);
    }

    public void StopOutOfMapNotify()
    {
        Instance.outOfMapNotify.gameObject.SetActive(false);
        Instance.StopCoroutine(outOfMapRoutine);
        Instance.RefreshOutOfMapRoutine();
    }
    #endregion
}