using SuperOttie.Player;
using UnityEngine;

namespace SuperOttie.Entities
{
    /// <summary>Something the player picks up by touching it.</summary>
    public interface ICollectible
    {
        void Collect(PlayerController player);
    }

    /// <summary>Something that reacts when the player's head hits it from below.</summary>
    public interface IBumpable
    {
        void Bump(PlayerController player);
    }

    /// <summary>Sprite sorting orders, back to front.</summary>
    public static class Sorting
    {
        public const int Background = -100;
        public const int Decor = -20;
        public const int GoalPole = -10;
        public const int Terrain = 0;
        public const int Pipe = 1;
        public const int EmergingItem = 2;
        public const int Block = 3;
        public const int Item = 4;
        public const int Enemy = 5;
        public const int Player = 6;
        public const int Effects = 10;
        public const int DyingPlayer = 50;
    }

    public static class SpriteObjects
    {
        public static SpriteRenderer Create(string name, Sprite sprite, Transform parent, Vector3 localPosition, int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        static PhysicsMaterial2D _frictionless;

        /// <summary>Zero-friction material so actors slide along walls instead of sticking to them.</summary>
        public static PhysicsMaterial2D Frictionless
        {
            get
            {
                if (_frictionless == null) _frictionless = new PhysicsMaterial2D("Frictionless") { friction = 0f, bounciness = 0f };
                return _frictionless;
            }
        }

        /// <summary>Sets the layer on an object and all its children.</summary>
        public static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform) SetLayerRecursively(child.gameObject, layer);
        }
    }
}
