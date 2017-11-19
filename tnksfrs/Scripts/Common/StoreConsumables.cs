using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XD
{
    public class StoreConsumables : SubStore
    {
        private IStore          commonStore = null;
        private List<int>       consAutoBuyUnitIDs = new List<int>();

        private int CurrentConsumableID
        {
            get
            {
                return commonStore.CurrentUnitID;
            }

            set
            {
                commonStore.CurrentUnitID = value;
            }
        }
        
        /// <summary>
        /// Отправка на сервер запроса на установку расхордки.
        /// </summary>
        /*public void InstallConsumable(int vehicleID, int id, int count)
        {
            if (CurrentConsumableID >= 0)
            {
                return;
            }

            IConsumableHangar hangarConsumable = (IConsumableHangar)StaticContainer.Get<IMainData>(StaticType.MainData).GetConsumableByID(id);
            CurrentConsumableID = id;

            Dictionary<string, int> parameters = new Dictionary<string, int>();
            parameters.Add("unitId", vehicleID);
            parameters.Add(hangarConsumable.KeyParameter, id);
            parameters.Add("count", count);

            DataKey dataKey;

            Debug.LogError(hangarConsumable.SlotType + ": " + id + ", veh: " + vehicleID + ", count: " + count);
            switch (hangarConsumable.SlotType)
            {
                case ConsumableSlotType.Camouflages:
                    //Debug.LogError("InstallCamouflage: " + id + ", veh: " + vehicleID + ", count: " + count);
                    dataKey = DataKey.InstallCamouflage;
                    break;
                case ConsumableSlotType.Decals:
                    dataKey = DataKey.InstallDecal;
                    break;
                default:
                    dataKey = DataKey.InstallConsumable;
                    break;
            }

            commonStore.Event(Message.DataRequest, DataType.Server, dataKey, parameters);
        }*/

        /// <summary>
        /// Отправка на сервер запроса на снятие расхордки с техники.
        /// </summary>
        public void UnInstall(int vehicleID, int id)
        {
            //МОЖЕТ НЕ ПОНАДОБИТЬСЯ
        }

        private void BuyConsumablesByResponse(IProfile profile, Dictionary<string, int> intParameters, DataKey dataKey)
        {
            int consID;
            bool local = false;

            switch (dataKey)
            {
                case DataKey.BuyCamouflage:
                    consID = intParameters["camouflageId"];
                    break;
                case DataKey.BuyDecal:
                    consID = intParameters["decalId"];
                    break;
                default:
                    consID = intParameters["consumableId"];
                    local = true;
                    break;
            }

            IConsumableHangar hangarConsumable = (IConsumableHangar)StaticContainer.MainData.GetConsumableByID(consID);
            profile.ChangeBalance(hangarConsumable.CurrentPrice, local);

            List<UniversalParams> res = new List<UniversalParams>();
            res.Add(new UniversalParams(consID, 0, 1));

            //Debug.LogError("BuyConsumablesByResponse: " + parameters.Count + ", gold: " + goldPrice.amount + ", silver: " + silverPrice.amount);
            commonStore.Event(Message.StoreCallBack, StoreActionType.SuccessBuy, StoreItemType.Consumable, res);
        }

        /// <summary>
        /// Покупка кучи расходок (ответ от сервера)
        /// </summary>
        private void BuyConsumablesByResponse(IProfile profile, Dictionary<string, object> objectParameters)
        {
            //Debug.LogError("BuyConsumablesByResponse: " + objectParameters.DebugString(), this);
            List<UniversalParams> parameters = (List<UniversalParams>)objectParameters["consumables"];
            List<UniversalParams> res = new List<UniversalParams>();
            int consID, count;
            IConsumableHangar hangarConsumable = null;

            CurrencyValue silverPrice = new CurrencyValue(CurrencyType.Silver, 0);
            CurrencyValue goldPrice = new CurrencyValue(CurrencyType.Gold, 0);

            //Debug.LogError("Покупка BuyConsumablesByResponse: " + parameters.Count);
            for (int i = 0; i < parameters.Count; i++)
            {
                consID = parameters[i].Get<int>(0);
                //slotID = parameters[i].Get<int>(1);
                count = parameters[i].Get<int>(2);
                hangarConsumable = (IConsumableHangar)StaticContainer.MainData.GetConsumableByID(consID);

                switch (hangarConsumable.CurrentPrice.Type)
                {
                    case CurrencyType.Silver:
                        silverPrice.SetAmount(silverPrice.Amount - hangarConsumable.CurrentPrice.Amount * count);
                        if (!profile.Balance.Enough(silverPrice))
                        {
                            continue;
                        }
                        break;

                    case CurrencyType.Gold:
                        goldPrice.SetAmount(goldPrice.Amount - hangarConsumable.CurrentPrice.Amount * count);
                        if (!profile.Balance.Enough(goldPrice))
                        {
                            continue;
                        }
                        break;
                }

                res.Add(new UniversalParams(consID, parameters[i].Get<int>(1), count));
                //Debug.LogError("BuyConsumablesByResponse: " + consID + ", slot: " + parameters[i].Get<int>(1));
            }
            
            //Debug.LogError("BuyConsumablesByResponse: " + parameters.Count + ", gold: " + goldPrice.Amount + ", silver: " + silverPrice.Amount);
            commonStore.Event(Message.StoreCallBack, StoreActionType.SuccessBuy, StoreItemType.Consumable, res);
        }

        /// <summary>
        /// Продажа кучи расходок (ответ от сервера)
        /// </summary>
        private void SellConsumablesByResponse(IProfile profile, Dictionary<string, object> objectParameters)
        {
            List<UniversalParams> parameters = (List<UniversalParams>)objectParameters["consumables"];

            int consID, count;
            IConsumableHangar hangarConsumable;
            List<IUIConsumable> consums = new List<IUIConsumable>();

            CurrencyValue silverPrice = new CurrencyValue(CurrencyType.Silver, 0);
            CurrencyValue goldPrice = new CurrencyValue(CurrencyType.Gold, 0);

            for (int i = 0; i < parameters.Count; i++)
            {
                consID = parameters[i].Get<int>(0);
                count = parameters[i].Get<int>(2);
                hangarConsumable = (IConsumableHangar)StaticContainer.MainData.GetConsumableByID(consID);

                switch (hangarConsumable.CurrentPrice.Type)
                {
                    case CurrencyType.Silver:
                        silverPrice.SetAmount(silverPrice.Amount - hangarConsumable.SellPrice.Amount * count);
                        break;
                    case CurrencyType.Gold:
                        goldPrice.SetAmount(goldPrice.Amount - hangarConsumable.SellPrice.Amount * count);
                        break;
                }

                consums.Add((IUIConsumable)hangarConsumable);
            }

            if (silverPrice.Amount != 0)
            {
                profile.ChangeBalance(silverPrice);
            }

            if (goldPrice.Amount != 0)
            {
                profile.ChangeBalance(goldPrice);
            }
            //Debug.LogError("SellConsumablesByResponse: " + parameters.Count + ", gold: " + goldPrice.Amount + ", silver: " + silverPrice.Amount);

            commonStore.Event(Message.StoreCallBack, StoreActionType.SuccessSell, StoreItemType.Consumable, parameters);
        }

        /// <summary>
        /// Покупка/продажа кучи расходок (запрос серверу)
        /// </summary>
        private void BuySellConsumablesRequest(int vehicleID, List<UniversalParams> parameters, ConsumableSlotType consType)
        {
            if (parameters.Count == 0)
            {
                return;
            }

            //Debug.LogError("BuySellConsumablesRequest: " + parameters.Count);
            int consID = 0;
            int consSlotID = 0;
            int count = 0;

            IConsumableHangar hangarConsumable;

            IProfile profile = StaticContainer.Get<IProfile>(StaticType.Profile);
            List<UniversalParams> buys = new List<UniversalParams>();
            List<UniversalParams> installs = new List<UniversalParams>();
            CurrencyValue silverPrice = new CurrencyValue(CurrencyType.Silver, 0);
            CurrencyValue goldPrice = new CurrencyValue(CurrencyType.Gold, 0);

            Dictionary<string, int> intParameters = new Dictionary<string, int>();

            for (int i = 0; i < parameters.Count; i++)
            {
                consID = parameters[i].Get<int>(0);
                consSlotID = parameters[i].Get<int>(1);
                count = parameters[i].Get<int>(2);
                hangarConsumable = (IConsumableHangar)StaticContainer.MainData.GetConsumableByID(consID);
                
                if (count > 0)
                {
                    switch (hangarConsumable.CurrentPrice.Type)//КОСТЫЛЬ нужно нормально расчитывать...
                    {
                        case CurrencyType.Silver:
                            silverPrice.SetAmount(silverPrice.Amount + hangarConsumable.CurrentPrice.Amount * count);
                            if (!profile.Balance.Enough(silverPrice))
                            {
                                continue;
                            }
                            break;

                        case CurrencyType.Gold:
                            goldPrice.SetAmount(goldPrice.Amount + hangarConsumable.CurrentPrice.Amount * count);
                            if (!profile.Balance.Enough(goldPrice))
                            {
                                continue;
                            }
                            break;
                    }

                    //Debug.LogError("buys.add: " + consID + " , count: " + count);
                    buys.Add(new UniversalParams(consID, consSlotID, count));
                }
                else if (count < 0)
                {
                    if (hangarConsumable.SlotType == ConsumableSlotType.Camouflages || hangarConsumable.SlotType == ConsumableSlotType.Decals)
                    {
                        //TODO наклейки и камуфляжи не продаются, а просто снимаются...
                    }
                    else
                    {
                        buys.Add(new UniversalParams(consID, consSlotID, count));
                    }
                }
                else
                {
                    //Debug.LogError("installs.add: " + consID);
                    installs.Add(new UniversalParams(consID, consSlotID, 1));
                }
            }

            if (buys.Count == 0 && installs.Count == 0)
            {
                CurrencyValue price = goldPrice.Amount > 0 ? goldPrice : silverPrice;

                if (!profile.Balance.Enough(price))
                {
                    price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                    commonStore.NotEnoughMoney(price);
                    //Debug.LogError("Попытка купить расходку! Не хватает лавэ.");
                    StaticContainer.Get(StaticType.StaticContainer).Event(Message.StoreCallBack, StoreActionType.BuyFail, StoreItemType.Consumable);
                    return;
                }

                //Debug.LogError("Попытка купить расходки! Не хватает лавэ.");
                commonStore.Event(Message.StoreCallBack, StoreActionType.BuyFail, StoreItemType.Consumable);
                return;
            }

            intParameters.Add("unitId", vehicleID);
            Dictionary<string, object> objectParameters = new Dictionary<string, object>();

            DataKey dataKey;
            string stringKey = "consumables";
            
            objectParameters = new Dictionary<string, object>();
            if (buys.Count > 0)
            {
                switch (consType)
                {
                    case ConsumableSlotType.Camouflages:
                        dataKey = DataKey.BuyCamouflage;
                        //stringKey = "camouflageId";
                        break;

                    case ConsumableSlotType.Decals:
                        dataKey = DataKey.BuyDecal;
                        //stringKey = "decalId";
                        break;

                    default:
                        dataKey = DataKey.BuyConsumable;
                        break;
                }

                Dictionary<string, string> stringParameters = new Dictionary<string, string>();
                List<Dictionary<string, int>> consumablesToBuy = new List<Dictionary<string, int>>(buys.Count);
                for (int i = 0; i < buys.Count; i++)
                {
                    consumablesToBuy.Add(buys[i].ToConsumableJson());
                }

                stringParameters.Add("json", MiniJSON.Json.Serialize(consumablesToBuy));
                objectParameters.Add(stringKey, buys);
                
                //Debug.LogError("DataRequest: " + dataKey + ", " + intParameters.Count);
                commonStore.Event(Message.DataRequest, DataType.Server, dataKey, intParameters, stringParameters, objectParameters);
            }

            objectParameters = new Dictionary<string, object>();
            if (installs.Count > 0)
            {
                switch (consType)
                {
                    case ConsumableSlotType.Camouflages:
                        dataKey = DataKey.InstallCamouflage;
                        stringKey = "camouflage";
                        break;

                    case ConsumableSlotType.Decals:
                        dataKey = DataKey.InstallDecal;
                        stringKey = "decal";
                        break;

                    default:
                        dataKey = DataKey.InstallConsumable;
                        stringKey = "consumables";
                        break;
                }

                objectParameters.Add(stringKey, installs);
                //Debug.LogError("DataRequest: InstallCamouflage, " + intParameters.Count);
                commonStore.Event(Message.DataRequest, DataType.Server, dataKey, intParameters, objectParameters);//КОСТЫЛЬ
            }
        }

        /// <summary>
        /// Установка кучи расходок (запрос серверу)
        /// </summary>
        private void InstallConsumablesRequest(int vehicleID, List<UniversalParams> parameters, ConsumableSlotType slotType)
        {
            Dictionary<string, int> intParameters = new Dictionary<string, int>();
            Dictionary<string, object> objectParameters = new Dictionary<string, object>();

            Dictionary<string, string> stringParameters = new Dictionary<string, string>();
            List<Dictionary<string, int>> consumablesToInstall = new List<Dictionary<string, int>>(parameters.Count);
            DataKey dataKey;

            intParameters.Add("unitId", vehicleID);

            switch (slotType)
            {
                case ConsumableSlotType.Camouflages:
                    dataKey = DataKey.InstallCamouflage;
                    break;

                case ConsumableSlotType.Decals:
                    dataKey = DataKey.InstallDecal;
                    break;

                default:
                    dataKey = DataKey.InstallConsumable;
                    break;
            }

            //if (slotType == ConsumableSlotType.None)
            //{
            //    InstallConsumablesByResponse(parameters);
            //}
            //else if (slotType == ConsumableSlotType.Camouflages)
            //{
                for (int i = 0; i < parameters.Count; i++)
                {
                    consumablesToInstall.Add(parameters[i].ToConsumableJson());
                }

                stringParameters.Add("json", MiniJSON.Json.Serialize(consumablesToInstall));
                objectParameters.Add("consumables", parameters);

                //Debug.LogError("DataRequest: " + dataKey + ", " + intParameters.Count);
                commonStore.Event(Message.DataRequest, DataType.Server, dataKey, intParameters, stringParameters, objectParameters);
            //}
        }

        /// <summary>
        /// Установка кучи расходок (ответ от сервера)
        /// </summary>
        /*private void InstallConsumablesByResponse(List<UniversalParams> parameters)
        {
            //Debug.LogError("InstallConsumablesByResponse: " + parameters.Count);
            commonStore.Event(Message.StoreCallBack, StoreActionType.Install, StoreItemType.Consumable, parameters);
        }*/

        /// <summary>
        /// Установка кучи расходок (ответ от сервера)
        /// </summary>
        private void InstallConsumablesByResponse(Dictionary<string, object> objectParameters)
        {
            List<UniversalParams> parameters = (List<UniversalParams>)objectParameters["consumables"];
            commonStore.Event(Message.StoreCallBack, StoreActionType.Install, StoreItemType.Consumable, parameters);
        }

        public override void Init(IStore store)
        {
            commonStore = store;
        }

        public override void Reaction(Message message, params object[] parameters)
        {
            //Debug.LogError("Store: Reaction(), " + message);

            DataKey dataKey = parameters.Get<DataKey>();
            IProfile profile = null;

            switch (message)
            {
                case Message.DataResponse:
                    bool res = false;
                    IUnitHangar vehicle = null;
                    Dictionary<string, object> objectParameters;
                    Dictionary<string, int> intParameters;

                    //Debug.LogError("DataResponse: " + dataKey + ", " + parameters.Get<bool>());
                    switch (dataKey)
                    {
                        case DataKey.BuyCamouflage:
                        case DataKey.BuyDecal:
                        case DataKey.BuyConsumable:
                            if (!parameters.Get<bool>())
                            {
                                Debug.LogError("Сервак не позволил купить расходки!");
                                return;
                            }

                            profile = StaticContainer.Get<IProfile>(StaticType.Profile);
                            objectParameters = parameters.Get<Dictionary<string, object>>();
                            intParameters = parameters.Get<Dictionary<string, int>>();

                            //Debug.LogError("BuyConsumablesByResponse: " + parameters.Get<bool>());
                            if (objectParameters != null)
                            {
                                bool autoBuy = consAutoBuyUnitIDs.Count > 0;
                                BuyConsumablesByResponse(profile, objectParameters);
                                IUnitHangar unit = StaticContainer.Get<IMainData>(StaticType.MainData).GetUnitHangar(intParameters["unitId"]);
                                consAutoBuyUnitIDs.Remove(unit.ID);

                                //Debug.LogError("BuyConsumable, autobuy: " + autoBuy + ", count: " + consAutoBuyVehicleIDs.Count + ", veh: " + intParameters["unitId"]);
                                if (autoBuy && consAutoBuyUnitIDs.Count == 0)
                                {
                                    StaticContainer.MainData.BeforeHangarEnter();
                                    unit.RefreshParameters();
                                }

                                return;
                            }

                            if (intParameters != null)
                            {
                                //Debug.LogError("IntParameters");
                                BuyConsumablesByResponse(profile, intParameters, dataKey);
                            }
                            break;

                        case DataKey.InstallCamouflage:
                        case DataKey.InstallDecal:
                        case DataKey.InstallConsumable:
                            if (!parameters.Get<bool>())
                            {
                                Debug.LogError("Сервак не позволил установить расходки!");
                                return;
                            }

                            objectParameters = parameters.Get<Dictionary<string, object>>();

                            if (objectParameters != null)
                            {
                                InstallConsumablesByResponse(objectParameters);
                            }
                            break;
                    }
                    break;

                case Message.StoreAction:
                    StoreActionType actionType = parameters.Get<StoreActionType>();
                    StoreItemType itemType = parameters.Get<StoreItemType>();

                    switch (itemType)
                    {
                        case StoreItemType.Consumable:
                            int unitID = parameters.Get<int>(0);
                            List<UniversalParams> universalParameters = parameters.Get<List<UniversalParams>>();
                            List<UniversalParams> camoParameters = new List<UniversalParams>();
                            List<UniversalParams> decalParameters = new List<UniversalParams>();

                            if (universalParameters != null)
                            {
                                IConsumable cons = null;
                                for (int i = 0; i < universalParameters.Count; i++)
                                {
                                    int slotID = universalParameters[i].Get<int>(1);
                                    int consID = universalParameters[i].Get<int>(0);

                                    cons = StaticContainer.MainData.GetConsumableByID(consID);

                                    ConsumableSlotType trueSlotType = StaticContainer.MainData.GetSlotTypeById(slotID);

                                    if (cons.SlotType != trueSlotType)
                                    {
                                        universalParameters[i].Set(1, StaticContainer.MainData.GetSlotByType(cons.SlotType));
                                        //Debug.LogError("Попытка применить расходку к неправильному слоту!! " + cons.Name + ", slotType: " + cons.SlotType + ", slotID: " + slotID);
                                    }

                                    switch (trueSlotType)
                                    {
                                        case ConsumableSlotType.Camouflages:
                                            //Debug.LogError("camoParameters: " + consID + ", slotID: " + slotID);
                                            camoParameters.Add(universalParameters[i]);
                                            break;

                                        case ConsumableSlotType.Decals:
                                            //Debug.LogError("decalParameters: " + consID + ", slotID: " + slotID);
                                            decalParameters.Add(universalParameters[i]);
                                            break;
                                    }
                                }

                                switch (actionType)
                                {
                                    case StoreActionType.Buy:
                                        if (camoParameters.Count > 0)
                                        {
                                            BuySellConsumablesRequest(unitID, camoParameters, ConsumableSlotType.Camouflages);
                                        }
                                        else if (decalParameters.Count > 0)
                                        {
                                            BuySellConsumablesRequest(unitID, decalParameters, ConsumableSlotType.Decals);
                                        }
                                        else
                                        {
                                            if (universalParameters.Count > 0)
                                            {
                                                if (parameters.Get<bool>()) //автодокупка
                                                {
                                                    consAutoBuyUnitIDs.Add(unitID);
                                                }

                                                BuySellConsumablesRequest(unitID, universalParameters, ConsumableSlotType.Consumables);
                                            }
                                        }
                                        break;

                                    case StoreActionType.Install:
                                        if (camoParameters.Count > 0)
                                        {
                                            InstallConsumablesRequest(unitID, camoParameters, ConsumableSlotType.Camouflages);
                                        }
                                        else if (decalParameters.Count > 0)
                                        {
                                            InstallConsumablesRequest(unitID, decalParameters, ConsumableSlotType.Decals);
                                        }
                                        else
                                        {
                                            InstallConsumablesRequest(unitID, universalParameters, ConsumableSlotType.None);
                                        }
                                        break;
                                }
                            }
                            break;
                    }
                    break;
            }
        }
    }
}