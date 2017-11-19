using UnityEngine;

public class SpriteCollectionsRefs : MonoBehaviour
{

    [Header("Hangar GUI")]
    [SerializeField] private tk2dSpriteCollectionData WWT2SpriteCollection;

    public tk2dSpriteCollectionData WWT2_SpriteCollection { get { return WWT2SpriteCollection; } }
}
