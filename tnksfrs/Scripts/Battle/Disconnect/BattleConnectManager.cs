using System;
using System.CodeDom;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Http;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using XD;

namespace Disconnect
{
    public class BattleConnectManager : MonoBehaviour, IConnector
    {
        private enum CatcherState
        {
            Sleep,
            InBattle,
            InRoom,
            Connected,
            Reconnecting,
            ReconnectNeeded,
            CannotConnect,
            WaitingMasterResponse,
            ForcedDisconnect
        }

        public const float RECONNECT_TIMEOUT = 45f;
        private const float RECONNECT_DELAY = 0.5f;
        private const float RECONNECT_REPEAT_INTERVAL = 2f;
        private const float MAX_MASTER_RESPONSE_WAITING = 3f;

        public static BattleConnectManager Instance
        {
            get; private set;
        }

        public string LastRoomName
        {
            get;
            set;
        }

        public Vector3 MyLastPosition
        {
            get;
            set;
        }

        public Quaternion MyLastRotation
        {
            get;
            set;
        }

        public TankData MyLastTankData
        {
            get; private set;
        }

        public PlayerStat MyLastPlayerStat
        {
            get; set;
        }

        public bool RespawnAfterReconnect
        {
            get;
            set;
        }
        
        private bool IsEnabled
        {
            get
            {
                return GameData.isReconnectEnabled && !PhotonNetwork.offlineMode;
            }
        }
        private CatcherState state = CatcherState.Sleep;
        private int playersCount = 0;
        private HashSet<int> kicked = new HashSet<int>(); // Игроки, выкинутые из текущей комнаты за некорректный вход (в полную комнату без соотв. резерва)
        private HashSet<int> normalDisconnect = new HashSet<int>(); // Игроки отсоединившиеся штатным способом
        private Dictionary<string, object> storeForReconnect = new Dictionary<string, object>();
        private Dictionary<int, PlayerDisconnectInfo> disconnectDictionary = new Dictionary<int, PlayerDisconnectInfo>();

        private void Awake()
        {
            ColoredDebug.Log(name + " [Awake]", this, "yellow");
            SaveInstance();
            RespawnAfterReconnect = false;
            Instance = this;
            FirstConnect = true;
            LastRoomName = null;
            PhotonNetwork.SendMonoMessageTargets = new HashSet<GameObject>();
            AddPhotonMessageTarget(gameObject);

            Subscribes();
            //StaticType.UI.AddSubscriber(this);
            AddSubscriber(StaticType.UI.Instance());
        }

        private void OnApplicationQuit()
        {
            state = CatcherState.ForcedDisconnect;
        }

        private void OnDestroy()
        {
            DeleteInstance();
            Instance = null;
            Unsubscribes();
            //StaticType.UI.RemoveSubscriber(this);
            RemoveSubscriber(StaticType.UI.Instance());
        }

        //TODO:bcm Добавить реакцию на ситуацию, когда комната закрылась, пока пытался переподключиться
        #region PHOTON SECTION
        private void OnConnectedToPhoton()
        {
            state = CatcherState.Connected;
        }

        void OnMasterClientSwitched(PhotonPlayer newMaster)
        {
            Debug.LogError("OnMasterClientSwitched");
            if (newMaster.Equals(PhotonNetwork.player))
            {
                Dispatcher.Send(EventId.NowImMaster, new EventInfo_B(false));
            }
        }

        private void OnReceivedRoomListUpdate()
        {
            ColoredDebug.Log("[" + name + " OnReceivedRoomListUpdate!]", this, "yellow");
            Dispatcher.Send(EventId.PhotonRoomListReceived, new EventInfo_SimpleEvent());
        }

        public void OnJoinedRoom()
        {
            ColoredDebug.Log("[Joined: " + PhotonNetwork.room.Name + "]", this, "magenta");
            Debug.LogFormat("OnJoinedRoom({0})", PhotonNetwork.room.Name);
            state = CatcherState.InRoom;
            Dispatcher.Send(EventId.PhotonJoinedRoom, new EventInfo_SimpleEvent());
        }

        private void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            if (player.IsLocal)
            {
                return;
            }

            playersCount = PhotonNetwork.room.PlayerCount;
            Dispatcher.Send(EventId.NewPlayerConnected, new EventInfo_I(player.ID));
        }

