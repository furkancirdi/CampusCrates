using System;
using System.Collections.Generic;
using static CampusCrates.SupplyKind;

namespace CampusCrates
{
    public sealed class ShapeTile
    {
        public SupplyKind Kind { get; }
        public float X { get; }
        public float Y { get; }
        public int Layer { get; }
        public IReadOnlyList<int> Covers => covers;
        public int FullCover { get; private set; } = -1;

        private readonly List<int> covers = new List<int>();

        internal ShapeTile(SupplyKind kind, float x, float y, int layer)
        {
            Kind = kind;
            X = x;
            Y = y;
            Layer = layer;
        }

        internal void AddCover(int index, bool fullyCovered)
        {
            covers.Add(index);
            if (fullyCovered && FullCover < 0)
                FullCover = index;
        }
    }

    public static class ShapeLevel
    {
        public const int First = OpeningLevel.Count + 1;
        public const int StackUp = First;
        public const int Pyramid = First + 1;
        public const int HiddenOne = First + 2;
        public const int DoubleDeck = First + 3;
        public const int Star = First + 4;
        public const int Last = Star;
        public const float TileSize = .155f;
        public const float Pitch = TileSize * 1.06f;

        public static ShapeTile[] Create(int level)
        {
            var tiles = new List<ShapeTile>();
            switch (level)
            {
                // First overlapping layout: a small block where three caps hold down the grid.
                case StackUp:
                    Row(tiles, 1, -1, Notebook, Pencil, Pencil);
                    Row(tiles, 0, -1, Notebook, Cup, Pencil);
                    Row(tiles, -1, -1, Cup, Notebook, Badge);
                    tiles.Add(new ShapeTile(Cup, -.5f, .5f, 1));
                    tiles.Add(new ShapeTile(Badge, .5f, -.5f, 1));
                    tiles.Add(new ShapeTile(Badge, -.5f, -.5f, 1));
                    break;
                case Pyramid:
                    Row(tiles, 2, 0, Pencil);
                    Row(tiles, 1, -.5f, Pencil, Pencil);
                    Row(tiles, 0, -1, Notebook, Notebook, Pencil);
                    Row(tiles, -1, -1.5f, Notebook, Cup, Cup, Badge);
                    Row(tiles, -2, -2, Badge, Notebook, Cup, Pencil, Badge);
                    tiles.Add(new ShapeTile(Notebook, -.25f, .5f, 1));
                    tiles.Add(new ShapeTile(Notebook, -1.25f, -1.5f, 1));
                    tiles.Add(new ShapeTile(Pencil, 1.25f, -1.5f, 1));
                    break;
                // Three layers, and the first tile that is completely hidden under another.
                case HiddenOne:
                    Row(tiles, 1, -1.5f, Notebook, Pencil, Cup, Notebook);
                    Row(tiles, 0, -1.5f, Cup, Badge, Pencil, Pencil);
                    Row(tiles, -1, -1.5f, Notebook, Notebook, Badge, Notebook);
                    tiles.Add(new ShapeTile(Badge, -1, .5f, 1));
                    tiles.Add(new ShapeTile(Cup, 1, .5f, 1));
                    tiles.Add(new ShapeTile(Notebook, -1, -.5f, 1));
                    tiles.Add(new ShapeTile(Pencil, 1, -.5f, 1));
                    tiles.Add(new ShapeTile(Pencil, 0, 0, 2));
                    tiles.Add(new ShapeTile(Pencil, 1, -.5f, 2));
                    break;
                // Same deck with two side tiles and two hidden ones.
                case DoubleDeck:
                    tiles.Add(new ShapeTile(Cup, -2.5f, 0, 0));
                    tiles.Add(new ShapeTile(Pencil, 2.5f, 0, 0));
                    Row(tiles, 1, -1.5f, Notebook, Badge, Notebook, Notebook);
                    Row(tiles, 0, -1.5f, Cup, Cup, Notebook, Notebook);
                    Row(tiles, -1, -1.5f, Pencil, Pencil, Cup, Cup);
                    tiles.Add(new ShapeTile(Pencil, -1, .5f, 1));
                    tiles.Add(new ShapeTile(Cup, 1, .5f, 1));
                    tiles.Add(new ShapeTile(Badge, -1, -.5f, 1));
                    tiles.Add(new ShapeTile(Notebook, 1, -.5f, 1));
                    tiles.Add(new ShapeTile(Badge, 0, 0, 2));
                    tiles.Add(new ShapeTile(Pencil, -1, .5f, 2));
                    tiles.Add(new ShapeTile(Pencil, 1, -.5f, 2));
                    break;
                case Star:
                    Row(tiles, 2.5f, 0, Pencil);
                    Row(tiles, 1.5f, 0, Notebook);
                    Row(tiles, .5f, -2, Cup, Notebook, Cup, Pencil, Notebook);
                    Row(tiles, -.5f, -1, Pencil, Badge, Notebook);
                    Row(tiles, -1.5f, -1, Badge, Pencil, Notebook);
                    tiles.Add(new ShapeTile(Pencil, -1.5f, -2.5f, 0));
                    tiles.Add(new ShapeTile(Cup, 1.5f, -2.5f, 0));
                    tiles.Add(new ShapeTile(Pencil, 0, 2.5f, 1));
                    tiles.Add(new ShapeTile(Cup, -2, .5f, 1));
                    tiles.Add(new ShapeTile(Cup, 2, .5f, 1));
                    tiles.Add(new ShapeTile(Badge, -.5f, 0, 1));
                    tiles.Add(new ShapeTile(Cup, .5f, -1, 1));
                    tiles.Add(new ShapeTile(Notebook, .5f, -1, 2));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level));
            }

            for (int lower = 0; lower < tiles.Count; lower++)
            for (int upper = lower + 1; upper < tiles.Count; upper++)
            {
                ShapeTile bottom = tiles[lower];
                ShapeTile top = tiles[upper];
                float dx = Math.Abs(bottom.X - top.X) * Pitch;
                float dy = Math.Abs(bottom.Y - top.Y) * Pitch;
                if (bottom.Layer < top.Layer && dx < TileSize && dy < TileSize)
                    bottom.AddCover(upper, dx == 0f && dy == 0f);
            }

            return tiles.ToArray();
        }

        private static void Row(List<ShapeTile> tiles, float y, float left, params SupplyKind[] kinds)
        {
            for (int index = 0; index < kinds.Length; index++)
                tiles.Add(new ShapeTile(kinds[index], left + index, y, 0));
        }
    }
}
