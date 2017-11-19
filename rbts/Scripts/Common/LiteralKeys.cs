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
            IgnoreRaycast,
            GroundChecker
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
                { Key.IgnoreRaycast,    "Ignore Raycast" },
                { Key.GroundChecker,    "GroundChecker" }
            };
        }

        /// <summary>
        /// Словарь слоёв.
        /// Новые слои добавлять в enum Layer.Key, а потом сюда.
        /// </summary>
        public static Dictionary<Key, string> Items { get; private set; }
    }
}