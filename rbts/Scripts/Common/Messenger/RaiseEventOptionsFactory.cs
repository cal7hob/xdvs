using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaiseEventOptionsFactory
{
    private RaiseEventOptions options = new RaiseEventOptions();

    public RaiseEventOptions GetREO_ToAll()
    {
        options.Receivers = ReceiverGroup.All;
        options.TargetActors = null;

        return options;
    }

    public RaiseEventOptions GetREO_ToSpecific(int specificId)
    {
        options.Receivers = ReceiverGroup.Others;
        options.TargetActors = new [] {specificId};

        return options;
    }

    public RaiseEventOptions GetREO_ToOthers()
    {
        options.Receivers = ReceiverGroup.Others;
        options.TargetActors = null;

        return options;
    }

    public RaiseEventOptions GetREO_ToMaster()
    {
        options.Receivers = ReceiverGroup.MasterClient;
        options.TargetActors = null;

        return options;
    }
}
