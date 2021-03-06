﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public enum Tutorials
{
    battleTutorial,
    enterName,
    goToBattle,
    vehicleUpgrade,
    buyCamouflage,
    buyVehicle
}

public enum TutorialPages
{
    EnterName,
    MainMenu,
    Armory,
    PatternShop,
    VehicleShopWindow
}

public class TutorialsController : MonoBehaviour
{
    [Header("выключатель для туторов:")]
    [SerializeField] private bool isBattleTutorialOn = true;
    [SerializeField] private bool isHangarTutorialOn = true;

    [Header("кнопки для тутора:")]
    [SerializeField] private MainMenuButtons mainMenuButtons;

    public Dictionary<int, GameObject> tutorialGroups = new Dictionary<int, GameObject>((int) Tutorials.buyVehicle);

    //private static bool tutorsHardcoded = false;

    public static MainMenuButtons MainMenuButtons { get { return Instance.mainMenuButtons; } }

    public static TutorialsController Instance { get; private set; }

    void Awake()
    {
        Instance = this;

        Messenger.Subscribe(EventId.AfterHangarInit, Init);
        Messenger.Subscribe(EventId.TutorialIndexChanged, Init);
        Messenger.Subscribe(EventId.CamouflageBought, OnCamoBought);
        Messenger.Subscribe(EventId.ModuleBought, OnModuleBought);
        Messenger.Subscribe(EventId.VehicleBought, OnVehicleBought);
    }

    void OnDestroy()
    {
        Instance = null;

        Messenger.Unsubscribe(EventId.AfterHangarInit, Init);
        Messenger.Unsubscribe(EventId.TutorialIndexChanged, Init);
        Messenger.Unsubscribe(EventId.CamouflageBought, OnCamoBought);
        Messenger.Unsubscribe(EventId.ModuleBought, OnModuleBought);
        Messenger.Unsubscribe(EventId.VehicleBought, OnVehicleBought);
    }

    private static void OnCamoBought(EventId id, EventInfo info)
    {
        ProfileInfo.accomplishedTutorials[Tutorials.buyCamouflage] = true;
        ProfileInfo.SaveToServer();
    }

    private static void OnModuleBought(EventId id, EventInfo info)
    {
        ProfileInfo.accomplishedTutorials[Tutorials.vehicleUpgrade] = true;
        ProfileInfo.SaveToServer();
    }

    private static void OnVehicleBought(EventId id, EventInfo info)
    {
        ProfileInfo.accomplishedTutorials[Tutorials.buyVehicle] = true;
        ProfileInfo.SaveToServer();
    }

    private IEnumerator GoToFirstMap()
    {
        if (!ProfileInfo.IsBattleTutorial)
            yield break;

        HangarController.Instance.EnterBattle(GameData.GetTutorialMapId());
    }

