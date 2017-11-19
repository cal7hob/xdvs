using System.Collections.Generic;
using UnityEngine;

namespace XDevs.LiteralKeys
{
    public static class Tag
    {
        /// <summary>
        /// Ключи тегов.
        /// Новые теги добавлять сюда.
        /// </summary>
        public enum Key
        {
            Player,
            Friend,
            Enemy,
            CritZone,
            OutOfMapWarningCollider,
            OutOfMapCollider,
            IgnoreMaterial,
            IgnoreCameraCollision
        }

        static Tag()
        {
            Items = new Dictionary<Key, string>
            {
                { Key.OutOfMapWarningCollider,  "outOfMapWarningCol" },
                { Key.OutOfMapCollider,         "outOfMapCol" },
                { Key.Player,                   "Player" },
                { Key.Friend,                   "Friend" },
                { Key.Enemy,                    "Enemy" },
                { Key.CritZone,                 "CritZone" },
                { Key.IgnoreMaterial,           "IgnoreMaterial" },
                { Key.IgnoreCameraCollision,    "IgnoreCameraCollision" }
            };
        }

        /// <summary>
        /// Словарь тегов.
        /// Новые теги добавлять в enum Tag.Key, а потом сюда.
        /// </summary>
        public static Dictionary<Key, string> Items { get; private set; }

        /// <summary>
        /// Помечен ли данный игровой объект тегом tag?
        /// </summary>
        /// <param name="source">Игровой объект.</param>
        /// <param name="tag">Тег.</param>
        /// <returns></returns>
        public static bool CompareTag(this GameObject source, Key tag)
        {

            return source.CompareTag(Items[tag]);
        }
    }

    public static class Layer
    {
        /// <summary>
        /// Ключи слоёв.
        /// Новые слои добавлять сюда.
        /// </summary>
        public enum Key
        {
            Default,
            Player,
            Friend,
            Enemy,
            Bonus,
            Terrain,
            Water,
            ParallelWorld,
            TankBumper,
            OutOfMap,
            IgnoreRaycast
        }

        static Layer()
        {
            Items = new Dictionary<Key, string>
            {
                { Key.Default,          "Default" },
                { Key.Player,           "Player" },
                { Key.Friend,           "Friend" },
                { Key.Enemy,            "Enemy" },
                { Key.Bonus,            "Bonus" },
                { Key.Terrain,          "Terrain" },
                { Key.Water,            "Water" },
                { Key.ParallelWorld,    "ParallelWorld" },
                { Key.TankBumper,       "TankBumper" },
                { Key.OutOfMap,         "OutOfMap" },
                { Key.IgnoreRaycast,    "Ignore Raycast" }
            };
        }

        /// <summary>
        /// Словарь слоёв.
        /// Новые слои добавлять в enum Layer.Key, а потом сюда.
        /// </summary>
        public static Dictionary<Key, string> Items { get; private set; }
    }


    public enum StatisticKey
    {
        Health,
        Score,
        Kills,
        Deaths,
        Existance,
        Attack,
        RoF,
        Speed,
        MaxArmor,
        Regen,
        Shield,
        DamageRatio,
    }

    public static class StatisticType 
    {
        static Dictionary<StatisticKey, string> typeToKey = new Dictionary<StatisticKey, string>
        {
            {StatisticKey.Deaths, "dt"},
            {StatisticKey.Kills,"kl"},
            {StatisticKey.Score, "sc"},
            {StatisticKey.Health, "hl"},
            {StatisticKey.Existance, "ex"},
            {StatisticKey.Attack, "at"},
            {StatisticKey.RoF, "rf"},
            {StatisticKey.Speed, "sp"},
            {StatisticKey.MaxArmor, "mar"}
        };

        public static string GetKey(StatisticKey type)
        {
            return typeToKey[type];
        }
        
       /* public string this[StatisticKey key]
        {
            get
            {
                return typeToKey[key];
            }
        }*/

        /*
        /// <summary>
        /// Получение по ключу
        /// </summary>
        /// <param name="_type"></param>
        /// <returns></returns>
        public string this[int shellType, Vector3 startPos]
        {
            get
            {
                int actInd = -1;
                for (int ind = 0; ind < shells.Count; ind++)
                {
                    if (shells[ind] == null)
                    {
                        continue;
                    }
                    if (shells[ind].shellType == shellType)
                    {
                        actInd = ind;
                        break;
                    }
                }

                if (actInd != -1)
                {
                    JNShell shell = shells[actInd];//?
                    shells.RemoveAt(actInd);
                    return shell;
                }
                else
                {
                    JNShell shell = ((GameObject)GameObject.Instantiate(JNShellObj.instance.shellItems[shellType].pref, startPos, Quaternion.identity)).GetComponent<JNShell>();//?gunDispatcher.transform.position
                    shell.shellType = shellType;//
                    activeInd++;
                    shell.name += activeInd;
                    return shell;
                }
            }
            set
            {
                if (shells.Contains(value))
                {
                    return;
                }
                value.rigidbody.velocity = Vector3.zero;
                value.rigidbody.MovePosition(gunDispatcher.transform.position);
                value.gameObject.SetActive(false);
                shells.Add(value);
            }
        }
        */
    }
}