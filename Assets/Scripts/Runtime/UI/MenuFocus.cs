namespace SuperOttie.UI
{
    /// <summary>
    /// Keyboard and gamepad focus over a list or grid of menu items. The highlight stays hidden until a
    /// direction is pressed, so touch players never see a stray selection ring.
    /// </summary>
    public sealed class MenuFocus
    {
        public int Count { get; }
        public int Columns { get; }
        public int Index { get; private set; }
        public bool Visible { get; private set; }

        public MenuFocus(int count, int columns = 1)
        {
            Count = count < 1 ? 1 : count;
            Columns = columns < 1 ? 1 : columns;
        }

        public void Reset(int index, bool visible = false)
        {
            Index = index < 0 ? 0 : index >= Count ? Count - 1 : index;
            Visible = visible;
        }

        public void Hide() => Visible = false;

        /// <summary>
        /// dx: -1 left, +1 right. dy: -1 up, +1 down. The first press only reveals the highlight.
        /// Returns true when the highlight appeared or moved.
        /// </summary>
        public bool Move(int dx, int dy)
        {
            if (!Visible)
            {
                Visible = true;
                return true;
            }
            int rows = (Count + Columns - 1) / Columns;
            int col = Index % Columns + dx;
            int row = Index / Columns + dy;
            if (col < 0 || col >= Columns || row < 0 || row >= rows) return false;
            int next = row * Columns + col;
            if (next >= Count) next = Count - 1;
            if (next == Index) return false;
            Index = next;
            return true;
        }
    }
}
