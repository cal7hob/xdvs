using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XD
{
    public class StoreModules : SubStore
    {
        private IStore                  commonStore = null;
        private Dictionary<int, bool>   modulesAllreadyBoosted = new Dictionary<int, bool>();
        private int                     tryingToBoost = -1;

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
        
        /// <summary>
        /// Прокачка модуля (запрос серверу)
        /// </summary>
        private void UpgradeModuleByRequest(int vehID, VehicleModule module)
        {
            Dictionary<string, int> intParameters = new Dictionary<string, int>();
            intParameters.Add("unitId", vehID);
            Dictionary<string, string> stringParameters = new Dictionary<string, string>();
            stringParameters.Add("module", module.Type.ToString());
            commonStore.Event(Message.DataRequest, DataType.Server, DataKey.UpgradeModule, intParameters, stringParameters);
            //Debug.Log("Sent message to server (UpgradeModule), veh: " + vehID + ", module: " + module.Type);
        }

        /// <summary>
        /// Ускорение прокачки модуля (запрос серверу)
        /// </summary>
        private void BoostModuleByRequest(int vehID, VehicleModule module)
        {
            Dictionary<string, int> intParameters = new Dictionary<string, int>();
            intParameters.Add("unitId", vehID);
            Dictionary<string, string> stringParameters = new Dictionary<string, string>();
            stringParameters.Add("module", module.Type.ToString());

            //Debug.LogError("BoostModuleByRequest() vehicle: " + vehID + ", param: " + stringParameters["module"]);
            commonStore.Event(Message.DataRequest, DataType.Server, DataKey.BoostModule, intParameters, stringParameters);
            Debug.Log("Sent message to server (BoostModule), veh: " + vehID + ", module: " + module.Type);
        }

        /// <summary>
        /// Прокачка модуля (ответ от сервера)
        /// </summary>
        private void UpgradeModuleByResponse(Dictionary<string, string> stringParameters)
        {
            ModuleType moduleType = stringParameters["module"].ToEnum<ModuleType>();
            VehicleModule module = commonStore.CurrentUnit.Modules[moduleType];
            StaticContainer.Profile.ChangeBalance(module.CurrentPrice);
            commonStore.Event(Message.StoreCallBack, StoreActionType.SuccessBuy, StoreItemType.Module, moduleType);
            SetAllreadyBoostedModule(CurrentUnitID, false);
            //Debug.Log("Recieve message from server (UpgradeModule), veh: " + commonStore.CurrentUnit.Name + ", module: " + moduleType + ", inProgress: " + module.InProgress + ", price: " + module.CurrentPrice);
        }

        /// <summary>
        /// Ускорение прокачки модуля (ответ от сервера)
        /// </summary>
        private void BoostModuleByResponse(Dictionary<string, string> stringParameters)
        {
            ModuleType moduleType = stringParameters["module"].ToEnum<ModuleType>();
            VehicleModule module = commonStore.CurrentUnit.Modules[moduleType];
            StaticContainer.Profile.ChangeBalance(module.CurrentPrice);
            commonStore.Event(Message.StoreCallBack, StoreActionType.SuccessBoost, StoreItemType.Module, moduleType);
            //Debug.Log("Recieve message from server (BoostModule), veh: " + commonStore.CurrentUnit.Name + ", module: " + moduleType + ", inProgress: " + module.InProgress + ", price: " + module.CurrentPrice);
        }

        public override void Reaction(Message message, params object[] parameters)
        {
            DataKey dataKey = parameters.Get<DataKey>();
            IProfile profile = null;
            CurrencyValue price;

            switch (message)
            {
                case Message.DataResponse:
                    bool res = false;
                    IUnitHangar vehicle = null;
                    Dictionary<string, string> stringParameters;

                    //Debug.LogError("DataResponse: " + dataKey + ", " + parameters.Get<bool>());
                    switch (dataKey)
                    {
                        case DataKey.UpgradeModule:
                            stringParameters = parameters.Get<Dictionary<string, string>>();

                            if (parameters.Get<bool>())
                            {
                                UpgradeModuleByResponse(stringParameters);
                            }

                            CurrentUnitID = -1;
                            break;
                        case DataKey.BoostModule:
                            stringParameters = parameters.Get<Dictionary<string, string>>();

                            if (parameters.Get<bool>())
                            {
                                BoostModuleByResponse(stringParameters);
                            }

                            CurrentUnitID = -1;
                            break;
                    }
                    break;

                case Message.StoreAction:
                    StoreActionType actionType = parameters.Get<StoreActionType>();
                    StoreItemType itemType = parameters.Get<StoreItemType>();

                    //Debug.LogError("StoreAction, actionType: " + actionType + ", itemType: " + itemType + ", param: " + _parameters.Count);

                    switch (itemType)
                    {
                        case StoreItemType.Module:
                            VehicleModule module;
                            CurrentUnitID = parameters.Get<int>();
                            vehicle = StaticContainer.MainData.GetUnitHangar(CurrentUnitID);
                            List<UniversalParams> _parameters = parameters.Get<List<UniversalParams>>();

                            for (int i = 0; i < _parameters.Count; i++)
                            {
                                module = vehicle.Modules[_parameters[i].Get<ModuleType>(0)];

                                if (commonStore.CurrentUnit.WaitingModuleType == ModuleType.None)
                                {
                                    if (tryingToBoost != -1 && GetAllreadyBoostedModule(CurrentUnitID))
                                    {
                                        commonStore.Event(Message.MessageBox, MessageBoxType.Notification, "UI_MB_ModuleAllreadyBoosted_Title", "UI_MB_ModuleAllreadyBoosted_Text", "UI_Ok");
                                        SetAllreadyBoostedModule(CurrentUnitID, false);
                                        CurrentUnitID = -1;
                                        return;
                                    }
                                }
                                else if (commonStore.CurrentUnit.WaitingModuleType != module.Type)
                                {
                                    tryingToBoost = commonStore.CurrentUnit.ID;
                                    //Debug.LogError("УЖЕ ОЖИДАЕТСЯ ДРУГОЙ МОДУЛЬ: " + commonStore.CurrentUnit.WaitingModule);
                                    commonStore.Event(Message.MessageBox, MessageBoxType.ModuleBoost, "UI_MB_ModuleBoost_Title", PSYGUI.Localize("UI_MB_ModuleBoost_Text") + " \n" + PSYGUI.Localize("UI_ModuleName_" + commonStore.CurrentUnit.WaitingModuleType), "UI_MB_ModuleBoost", "UI_Cancel", vehicle.Modules[vehicle.WaitingModuleType].CurrentPrice);
                                    return;
                                }

                                price = module.CurrentPrice.Clone();

                                profile = StaticContainer.Get<IProfile>(StaticType.Profile);
                                if (!profile.Balance.Enough(price))
                                {
                                    //Debug.LogError("Module " + actionType + ": Not enough money! need: " + module.CurrentPrice + ", have: " + profile.Balance[module.CurrentPrice.Type]);
                                    price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                                    commonStore.NotEnoughMoney(price);
                                    return;
                                }

                                switch (actionType)
                                {
                                    case StoreActionType.Buy:
                                        UpgradeModuleByRequest(CurrentUnitID, module);
                                        break;
                                    case StoreActionType.Boost:
                                        BoostModuleByRequest(CurrentUnitID, module);
                                        break;
                                }
                            }
                            break;
                    }
                    break;
                
                case Message.ModuleBought:
                    SetAllreadyBoostedModule(parameters.Get<int>(), !parameters.Get<bool>());
                    break;

                case Message.MessageBoxResult:
                    switch (parameters.Get<MessageBoxType>())
                    {
                        case MessageBoxType.NotEnoughMoney:
                            CurrentUnitID = -1;
                            break;

                        case MessageBoxType.ModuleBoost:
                            tryingToBoost = -1;
                            break;
                    }
                    break;
            }
        }

        private bool GetAllreadyBoostedModule(int unitID)
        {
            bool res = false;
            modulesAllreadyBoosted.TryGetValue(unitID, out res);
            return res;
        }

        private void SetAllreadyBoostedModule(int unitID, bool val)
        {
            modulesAllreadyBoosted[unitID] = val;
        }

        public override void Init(IStore store)
        {
            commonStore = store;
        }
    }
}