        private void OnPhotonPlayerDisconnected(PhotonPlayer other)
        {
            if (other.IsLocal)
                return;

            playersCount = PhotonNetwork.room.PlayerCount;
            if (IsEnabled)
            {
                VehicleController vehicle;
                if (!XD.StaticContainer.BattleController.Units.TryGetValue(other.ID, out vehicle))
                    return;

                CatchOthersDisconnect(vehicle.data);
            }
            Dispatcher.Send(EventId.PlayerDisconnected, new EventInfo_I(other.ID));
        }

        private void OnDisconnectedFromPhoton() /*Catch own disconnect*/
        {
            Debug.LogError("OnDisconnectedFromPhoton: " + state + ", players: " + playersCount + ", IsEnabled: " + IsEnabled);
            if (!IsEnabled || state == CatcherState.ForcedDisconnect || playersCount < 2)
            {
                if (!BattleController.Instance.NormalDisconnect)
                {
                    StopAllCoroutines();
                    CancelInvoke();
                    Dispatcher.Send(EventId.PhotonDisconnectWithCause, new EventInfo_S("Disconnect due to communication problems"));
                    Dispatcher.Send(EventId.TroubleDisconnect, new EventInfo_SimpleEvent());
                }
                return;
            }

            if (state == CatcherState.InBattle && XD.StaticContainer.BattleController.CurrentUnit != null)
            {
                MyLastPosition = XD.StaticContainer.BattleController.CurrentUnit.transform.position;
                MyLastRotation = XD.StaticContainer.BattleController.CurrentUnit.transform.rotation;
                MyLastTankData = XD.StaticContainer.BattleController.CurrentUnit.data;
                MyLastPlayerStat = XD.StaticContainer.BattleController.CurrentUnit.Statistics;

                BeforeReconnect();
                Dispatcher.Send(EventId.BeforeReconnecting, new EventInfo_SimpleEvent());
                StartCoroutine(Reconnect());
            }
        }

        private void OnCreatedRoom()
        {
            //Debug.LogError("OnCreatedRoom");
            Dispatcher.Send(EventId.NowImMaster, new EventInfo_B(true));
            // В случае включенных ботов - обещание, что комната будет заполнена как только сервер пришлёт имена для ботов
        }

        private void OnLeftRoom()
        {
            if (state == CatcherState.ForcedDisconnect)
            {
                Dispatcher.Send(EventId.LeftRoom, new EventInfo_SimpleEvent());
            }
        }

        private void OnFailedToConnectToPhoton(DisconnectCause cause)
        {
            if (state == CatcherState.Reconnecting)
            {
                state = CatcherState.ReconnectNeeded;
                return;
            }

            Debug.LogError("Cannot connect to Photon cloud. Returning to hangar.");
            ToHangar(cause.ToString());
        }

        private void OnConnectionFail(DisconnectCause cause)
        {
            BattleStatisticsManager.BattleStats["PhotonDisconnect"] = 1;

            var query = new Dictionary<string, string>
            {
                { "tankId", ProfileInfo.currentVehicle.ToString() },
                { "DisconnectCause", cause.ToString()}
            };

            Http.Manager.ReportStats(
                location: "battle",
                action: "ConnectionFail",
                query: query);
        }

        private void OnPhotonJoinRoomFailed()
        {
            if (FirstConnect)
            {
                Dispatcher.Send(EventId.JoinRoomFailed, new EventInfo_SimpleEvent());
            }
            else
            {
                //MessageBox.Show(MessageBox.Type.Hard, Localizer.GetText("NoRoomForReconnect"));
                Event(Message.MessageBox, MessageBoxType.Notification, "UI_MB_ApplicationErrorTitle", "UI_MB_NoRoomForReconnect", "UI_Quit");
            }
            ToHangar("RoomWasDestroyedWhileReconnecting");
        }

        #endregion

        public int LastMasterId
        {
            get; set;
        }

        public bool Reconnecting
        {
            get
            {
                return state == CatcherState.Reconnecting || state == CatcherState.ReconnectNeeded;
            }
        }

        public bool InBattle
        {
            get
            {
                return state == CatcherState.InBattle;
            }

            set
            {
            }
        }


        private bool firstConnect = false;
        public bool FirstConnect
        {
            get
            {
                return firstConnect;
            }

            private set
            {
                Debug.LogErrorFormat("Set first connect as '{0}'", value);
                firstConnect = value;
            }
        }

