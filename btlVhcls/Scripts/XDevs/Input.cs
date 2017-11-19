using UnityEngine;
using System.Collections;

#if !NO_REWIRED
using Rewired;
#endif

namespace XDevs
{

    public class Input
    {
#if !NO_REWIRED
        #region Rerwired data for input tracking
        static protected int inputPlayerId = 0;
        static protected Player inputPlayer;
        static protected CustomController touchController;

        static public Player InputPlayer {
            get {
                if (inputPlayer == null) {
                    inputPlayer = ReInput.players.GetPlayer(inputPlayerId);
                    touchController = inputPlayer.controllers.GetControllerWithTag<CustomController>("touchController");
                    UpdateMapsStatus();
                }
                return inputPlayer;
            }
        }

        static public void UpdateMapsStatus()
        {
            switch (GameData.CurInterface)
            {
                case Interface.IronTanks:
                case Interface.FutureTanks:
                case Interface.ToonWars:
                case Interface.Armada:
                    InputPlayer.controllers.maps.SetMapsEnabled(true, "Tank");
                    break;

                case Interface.SpaceJet:
                    InputPlayer.controllers.maps.SetMapsEnabled(true, "Space Ship");
                    break;

                case Interface.BattleOfWarplanes:
                    InputPlayer.controllers.maps.SetMapsEnabled(true, "Aircraft");
                    break;

                case Interface.BattleOfHelicopters:
                    InputPlayer.controllers.maps.SetMapsEnabled(true, "Helicopter");
                    break;

                case Interface.MetalForce:
                    InputPlayer.controllers.maps.SetMapsEnabled(true, "Tank");
                    break;

                default:
                    Debug.LogError("Set required input category for new game!");
                    break;
            }

            //foreach (ControllerMap map in inputPlayer.controllers.maps.GetAllMaps())
            //{
            //    Debug.Log("Input map '"+map.name+"', type '"+map.controllerType+"', is "+(map.enabled ? "enabled" : "disabled"));
            //}
        }

        static public CustomController TouchController {
            get {
                if (inputPlayer == null) {
                    InputPlayer.GetAnyButton();
                }
                return touchController;
            }
        }
        #endregion
#endif

        static public bool GetButton (string actionName)
        {
#if !NO_REWIRED
            return InputPlayer.GetButton (actionName);
#else
            return UnityEngine.Input.GetButton(actionName);
#endif
        }

        static public bool GetButtonDown(string actionName)
        {
#if !NO_REWIRED
            return InputPlayer.GetButtonDown(actionName);
#else
            return UnityEngine.Input.GetButtonDown(actionName);
#endif
        }

        static public float GetAxis(string actionName)
        {
#if !NO_REWIRED
            return InputPlayer.GetAxis(actionName);
#else
            return UnityEngine.Input.GetAxis(actionName);
#endif
        }
    }

}