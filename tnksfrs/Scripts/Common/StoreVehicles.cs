using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XD
{
    public class StoreVehicles : SubStore
    {
        private IStore          commonStore = null;
        private IUnitHangar     unitForSale = null;

        private int CurrentUnitID
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

        IUnitHangar CurrentUnit
        {
            get
            {
                return commonStore.CurrentUnit;
            }
        }

        public void BuyUnit(int id)
        {
            if (CurrentUnitID >= 0)
            {
                Debug.LogError("DataRequest BuyVehicle RETURN: " + CurrentUnitID);
                return;
            }
            
            IUnitHangar newVeh = StaticContainer.Get<IMainData>(StaticType.MainData).GetUnitHangar(id);
            IProfile profile = StaticContainer.Get<IProfile>(StaticType.Profile);
            
            if (!newVeh.IsPremium && !profile.HasFreeHangarSlot)
            {
                if (profile.HasMaxSlots)
                {
                    PSYGUI.Event(PSYEvent.MessageBox, PSYParams.New(MessageBoxType.Notification, "UI_MB_Max_Slots_Title", "UI_MB_Max_Slots_Text", "UI_OK"));
                    CurrentUnitID = -1;
                    return;
                }

                commonStore.Event(Message.MessageBox, MessageBoxType.BuySlot, "UI_MB_No_Hangar_Slots_Title", "UI_MB_BuySlot_Message", "UI_MB_Buy", "UI_MB_Cancel", StaticContainer.Profile.HangarSlotPrice);
                //Debug.LogError("Попытка купить танк! " + newVeh.Name + ". Не хватает места в ангаре. " + StaticContainer.MainData.MyUnits.Count + " / " + profile.HangarSlots);
                CurrentUnitID = -1;
                return;
            }
            
            CurrencyValue price = newVeh.CurrentPrice.Clone();
            if (!profile.Balance.Enough(price))
            {
                price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                commonStore.NotEnoughMoney(price);
                commonStore.Event(Message.StoreCallBack, DataType.UI, DataKey.BuyUnit, StoreItemType.Vehicle);
                //Debug.LogError("Попытка купить танк! " + newVeh.Name + ". Не хватает лавэ.");
                CurrentUnitID = -1;
                return;
            }

            //ПАМЯТКА
            //Dictionary<string, int> ids = new Dictionary<string, int>();
            //Dictionary<string, string> names = new Dictionary<string, string>();
            CurrentUnitID = id;
            commonStore.Event(Message.DataRequest, DataType.Server, DataKey.BuyUnit, "unitId", newVeh.ID);
        }

        public void ResearchUnit(int id)
        {
            if (CurrentUnitID >= 0)
            {
                Debug.LogError("DataRequest ResearchVehicle RETURN: " + CurrentUnitID);
                return;
            }

            IUnitHangar newVeh = StaticContainer.Get<IMainData>(StaticType.MainData).GetUnitHangar(id);
            IProfile profile = StaticContainer.Get<IProfile>(StaticType.Profile);
            CurrencyValue price = newVeh.CurrentPrice.Clone();

            if (!profile.Balance.Enough(price))
            {
                price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                commonStore.NotEnoughMoney(price);
                //Debug.LogError("Попытка изучить танк! " + newVeh.Name + ". Не хватает опыта.");
                CurrentUnitID = -1;
                return;
            }

            //Debug.LogError("DataRequest ResearchVehicle: " + id);
            CurrentUnitID = id;
            commonStore.Event(Message.DataRequest, DataType.Server, DataKey.ResearchUnit, "unitId", newVeh.ID);
        }

        public void SellUnit(int id)
        {
            if (CurrentUnitID >= 0)
            {
                Debug.LogError("DataRequest SellVehicle RETURN: " + CurrentUnitID);
                return;
            }
            
            if (StaticContainer.MainData.MyUnits.Count < 2)
            {
                commonStore.Event(Message.MessageBox, MessageBoxType.Notification, "UI_MB_LastUnitSell_Title", "UI_MB_LastUnitSell_Text", "UI_Ok");
                return;
            }

            CurrentUnitID = id;
            IUnitHangar veh = StaticContainer.Get<IMainData>(StaticType.MainData).GetUnitHangar(id);
            Currency fullCost = veh.ConsumeblesCost;
            fullCost[veh.SellPrice.Type].ChangeAmount(veh.SellPrice.Amount);
            //Debug.LogError("ConsumeblesCost: " + newVeh.ConsumeblesCost[CurrencyType.Silver] + " +  veh: " + newVeh.SellPrice.Amount + " = " + fullCost[CurrencyType.Silver] + ", " + fullCost[CurrencyType.Gold]);

            commonStore.Event(Message.MessageBox, MessageBoxType.UnitSell, "UI_Unit_Sell_Title", "UI_Unit_Sell_Message", "UI_MB_Sell", "UI_MB_Cancel", fullCost, (IUIUnit)veh);
            unitForSale = veh;
        }

        public override void Init(IStore store)
        {
            commonStore = store;
        }

        #region ISender

        public override void Reaction(Message message, params object[] parameters)
        {
            //Debug.LogError("StoreVehicles: Reaction(), " + message);

            DataKey dataKey = parameters.Get<DataKey>();
            IProfile profile = null;
            CurrencyValue price;

            switch (message)
            {
                #region RESPONSE
                case Message.DataResponse:
                    bool res = false;
                    IUnitHangar unit = null;
                    Dictionary<string, object> objectParameters;

                    //Debug.LogError("DataResponse: " + dataKey + ", " + parameters.Get<bool>());
                    switch (dataKey)
                    {
                        case DataKey.UnlockVehicle:
                            objectParameters = parameters.Get<Dictionary<string, object>>();
                            List<int> vehIDs = (List<int>)objectParameters["unitId"];

                            if (vehIDs == null)
                            {
                                Debug.LogError("Unlock response: NULL");
                                return;
                            }

                            //Debug.LogError("Unlock response: " + vehIDs.Count + ", " + parameters.Get<bool>());

                            for (int i = 0; i < vehIDs.Count; i++)
                            {
                                unit = StaticContainer.MainData.GetUnitHangar(vehIDs[i]);
                                unit.Unlock();
                                commonStore.Event(Message.UnitBought, (IUIUnit)unit);
                            }

                            break;

                        case DataKey.BuyUnit:
                            res = parameters.Get<bool>();
                            unit = StaticContainer.MainData.GetUnitHangar(CurrentUnitID);

                            if (res)
                            {
                                profile = StaticContainer.Get<IProfile>(StaticType.Profile);
                                unit.ChangeStatus(Status.Bought);
                                commonStore.Event(Message.StoreCallBack, StoreActionType.SuccessBuy, StoreItemType.Vehicle, CurrentUnitID);
                                commonStore.Event(Message.UnitBought, (IUIUnit)unit);
                                unit.Init();
                                profile.ChangeBalance(unit.SellPrice);
                                //Debug.LogError("Куплен танк: " + vehicle.Name + ", " + vehicle.Status);
                            }

                            CurrentUnitID = -1;
                            break;

                        case DataKey.ResearchUnit:
                            res = parameters.Get<bool>();
                            unit = StaticContainer.MainData.GetUnitHangar(CurrentUnitID);

                            if (res)
                            {
                                unit.Research();
                                commonStore.Event(Message.UnitBought, (IUIUnit)unit);
                                //Debug.LogError("Исследован танк: " + vehicle.Name + ", " + vehicle.Status + ", id: " + CurrentUnitID);
                            }
                            else
                            {
                                Event(Message.MessageBox, MessageBoxType.Notification, "Ошибка", "Что-то пошло не так при изучении техники!", "UI_Ok");
                            }

                            CurrentUnitID = -1;
                            break;

                        case DataKey.SellUnit:
                            res = parameters.Get<bool>();
                            profile = StaticContainer.Get<IProfile>(StaticType.Profile);
                            unit = StaticContainer.MainData.GetUnitHangar(CurrentUnitID);
                            
                            if (res)
                            {
                                unit.ChangeStatus(Status.Examined);
                                profile.ChangeBalance(unit.SellPrice);
                                commonStore.Event(Message.StoreCallBack, StoreActionType.SuccessSell, StoreItemType.Vehicle, true, CurrentUnitID);
                                StaticContainer.MainData.MyUnits.Remove(unit);
                                unit.InstalledConsumables.RemoveAll();
                                unit.RefreshParameters();
                                commonStore.Event(Message.UnitBought, (IUIUnit)unit);
                                //Debug.LogError("Продан танк: " + vehicle.Name + ", " + vehicle.Status);
                            }

                            CurrentUnitID = -1;
                            break;
                    }

                    commonStore.Event(Message.BalanceChanged);
                    break;
                #endregion

                #region REQUEST
                case Message.StoreAction:
                    StoreActionType actionType = parameters.Get<StoreActionType>();
                    StoreItemType itemType = parameters.Get<StoreItemType>();

                    switch (itemType)
                    {
                        case StoreItemType.Vehicle:
                            int itemID = parameters.Get<int>();
                            switch (actionType)
                            {
                                case StoreActionType.Buy:
                                    BuyUnit(itemID);
                                    break;
                                case StoreActionType.Sell:
                                    SellUnit(itemID);
                                    break;
                                case StoreActionType.Examine:
                                    ResearchUnit(itemID);
                                    break;
                            }
                            break;
                    }
                    break;
                #endregion
                
                case Message.BuyUnit:
                    int vehID = parameters.Get<int>();
                    IUnitHangar veh = StaticContainer.MainData.GetUnitHangar(vehID);

                    switch (veh.Status)
                    {
                        case Status.Unlocked:
                            ResearchUnit(vehID);
                            break;

                        case Status.Examined:
                            BuyUnit(vehID);
                            break;

                        case Status.Bought:
                            SellUnit(vehID);
                            break;
                    }
                    break;

                case Message.MessageBoxResult:
                    switch (parameters.Get<MessageBoxType>())
                    {
                        case MessageBoxType.UnitSell:
                            bool result = parameters.Get<bool>();
                            if (unitForSale != null)
                            {
                                if (result)
                                {
                                    commonStore.Event(Message.DataRequest, DataType.Server, DataKey.SellUnit, "unitId", unitForSale.ID);
                                }
                                else
                                {
                                    CurrentUnitID = -1;
                                }

                                unitForSale = null;
                            }
                            break;
                    }
                    break;

                case Message.UnitHangarSelected:
                    AddSubscriber(CurrentUnit);
                    break;
            }
        }

        public string Description
        {
            get
            {
                return "[Store] " + name;
            }

            set
            {
                name = value;
            }
        }

        private List<ISubscriber> subscribers = null;

        public List<ISubscriber> Subscribers
        {
            get
            {
                if (subscribers == null)
                {
                    subscribers = new List<ISubscriber>();
                }
                return subscribers;
            }
        }

        public void AddSubscriber(ISubscriber subscriber)
        {
            if (Subscribers.Contains(subscriber))
            {
                return;
            }
            Subscribers.Add(subscriber);
        }

        public void RemoveSubscriber(ISubscriber subscriber)
        {
            Subscribers.Remove(subscriber);
        }

        public void Event(Message message, params object[] _parameters)
        {
            for (int i = 0; i < Subscribers.Count; i++)
            {
                Subscribers[i].Reaction(message, _parameters);
            }
        }
        #endregion
    }
}