using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace SuperOttie.Level
{
    public sealed class LevelFormatException : Exception
    {
        public LevelFormatException(string message) : base(message) { }
    }

    /// <summary>
    /// Parses the plain-text level format:
    /// <code>
    /// name: 1-1 Sunny Meadow
    /// theme: day
    /// time: 300
    /// ---
    /// ....c c c.......
    /// ...?.B?B........
    /// P.....e.....[]..
    /// ############||##
    /// </code>
    /// Rows are listed top to bottom. Legend:
    /// <c>.</c>/space empty, <c>#</c> ground, <c>S</c> stone, <c>c</c> coin, <c>?</c> coin block,
    /// <c>M</c> power-up block, <c>B</c> brick, <c>e</c> crab, <c>f</c> pufferfish, <c>P</c> player start,
    /// <c>F</c> flagpole base, <c>K</c> mid-level checkpoint, <c>[]</c> pipe top with <c>||</c> pipe body below it,
    /// decorations: <c>d</c> bush, <c>w</c> flowers, <c>r</c> reeds, <c>n</c> signpost.
    /// </summary>
    public static class LevelParser
    {
        public const string Separator = "---";
        public const int DefaultTime = 300;

        public static LevelData Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) throw new LevelFormatException("Level text is empty.");

            var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            string name = "Unnamed", theme = "day";
            int time = DefaultTime;
            int i = 0;
            bool sawSeparator = false;
            for (; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line == Separator)
                {
                    sawSeparator = true;
                    i++;
                    break;
                }
                if (line.Length == 0 || line.StartsWith("//")) continue;
                int colon = line.IndexOf(':');
                if (colon < 0) throw new LevelFormatException($"Header line {i + 1} is not 'key: value': '{line}'.");
                var key = line.Substring(0, colon).Trim().ToLowerInvariant();
                var value = line.Substring(colon + 1).Trim();
                switch (key)
                {
                    case "name": name = value; break;
                    case "theme": theme = value.ToLowerInvariant(); break;
                    case "time":
                        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out time) || time <= 0)
                            throw new LevelFormatException($"Invalid time '{value}'.");
                        break;
                    default: throw new LevelFormatException($"Unknown header key '{key}'.");
                }
            }
            if (!sawSeparator) throw new LevelFormatException($"Missing '{Separator}' line between header and map.");

            var rows = new List<string>();
            for (; i < lines.Length; i++)
            {
                var row = lines[i].TrimEnd();
                if (row.Length == 0) continue;
                rows.Add(row);
            }
            if (rows.Count == 0) throw new LevelFormatException("Map has no rows.");

            int height = rows.Count;
            int width = 0;
            foreach (var r in rows) width = Math.Max(width, r.Length);

            // Short rows are padded with empty cells so authors don't need trailing dots.
            char At(int x, int y)
            {
                var row = rows[height - 1 - y];
                return x < row.Length ? row[x] : '.';
            }

            var tiles = new TileKind[width, height];
            var spawns = new List<Spawn>();
            Vector2Int? player = null, flag = null, checkpoint = null;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char c = At(x, y);
                    var cell = new Vector2Int(x, y);
                    switch (c)
                    {
                        case '.':
                        case ' ':
                        case '|':
                        case ']':
                            break; // pipe body / right half are handled from the '[' marker
                        case '#': tiles[x, y] = TileKind.Ground; break;
                        case 'S': tiles[x, y] = TileKind.Stone; break;
                        case 'c': spawns.Add(new Spawn(SpawnKind.Coin, cell)); break;
                        case '?': spawns.Add(new Spawn(SpawnKind.QuestionCoin, cell)); break;
                        case 'M': spawns.Add(new Spawn(SpawnKind.QuestionPowerUp, cell)); break;
                        case 'B': spawns.Add(new Spawn(SpawnKind.Brick, cell)); break;
                        case 'e': spawns.Add(new Spawn(SpawnKind.Crab, cell)); break;
                        case 'f': spawns.Add(new Spawn(SpawnKind.Puffer, cell)); break;
                        case 'd': spawns.Add(new Spawn(SpawnKind.Bush, cell)); break;
                        case 'w': spawns.Add(new Spawn(SpawnKind.Flowers, cell)); break;
                        case 'r': spawns.Add(new Spawn(SpawnKind.Reeds, cell)); break;
                        case 'n': spawns.Add(new Spawn(SpawnKind.Sign, cell)); break;
                        case 'P':
                            if (player.HasValue) throw new LevelFormatException($"Second player start at {cell}.");
                            player = cell;
                            break;
                        case 'K':
                            if (checkpoint.HasValue) throw new LevelFormatException($"Second checkpoint at {cell}.");
                            checkpoint = cell;
                            spawns.Add(new Spawn(SpawnKind.Checkpoint, cell));
                            break;
                        case 'F':
                            if (flag.HasValue) throw new LevelFormatException($"Second flagpole at {cell}.");
                            flag = cell;
                            tiles[x, y] = TileKind.Stone; // the pole stands on a stone base block
                            spawns.Add(new Spawn(SpawnKind.Flag, cell));
                            break;
                        case '[':
                            spawns.Add(new Spawn(SpawnKind.Pipe, cell, MeasurePipe(At, x, y)));
                            break;
                        default:
                            throw new LevelFormatException($"Unknown map character '{c}' at {cell} (row {height - y} from the top).");
                    }
                }
            }

            if (!player.HasValue) throw new LevelFormatException("Map has no player start 'P'.");
            if (!flag.HasValue) throw new LevelFormatException("Map has no flagpole 'F'.");

            return new LevelData(name, theme, time, tiles, spawns, player.Value, flag.Value, checkpoint);
        }

        /// <summary>A pipe is '[' ']' on its top row with '||' below, down to something solid.</summary>
        static int MeasurePipe(Func<int, int, char> at, int x, int y)
        {
            if (at(x + 1, y) != ']') throw new LevelFormatException($"Pipe top '[' at ({x},{y}) must be followed by ']'.");
            int height = 1;
            int yy = y - 1;
            while (yy >= 0 && at(x, yy) == '|' && at(x + 1, yy) == '|')
            {
                height++;
                yy--;
            }
            bool grounded = yy >= 0 && IsSolidChar(at(x, yy)) && IsSolidChar(at(x + 1, yy));
            if (!grounded) throw new LevelFormatException($"Pipe at ({x},{y}) must rest on solid ground ('#' or 'S') under '||'.");
            return height;
        }

        static bool IsSolidChar(char c) => c == '#' || c == 'S';
    }
}
