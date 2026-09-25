using System.Collections.Generic;
using UnityEngine;

namespace SuperOttie.Level
{
    /// <summary>
    /// Static sanity checks that catch unbeatable or broken layouts before anyone plays them.
    /// Conservative: it checks the floor route only (pits and steps), not every optional platform.
    /// </summary>
    public static class LevelLint
    {
        /// <summary>Widest pit (in cells) a running jump clears with a comfortable margin.</summary>
        public const int MaxPitWidth = 4;

        /// <summary>Tallest single step up (in cells) reachable with a full jump.</summary>
        public const int MaxStepUp = 4;

        public static List<string> Validate(LevelData level)
        {
            var problems = new List<string>();
            var solid = BuildSolidGrid(level, problems);

            var surface = FloorSurface(solid, level.Width, level.Height);

            int pit = 0;
            for (int x = 0; x < level.Width; x++)
            {
                if (surface[x] == 0)
                {
                    pit++;
                    if (pit == MaxPitWidth + 1) problems.Add($"Pit wider than {MaxPitWidth} cells ending near x={x}.");
                }
                else pit = 0;
            }
            if (surface[0] == 0) problems.Add("Level must start on solid ground at x=0.");

            for (int x = 1; x < level.Width; x++)
            {
                if (surface[x] == 0 || surface[x - 1] == 0) continue;
                int step = surface[x] - surface[x - 1];
                if (step > MaxStepUp) problems.Add($"Step up of {step} cells at x={x} is higher than a jump ({MaxStepUp}).");
            }

            if (level.Checkpoint is Vector2Int k)
            {
                if (k.y == 0 || !solid[k.x, k.y - 1]) problems.Add($"Checkpoint {k} must stand on solid ground.");
                if (k.x <= level.PlayerStart.x || k.x >= level.FlagCell.x) problems.Add("Checkpoint must be between the start and the flag.");
            }

            var start = level.PlayerStart;
            if (solid[start.x, start.y]) problems.Add($"Player start {start} is inside a solid cell.");
            if (level.FlagCell.x <= start.x) problems.Add("Flagpole must be to the right of the player start.");

            foreach (var s in level.Spawns)
            {
                if (s.Kind == SpawnKind.Pipe || s.Kind == SpawnKind.Flag) continue;
                if (level.IsSolidTile(s.Cell.x, s.Cell.y)) problems.Add($"{s} overlaps a solid tile.");
            }
            return problems;
        }

        /// <summary>Height of the solid stack starting at row 0 for each column, including pipes (0 = pit).</summary>
        public static int[] FloorSurface(LevelData level) => FloorSurface(BuildSolidGrid(level, new List<string>()), level.Width, level.Height);

        static int[] FloorSurface(bool[,] solid, int width, int height)
        {
            var surface = new int[width];
            for (int x = 0; x < width; x++)
            {
                int h = 0;
                while (h < height && solid[x, h]) h++;
                surface[x] = h;
            }
            return surface;
        }

        static bool[,] BuildSolidGrid(LevelData level, List<string> problems)
        {
            var solid = new bool[level.Width, level.Height];
            for (int x = 0; x < level.Width; x++)
            for (int y = 0; y < level.Height; y++)
                solid[x, y] = level.IsSolidTile(x, y);

            foreach (var s in level.Spawns)
            {
                if (s.Kind != SpawnKind.Pipe) continue;
                if (s.Size > MaxStepUp + 1) problems.Add($"Pipe at {s.Cell} is {s.Size} tall; too high to jump over.");
                for (int dy = 0; dy < s.Size; dy++)
                for (int dx = 0; dx < 2; dx++)
                {
                    int x = s.Cell.x + dx, y = s.Cell.y - dy;
                    if (x < level.Width && y >= 0) solid[x, y] = true;
                }
            }
            return solid;
        }
    }
}
