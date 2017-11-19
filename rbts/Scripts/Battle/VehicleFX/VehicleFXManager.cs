using System.Collections;
using System.Collections.Generic;
using Pool;
using UnityEngine;

public class VehicleFXManager
{
    private VehicleController owner;

    // Эффекты
    private PoolEffect stunFX;
    private ShieldFX shieldFX;

    private AudioPlayer stunPlayer;
    private AudioPlayer blindnessPlayer;


    public VehicleFXManager(VehicleController owner)
    {
        this.owner = owner;
    }

    public void ReactToEffectApply(VehicleEffect effect)
    {
        switch (effect.ParamType)
        {
            case VehicleEffect.ParameterType.Stun:
                ShowStunFX();
                break;
            case VehicleEffect.ParameterType.Blindness:
                ShowBlindness();
                break;
            case VehicleEffect.ParameterType.TakenDamageRatio:
                ShowShield(effect.ModValue);
                break;
        }
    }

    public void ReactToEffectModify(VehicleEffect effect)
    {
        switch (effect.ParamType)
        {
            case VehicleEffect.ParameterType.TakenDamageRatio:
                ModifyShield(effect.ModValue);
                break;
            default:
                return;
        }
    }

    public void ReactToEffectCancel(VehicleEffect effect)
    {
        switch (effect.ParamType)
        {
            case VehicleEffect.ParameterType.Stun:
                HideStunFX();
                break;
            case VehicleEffect.ParameterType.Blindness:
                HideBlindness();
                break;
            case VehicleEffect.ParameterType.TakenDamageRatio:
                HideShield();
                break;
        }
    }


    private void ShowStunFX()
    {
        if (stunFX != null) // Отказ от наложения эффектов друг на друга
            return;

        stunFX = PoolManager.GetObject<PoolEffect>(GameSettings.Instance.StunFX.GetResourcePath(owner.IsMain), Vector3.zero,
            Quaternion.identity);

        VehicleFXShaper shaper = owner.FXShaper;
        shaper.ConfigurateParticleSystem(stunFX.ParticleSystem);
        GameSettings.Instance.StunSound.GetObjectAsync(StunSoundLoaded);
    }
    private void HideStunFX()
    {
        if (stunFX == null)
            return;

        stunPlayer.Stop();
        stunFX.gameObject.layer = LayerMask.NameToLayer("Default");
        stunFX.Disactivate();
        stunFX = null;
    }
    private void StunSoundLoaded(Object stunSoundObj)
    {
        AudioClip stunSound = stunSoundObj as AudioClip;
        stunPlayer = AudioDispatcher.PlayClipAtPosition(stunSound, Settings.SoundVolume * SoundSettings.STUN_LOOP_VOLUME, AudioPlayer.Channel.Important, owner.transform, true);
    }

    private void ShowBlindness()
    {
        if (owner.IsMain)
        {
            blindnessPlayer = AudioDispatcher.PlayClipAtPosition(GameSettings.Instance.BlindingSound.GetObject<AudioClip>(), BattleCamera.Camera.transform, true);
            Messenger.Send(EventId.IsMainCameraSighted, new EventInfo_B(false));
        }
    }
    private void HideBlindness()
    {
        if (owner.IsMain)
        {
            blindnessPlayer.Stop();
            Messenger.Send(EventId.IsMainCameraSighted, new EventInfo_B(true));
        }
    }

    private void ShowShield(float damageRatio)
    {
        shieldFX = owner.gameObject.AddComponent<ShieldFX>();
        shieldFX.SetTakenDamageRatio(damageRatio);
    }
    private void ModifyShield(float damageRatio)
    {
        if (shieldFX != null)
        {
            HideShield();
        }
        ShowShield(damageRatio);
    }
    private void HideShield()
    {
        shieldFX.Disactivate();
        shieldFX = null;
    }
}
