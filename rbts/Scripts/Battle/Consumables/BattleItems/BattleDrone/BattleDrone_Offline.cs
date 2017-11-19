using System.Collections;
using System.Collections.Generic;
using Pool;
using StateMachines;
using UnityEngine;

public class BattleDrone_Offline : BattleDroneState
{
    private Vector3 fallingRotEuler;
    private Vector3 groundPosition;
    private float fallingSpeed = 0f;
    private Transform transform;

    private AudioPlayer disablingPlayer;

    public BattleDrone_Offline(BattleDrone owner) : base(owner) { }
    
    public override string Name { get { return "Offline"; } }

    public override void OnEnter(IState prevState)
    {
        transform = battleDrone.transform;
        fallingRotEuler = Random.onUnitSphere;
        groundPosition = GetGround();
        disablingPlayer = AudioDispatcher.PlayClipAtPosition(battleDrone.DisablingSound, transform);
        battleDrone.SwitchOff();
    }

    public override void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, groundPosition, fallingSpeed * Time.deltaTime);
        transform.Rotate(fallingRotEuler * Time.deltaTime * fallingSpeed * 10f, Space.World);
        fallingSpeed += battleDrone.FallingAcceleration * Time.deltaTime;
        if (transform.position == groundPosition)
        {
            disablingPlayer.Stop();
            battleDrone.Death();
        }
    }

    private Vector3 GetGround()
    {
        RaycastHit hit;
        if (!Physics.Raycast(battleDrone.transform.position, Vector3.down, out hit, 20f, BattleController.HitMask))
        {
            return battleDrone.transform.position + Vector3.down * 20f;
        }

        return hit.point;
    }
}
