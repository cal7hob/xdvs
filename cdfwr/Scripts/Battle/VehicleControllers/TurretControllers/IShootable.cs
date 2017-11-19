using System.Collections;
using System.Collections.Generic;
using CodeStage.AntiCheat.ObscuredTypes;
using UnityEngine;

public interface IShootable
{    
    bool IsReady
    {
        get;
    }
    /*Vector3 GunsightPoint
    {
        get;
    }
    
    ObscuredFloat ReloadingTimeSeconds
    {
        get;
    }*/

    float GetReloadingTime();

    float ReloadRemainingSeconds(ShellType shell);
    void FillReloadingData(TankData data);

    void TurretRotation();
    bool Fire();

    void StopTurretAudio();
    void SetTurretAudio();

    void FullRealoadingUpdate();
    void ResetAimingState();

    void InstantReload();
    void FullInstantReload();

    void ResetLocalRotation();

    void SetAudioVolume(float volume);

    void SetAnimation(Animation aimation);
    void SetProgressBar(SelfTankProgressBars progressBar);

    void SetFullAutoAiming();

    void Reload(float reloadTime);

    //void MoveBotGunsight();
    float WeaponReloadingProgress
    {
        get;
    }
    Transform CannonEnd
    {
        get;
    }
    bool TurretCentering
    {
        get;
        set;
    }
    float MaxShootAngleCos
    {
        get;
    }

    void OnDestroy();
}
