using UnityEngine;
using System.Collections.Generic;

namespace BankKits
{
    /// <summary>
    /// Тип стартерпаков (разделение для удобства)
    /// </summary>
    public enum Type
    {
        Newbie,//Наборы для начинающих
    }

    public enum Content
    {
        Decal,
        Pattern
    }

    public class Data
    {
        public bool needToShow = false;
        public double startTime;
        public double endTime;
        public string unibillId;
        public bool isPurchased = false;
        public Type type = Type.Newbie;
        public GoodsKit goodsKit;
        public Dictionary<Content, int> content;
        public int displayedAmount;//Параметр который устанавливают на сервере в 100 чтобы пройти апрув, после апрува делают равным 400 (100 голды сразу + 10/день)
        public int duration;
        public int dailyReward;
        public const int GOLD_KIT_INAPP_CURRENCY_VAL = 100;//захардкоженная сумма голды при покупке инапа (меняться не может, т.к. установлена в сторах)
        //public int DisplayedAmount { get { return displayedAmount + duration * dailyReward; } }

        public Data(string _unibillId, double _startTime, double _endTime, Type _type)
        {
            startTime = _startTime;
            endTime = _endTime;
            unibillId = _unibillId;
            type = _type;
        }

        public bool IsActive
        {
            get
            {
                return needToShow;
                //Если у нас уже есть танк который в наборе xdevs.vehicle_kit - не показываем этот набор
                //if (type == Type.Newbie && unibillId == "xdevs.vehicle_kit" && content != null && content.ContainsKey(Content.Vehicle) &&
                //    ProfileInfo.vehicleUpgrades != null && ProfileInfo.vehicleUpgrades.ContainsKey(content[Content.Vehicle]))
                //    return false;
                //return startTime <= GameData.CorrectedCurrentTimeStamp && endTime > GameData.CorrectedCurrentTimeStamp;
            }
        }

        public string Page { get{ return string.Format("{0}_details", PrefabName);}}

        public string PrefabName
        {
            get
            {
                string[] arr = unibillId.Split(new char[] { '.' });
                return arr[1];
            }
        }

        public string PrefabPath { get{ return string.Format("{0}/GuiPrefabs/Hangar/Bank/BankKits/{1}/{2}", GameManager.CurrentResourcesFolder, type, PrefabName); }}

        public bool HasContent { get { return content != null && content.Count > 0; } }
    }

    public class GoodsKit : BankLotBase
    {
        [SerializeField] private tk2dTextMesh lblHeader;
        [SerializeField] private tk2dTextMesh lblTimeRemains;
        [SerializeField] private tk2dTextMesh lblDisplayedAmount;

        public Data data;

        public void ShowDetails()
        {
            MenuController.NextSound();
            GUIPager.SetActivePage(data.Page, true, true);
        }

        private void Awake()
        {
            Dispatcher.Subscribe(EventId.HangarTimerTick, OnTimerTick);
        }

        private void OnDestroy()
        {
            Dispatcher.Unsubscribe(EventId.HangarTimerTick, OnTimerTick);
        }

        private void Start()
        {
            OnTimerTick(EventId.Manual, null);
        }
        private void OnTimerTick(EventId id, EventInfo info)
        {
            if (data == null || !gameObject.activeSelf)
                return;
            var timeRemains = Clock.GetTimerString((long)(data.endTime - GameData.CorrectedCurrentTimeStamp));
            lblTimeRemains.text = Localizer.GetText("lblTimeRemains") + " " + timeRemains;
        }

        public override void Initialize(params object[] parameters)
        {
            data = parameters[0] as BankKits.Data;

            if (lblDisplayedAmount && data.unibillId == "xdevs.gold_kit")
                lblDisplayedAmount.text = data.displayedAmount.ToString();
            #region test устанавливаем время окончания действия набора через минуту после текущего времени
            //data.endTime = GameData.DateTimeToUnixTimeStamp(GameData.UnixTimeStampToDateTime(GameData.CorrectedCurrentTimeStamp).AddSeconds(20));
            #endregion
        }
    }

}


