using System;
using System.CodeDom;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using Http;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace Disconnect
{
    public class BattleConnectManager : MonoBehaviour
    {
        public const string NORMAL_DISCONNECT_CAUSE = "NormalDisconnectForUserRequest";
        public const string FAILURE_DISCONNECT_CAUSE = "FailureDisconnect";

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

        public static BattleConnectManager Instance { get; private set; }
        public string LastRoomName { get; private set; }
        public Vector3 MyLastPosition { get; private set; }
        public Quaternion MyLastRotation { get; private set; }
        public TankData MyLastTankData { get; private set; }
        public PlayerStat MyLastPlayerStat { get; private set; }
        public bool RespawnAfterReconnect { get; private set; }
        
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

        void Awake()
        {
            RespawnAfterReconnect = false;
            Instance = this;
            FirstConnect = true;
            LastRoomName = null;
            MyLastPlayerStat = null;
            MyLastTankData = null;
            PhotonNetwork.SendMonoMessageTargets = new HashSet<GameObject>();
            AddPhotonMessageTarget(gameObject);

            Subscribes();
        }

        void OnApplicationQuit()
        {
            state = CatcherState.ForcedDisconnect;
        }

        void OnDestroy()
        {
            Instance = null;
            Unsubscribes();
        }

        //TODO:bcm Добавить реакцию на ситуацию, когда комната закрылась, пока пытался переподключиться
        #region PHOTON SECTION
        void OnConnectedToPhoton()
        {
            state = CatcherState.Connected;
        }

        void OnMasterClientSwitched(PhotonPlayer newMaster)
        {
            if (newMaster.Equals(PhotonNetwork.player))
                Dispatcher.Send(EventId.NowImMaster, new EventInfo_B(false));
        }

        void OnReceivedRoomListUpdate()
        {
            Dispatcher.Send(EventId.PhotonRoomListReceived, new EventInfo_SimpleEvent());
        }

        public void OnJoinedRoom()
        {
            state = CatcherState.InRoom;
            Dispatcher.Send(EventId.PhotonJoinedRoom, new EventInfo_SimpleEvent());
        }

        void OnPhotonPlayerConnected(PhotonPlayer player)
        {
            if (player.isLocal)
                return;

            playersCount = PhotonNetwork.room.playerCount;
            Dispatcher.Send(EventId.NewPlayerConnected, new EventInfo_I(player.ID));
        }

        void OnPhotonPlayerDisconnected(PhotonPlayer other)
        {
            if (other.isLocal)
                return;

            playersCount = PhotonNetwork.room.playerCount;
            if (IsEnabled)
            {
                VehicleController vehicle;
                if (!BattleController.allVehicles.TryGetValue(other.ID, out vehicle))
                    return;

                CatchOthersDisconnect(vehicle.data);
            }
            Dispatcher.Send(EventId.PlayerDisconnected, new EventInfo_I(other.ID));
        }

        void OnDisconnectedFromPhoton() /*Catch own disconnect*/
        {
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

            if (state == CatcherState.InBattle && BattleController.MyVehicle != null)
            {
                MyLastPosition = BattleController.MyVehicle.transform.position;
                MyLastRotation = BattleController.MyVehicle.transform.rotation;
                MyLastTankData = BattleController.MyVehicle.data;
                MyLastPlayerStat = BattleController.MyVehicle.Statistics;

                BeforeReconnect();
                Dispatcher.Send(EventId.BeforeReconnecting, new EventInfo_SimpleEvent());
                StartCoroutine(Reconnect());
            }
        }

        void OnCreatedRoom()
        {
            Dispatcher.Send(EventId.NowImMaster, new EventInfo_B(true));
            // В случае включенных ботов - обещание, что комната будет заполнена как только сервер пришлёт имена для ботов
        }

        void OnLeftRoom()
        {
            if (state == CatcherState.ForcedDisconnect)
                Dispatcher.Send(EventId.LeftRoom, new EventInfo_SimpleEvent());
        }

        void OnFailedToConnectToPhoton(DisconnectCause cause)
        {
            if (state == CatcherState.Reconnecting)
            {
                state = CatcherState.ReconnectNeeded;
                return;
            }

            Debug.LogError("Cannot connect to Photon cloud. Returning to hangar.");
            ToHangar(cause.ToString());
        }

        void OnConnectionFail(DisconnectCause cause)
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

        void OnPhotonJoinRoomFailed()
        {
            if (FirstConnect)
                Dispatcher.Send(EventId.JoinRoomFailed, new EventInfo_SimpleEvent());
            else
                MessageBox.Show(MessageBox.Type.Hard, Localizer.GetText("NoRoomForReconnect"));
            ToHangar("RoomWasDestroyedWhileReconnecting");
        }

        #endregion

        public int LastMasterId { get; set; }

        public bool Reconnecting
        {
            get { return state == CatcherState.Reconnecting || state == CatcherState.ReconnectNeeded; }
        }

        public bool InBattle
        {
            get { return state == CatcherState.InBattle; }
        }

        public bool FirstConnect { get; private set; }

        public static void AddPhotonMessageTarget(GameObject target)
        {
            if (PhotonNetwork.SendMonoMessageTargets == null)
                PhotonNetwork.SendMonoMessageTargets = new HashSet<GameObject>();
            PhotonNetwork.SendMonoMessageTargets.Add(target);
        }

        public static void ClearPhotonMessageTargets()
        {
            if (PhotonNetwork.SendMonoMessageTargets != null)
                PhotonNetwork.SendMonoMessageTargets.Clear();
        }

        public static void RemovePhotonMessageTarget(GameObject target)
        {
            if (PhotonNetwork.SendMonoMessageTargets != null)
                PhotonNetwork.SendMonoMessageTargets.Remove(target);
        }

        public void ForcedDisconnect(Action afterDisconnectCallback = null)
        {
            StartCoroutine(ForcedDisconnecting(afterDisconnectCallback));
        }

        public bool GetStoredValue(string valueName, out object value)
        {
            return storeForReconnect.TryGetValue(valueName, out value);
        }

        public void StoreValue(string valueName, object value)
        {
            storeForReconnect[valueName] = value;
        }

        public PlayerDisconnectInfo GetDisconnectInfoForPlayer(int profileId)
        {
            PlayerDisconnectInfo info;
            return disconnectDictionary.TryGetValue(profileId, out info) ? info : null;
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
                Dispatcher.Send(EventId.NormalDisconnectNotice, new EventInfo_I(BattleController.MyPlayerId),
                    Dispatcher.EventTargetType.ToMaster);
            else
                Dispatcher.Send(EventId.NormalDisconnectNotice, new EventInfo_I(BattleController.MyPlayerId),
                    Dispatcher.EventTargetType.ToSpecific, GetNextMasterId());
            state = CatcherState.WaitingMasterResponse;
            float responseWaitingStart = Time.time;
            while (state != CatcherState.ForcedDisconnect &&
                    Time.time - responseWaitingStart < MAX_MASTER_RESPONSE_WAITING)
                yield return null;

            state = CatcherState.ForcedDisconnect;
            PhotonNetwork.Disconnect();
            if (afterDisconnectCallback != null)
                afterDisconnectCallback();
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

            if (kicked.Contains(data.profileId))
            {
                kicked.Remove(data.profileId);
                return;
            }

            /*Бронируем на некоторое время одно место в комнате под ушедшего*/
            Hashtable roomProps = PhotonNetwork.room.customProperties;
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

            newBusy[newBusy.Length - 1] = new PlayerDisconnectInfo(data.profileId, (float)PhotonNetwork.time + GameData.reconnectTimeout, data.teamId);
            roomProps["bs"] = newBusy;
            roomProps["rp"] = newBusy.Length;
            FillDisconnectDictionary(new List<PlayerDisconnectInfo>(newBusy));

            PhotonNetwork.room.SetCustomProperties(roomProps);
        }

        private void OnTankConnected(EventId id, EventInfo ei)
        {
            if (!PhotonNetwork.isMasterClient || !IsEnabled)
                return;

            Hashtable properties = PhotonNetwork.room.customProperties;
            EventInfo_I info = ei as EventInfo_I;
            PlayerDisconnectInfo[] reservedInfos = properties["bs"] as PlayerDisconnectInfo[];
            if (reservedInfos == null || reservedInfos.Length == 0)
                return;

            TankData data = BattleController.allVehicles[info.int1].data;

            // Поиск брони на вошедшего игрока
            List<PlayerDisconnectInfo> reservedList = new List<PlayerDisconnectInfo>(reservedInfos);
            if (reservedList.RemoveAll(x => x.InnerId == data.profileId) > 0)
            {
                properties = new Hashtable { { "bs", reservedList.ToArray() }, { "rp", reservedList.Count } };
                PhotonNetwork.room.SetCustomProperties(properties);
                FillDisconnectDictionary(reservedList);
                return;
            }

            if (PhotonNetwork.room.playerCount + reservedList.Count <= PhotonNetwork.room.maxPlayers)
                return;

            #region Выкидываем лишнего
            kicked.Add(data.profileId);
            PhotonNetwork.CloseConnection(PhotonPlayer.Find(data.playerId));
            var query = new Dictionary<string, string>
            {
                { "tankId", data.profileId.ToString() },
                { "DisconnectCause", "Excess in room"}
            };

            Manager.ReportStats("battle", "KickedByMaster", query);
            #endregion
        }

        private void OnMainTankAppeared(EventId id, EventInfo ei) /* CATCH MY CONNECT*/
        {
            state = CatcherState.InBattle;
            LastRoomName = PhotonNetwork.room.name;
            playersCount = PhotonNetwork.room.playerCount;
        }

        private void OnImMaster(EventId id, EventInfo ei)
        {
            StartCoroutine(CheckBusyList());
        }

        private IEnumerator Reconnect()
        {
            MessageBox.Show(new MessageBox.Data(MessageBox.Type.Hard, Localizer.GetText("PhotonReconnect"), delegate () { }));
            XdevsSplashScreen.SetActiveWaitingIndicator(true);
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
                    ToHangar("Reconnection failed");
                    yield break;
                }
                switch (state)
                {
                    case CatcherState.ReconnectNeeded:
                        yield return new WaitForSeconds(RECONNECT_REPEAT_INTERVAL);
                        PhotonNetwork.ConnectUsingSettings(GameManager.PhotonRoomVersion);
                        state = CatcherState.Reconnecting;
                        break;
                    case CatcherState.Reconnecting:
                        if (Input.GetKeyDown(KeyCode.Escape))
                        {
                            state = CatcherState.CannotConnect;
                            ToHangar("Reconnection cancelled");
                            yield break;
                        }
                        yield return null;
                        break;
                    case CatcherState.Connected:
                        yield return null;
                        break;
                    case CatcherState.InRoom:
                    case CatcherState.InBattle:
                        MessageBox.HideHardMessage();
                        XdevsSplashScreen.SetActiveWaitingIndicator(false);
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
                Hashtable props = PhotonNetwork.room.customProperties;
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
            MessageBox.Show(MessageBox.Type.Hard, Localizer.GetText("NTPConnectionError"));

            BattleStatisticsManager.BattleStats["ConnectionFailed"] = 1;
            BattleStatisticsManager.BattleStats["PhotonDisconnect"] = 1;
            GameManager.ReturnToHangar();
            Manager.BattleServer.EndBattle(disconnectCause);
            Dispatcher.Send(EventId.PhotonDisconnectWithCause, new EventInfo_S(disconnectCause));
        }

        private void BeforeReconnect()
        {
            if (BattleController.MyVehicle.IsAvailable)
                RespawnAfterReconnect = false;
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
            if (!BattleController.allVehicles.ContainsKey(playerId))
                return;

            normalDisconnect.Add(playerId);
            Dispatcher.Send(EventId.MasterDisconnectAnswer, new EventInfo_SimpleEvent(), Dispatcher.EventTargetType.ToSpecific, playerId);
        }

        private static int GetNextMasterId()
        {
            if (PhotonNetwork.otherPlayers.Length == 0)
                return -1;

            int min = PhotonNetwork.otherPlayers[0].ID;
            for (int i = 1; i < PhotonNetwork.otherPlayers.Length; i++)
            {
                PhotonPlayer player = PhotonNetwork.otherPlayers[i];
                if (player.ID < min)
                    min = player.ID;
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
    }
}