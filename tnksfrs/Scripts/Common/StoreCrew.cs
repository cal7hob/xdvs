using UnityEngine;

namespace XD
{
    public class StoreCrew : SubStore
    {
        private IStore commonStore = null;

        public override void Reaction(Message message, params object[] parameters)
        {
            IProfile profile = null;
            CurrencyValue price;

            if (message == Message.CrewTrainButton)
            {
                bool res = false;
                IUnitHangar vehicle = null;

                int vehCurrentID = StaticContainer.Get<IModelsDispatcher>(StaticType.ModelsDispatcher).SelectedUnit;
                vehicle = StaticContainer.MainData.GetUnitHangar(vehCurrentID);
                //Debug.LogError("CrewTrainButton, current price:" + vehicle.CrewInfo.CurrentPrice.amount + " " + vehicle.CrewInfo.CurrentPrice.type + ", lvl: ");
                price = vehicle.CrewInfo.CurrentPrice.Clone();
                profile = StaticContainer.Get<IProfile>(StaticType.Profile);

                if (!profile.Balance.Enough(price))
                {
                    price.SetAmount(price.Amount - profile.Balance[price.Type].Amount);
                    commonStore.NotEnoughMoney(price);
                    //PSYGUI.Event(PSYEvent.MessageBox, PSYParams.New(MessageBoxType.NotEnoughMoney, "UI_No_Money_Message", "UI_Go_To_Bank", price));
                    return;
                }

                commonStore.Event(Message.DataRequest, DataType.Server, vehicle.CrewTraining(false), "unitId", vehCurrentID);
                //Debug.LogError("Запрос прокачки экипажа!");
            }
        }

        public override void Init(IStore store)
        {
            commonStore = store;
        }
    }
}