using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class SoldierController
{
    [Header("Sounds")]
    public AudioSource stepAudioSource;
    public AudioSource reloadAudioSource;
    public AudioSource shotAudioSource;

    private AudioClip reloadOnSound;
    private AudioClip reloadOffSound;

    private List<AudioClip> steps;
    private StepStat stat;
    private enum StepStat
    {
        Right,
        Left,
        None
    }


    protected override void PlayExplosionSound()
    {
        AudioDispatcher.PlayClipAtPosition(
            explosionSound, 
            IsMain ? BattleCamera.Instance.transform.position : transform.position,
            SoundControllerBase.EXPLOSION_VOLUME,
            IsMain ? BattleCamera.Instance.transform : transform);
    }
    protected override void SetEngineAudio() { }

    protected override void SetEngineNoise(float t) { }

    private void PlaySound(AudioClip clip, Vector3 position) 
    {
        AudioDispatcher.PlayClipAtPosition(clip, position);
    }

    private void PlayShotSound()
    {
        AudioDispatcher.PlayClipAtPosition(shotSound, weapon.position);
    }

    public void PlayReloadSound(bool start) 
    {
        if (reloadOnSound == null || reloadOffSound == null) 
        {
            return;
        }
        AudioDispatcher.PlayClipAtPosition(start? reloadOnSound: reloadOffSound, weapon.position);
    }

    public void Step()
    {
        if (IsAvailable && !IsDead && steps != null && steps.Count > 0)
        {
            int index = Random.Range(0, steps.Count-1);
            stepAudioSource.PlayOneShot(steps[index], BattleSettings.Instance.SoundVolume);
        }
    }
}
