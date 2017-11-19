using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Pool;

public class IECell : PoolObject
{
    [SerializeField]private tk2dSprite sprite;
	[SerializeField]private tk2dTextMesh timerTextMesh;
	[SerializeField]private tk2dTextMesh countTextMesh;
    [SerializeField]private List<Notifier.BonusData> bonusesData;

    private VehicleEffect effect;
    private IEPanel effectPanel;
    private static Dictionary<string, Notifier.BonusData> bonusesDataDic;
    public int position;

    public void Assign(IEPanel effectPanel)
    {
        this.effectPanel = effectPanel;
    }

    void Awake()
	{
	    CheckBonusesDataDic();
        SetKind();
        HideAdditionalBonusObjects();
    }

    void Update()
    {
        if (effect == null)
            return;

        int timer = Mathf.CeilToInt(effect.Remain - 0.01f);
        timerTextMesh.text = timer.ToString();
        if (timer == 0)
        {
            Release();
        }
    }

    public override void OnGetFromPool()
    {
        gameObject.SetActive(true);
    }

    public void Release()
    {
        ReturnObject();
        if (effectPanel != null)
        {
            effectPanel.CellReleased(this);
        }
    }

    public void SetEffect(VehicleEffect effect)
    {
        this.effect = effect;
        timerTextMesh.gameObject.SetActive(effect != null);
        RefreshSprite();
    }

    public string IconName
    {
        set
        {
            Notifier.BonusData bonusData = FindBonusData(value);
            if (bonusData != null)
            {
                // Временно закомментировано пока нет pos neg спрайтов
                //string.Format("{0}_{1}", effect.ParamType, effect.IsPositive ? "pos" : "neg");

                var spriteName = bonusData.sprite;

                if (string.IsNullOrEmpty(spriteName))
                    spriteName = bonusData.name;

                sprite.SetSprite(string.Format("{0}_substrate", spriteName));
                sprite.scale = bonusData.spriteScale;

                MiscTools.SetObjectsActivity(bonusData.objectsToActivate, true);
            }
            else
            {
                Debug.LogErrorFormat("Can't show bonus {0}", value);
            }
        }
    }

    private void SetKind()
	{
		timerTextMesh.gameObject.SetActive(true);
		countTextMesh.gameObject.SetActive(false);
	}  //TODO: Устаревшая функция. Оставлена от потенциальных ошибок инициализации. При возможности избавиться от неё.

    private void RefreshSprite()
    {
        IconName = string.Format("{0}", effect.ParamType);
    }

    private void HideAdditionalBonusObjects()
    {
        foreach (var bonusDataPair in bonusesDataDic)
            MiscTools.SetObjectsActivity(bonusDataPair.Value.objectsToActivate, false);
    }

    private void CheckBonusesDataDic()
    {
        if (bonusesDataDic == null)
        {
            bonusesDataDic = new Dictionary<string, Notifier.BonusData>();
            Messenger.Subscribe(EventId.BattleEnd, OnBattleEnd);  //Однократная подписка на всё время работы приложения
        }

        if (bonusesDataDic.Count > 0)
            return;

        if (bonusesData != null)
        {
            for (int i = 0; i < bonusesData.Count; i++)
            {
                if (bonusesData[i] != null)
                    bonusesDataDic[bonusesData[i].name] = bonusesData[i];
            }
        }
    }

    private Notifier.BonusData FindBonusData(string iconNamePretender)
    {
        Notifier.BonusData bonusData;
        if (bonusesDataDic.TryGetValue(iconNamePretender, out bonusData))
            return bonusData;

        foreach (var data in bonusesDataDic.Values)
        {
            if (data.name == iconNamePretender)
                return data;
        }

        return bonusesDataDic.First().Value;
    }

    private static void OnBattleEnd(EventId eid, EventInfo ei)
    {
        bonusesDataDic.Clear();
    }
}
