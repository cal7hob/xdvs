using UnityEngine;
using System.Collections;

namespace XDevs.Loading {
    public class PingIndicator : MonoBehaviour {

        [SerializeField]
        InterfaceExtensions.ConditionHelper helper;

        [SerializeField]
        int pingGood = 100;
        [SerializeField]
        int pingNormal = 250;

        // Use this for initialization
        void Start () {
            helper.State = 0;
        }

        public void SetPing (int ping) {
            if (ping <= pingGood) {
                helper.State = 1;
            }
            else if (ping <= pingNormal) {
                helper.State = 2;
            }
            else {
                helper.State = 3;
            }
        }

    }
}