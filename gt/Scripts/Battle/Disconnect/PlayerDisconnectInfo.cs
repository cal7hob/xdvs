using UnityEngine;
using System.Collections;
using ExitGames.Client.Photon;

namespace Disconnect
{
    public class PlayerDisconnectInfo
    {
        public int InnerId { get; private set; }
        public float ReconnectTill { get; private set; }
        public int TeamId { get; private set; }

        public PlayerDisconnectInfo(int innerId, float reconnectTill, int teamId)
        {
            InnerId = innerId;
            ReconnectTill = reconnectTill;
            TeamId = teamId;
        }

        public static byte[] Serialize(object customObject)
        {
            byte[] bytes = new byte[4 + 4 + 4];
            int index = 0;
            PlayerDisconnectInfo info = customObject as PlayerDisconnectInfo;
            Protocol.Serialize(info.InnerId, bytes, ref index);
            Protocol.Serialize(info.ReconnectTill, bytes, ref index);
            Protocol.Serialize(info.TeamId, bytes, ref index);

            return bytes;
        }

        public static PlayerDisconnectInfo Deserialize(byte[] bytes)
        {
            int index = 0;
            int playerId;
            float reconnectTill;
            int teamId = 0;
            Protocol.Deserialize(out playerId, bytes, ref index);
            Protocol.Deserialize(out reconnectTill, bytes, ref index);
            Protocol.Deserialize(out teamId, bytes, ref index);

            return new PlayerDisconnectInfo(playerId, reconnectTill, teamId);
        }
    }
}