using UnityEngine;
using System.Collections;

public class MainMenuButtons : MonoBehaviour {

    [Header("ссылки на кнопки:")]
    [SerializeField] private Transform vehicleShopBtn;
    [SerializeField] private Transform moduleShopBtn;
    [SerializeField] private Transform patternShopBtn;
    [SerializeField] private Transform decalShopBtn;
    [SerializeField] private Transform goToBattleBtn;
    [SerializeField] private Transform backBtn;

    public Transform VehicleShopBtn { get { return vehicleShopBtn; } }
    public Transform ModuleShopBtn { get { return moduleShopBtn; } }
    public Transform PatternShopBtn { get { return patternShopBtn; } }
    public Transform DecalShopBtn { get { return decalShopBtn; } }
    public Transform GoToBattleBtn { get { return goToBattleBtn; } }
    public Transform BackBtn { get { return backBtn; } }
}
