using System.Collections.Generic;

namespace SuperOttie.Level
{
    /// <summary>
    /// Static sanity checks that catch unbeatable or broken layouts before anyone plays them.
    /// Conservative: it checks the floor route only (pits and steps), not every optional platform.
    /// </summary>
    public static class LevelLint
    {
        /// <summary>Widest pit (in cells) a running jump clears with a comfortable margin.</summary>
        public const int MaxPitWidth = 5;

        /// <summary>Tallest single step up (in cells) reachable with a full jump.</summary>
        public const int MaxStepUp = 4;

        public static List<string> Validate(LevelData level)
        {
            var problems = new List<string>();
            var solid = BuildSolidGrid(level, problems);

            // Floor surface per column: height of the solid stack that starts at row 0 (0 = pit).
            var surface = new int[level.Width];
            for (int x = 0; x < level.Width; x++)
            {
                int h = 0;
                while (h < level.Height && solid[x, h]) h++;
                surface[x] = h;
            }

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
