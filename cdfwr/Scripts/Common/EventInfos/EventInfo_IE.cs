using System;
using System.IO;
using ExitGames.Client.Photon;
using UnityEngine;
using System.Collections;

public class EventInfo_IE : EventInfo
{
    private const int SIZE = 31;

    public int int1;
    public VehicleEffect effect;

    public EventInfo_IE()
    { }

    public EventInfo_IE(int int1, VehicleEffect effect)
    {
        this.int1 = int1;
        this.effect = effect;
    }

    public override byte[] Serialize()
    {
        byte[] bytes = new byte[SIZE];
        BitConverter.GetBytes(int1).CopyTo(bytes, 0);
        BitConverter.GetBytes(effect.Id).CopyTo(bytes, 4);
        bytes[8] = (byte)effect.Type;
        bytes[9] = (byte)effect.ModType;
        BitConverter.GetBytes(effect.ModValue).CopyTo(bytes, 10);
        BitConverter.GetBytes(effect.Duration).CopyTo(bytes, 14);
        BitConverter.GetBytes(effect.StartTime).CopyTo(bytes, 18);
        bytes[26] = (byte)effect.Source;
        BitConverter.GetBytes(effect.ConsumableId).CopyTo(bytes, 27);

        return bytes;
    }

    public override void Deserialize(byte[] bytes, int index)
    {
        int1 = BitConverter.ToInt32(bytes, index);
        index += 4;
        int id = BitConverter.ToInt32(bytes, index);
        index += 4;
        VehicleEffect.ParameterType type = (VehicleEffect.ParameterType)bytes[index];
        index += 1;
        VehicleEffect.ModifierType modType = (VehicleEffect.ModifierType)bytes[index];
        index += 1;
        float modValue = BitConverter.ToSingle(bytes, index);
        index += 4;
        float duration = BitConverter.ToSingle(bytes, index);
        index += 4;
        double startTime = BitConverter.ToDouble(bytes, index);
        index += 8;
        BonusItem.BonusType bonus = (BonusItem.BonusType)bytes[index];
        index += 1;
        int consumableId = BitConverter.ToInt32(bytes, index);

        effect = new VehicleEffect(id, type, modType, modValue, duration, startTime, bonus, IECell.IEIcon.None, consumableId);
    }
}
