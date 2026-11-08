using System;
using System.Collections.Generic;

namespace CampusCrates
{
    public enum SupplyKind { Notebook, Pencil, Cup, Badge }
    public enum PackingStatus { Playing, Won, Lost }

    public sealed class PackingBoard
    {
        public const int TrayCapacity = 7;
        private const int CrateSize = 3;

        private readonly SupplyKind[] tiles;
        private readonly IReadOnlyList<int>[] covers;
        private readonly bool[] selected;
        private readonly int[] columnOffsets = Array.Empty<int>();
        private readonly List<SupplyKind> tray = new List<SupplyKind>(TrayCapacity);
        private int remainingItems;

        public IReadOnlyList<SupplyKind> Tray { get; }
        public int TileCount => tiles.Length;
        public int TotalCrates => tiles.Length / CrateSize;
        public int CratesPacked { get; private set; }
        public PackingStatus Status { get; private set; }

        public PackingBoard(SupplyKind[][] layout)
        {
            columnOffsets = new int[layout.Length + 1];
            for (int column = 0; column < layout.Length; column++)
                columnOffsets[column + 1] = columnOffsets[column] + layout[column].Length;

            tiles = new SupplyKind[columnOffsets[layout.Length]];
            covers = new IReadOnlyList<int>[tiles.Length];
            selected = new bool[tiles.Length];
            for (int column = 0; column < layout.Length; column++)
            for (int index = 0; index < layout[column].Length; index++)
            {
                int tile = columnOffsets[column] + index;
                tiles[tile] = layout[column][index];
                covers[tile] = index == 0 ? Array.Empty<int>() : new[] { tile - 1 };
            }

            Tray = tray.AsReadOnly();
            Reset();
        }

        public PackingBoard(ShapeTile[] layout)
        {
            tiles = Array.ConvertAll(layout, tile => tile.Kind);
            covers = Array.ConvertAll(layout, tile => tile.Covers);
            selected = new bool[layout.Length];
            Tray = tray.AsReadOnly();
            Reset();
        }

        public int FrontIndex(int column)
        {
            int start = columnOffsets[column];
            int index = start;
            while (index < columnOffsets[column + 1] && selected[index])
                index++;
            return index - start;
        }

        public bool TrySelect(int column, int cardIndex)
        {
            if (column < 0 || column >= columnOffsets.Length - 1 || cardIndex < 0 ||
                cardIndex >= columnOffsets[column + 1] - columnOffsets[column])
                return false;
            return TrySelect(columnOffsets[column] + cardIndex);
        }

        public bool IsPresent(int tile) => tile >= 0 && tile < selected.Length && !selected[tile];

        public bool CanSelect(int tile)
        {
            if (Status != PackingStatus.Playing || !IsPresent(tile))
                return false;

            foreach (int cover in covers[tile])
                if (!selected[cover])
                    return false;
            return true;
        }

        public bool TrySelect(int tile)
        {
            if (!CanSelect(tile))
                return false;

            SupplyKind kind = tiles[tile];
            selected[tile] = true;
            remainingItems--;
            tray.Add(kind);

            // Resolve the selection before checking whether the tray is full.
            if (CountInTray(kind) == CrateSize)
            {
                tray.RemoveAll(item => item == kind);
                CratesPacked++;
            }

            if (remainingItems == 0 && tray.Count == 0)
                Status = PackingStatus.Won;
            else if (tray.Count == TrayCapacity)
                Status = PackingStatus.Lost;

            return true;
        }

        public int FindMatchHint()
        {
            for (int tile = 0; tile < tiles.Length; tile++)
                if (CanSelect(tile) && CountInTray(tiles[tile]) == CrateSize - 1)
                    return tile;

            return -1;
        }

        private int CountInTray(SupplyKind kind)
        {
            int count = 0;
            foreach (SupplyKind item in tray)
                if (item == kind)
                    count++;
            return count;
        }

        public void Reset()
        {
            Array.Clear(selected, 0, selected.Length);
            tray.Clear();
            remainingItems = tiles.Length;
            CratesPacked = 0;
            Status = PackingStatus.Playing;
        }
    }
}
