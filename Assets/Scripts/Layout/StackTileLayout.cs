namespace CampusCrates
{
    public static class StackTileLayout
    {
        private static readonly float[] ColumnX = { .065f, .377f, .690f };
        private static readonly float[] FrontBottom = { .408f, .454f, .421f };
        private static readonly float[] Drift = { .013f, -.003f, -.012f };

        public static (float Left, float Bottom, float Right, float Top) Bounds(int column, int depth)
        {
            float left = ColumnX[column] + depth * Drift[column];
            float bottom = FrontBottom[column] + depth * .054f;
            return (left, bottom, left + .245f, bottom + .128f);
        }
    }
}
