using System.Collections.Generic;
using UnityEngine;

public class HangarWeaponsHolder : MonoBehaviour
{
    private Dictionary<int, WeaponKit> weapons = new Dictionary<int, WeaponKit>();
    private WeaponKit currentWeapon;

    public static HangarWeaponsHolder Instance { get; private set; }

    void Awake()
    {
        Instance = this;

        Initialize();
        //Dispatcher.Subscribe(EventId.AfterHangarInit, Initialize);
    }

    void OnDestroy()
    {
        Instance = null;

        //Dispatcher.Unsubscribe(EventId.AfterHangarInit, Initialize);
    }

    public WeaponKit this[int id]
    {
        get { return weapons[id]; }
    }

    private void Initialize()
    {
        var weaponPrefabs = Resources.LoadAll<WeaponKit>(string.Format("{0}/Weapon/WeaponSoldier", GameManager.CurrentResourcesFolder));

        foreach (var weapon in weaponPrefabs)
        {
            var w = Instantiate(weapon, transform);
            w.name = weapon.name;
            w.gameObject.SetActive(false);
            weapons.Add(w.id, w);
            CorrectWeaponPosition(w.gameObject);
        }

        currentWeapon = weapons[weaponPrefabs[0].id];
    }

    private void CorrectWeaponPosition(GameObject weapon)
    {
        var minZ = float.MaxValue;
        var maxZ = float.MinValue;
        var renderers = weapon.GetComponentsInChildren<MeshRenderer>();

        foreach (var meshRenderer in renderers)
        {
            if (meshRenderer.bounds.min.z < minZ)
            {
                minZ = meshRenderer.bounds.min.z;
            }

            if (meshRenderer.bounds.max.z > maxZ)
            {
                maxZ = meshRenderer.bounds.max.z;
            }
        }

        var middleZ = (minZ + maxZ)*0.5f;
        var zOffset = transform.position.z - middleZ;
        weapon.transform.position = transform.position + Vector3.forward*zOffset; 
    }

    public void SetActiveWeaponById(int id)
    {
        if(currentWeapon != null)
            currentWeapon.gameObject.SetActive(false);
        currentWeapon = weapons[id];
        currentWeapon.gameObject.SetActive(true);
    }

    public void GiveCurrentWeaponToSoldier()
    {
        currentWeapon.transform.SetParent(Shop.VehicleInView.HangarVehicle.WeaponWrapper, false);
        Shop.VehicleInView.HangarVehicle.IKHangarController.SetTargets(currentWeapon.LeftHandTarget, currentWeapon.RightHandTarget, currentWeapon.LookTarget);
    }
}
