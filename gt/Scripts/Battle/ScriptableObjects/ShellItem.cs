using Pool;
using UnityEngine;
using VFX;

[System.Serializable]
public class ShellItem : ScriptableObject 
{
    public ShellType shellType;
    public bool ownerTracking;
    public float speed = 1000;
    public float maxDistance = 1500;
    public AudioClip shotSound;
    public AudioClip[] shotSounds;
    public AudioClip blowSound;
    public AudioClip[] blowSounds;

    [AssetPathGetter, SerializeField] private string hitPrefabPath;
    [AssetPathGetter, SerializeField] private string terrainHitPrefabPath;

    public AudioClip ShotSound
    {
        get { return shotSounds != null && shotSounds.Length > 0 ? shotSounds.GetRandomItem() : shotSound; }
    }

    public void Explosion(Vector3 position, bool hitsVehicle = false)
    {
        AudioDispatcher.PlayClipAtPosition(blowSounds.Length > 0 ? blowSounds.GetRandomItem() : blowSound, position);
        var hitEffect = PoolManager.GetObject<Effect>(hitsVehicle ? hitPrefabPath : terrainHitPrefabPath);
        hitEffect.SetOrientation(position, Quaternion.identity);
    }
}