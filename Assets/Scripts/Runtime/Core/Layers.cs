using UnityEngine;

namespace SuperOttie.Core
{
    /// <summary>
    /// Physics layer ids (named in TagManager by the editor setup) and the 2D collision matrix.
    /// </summary>
    public static class Layers
    {
        public const int Ground = 6;
        public const int Player = 7;
        public const int Enemy = 8;
        public const int Item = 9;
        public const int Effects = 10;

        public static readonly string[] Names = { "Ground", "Player", "Enemy", "Item", "Effects" };

        public const int GroundMask = 1 << Ground;
        public const int EnemyMask = 1 << Enemy;
        public const int ItemMask = 1 << Item;

        /// <summary>
        /// Only the ground is physically solid for actors. Player/enemy/item interactions are resolved
        /// with overlap queries instead of collisions, which keeps stomps and pickups deterministic.
        /// </summary>
        public static void ConfigureCollisionMatrix()
        {
            Physics2D.IgnoreLayerCollision(Player, Enemy, true);
            Physics2D.IgnoreLayerCollision(Player, Item, true);
            Physics2D.IgnoreLayerCollision(Enemy, Enemy, true);
            Physics2D.IgnoreLayerCollision(Enemy, Item, true);
            Physics2D.IgnoreLayerCollision(Item, Item, true);
            for (int layer = 0; layer < 32; layer++)
                Physics2D.IgnoreLayerCollision(Effects, layer, true);
        }
    }
}
