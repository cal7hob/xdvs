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

        static public Player InputPlayer
        {
            get
            {
                if (inputPlayer == null)
                {
                    inputPlayer = ReInput.players.GetPlayer(inputPlayerId);
                    touchController = inputPlayer.controllers.GetControllerWithTag<CustomController>("touchController");

                    switch (GameData.CurInterface)
                    {
                        case Interface.WWT2:
                            inputPlayer.controllers.maps.SetMapsEnabled(true, "Tank");
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
                return inputPlayer;
            }
        }

        static public CustomController TouchController
        {
            get
            {
                if (inputPlayer == null)
                {
                    InputPlayer.GetAnyButton();
                }
                return touchController;
            }
        }
        #endregion
#endif

        static public bool GetButton(string actionName)
        {
#if !NO_REWIRED
            return InputPlayer.GetButton(actionName);
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
        static public bool GetButtonUp(string actionName)
        {
#if !NO_REWIRED
            return InputPlayer.GetButtonUp(actionName);
#else
            return UnityEngine.Input.GetButtonUp(actionName);
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