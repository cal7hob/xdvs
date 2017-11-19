using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using XDevs;

public class DailyBonusItem : MonoBehaviour, IItem
{
    [SerializeField] private tk2dSlicedSprite sizeBg;//для определения размера итема
    [SerializeField] private tk2dSlicedSprite sprite;//Именно tk2dSlicedSprite - нужно для пропорционального масштабирования
    [SerializeField] private bool useConsumableSpriteWithFrame = true;
    [SerializeField] private tk2dTextMesh[] dayNumLabels;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField] private tk2dBaseSprite currencySprite;
    [SerializeField] private ActivatedUpDownButton objectsToChangeAlphaWhenDayPassed;
    [SerializeField] private ActivatedUpDownButton objectsToActivateForCurrentDay;//Объекты, активируемые на плашке текущего дня
    [SerializeField] private ActivatedUpDownButton objectsToActivateInFutureDays;//Объекты, активируемые на плашках последующих дней(после текущего дня)
    [SerializeField] private ActivatedUpDownButton objectsToActivateInMissedDays;//Объекты, активируемые на плашках упущенных дней
    [SerializeField] private BgChanger bgChanger;
    [SerializeField] private ActivatedUpDownButton specialActivator;//Отдельный скрипт для регулировкой альфы в специфических условиях
    [Header("Определяем условия для активации specialActivator")]
    [SerializeField] bool specialActivator_passedDay;
    [SerializeField] bool specialActivator_curDay;
    [SerializeField] bool specialActivator_futureDay;//Кроме missed
    [SerializeField] bool specialActivator_missedDay;



    public int DayIndex { get; private set; }
    private DailyBonusInfo data;

    public void Initialize(object[] parameters)
    {
        data = (DailyBonusInfo)parameters[0];
        DayIndex = data.day - 1;
        gameObject.name = string.Format("day_{0:00}", data.day);

        UpdateElements();
    }

    public virtual void UpdateElements()
    {
        if(bgChanger)
            bgChanger.SetBg(data.bgType);

        if (dayNumLabels != null)
            for (int j = 0; j < dayNumLabels.Length; j++)
                if (dayNumLabels[j] != null)
                    dayNumLabels[j].text = (DayIndex + 1).ToString();

        lblCount.text = data.entity.Text;

        currencySprite.gameObject.SetActive(data.entity.type == EntityTypes.money);
        if(data.entity.type == EntityTypes.money)
            currencySprite.SetSprite(data.entity.Price.SpriteName);

        string spriteName = data.entity.GetSprite(useConsumableSpriteWithFrame);
        if (string.IsNullOrEmpty(spriteName))
            sprite.gameObject.SetActive(false);
        else
        {
            sprite.SetSprite(AtlasesManager.GetAtlasDataByEntity(data.entity.type), spriteName);
            MiscTools.ResizeSlicedSpriteAccordingToTextureProportions(sprite);
        }
            
        #region Переключаем специфичный объекты
        if (objectsToChangeAlphaWhenDayPassed)
            objectsToChangeAlphaWhenDayPassed.Activated = false;//Выключаем объекты, активируемые для прошедших дней по умолчанию
        if (objectsToActivateInFutureDays)
            objectsToActivateInFutureDays.Activated = false;//Выключаем объекты, активируемые для будущих дней по умолчанию
        if (objectsToActivateForCurrentDay)
            objectsToActivateForCurrentDay.Activated = false;//Выключаем объекты, активируемые для текущего дня по умолчанию
        if (objectsToActivateInMissedDays)
            objectsToActivateInMissedDays.Activated = false;//Выключаем объекты, активируемые для текущего дня по умолчанию
        if (specialActivator)//Выключаем объекты, активируемые для текущего дня по умолчанию
            specialActivator.Activated = false;

        //Взаимоисключающие варианты
        if(DayIndex >= GameData.dailyBonusInfos.Count - ProfileInfo.dailyBonusMissed)//Пропущенный день
        {
            if (objectsToActivateInMissedDays)
                objectsToActivateInMissedDays.Activated = true;
            if (specialActivator && specialActivator_missedDay)
                specialActivator.Activated = true;
        }
        else if (data.day == ProfileInfo.dailyBonusDay)//Текущий день
        {
            if (objectsToActivateForCurrentDay)
                objectsToActivateForCurrentDay.Activated = true;
            if (specialActivator && specialActivator_curDay)
                specialActivator.Activated = true;
        }
        else if (data.day < ProfileInfo.dailyBonusDay)//Раньше текущего
        {
            if (objectsToChangeAlphaWhenDayPassed)
                objectsToChangeAlphaWhenDayPassed.Activated = true;
            if (specialActivator && specialActivator_passedDay)
                specialActivator.Activated = true;
        }
        else if (data.day > ProfileInfo.dailyBonusDay)//После текущего и не пропущенный день (иначе бы мы пошли в первый if )
        {
            if (objectsToActivateInFutureDays)
                objectsToActivateInFutureDays.Activated = true;
            if (specialActivator && specialActivator_futureDay)
                specialActivator.Activated = true;
        }

        #endregion
    }

    public void DesrtoySelf()
    {
    }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    public string GetUniqId { get { return DayIndex.ToString(); } }

    public tk2dUIItem MainUIItem { get { return null; } }

    public Transform MainTransform { get { return transform; } }
}
