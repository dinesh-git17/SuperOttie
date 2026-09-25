using System.Collections.Generic;
using UnityEngine;

namespace SuperOttie.Level
{
    public enum TileKind : byte
    {
        Empty,
        Ground,
        Stone,
    }

    public enum SpawnKind
    {
        Coin,
        QuestionCoin,
        QuestionPowerUp,
        Brick,
        Crab,
        Puffer,
        Pipe,
        Flag,
        Bush,
        Flowers,
        Reeds,
        Sign,
        Checkpoint,
    }

    public readonly struct Spawn
    {
        public readonly SpawnKind Kind;

        /// <summary>Grid cell, (0,0) = bottom-left of the level.</summary>
        public readonly Vector2Int Cell;

        /// <summary>Extra size information: pipe height in cells. 0 otherwise.</summary>
        public readonly int Size;

        public Spawn(SpawnKind kind, Vector2Int cell, int size = 0)
        {
            Kind = kind;
            Cell = cell;
            Size = size;
        }

        public override string ToString() => $"{Kind}@{Cell}";
    }

    /// <summary>Immutable result of parsing a level file.</summary>
    public sealed class LevelData
    {
        readonly TileKind[,] _tiles;

        public LevelData(string name, string theme, int timeLimit, TileKind[,] tiles, List<Spawn> spawns, Vector2Int playerStart, Vector2Int flagCell, Vector2Int? checkpoint = null)
        {
            Name = name;
            Theme = theme;
            TimeLimit = timeLimit;
            _tiles = tiles;
            Spawns = spawns;
            PlayerStart = playerStart;
            FlagCell = flagCell;
            Checkpoint = checkpoint;
        }

        public string Name { get; }
        public string Theme { get; }
        public int TimeLimit { get; }
        public int Width => _tiles.GetLength(0);
        public int Height => _tiles.GetLength(1);
        public IReadOnlyList<Spawn> Spawns { get; }
        public Vector2Int PlayerStart { get; }
        public Vector2Int FlagCell { get; }

        /// <summary>Optional mid-level restart point ('K').</summary>
        public Vector2Int? Checkpoint { get; }

        public TileKind GetTile(int x, int y) =>
            x < 0 || y < 0 || x >= Width || y >= Height ? TileKind.Empty : _tiles[x, y];

        public bool IsSolidTile(int x, int y) => GetTile(x, y) != TileKind.Empty;
    }
}