        public void AddPhotonMessageTarget(GameObject target)
        {
            if (PhotonNetwork.SendMonoMessageTargets == null)
            {
                PhotonNetwork.SendMonoMessageTargets = new HashSet<GameObject>();
            }

            PhotonNetwork.SendMonoMessageTargets.Add(target);
        }

        public void ClearPhotonMessageTargets()
        {
            if (PhotonNetwork.SendMonoMessageTargets != null)
            {
                PhotonNetwork.SendMonoMessageTargets.Clear();
            }
        }

        public void RemovePhotonMessageTarget(GameObject target)
        {
            if (PhotonNetwork.SendMonoMessageTargets != null)
            {
                PhotonNetwork.SendMonoMessageTargets.Remove(target);
            }
        }

        public void ForcedDisconnect(Action afterDisconnectCallback = null)
        {
            StartCoroutine(ForcedDisconnecting(afterDisconnectCallback));
        }

        public bool GetValue(string valueName, out object value)
        {
            return storeForReconnect.TryGetValue(valueName, out value);
        }

        public void SetValue(string valueName, object value)
        {
            storeForReconnect[valueName] = value;
        }

        public PlayerDisconnectInfo GetDisconnectInfoForPlayer(int innerId)
        {
            PlayerDisconnectInfo info;
            return disconnectDictionary.TryGetValue(innerId, out info) ? info : null;
        }
        
        private void Subscribes()
        {
            Dispatcher.Subscribe(EventId.MainTankAppeared, OnMainTankAppeared);
            Dispatcher.Subscribe(EventId.NowImMaster, OnImMaster);
            Dispatcher.Subscribe(EventId.TankJoinedBattle, OnTankConnected);
            Dispatcher.Subscribe(EventId.NormalDisconnectNotice, OnDisconnectRequest);
            Dispatcher.Subscribe(EventId.MasterDisconnectAnswer, OnMasterDisconnectResponse);
        }

        private void Unsubscribes()
        {
            Dispatcher.Unsubscribe(EventId.MainTankAppeared, OnMainTankAppeared);
            Dispatcher.Unsubscribe(EventId.NowImMaster, OnImMaster);
            Dispatcher.Unsubscribe(EventId.TankJoinedBattle, OnTankConnected);
            Dispatcher.Unsubscribe(EventId.NormalDisconnectNotice, OnDisconnectRequest);
            Dispatcher.Unsubscribe(EventId.MasterDisconnectAnswer, OnMasterDisconnectResponse);
        }

        private IEnumerator ForcedDisconnecting(Action afterDisconnectCallback)
        {
            if (!PhotonNetwork.isMasterClient || PhotonNetwork.otherPlayers.Length == 0)
            {
                Dispatcher.Send(EventId.NormalDisconnectNotice, new EventInfo_I(StaticType.BattleController.Instance<IBattleController>().MyPlayerId), Dispatcher.EventTargetType.ToMaster);
            }
            else
            {
                Dispatcher.Send(EventId.NormalDisconnectNotice, new EventInfo_I(StaticType.BattleController.Instance<IBattleController>().MyPlayerId), Dispatcher.EventTargetType.ToSpecific, GetNextMasterId());
            }

            state = CatcherState.WaitingMasterResponse;
            float responseWaitingStart = Time.time;
            
            while (state != CatcherState.ForcedDisconnect && Time.time - responseWaitingStart < MAX_MASTER_RESPONSE_WAITING)
            {
                yield return null;
            }

            state = CatcherState.ForcedDisconnect;
            PhotonNetwork.Disconnect();
            
            if (afterDisconnectCallback != null)
            {
                afterDisconnectCallback();
            }
        }

        private void OnMasterDisconnectResponse(EventId id, EventInfo ei)
        {
            state = CatcherState.ForcedDisconnect;
        }

