using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using XDevs;


public class ConsumableKitInfoItem : MonoBehaviour, IItem
{
    [SerializeField] private tk2dSlicedSprite sizeBg;
    [SerializeField] private tk2dTextMesh lblText;
    [SerializeField] private tk2dSlicedSprite sprite;
    [SerializeField] private bool useConsumableSpriteWithFrame;
    [SerializeField] private BgChanger bgChanger;

    private Entity entity;
    private int index = 0;

    private void Awake()
    {
        Messenger.Subscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    protected virtual void OnDestroy()
    {
        Messenger.Unsubscribe(EventId.OnLanguageChange, OnLanguageChange);
    }

    public void Initialize(object[] parameters)
    {
        entity = (Entity)parameters[0];
        index = (int)parameters[1];

        UpdateElements();
    }

    public void UpdateElements()
    {
        lblText.text = entity.Text;

        sprite.SetSprite(entity.GetSprite(useConsumableSpriteWithFrame));

        //sprite.SetSprite(AtlasesManager.GetAtlasDataByEntity(entity.type), entity.GetSprite(useConsumableSpriteWithFrame));
        sprite.ResizeSlicedSpriteAccordingToTextureProportions();

        if (bgChanger)
            bgChanger.SetBg(entity.bgType);
    }

    private void OnLanguageChange(EventId evId, EventInfo ev)
    {
        UpdateElements();
    }

    public void DestroySelf()
    {
    }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    public string GetUniqId { get { return index.ToString(); } }

    public tk2dUIItem MainUIItem { get { return null; } }

    public Transform MainTransform { get { return transform; } }
}