    private void Init(EventId id = 0, EventInfo info = null)
    {
        if (ProfileInfo.TutorialIndex > (int)Tutorials.buyVehicle)
            Destroy(gameObject);

        if (!isBattleTutorialOn)
        {
            ProfileInfo.accomplishedTutorials[Tutorials.battleTutorial] = true;

            if (!isHangarTutorialOn)
            {
                for (int i = 0; i < ProfileInfo.accomplishedTutorials.Count; i++)
                {
                    ProfileInfo.accomplishedTutorials[(Tutorials) i] = true;
                }
            }
        }

        if (!isHangarTutorialOn)
        {
            Destroy(gameObject);
            foreach (var tutorialGroup in tutorialGroups.Values)
            {
                Destroy(tutorialGroup);
            }
            return;
        }

        var currentTutorial = (Tutorials) ProfileInfo.TutorialIndex;
        var tutorialsHolder = (GameObject) Resources.Load("Tutorials/TutorialHolder");

        // Для отладки:
        //if (!tutorsHardcoded)
        //{
        //    ProfileInfo.TutorialIndex = (int)Tutorials.enterName;
        //    currentTutorial = (Tutorials)(int)ProfileInfo.TutorialIndex;
        //
        //    ProfileInfo.accomplishedTutorials[Tutorials.enterName.ToString()] = false;
        //
        //    tutorsHardcoded = true;
        //}

        GoBackTutorial backTutor;

        switch (currentTutorial)
        {
            case Tutorials.battleTutorial:
                if (GameData.IsHangarScene && HangarController.FirstEnter)
                    StartCoroutine(GoToFirstMap());
                break;

            case Tutorials.enterName:
                tutorialGroups.Add((int) Tutorials.enterName, new GameObject(Tutorials.enterName.ToString()));
                tutorialGroups.Add((int) Tutorials.goToBattle, new GameObject(Tutorials.goToBattle.ToString()));

                InstantiateTutorial<EnterNameTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.enterName]);
                InstantiateTutorial<GoToBattleTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.goToBattle]);
                break;

            case Tutorials.goToBattle:
                if (!tutorialGroups.ContainsKey((int) Tutorials.goToBattle) ||
                    tutorialGroups[(int) Tutorials.goToBattle] == null)
                {
                    tutorialGroups.Add((int)Tutorials.goToBattle, new GameObject(Tutorials.goToBattle.ToString()));
                    InstantiateTutorial<GoToBattleTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.goToBattle]);
                }
                break;

            case Tutorials.vehicleUpgrade:
                tutorialGroups.Add((int) Tutorials.vehicleUpgrade, new GameObject(Tutorials.vehicleUpgrade.ToString()));

                InstantiateTutorial<GoToModuleShopTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.vehicleUpgrade]);
                InstantiateTutorial<BuyModuleUpgradeTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.vehicleUpgrade]);
                InstantiateTutorial<GoToBattleAfterModuleUpgradeTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.vehicleUpgrade]);

                backTutor = InstantiateTutorial<GoBackTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.vehicleUpgrade]);

                backTutor.SetParams(TutorialPages.Armory.ToString(), (int)Tutorials.vehicleUpgrade);
                break;

            case Tutorials.buyCamouflage:
                tutorialGroups.Add((int) Tutorials.buyCamouflage, new GameObject(Tutorials.buyCamouflage.ToString()));

                InstantiateTutorial<GoToPatternShopTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyCamouflage]);
                InstantiateTutorial<BuyPatternTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyCamouflage]);
                InstantiateTutorial<GoToBattleAfterBuyingCamoTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyCamouflage]);

                backTutor = InstantiateTutorial<GoBackTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyCamouflage]);

                backTutor.SetParams(TutorialPages.PatternShop.ToString(), (int)Tutorials.buyCamouflage);
                break;

            //case Tutorials.buyVehicle:
            //    tutorialGroups.Add((int) Tutorials.buyVehicle, new GameObject(Tutorials.buyVehicle.ToString()));

            //    InstantiateTutorial<GoToVehicleShopTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyVehicle]);
            //    InstantiateTutorial<SelectVehicleTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyVehicle]);
            //    InstantiateTutorial<BuyVehicleTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyVehicle]);
            //    InstantiateTutorial<GoToBattleAfterBuyingVehicleTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyVehicle]);

            //    backTutor = InstantiateTutorial<GoBackTutorial>(tutorialsHolder, tutorialGroups[(int)Tutorials.buyVehicle]);

            //    backTutor.SetParams(TutorialPages.VehicleShopWindow.ToString(), (int)Tutorials.buyVehicle);
            //    break;
        }

        foreach (var group in tutorialGroups.Values)
        {
            if(group != null)
                group.transform.SetParent(transform, false);
        }

        Messenger.Send(EventId.TutorialsInitialized, new EventInfo_SimpleEvent());
    }

    public T InstantiateTutorial<T>(GameObject prefab, GameObject parent, string tutorialName = null) where T : Tutorial
    {
        var tutorialHolder = Instantiate(prefab);
        var tutorial = tutorialHolder.AddComponent(typeof(T));

        tutorialHolder.transform.SetParent(parent.transform);

        return (T) tutorial;
    }
}