        private void CatchOthersDisconnect(TankData data)
        {
            if (!PhotonNetwork.isMasterClient)
                return;

            if (LastMasterId == data.playerId)
                return;

            if (normalDisconnect.Contains(data.playerId))
            {
                normalDisconnect.Remove(data.playerId);
                return;
            }

            if (kicked.Contains(data.innerId))
            {
                kicked.Remove(data.innerId);
                return;
            }

            /*Бронируем на некоторое время одно место в комнате под ушедшего*/
            Hashtable roomProps = PhotonNetwork.room.CustomProperties;
            PlayerDisconnectInfo[] busy = roomProps.ContainsKey("bs")
                ? (PlayerDisconnectInfo[])roomProps["bs"]
                : null;
            PlayerDisconnectInfo[] newBusy;
            if (busy != null)
            {
                newBusy = new PlayerDisconnectInfo[busy.Length + 1];
                Array.Copy(busy, newBusy, busy.Length);
            }
            else
                newBusy = new PlayerDisconnectInfo[1];

            newBusy[newBusy.Length - 1] = new PlayerDisconnectInfo(data.innerId, (float)PhotonNetwork.time + GameData.reconnectTimeout, data.teamId);
            roomProps["bs"] = newBusy;
            roomProps["rp"] = newBusy.Length;
            FillDisconnectDictionary(new List<PlayerDisconnectInfo>(newBusy));

            PhotonNetwork.room.SetCustomProperties(roomProps);
        }

        private void OnTankConnected(EventId id, EventInfo ei)
        {
            if (!PhotonNetwork.isMasterClient || !IsEnabled)
            {
                return;
            }

            Hashtable properties = PhotonNetwork.room.CustomProperties;
            EventInfo_I info = ei as EventInfo_I;
            PlayerDisconnectInfo[] reservedInfos = properties["bs"] as PlayerDisconnectInfo[];
            
            if (reservedInfos == null || reservedInfos.Length == 0)
            {
                return;
            }

            TankData data = StaticContainer.BattleController.Units[info.int1].data;

            // Поиск брони на вошедшего игрока
            List<PlayerDisconnectInfo> reservedList = new List<PlayerDisconnectInfo>(reservedInfos);
            if (reservedList.RemoveAll(x => x.InnerId == data.innerId) > 0)
            {
                properties = new Hashtable { { "bs", reservedList.ToArray() }, { "rp", reservedList.Count } };
                PhotonNetwork.room.SetCustomProperties(properties);
                FillDisconnectDictionary(reservedList);
                return;
            }

            if (PhotonNetwork.room.PlayerCount + reservedList.Count <= PhotonNetwork.room.MaxPlayers)
            {
                return;
            }

            #region Выкидываем лишнего
            kicked.Add(data.innerId);
            PhotonNetwork.CloseConnection(PhotonPlayer.Find(data.playerId));
            var query = new Dictionary<string, string>
            {
                { "tankId", data.innerId.ToString() },
                { "DisconnectCause", "Excess in room"}
            };

            Manager.ReportStats("battle", "KickedByMaster", query);
            #endregion
        }

        private void OnMainTankAppeared(EventId id, EventInfo ei) /* CATCH MY CONNECT*/
        {
            state = CatcherState.InBattle;
            LastRoomName = PhotonNetwork.room.Name;
            playersCount = PhotonNetwork.room.PlayerCount;
        }

        private void OnImMaster(EventId id, EventInfo ei)
        {
            StartCoroutine(CheckBusyList());
        }

