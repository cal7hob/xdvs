using System;
using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

namespace XDevs
{
    public class Entity
    {
        public readonly EntityTypes type = EntityTypes.money;
        public readonly ObscuredInt id = 0;
        public readonly ObscuredInt amount = 0;
        public readonly ObscuredFloat lifeTime = 0;
        public readonly SimplePrice price;

        public BgType bgType = BgType.none;

        public ProfileInfo.Price Price { get { return price.ToPrice(); } }

        public string Text
        {
            get
            {
                switch (type)
                {
                    case EntityTypes.money: return price.ToPrice().LocalizedValue;
                    case EntityTypes.consumable:
                        return GameData.consumableInfos[id].isSuperWeapon ?
                                   Clock.GetTimerString(Convert.ToInt64(lifeTime * 3600), true) :
                                   MiscTools.GetCultureSpecificFormatOfNumber(amount);
                    case EntityTypes.cam:
                    case EntityTypes.decal: return string.Format("{0}", Clock.GetTimerString(Convert.ToInt64(lifeTime), true));
                    default: return "";
                };
            }
        }

        public string GetSprite(bool useConsumableSpriteWithFrame)
        {
            switch (type)
            {
                case EntityTypes.money: return price.ToPrice().GetBankSpriteByPriceRange();
                case EntityTypes.consumable: return GameData.consumableInfos[id].GetIcon(useConsumableSpriteWithFrame);
                case EntityTypes.cam:
                case EntityTypes.decal: return string.Format("{0:00}", (int)id);
                default: return "";
            };
        }

        public Entity(Dictionary<string, object> initDict)
        {
            Dictionary<string, object> priceDict = null;

            bool allDataReceived = true;
            allDataReceived &= initDict.Extract("type", ref type);
            initDict.Extract("backColor", ref bgType, false);//Может и не прийти, поэтому не проверяем в allDataReceived
            

            if (!allDataReceived)
                Debug.LogErrorFormat("Entity parsing errors!!! type  = {0}", type);

            initDict.Extract("price", ref priceDict, false);
            if(priceDict != null)
                price = SimplePrice.FromDictionary(priceDict);

            Dictionary<string, object> entityParams = null;
            switch (type)
            {
                case EntityTypes.money:
                    initDict.Extract("money", ref entityParams);
                    if (entityParams != null)
                        price = SimplePrice.FromDictionary(entityParams);
                    break;
                case EntityTypes.consumable:
                    initDict.Extract("consumable", ref entityParams);
                    if (entityParams != null)
                    {
                        if (entityParams.ContainsKey("id"))
                            id = Convert.ToInt32(entityParams["id"]);
                        if (entityParams.ContainsKey("amount"))
                        {
                            if(!GameData.consumableInfos.ContainsKey(id))
                            {
                                Debug.LogErrorFormat("Reference for unknown consumable! id = {0}", id);
                                break;
                            }
                            if (GameData.consumableInfos[id].isSuperWeapon)
                                lifeTime = (float)Convert.ToDouble(entityParams["amount"]);
                            else
                                amount = Convert.ToInt32(entityParams["amount"]);
                        }
                    }
                    break;
                case EntityTypes.cam:
                    initDict.Extract("camouflage", ref entityParams);
                    if (entityParams != null)
                    {
                        if (entityParams.ContainsKey("id"))
                            id = Convert.ToInt32(entityParams["id"]);
                        if (entityParams.ContainsKey("lifetime"))
                            lifeTime = Convert.ToInt32(entityParams["lifetime"]);
                    }
                    break;
                case EntityTypes.decal:
                    initDict.Extract("decal", ref entityParams);
                    if (entityParams != null)
                    {
                        if (entityParams.ContainsKey("id"))
                            id = Convert.ToInt32(entityParams["id"]);
                        if (entityParams.ContainsKey("lifetime"))
                            lifeTime = Convert.ToInt32(entityParams["lifetime"]);
                    }
                    break;
            }
        }
    }
}
