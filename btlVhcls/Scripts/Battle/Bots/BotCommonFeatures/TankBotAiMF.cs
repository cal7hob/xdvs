using System.Collections;
using UnityEngine;
using XDevs.LiteralKeys;

public class TankBotAiMF : TankBotAI
{
    public TankBotAiMF (VehicleController vehicle) : base(vehicle) { }


    public override void MyBotUpdate () {
        ReloadWeapons ();
        CheckIfFireNeed ();
        CheckBotLifetime ();
    }

    public override void Move(float speed, float rotSpeed)
    {
        botYAxisControl = speed;
        botXAxisControl = rotSpeed;
    }

}