        private IEnumerator Reconnect()
        {
            Event(Message.MessageBox, MessageBoxType.Notification, "UI_MB_ApplicationErrorTitle", "UI_MB_PhotonReconnect", "UI_Wait");

            //MessageBox.Show(new MessageBox.Data(MessageBox.Type.Hard, Localizer.GetText("PhotonReconnect"), delegate () { }));
            //XdevsSplashScreen.SetActiveWaitingIndicator(true);

            FirstConnect = false;
            state = CatcherState.ReconnectNeeded;
            yield return new WaitForSeconds(RECONNECT_DELAY);
            float startsAt = Time.time;

            #region Основной цикл переподключения
            while (true)
            {
                if (Time.time - startsAt > GameData.reconnectTimeout)
                {
                    state = CatcherState.CannotConnect;
                    ToHangar("UI_MB_PhotonReconnectFailure");
                    yield break;
                }

                switch (state)
                {
                    case CatcherState.Reconnecting:
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            state = CatcherState.CannotConnect;
                            ToHangar("UI_MB_PhotonReconnectCanceled");
                            yield break;
                        }
                        yield return null;
                        break;

                    case CatcherState.ReconnectNeeded:
                        yield return new WaitForSeconds(RECONNECT_REPEAT_INTERVAL);
                        PhotonNetwork.ConnectUsingSettings(StaticContainer.GameManager.PhotonRoomVersion);
                        state = CatcherState.Reconnecting;
                        break;

                    case CatcherState.Connected:
                        yield return null;
                        break;

                    case CatcherState.InRoom:
                    case CatcherState.InBattle:
                        yield break;
                }
            }
            #endregion
        }

        private IEnumerator CheckBusyList()
        {
            yield return new WaitForSeconds(RECONNECT_DELAY);
            WaitForSeconds wait = new WaitForSeconds(RECONNECT_REPEAT_INTERVAL);
            while (PhotonNetwork.room != null)
            {
                Hashtable props = PhotonNetwork.room.CustomProperties;
                if (!props.ContainsKey("bs"))
                {
                    yield return wait;
                    continue;
                }

                PlayerDisconnectInfo[] busy = props["bs"] as PlayerDisconnectInfo[];
                if (busy == null)
                {
                    yield return wait;
                    continue;
                }

                List<PlayerDisconnectInfo> discInfos = new List<PlayerDisconnectInfo>(busy);
                float photonTime = (float)PhotonNetwork.time;
                int removedCount = discInfos.RemoveAll(x => x == null || x.ReconnectTill < photonTime);
                if (removedCount > 0)
                {
                    FillDisconnectDictionary(discInfos);
                    props["bs"] = discInfos.Count != 0 ? discInfos.ToArray() : null;
                    props["rp"] = discInfos.Count;
                    PhotonNetwork.room.SetCustomProperties(props);
                    Dispatcher.Send(EventId.RoomBusyListTrimmed, new EventInfo_SimpleEvent());
                }
                yield return wait;
            }
        }

        private void ToHangar(string disconnectCause)
        {
            Event(Message.MessageBox, MessageBoxType.Disconnect, "UI_MB_ApplicationErrorTitle", disconnectCause, "UI_OK");
            BattleStatisticsManager.BattleStats["ConnectionFailed"] = 1;
            BattleStatisticsManager.BattleStats["PhotonDisconnect"] = 1;
            Loading.GoToLoadingScene();
            Manager.Instance().battleServer.EndBattle(disconnectCause);
            Dispatcher.Send(EventId.PhotonDisconnectWithCause, new EventInfo_S(disconnectCause));
        }

        private void BeforeReconnect()
        {
            if (StaticContainer.BattleController.CurrentUnit.IsAvailable)
            {
                RespawnAfterReconnect = false;
            }
            else
            {
                RespawnAfterReconnect = true;
                MyLastTankData.armor = MyLastTankData.maxArmor;
            }

            CancelInvoke();
            StopAllCoroutines();
        }

        private void OnDisconnectRequest(EventId id, EventInfo ei)
        {
            EventInfo_I info = ei as EventInfo_I;
            int playerId = info.int1;
            if (!StaticContainer.BattleController.Units.ContainsKey(playerId))
                return;

            normalDisconnect.Add(playerId);
            Dispatcher.Send(EventId.MasterDisconnectAnswer, new EventInfo_SimpleEvent(), Dispatcher.EventTargetType.ToSpecific, playerId);
        }

        private int GetNextMasterId()
        {
            if (PhotonNetwork.otherPlayers.Length == 0)
            {
                return -1;
            }

            int min = PhotonNetwork.otherPlayers[0].ID;
            
            for (int i = 1; i < PhotonNetwork.otherPlayers.Length; i++)
            {
                PhotonPlayer player = PhotonNetwork.otherPlayers[i];
                if (player.ID < min)
                {
                    min = player.ID;
                }
            }
            
            return min;
        }

        private void FillDisconnectDictionary(List<PlayerDisconnectInfo> busy)
        {
            disconnectDictionary.Clear();
            for(int i = 0; i < busy.Count; i++)
            {
                PlayerDisconnectInfo info = busy[i];
                disconnectDictionary.Add(info.InnerId, info);
            }
        }

        #region IConnector
        public bool IsEmpty
        {
            get
            {
                return false;
            }
        }

        public int Team
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public string Login
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string Description
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public string GetRandomPrefab()
        {
            throw new NotImplementedException();
        }

        public StaticType StaticType
        {
            get
            {
                return StaticType.Connector;
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

        public void Reaction(Message message, params object[] parameters)
        {
            switch (message)
            {
                case Message.MessageBoxResult:
                    if (parameters.Get<MessageBoxType>() == MessageBoxType.Disconnect)
                    {
                        Event(Message.ToHangar);
                    }
                    break;
                //case Message.UnitBattleDestroyed:
                //    RemovePhotonMessageTarget(parameters.Get<IUnitBehaviour>().GameObject);
                //    break;
            }
        }        
        #endregion
    }
}