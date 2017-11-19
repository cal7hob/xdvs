using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace XD
{
    //[System.Serializable]
    public class HangarConsumableForSave
    {
        public string iconName;
        public int id = -1;
        public string name = String.Empty;
        public ConsumableSlotType   slotType = ConsumableSlotType.None;
        public int slotID = -1;
        public Clamper amount = new Clamper(-1);
        public Clamper amountBase = new Clamper(-1);
        public bool automaticallyBuy;

        public HangarConsumableForSave()
        {
        }

        public HangarConsumableForSave(IConsumableHangar hangarConsumable)
        {
            //HangarConsumableForSave res = new HangarConsumableForSave();
            id = hangarConsumable.ID;
            name = hangarConsumable.Name;
            slotType = hangarConsumable.SlotType;
            slotID = hangarConsumable.SlotID;
            amount = hangarConsumable.Amount.Clone();
            amount = hangarConsumable.AmountBase.Clone();
            //settings = settings.Clone();
            //automaticallyBuy = hangarConsumable.AutoBuy;
        }
    }
}