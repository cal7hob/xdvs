using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XD
{
    public class Store : MonoBehaviour, IStore
    {
        private int             currentUnitId = -1;
        private int             currentConsumableID = -1;
        private IUnitHangar     currentUnit = null;
        private ISubStore[]     subStores = null;

        public int CurrentUnitID
        {
            get
            {
                return currentUnitId;
            }

            set
            {
                currentUnitId = value;
            }
        }

        public IUnitHangar CurrentUnit
        {
            get
            {
                return currentUnit;
            }
        }

        public int CurrentConsumableID
        {
            get
            {
                return currentConsumableID;
            }

            set
            {
                currentConsumableID = value;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            SaveInstance();
            subStores = GetComponents<ISubStore>();

            foreach (var subStore in subStores)
            {
                subStore.Init(this);
            }
        }

        private void OnDestroy()
        {
            if (currentUnit != null)
            {
                currentUnit.RemoveSubscriber(this);
                RemoveSubscriber(currentUnit);
            }

            DeleteInstance();
        }

        public void Start()
        {
            StaticType.StaticContainer.AddSubscriber(this);
            AddSubscriber(StaticContainer.Get(StaticType.StaticContainer));

            StaticType.UI.AddSubscriber(this);
            AddSubscriber(StaticContainer.Get(StaticType.UI));

            StaticType.DataHandler.AddSubscriber(this);
            AddSubscriber(StaticContainer.Get(StaticType.DataHandler));
        }

        public void NotEnoughMoney(CurrencyValue price)
        {
            if (price.Type == CurrencyType.Exp || (!Application.isEditor && Application.platform == RuntimePlatform.WindowsPlayer))
            {
                Event(Message.MessageBox, MessageBoxType.NotEnoughMoney, "UI_MB_No_Money_Title", "UI_MB_No_Money_Text", "UI_Ok", price);
                return;
            }

            Event(Message.MessageBox, MessageBoxType.NotEnoughMoney, "UI_MB_No_Money_Title", "UI_MB_No_Money_Text", "UI_Go_To_Bank", "UI_Cancel", price);
        }

        private void SubStoresReaction(Message message, params object[] parameters)
        {
            foreach (var subStore in subStores)
            {
                subStore.Reaction(message, parameters);
            }
        }

        public void Reaction(Message message, params object[] parameters)
        {
            //Debug.LogError("Store: Reaction(), " + message);
            DataKey dataKey = parameters.Get<DataKey>();
            IProfile profile = null;
            CurrencyValue price;
            profile = StaticContainer.Profile;

            switch (message)
            {
                case Message.DataResponse:
                    bool res = false;
                    IUnitHangar vehicle = null;
                    Dictionary<string, int> intParameters;

                    //Debug.LogError("DataResponse: " + dataKey + ", " + parameters.Get<bool>());
                    switch (dataKey)
                    {
                        case DataKey.BuyHangarSlot:
                            if (parameters.Get<bool>())
                            {
                                //Debug.LogError("Слот куплен!");
                                Event(Message.SlotBought);
                            }
                            break;

                        case DataKey.TaskUpdate:
                            if (parameters.Get<bool>())
                            {
                                intParameters = parameters.Get<Dictionary<string, int>>();
                                //Debug.LogError("4 TaskUpdate: " + intParameters["TaskUpdate"]);
                                StaticContainer.Get<ITaskManager>(StaticType.TaskManager).ReplaceActiveTask(intParameters["TaskUpdate"]);
                            }
                            break;
                    }
                    break;

                case Message.BuySlot:
                    price = profile.HangarSlotPrice.Clone();
                    
                    if (price.Amount < 0)
                    {
                        return;
                    }

                    if (!profile.Balance.Enough(price))
                    {
                        price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                        NotEnoughMoney(price);
                        Debug.LogError("Попытка купить ангарное место: не хватает лавэ!");
                        return;
                    }

                    Event(Message.MessageBox, MessageBoxType.BuySlot, "UI_MB_BuySlot_Title", "UI_MB_BuySlot_Message", "UI_MB_Buy", "UI_MB_Cancel", price);
                    break;

                case Message.ChangeNick:
                    price = parameters.Get<CurrencyValue>();

                    if (!profile.Balance.Enough(price))
                    {
                        price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                        NotEnoughMoney(price);
                        Debug.LogError("Попытка сменить ник: не хватает лавэ!");
                        return;
                    }
                    
                    Event(Message.DataRequest, DataKey.ChangeNickName, DataType.UI, parameters.Get<string>());
                    break;

                case Message.MessageBoxResult:
                    switch (parameters.Get<MessageBoxType>())
                    {
                        case MessageBoxType.BuySlot:
                            if (parameters.Get<bool>())
                            {
                                //Debug.LogError("Попытка купить слот!");
                                price = profile.HangarSlotPrice.Clone();

                                if (!profile.Balance.Enough(price))
                                {
                                    price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                                    NotEnoughMoney(price);
                                    Debug.LogError("Попытка купить ангарное место: не хватает лавэ!");
                                }
                                else
                                {
                                    Event(Message.DataRequest, DataType.Server, DataKey.BuyHangarSlot);
                                }
                            }
                            break;
                        case MessageBoxType.ModuleBoost:
                            if (parameters.Get<bool>())
                            {
                                //Debug.LogError("ModuleBoost by MessageBox");
                                List<UniversalParams> _parameters = new List<UniversalParams> { new UniversalParams(CurrentUnit.WaitingModuleType) };
                                message = Message.StoreAction;
                                parameters = new[] { (object)StoreActionType.Boost, StoreItemType.Module, CurrentUnit.ID, _parameters };
                            }

                            CurrentUnitID = -1;
                            break;
                    }
                    break;

                case Message.UnitHangarSelected:
                    int id = parameters.Get<int>();

                    if (currentUnit != null && id == currentUnit.ID)
                    {
                        return;
                    }

                    if (currentUnit != null)
                    {
                        RemoveSubscriber(currentUnit);
                        currentUnit.RemoveSubscriber(this);
                    }

                    currentUnit = StaticContainer.MainData.GetUnitHangar(id);
                    AddSubscriber(currentUnit);
                    currentUnit.AddSubscriber(this);
                    break;

                case Message.TaskUpdate:
                    if (parameters.Get<bool>())
                    {
                        return;
                    }

                    profile = StaticContainer.Get<IProfile>(StaticType.Profile);
                    price = StaticContainer.Get<ITaskManager>(StaticType.TaskManager).TaskUpdatePrice.Clone();

                    if (!profile.Balance.Enough(price))
                    {
                        price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                        NotEnoughMoney(price);
                        Debug.LogError("Task updating failed: not enough money!");
                        return;
                    }

                    //Debug.LogError("3 TaskUpdateButton: " + parameters.Get<int>());
                    intParameters = new Dictionary<string, int>();
                    intParameters.Add("TaskUpdate", parameters.Get<int>());
                    Event(Message.DataRequest, DataType.Server, DataKey.TaskUpdate, intParameters);
                    break;
            }

            SubStoresReaction(message, parameters);
        }
        
        #region ISender
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

        #region IStatic
        public bool IsEmpty
        {
            get
            {
                return false;
            }
        }

        public StaticType StaticType
        {
            get
            {
                return StaticType.Store;
            }
        }

        public void SaveInstance()
        {
            StaticContainer.Set(StaticType, this);
        }

        public void DeleteInstance()
        {
            StaticContainer.Set(StaticType, null);
        }
        #endregion
    }
}