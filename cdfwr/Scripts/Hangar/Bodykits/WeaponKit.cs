using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponKit : StickerKit
{
    [SerializeField]
    private Transform leftHandTarget;
    [SerializeField]
    private Transform rightHandTarget;
    [SerializeField]
    private Transform lookTarget;
    [SerializeField]
    private Transform cannonEnd;
    [SerializeField]
    private AudioClip reloadOnSound;
    [SerializeField]
    private AudioClip reloadOffSound;

    public Transform LeftHandTarget { get { return leftHandTarget; } }
    public Transform RightHandTarget { get { return rightHandTarget; } }
    public Transform LookTarget { get { return lookTarget; } }

    public virtual void GetWeaponParams(SoldierController soldier)
    {
        soldier.GetWeaponParams
            (weapon: transform, 
            leftTarget:leftHandTarget, 
            rightTarget: rightHandTarget, 
            lookTarget:lookTarget, 
            cannonEnd:cannonEnd, 
            gunLocation: transform.parent, 
            reloadOnSound: reloadOnSound,
            reloadOffSound: reloadOffSound
            );
    }

    public override void TryActivate(Decal decal)
    {
        if (MenuController.Instance == null || MenuController.Instance.isBattleEntering)
        {
            if (decal != null && decal.id == id)
            {
                gameObject.SetActive(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else
        {
            if (decal != null && decal.id == id)
            {
                gameObject.SetActive(true);
            }
            else 
            {
                gameObject.SetActive(false);
            }
        }
    }

    public virtual bool TryActivate(Decal decal, SoldierController soldier)
    {    
        if (decal != null && decal.id == id)
        {
            gameObject.SetActive(true);
            soldier.GetWeaponParams(transform, leftHandTarget, rightHandTarget, lookTarget, cannonEnd, transform.parent, reloadOnSound, reloadOffSound);
            return true;
        }
        else
        {
            if (MenuController.Instance == null || MenuController.Instance.isBattleEntering)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return false;
        }
    }
}
