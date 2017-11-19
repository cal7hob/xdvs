using System;
using System.Collections.Generic;
using UnityEngine;
using JSONObject = System.Collections.Generic.Dictionary<string, object>;

namespace Tanks.Models
{
    public enum ChatRoom
    {
        Unspecified = 0,
        Country = 1,
        Region = 2,
        City = 3,
        Clan = 4
    }

    [Serializable]
    public class Message
    {
        public Player Sender { get { return sender; } }
        public int Id { get { return id; } }
        public string Content { get { return content; } }
        public long CreateTime { get { return createTime; } }
        public bool IsModerator { get { return isModerator; } }

        [SerializeField]
        private Player sender;
        [SerializeField]
        private int id;
        [SerializeField]
        private string content;
        [SerializeField]
        private long createTime;
        [SerializeField]
        private bool isModerator;

        public Message(JSONObject message)
        {
            //Debug.LogWarning("Message: " + message.ToStringFull());

            var prefs = new JsonPrefs(message);

            sender = Player.Create(prefs);

            id = prefs.ValueInt("messageId");
            content = prefs.ValueString("message");
            createTime = prefs.ValueLong("createTime");
            isModerator = prefs.ValueBool("isModerator");

            //Debug.LogWarning("Nick: " + sender.NickName + ", " + sender.Platform + "\nMessage: " + Content + "\nMessageId: " + Id + "\nCreateTime: " + CreateTime + "\nVip: " + sender.IsVip);
        }
    }

    [Serializable]
    public class Request
    {
        public int Id { get { return id; } }
        public long CreateTime { get { return createTime; } }
        public Player Applicant { get { return applicant; } }

        [SerializeField]
        private int id;
        [SerializeField]
        private long createTime;
        [SerializeField]
        private Player applicant;

        public Request(JSONObject request)
        {
            //Debug.LogWarning("Request: " + request.ToStringFull());

            var prefs = new JsonPrefs(request);

            id = prefs.ValueInt("requestId");
            createTime = prefs.ValueLong("createTime");
            applicant = Player.Create(prefs);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Request;
            if (other == null)
                return false;

            return id == other.id;
        }

        public bool Equals(Request other)
        {
            if (other == null)
                return false;

            return id == other.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }
    }

    public class Room
    {
        public string Name { get; private set; }
        public string Code { get; private set; }
        public List<Message> Messages = new List<Message>();
        public List<Request> Requests = new List<Request>();
        public ChatRoom type;

        public Room(string _name, string _code, ChatRoom _type )
        {
            Name = _name;
            Code = _code;
            type = _type;
        }

        public Room(JSONObject room)
        {
            //Debug.LogWarning("Room: " + Room.ToStringFull());

            var prefs = new JsonPrefs(room);

            Name = prefs.ValueString("name");
            Code = prefs.ValueString("code");
            var messages = prefs.ValueObjectList("messages", new List<object>());

            foreach (JSONObject message in messages)
            {
                Messages.Add(new Message(message));
            }

            #region Test mockup
            //var testRequests = "{\"requests\":[{\"requestId\":22,\"createTime\":1436528255,\"id\":226690,\"nickName\":\"DickButt\",\"score\":90000},{\"requestId\":23,\"createTime\":1436528300,\"id\":226508,\"nickName\":\"Bubka Gop\",,\"score\":50000}]}";
            //var requests = new JsonPrefs(testRequests).ValueObjectList("requests");
            #endregion

            var requests = prefs.ValueObjectList("requests", new List<object>());

            foreach (JSONObject request in requests)
            {
                Requests.Add(new Request(request));
            }
        }
    }

    public class Chat
    {
        public Dictionary<ChatRoom, Room> Rooms = new Dictionary<ChatRoom, Room>();
        public long Ban { get; private set; }

        public Chat(JSONObject data)
        {
            //Debug.LogWarning("Chat: " + data.ToStringFull());
            var prefs = new JsonPrefs(data);

            Ban = prefs.ValueLong("ban");

            prefs.BeginGroup("messages");

            foreach (ChatRoom room in Enum.GetValues(typeof(ChatRoom)))
            {
                var areaDict = prefs.ValueObjectDict(room.ToString().ToLower(), null);
                
                if (areaDict != null)
                    Rooms[room] = new Room(areaDict);
            }

            prefs.EndGroup();
        }

        public Chat(ChatRoom roomDictName, Room room, long ban)
        {
            Ban = ban;
            Rooms[roomDictName] = room;
        }
    }
}
