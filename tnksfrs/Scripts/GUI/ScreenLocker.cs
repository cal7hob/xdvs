using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XD
{
    public class ScreenLocker : MonoBehaviour, IStatic
    {
        [SerializeField]
        private List<DataKey>   notLockScreen = null;

        private bool            isLocked = false;
        private IUnitHangar     currentUnit = null;        

        public bool IsLocked
        {
            get
            {
                return isLocked;
            }
        }

        private void Awake()
        {
            SaveInstance();
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            AddSubscriber(StaticContainer.Get(StaticType.UI));

            StaticType.UI.AddSubscriber(this);
            StaticType.DataHandler.AddSubscriber(this);
            StaticType.MainData.AddSubscriber(this);
            StaticType.Profile.AddSubscriber(this);
            StaticType.Store.AddSubscriber(this);
            StaticType.Statistics.AddSubscriber(this);
        }

        private void Lock(bool val)
        {
            if (StaticType.SceneManager.Instance<ISceneManager>().InBattle)
            {
                return;
            }

            if (val == isLocked)
            {
                Debug.LogError("ScreenLocker: allready " + (val ? "unlocked!" : "locked!"));
                return;
            }

            isLocked = val;
            Event(Message.LockUI, val);
        }

        #region ISender

        public void Reaction(Message message, params object[] parameters)
        {
            switch (message)
            {
                case Message.DataRequest:
                    if (parameters.Get<DataType>() != DataType.Server)
                    {
                        return;
                    }

                    if (notLockScreen.Contains(parameters.Get<DataKey>()))
                    {
                        return;
                    }

                    Lock(true);
                    break;

                case Message.DataResponse:
                    if (parameters.Get<DataType>() != DataType.Server)
                    {
                        return;
                    }

                    if (notLockScreen.Contains(parameters.Get<DataKey>()))
                    {
                        return;
                    }

                    Lock(false);
                    break;

                case Message.UnitHangarSelected:
                    int id = parameters.Get<int>();

                    if (id < 0)
                    {
                        id = StaticContainer.Get<IModelsDispatcher>(StaticType.ModelsDispatcher).SelectedUnit;
                    }

                    if (currentUnit != null)
                    {
                        if (currentUnit.ID == id)
                        {
                            return;
                        }

                        currentUnit.RemoveSubscriber(this);
                    }

                    currentUnit = (VehicleHangar)StaticContainer.MainData.GetUnitHangar(id);

                    if (currentUnit != null)
                    {
                        currentUnit.AddSubscriber(this);
                    }
                    break;
            }
        }

        public string Description
        {
            get
            {
                return "[ScreenLocker] " + name;
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
            get { return false; }
        }

        public StaticType StaticType
        {
            get { return StaticType.ScreenLocker; }
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

        private void OnDestroy()
        {
            StaticType.UI.RemoveSubscriber(this);
            StaticType.DataHandler.RemoveSubscriber(this);
            StaticType.MainData.RemoveSubscriber(this);
            StaticType.Profile.RemoveSubscriber(this);
            StaticType.Store.RemoveSubscriber(this);
            StaticType.Statistics.RemoveSubscriber(this);

            if (currentUnit != null)
            {
                currentUnit.RemoveSubscriber(this);
            }
            
            DeleteInstance();
        }
    }
}