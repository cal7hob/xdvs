using System;
using UnityEngine;
using System.Collections.Generic;

public class IECell : MonoBehaviour
{
	[Serializable]
	public enum IEIcon
	{
		None = 0,
		Bonus_Attack,
        //Bonus_RocketAttack,
		Bonus_ROF,
		Boost,
		Landmine,
        Missile
	}
	
	[SerializeField]private bool isEffect;
    //[SerializeField]private List<Notifier.BonusData> bonusesData;

    //private static Dictionary<string, Notifier.BonusData> bonusesDataDic = new Dictionary<string, Notifier.BonusData>();
    public int position;

    private int count;
	private int timer;
	private IEIcon itemType;
	
	void Awake()
	{
		SetKind();

        //if (bonusesData != null)
        //    for (int i = 0; i < bonusesData.Count; i++)
        //        if (bonusesData[i] != null)
        //            bonusesDataDic[bonusesData[i].name] = bonusesData[i];
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
		}
	}
	
	public int Count
	{
		get { return count; }
		set
		{
			count = Mathf.Clamp(value, 0, Int32.MaxValue);
		}
	}

	public IECell.IEIcon ItemType
	{
		set
		{
            itemType = value;
            string bonusName = itemType.ToString();
            //if (bonusesDataDic.ContainsKey(bonusName))
            //{
            //    sprite.SetSprite(bonusesDataDic[bonusName].sprite);
            //    sprite.scale = bonusesDataDic[bonusName].spriteScale;
            //    MiscTools.SetObjectsActivity(bonusesDataDic[bonusName].objectsToActivate, true);
            //}
            //else
            //    Debug.LogErrorFormat("Cant show bonus {0}", bonusName);
        }
	}
	
	private void SetKind()
	{
	}

    private void HideAdditionalBonusObjects()
    {
        //foreach (var bonusDataPair in bonusesDataDic)
        //    MiscTools.SetObjectsActivity(bonusDataPair.Value.objectsToActivate, false);
    }
}
