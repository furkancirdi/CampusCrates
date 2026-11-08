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

        private readonly SupplyKind[][] columns;
        private readonly int[] fronts;
        private readonly List<SupplyKind> tray = new List<SupplyKind>(TrayCapacity);
        private readonly int totalItems;
        private int remainingItems;

        public IReadOnlyList<SupplyKind> Tray { get; }
        public int TotalCrates => totalItems / CrateSize;
        public int CratesPacked { get; private set; }
        public PackingStatus Status { get; private set; }

        public PackingBoard(SupplyKind[][] layout)
        {
            columns = Array.ConvertAll(layout, column => (SupplyKind[])column.Clone());
            fronts = new int[columns.Length];
            Tray = tray.AsReadOnly();
            foreach (var column in columns)
                totalItems += column.Length;
            Reset();
        }

        public int FrontIndex(int column) => fronts[column];

        public bool TrySelect(int column, int cardIndex)
        {
            if (Status != PackingStatus.Playing || column < 0 || column >= columns.Length ||
                cardIndex != fronts[column] || cardIndex >= columns[column].Length)
                return false;

            SupplyKind selected = columns[column][cardIndex];
            fronts[column]++;
            remainingItems--;
            tray.Add(selected);

            int matchingCards = 0;
            foreach (SupplyKind item in tray)
                if (item == selected)
                    matchingCards++;

            // Resolve the selection before checking whether the tray is full.
            if (matchingCards == CrateSize)
            {
                tray.RemoveAll(item => item == selected);
                CratesPacked++;
            }

            if (remainingItems == 0 && tray.Count == 0)
                Status = PackingStatus.Won;
            else if (tray.Count == TrayCapacity)
                Status = PackingStatus.Lost;

            return true;
        }

        public void Reset()
        {
            Array.Clear(fronts, 0, fronts.Length);
            tray.Clear();
            remainingItems = totalItems;
            CratesPacked = 0;
            Status = PackingStatus.Playing;
        }
    }
}
