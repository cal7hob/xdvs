using System;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using XD;

public class Decal : Bodykit, IDecal
{
    public Decal(DecalInEditor sourcePattern) : base(sourcePattern)
    {

    }

    public Decal(IDecal sourcePattern) : base(sourcePattern)
    {

    }

    public ObscuredFloat ArmorGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredInt AvailabilityLevel
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat DamageGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public int ID
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat IrcmRofGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public bool IsHidden
    {
        get
        {
            return false;
        }
    }

    public bool IsVip
    {
        get
        {
            return false;
        }
    }
    
    public List<ProfileInfo.Price> PricesToGroups
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat RocketDamageGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat RofGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public ObscuredFloat SpeedGain
    {
        get
        {
            throw new NotImplementedException();
        }
    }
}
