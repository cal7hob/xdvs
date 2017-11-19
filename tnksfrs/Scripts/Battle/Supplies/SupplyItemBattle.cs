using UnityEngine;
using System.Collections;
using CodeStage.AntiCheat.ObscuredTypes;

public struct SimplePrice
{
    public ProfileInfo.PriceCurrency currency;
    public ObscuredInt value;

    public SimplePrice(ProfileInfo.PriceCurrency currency, int value)
    {
        this.currency = currency;
        this.value = value;
    }
}

public class SupplyItemBattle
{
    public string Name { get; private set; }
    public int Count { get; private set; }
    public float LifeTime { get; private set; }
    public SimplePrice Price { get; private set; }
    public float Duration { get; private set; }

    public SupplyItemBattle(string name, SimplePrice price, float lifeTime, float duration, int count)
    {
        Name = name;
        Price = price;
        LifeTime = lifeTime;
        Duration = duration;
        Count = count;
    }
}