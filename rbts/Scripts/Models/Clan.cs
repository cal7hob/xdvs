using System;
using UnityEngine;

namespace Tanks.Models
{
    [Serializable]
    public class Clan
    {
        public enum ClanRank
        {
            commander,
            fighter
        }

        public int Id { get { return id; } set { id = value; } }
        public string Name { get { return name; } set { name = value; } }
        public string Slogan { get { return slogan; } set { slogan = value; } }
        public string Image { get { return image; } set { image = value; } }
        public int Place { get { return place; } set { place = value; } }
        public int Score { get { return score; } set { score = value; } }
        public int MembersCount { get { return membersCount; } set { score = membersCount; } }
        public ClanRank Rank { get { return rank; } set { rank = value; } }

        [SerializeField]
        private int id;
        [SerializeField]
        private string name;
        [SerializeField]
        private string slogan;
        [SerializeField]
        private string image;
        [SerializeField]
        private int place;
        [SerializeField]
        private int score;
        [SerializeField]
        private int membersCount;
        [SerializeField]
        private ClanRank rank;

        // Static factory
        public static Clan Create(JsonPrefs prefs)
        {
            var clan = new Clan
            {
                id = prefs.ValueInt("id"),
                name = prefs.ValueString("name"),
                slogan = prefs.ValueString("slogan"),
                place = prefs.ValueInt("place"),
                score = prefs.ValueInt("score"),
                membersCount = prefs.ValueInt("membersCount"),
                image = prefs.ValueString("image"),
                rank = (ClanRank)Enum.Parse(typeof(ClanRank), prefs.ValueString("rank", "fighter")),
            };
            return clan;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Clan;
            if (other == null)
                return false;

            return id == other.id;
        }

        public bool Equals(Clan other)
        {
            if (other == null)
                return false;

            return id == other.id;
        }

        public override int GetHashCode()
        {
            return id.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("[Clan: Id={0}, Name={1}, Slogan={2}, " +
                "Image={3}, Place={4}, Score={5}, MembersCount={6}, Rank={7}]",
                Id, Name, Slogan, Image, Place, Score, MembersCount, Rank);
        }
    }
}
