using System;
using UnityEngine;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;

namespace XDevs
{
    public class Entity
    {
        public readonly EntityTypes type = EntityTypes.Money;
        public readonly ObscuredInt id = 0;
        public readonly ObscuredInt amount = 0;
        public readonly ObscuredFloat lifeTime = 0;
        public readonly SimplePrice price;

        public BgType bgType = BgType.None;

        public ProfileInfo.Price Price { get { return price.ToPrice(); } }

        public string Text
        {
            get
            {
                switch (type)
                {
                    case EntityTypes.Money: return price.ToPrice().LocalizedValue;
                    case EntityTypes.Consumable:
                        return MiscTools.GetCultureSpecificFormatOfNumber(amount);
                    case EntityTypes.Camouflage:
                    case EntityTypes.Decal: return string.Format("{0}", Clock.GetTimerString(Convert.ToInt64(lifeTime), true));
                    default: return "";
                };
            }
        }

        public string GetSprite(bool useConsumableSpriteWithFrame)
        {
            switch (type)
            {
                case EntityTypes.Money: return price.ToPrice().GetBankSpriteByPriceRange();
                case EntityTypes.Consumable: return GameData.consumableInfos[id].GetIcon(useConsumableSpriteWithFrame);
                case EntityTypes.Camouflage:
                case EntityTypes.Decal: return string.Format("{0:00}", (int)id);
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
                case EntityTypes.Money:
                    initDict.Extract("money", ref entityParams);
                    if (entityParams != null)
                        price = SimplePrice.FromDictionary(entityParams);
                    break;
                case EntityTypes.Consumable:
                    initDict.Extract("consumable", ref entityParams);
                    if (entityParams != null)
                    {
                        if (entityParams.ContainsKey("id"))
                            id = Convert.ToInt32(entityParams["id"]);
                        if (entityParams.ContainsKey("amount"))
                        {
                           
                            amount = Convert.ToInt32(entityParams["amount"]);
                        }
                    }
                    break;
                case EntityTypes.Camouflage:
                    initDict.Extract("camouflage", ref entityParams);
                    if (entityParams != null)
                    {
                        if (entityParams.ContainsKey("id"))
                            id = Convert.ToInt32(entityParams["id"]);
                        if (entityParams.ContainsKey("lifetime"))
                            lifeTime = Convert.ToInt32(entityParams["lifetime"]);
                    }
                    break;
                case EntityTypes.Decal:
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
