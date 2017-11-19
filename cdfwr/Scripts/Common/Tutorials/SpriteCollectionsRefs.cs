using UnityEngine;

public class SpriteCollectionsRefs : MonoBehaviour
{

    [Header("Hangar GUI")]
    [SerializeField] private tk2dSpriteCollectionData codeOfWarSpriteCollection;

    public tk2dSpriteCollectionData CodeOfWarSpriteCollection { get { return codeOfWarSpriteCollection; } }
}
