using UnityEngine;

public class SpriteCollectionsRefs : MonoBehaviour
{

    [Header("Hangar GUI")]
    [SerializeField] private tk2dSpriteCollectionData ironTanksSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData toonWarsSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData futureTanksSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData spaceJetSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData battleOfWarplanesSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData battleOfHelicoptersSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData armadaSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData apocalipticCarsSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData wwrSpriteCollection;
    [SerializeField] private tk2dSpriteCollectionData ftRobotsInvasionSpriteCollection;

    public tk2dSpriteCollectionData IT_SpriteCollection { get { return ironTanksSpriteCollection; } }
    public tk2dSpriteCollectionData TW_SpriteCollection { get { return toonWarsSpriteCollection; } }
    public tk2dSpriteCollectionData FT_SpriteCollection { get { return futureTanksSpriteCollection; } }
    public tk2dSpriteCollectionData SJ_SpriteCollection { get { return spaceJetSpriteCollection; } }
    public tk2dSpriteCollectionData BW_SpriteCollection { get { return battleOfWarplanesSpriteCollection; } }
    public tk2dSpriteCollectionData BH_SpriteCollection { get { return battleOfHelicoptersSpriteCollection; } }
    public tk2dSpriteCollectionData AR_SpriteCollection { get { return armadaSpriteCollection; } }
    public tk2dSpriteCollectionData AC_SpriteCollection { get { return apocalipticCarsSpriteCollection; } }
    public tk2dSpriteCollectionData WWR_SpriteCollection { get { return wwrSpriteCollection; } }
    public tk2dSpriteCollectionData FTRI_SpriteCollection { get { return ftRobotsInvasionSpriteCollection; } }
}
