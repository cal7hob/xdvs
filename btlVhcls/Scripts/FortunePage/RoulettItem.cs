using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using XDevs;

public class RoulettItem : MonoBehaviour, IItem
{
    public tk2dSlicedSprite sizeBg;//для определения размера итема
    [SerializeField] private tk2dSlicedSprite sprite;//Именно tk2dSlicedSprite - нужно для пропорционального масштабирования
    [SerializeField] private bool useConsumableSpriteWithFrame = false;
    [SerializeField] private tk2dTextMesh lblCount;
    [SerializeField] private Transform[] objectsToRotateWhenRoulettRotates;
    [SerializeField] private BgChanger bgChanger;

    public RoulettItemInfo Data { get; private set; }
    public float AngleOffset{get; private set;}
    public float CirclePos { get { return 360 - AngleOffset; } }
    public bool UseConsumableSpriteWithFrame { get { return useConsumableSpriteWithFrame; } }

    public void Initialize(object[] parameters)
    {
        Data = (RoulettItemInfo)parameters[0];
        gameObject.name = string.Format("Item_{0:00}", Data.sector);

        UpdateElements();
    }

    public virtual void UpdateElements()
    {
        AngleOffset = 60 * Data.SectorIndex;
        transform.localRotation = Quaternion.Euler(transform.localRotation.x, transform.localRotation.y, -AngleOffset);

        if(bgChanger)
            bgChanger.SetBg(Data.bgType);

        lblCount.text = Data.entity.Text;

        string spriteName = Data.entity.GetSprite(useConsumableSpriteWithFrame);
        if(string.IsNullOrEmpty(spriteName))
            sprite.gameObject.SetActive(false);
        else
        {
            sprite.SetSprite(AtlasesManager.GetAtlasDataByEntity(Data.entity.type), spriteName);
            MiscTools.ResizeSlicedSpriteAccordingToTextureProportions(sprite);
        }
    }

    private void LateUpdate()
    {
        HelpTools.SetRotationToAllObjectsInCollection(objectsToRotateWhenRoulettRotates, Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, transform.rotation.eulerAngles.z.InvertSign()));
    }

    public void DesrtoySelf()
    {
    }

    public Vector2 GetSize()
    {
        return new Vector2(sizeBg.dimensions.x * sizeBg.scale.x, sizeBg.dimensions.y * sizeBg.scale.y);
    }

    public string GetUniqId { get { return Data.sector.ToString(); } }

    public tk2dUIItem MainUIItem { get { return null; } }

    public Transform MainTransform { get { return transform; } }
}
