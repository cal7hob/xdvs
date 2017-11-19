using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class IECell : MonoBehaviour
{
    [Serializable]
    public enum IEIcon
    {
        None = 0,
        Bonus_Attack,
        Bonus_ROF,
        Boost,
        Landmine,
        Missile
    }

    [SerializeField]
    private tk2dSprite sprite;
    [SerializeField]
    private tk2dTextMesh timerTextMesh;
    [SerializeField]
    private tk2dTextMesh countTextMesh;
    [SerializeField]
    private bool isEffect;
    [SerializeField]
    private List<Notifier.BonusData> bonusesData;

    private Dictionary<string, Notifier.BonusData> bonusesDataDic = new Dictionary<string, Notifier.BonusData>();
    public int position;

    private int count;
    private int timer;

    void Awake()
    {
        if (bonusesData != null)
            for (int i = 0; i < bonusesData.Count; i++)
                if (bonusesData[i] != null)
                    bonusesDataDic[bonusesData[i].name] = bonusesData[i];

        SetKind();
        HideAdditionalBonusObjects();
    }

    public bool IsEffect
    {
        get { return isEffect; }
        set
        {
            if (isEffect == value)
                return;

            isEffect = value;
            SetKind();
        }
    }

    public int Timer
    {
        get { return timer; }
        set
        {
            timer = Mathf.Clamp(value, 0, Int32.MaxValue);
            timerTextMesh.text = timer.ToString();
        }
    }

    public int Count
    {
        get { return count; }
        set
        {
            count = Mathf.Clamp(value, 0, Int32.MaxValue);
            countTextMesh.text = count.ToString();
        }
    }

    public string IconName
    {
        set
        {
            Notifier.BonusData bonusData = null;
            bonusesDataDic.TryGetValue(value, out bonusData);//для бонусов мы найдем спрайт из этого словаря

            if (bonusData == null)//если в словаре бонусов на нашли, значит это расходка, нужно выбрать версию с фоном путем добавления _square к имени спрайта
            {
                bonusData = new Notifier.BonusData();
                bonusData.sprite = value + "_square";
            }

            sprite.SetSprite(bonusData.sprite);
            sprite.scale = bonusData.spriteScale;
            MiscTools.SetObjectsActivity(bonusData.objectsToActivate, true);
        }
    }

    private void SetKind()
    {
        timerTextMesh.gameObject.SetActive(isEffect);
        countTextMesh.gameObject.SetActive(!isEffect);
    }

    private void HideAdditionalBonusObjects()
    {
        foreach (var bonusDataPair in bonusesDataDic)
            MiscTools.SetObjectsActivity(bonusDataPair.Value.objectsToActivate, false);
    }
}
