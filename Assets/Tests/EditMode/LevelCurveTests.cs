using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CampusCrates.Tests
{
    public sealed class LevelCurveTests
    {
        private static PackingBoard Board(int level) => level < ShapeLevel.First
            ? new PackingBoard(OpeningLevel.Create(level))
            : new PackingBoard(ShapeLevel.Create(level));

        private static SupplyKind[] Kinds(int level)
        {
            var kinds = new List<SupplyKind>();
            if (level < ShapeLevel.First)
                foreach (SupplyKind[] column in OpeningLevel.Create(level))
                    kinds.AddRange(column);
            else
                foreach (ShapeTile tile in ShapeLevel.Create(level))
                    kinds.Add(tile.Kind);
            return kinds.ToArray();
        }

        private static int[][] Covers(int level)
        {
            var covers = new List<int[]>();
            if (level < ShapeLevel.First)
            {
                int tile = 0;
                foreach (SupplyKind[] column in OpeningLevel.Create(level))
                    for (int index = 0; index < column.Length; index++, tile++)
                        covers.Add(index == 0 ? Array.Empty<int>() : new[] { tile - 1 });
            }
            else
            {
                foreach (ShapeTile tile in ShapeLevel.Create(level))
                    covers.Add(new List<int>(tile.Covers).ToArray());
            }
            return covers.ToArray();
        }

        // Share of games won by a player who taps any open tile at random.
        private static double RandomPlayWinRate(int level, int games)
        {
            var random = new Random(level * 7919);
            PackingBoard board = Board(level);
            var open = new List<int>();
            int wins = 0;
            for (int game = 0; game < games; game++)
            {
                board.Reset();
                while (board.Status == PackingStatus.Playing)
                {
                    open.Clear();
                    for (int tile = 0; tile < board.TileCount; tile++)
                        if (board.CanSelect(tile))
                            open.Add(tile);
                    board.TrySelect(open[random.Next(open.Count)]);
                }
                if (board.Status == PackingStatus.Won)
                    wins++;
            }
            return (double)wins / games;
        }

        private static bool CanBeSolved(SupplyKind[] kinds, int[][] covers)
        {
            int all = (1 << kinds.Length) - 1;
            var dead = new HashSet<int>();
            return Search(0);

            bool Search(int taken)
            {
                if (taken == all)
                    return true;
                if (dead.Contains(taken))
                    return false;

                for (int tile = 0; tile < kinds.Length; tile++)
                {
                    if ((taken & (1 << tile)) != 0 || !Open(tile, taken))
                        continue;
                    int next = taken | (1 << tile);
                    if (TraySize(next) < PackingBoard.TrayCapacity && Search(next))
                        return true;
                }

                dead.Add(taken);
                return false;
            }

            bool Open(int tile, int taken)
            {
                foreach (int cover in covers[tile])
                    if ((taken & (1 << cover)) == 0)
                        return false;
                return true;
            }

            // Triples leave the tray, so only the remainder of each kind stays in it.
            int TraySize(int taken)
            {
                var counts = new int[4];
                for (int tile = 0; tile < kinds.Length; tile++)
                    if ((taken & (1 << tile)) != 0)
                        counts[(int)kinds[tile]]++;
                int size = 0;
                foreach (int count in counts)
                    size += count % 3;
                return size;
            }
        }

        [Test]
        public void EveryLevelPacksIntoFullCratesAndCanBeSolved()
        {
            for (int level = 1; level <= ShapeLevel.Last; level++)
            {
                SupplyKind[] kinds = Kinds(level);
                var counts = new int[4];
                foreach (SupplyKind kind in kinds)
                    counts[(int)kind]++;
                foreach (int count in counts)
                    Assert.That(count % 3, Is.Zero, $"Level {level} leaves a supply without a full crate");

                Assert.That(CanBeSolved(kinds, Covers(level)), Is.True, $"Level {level} has no winning order");
            }
        }

        [Test]
        public void EachLevelIsHarderForCarelessPlayThanTheOneBefore()
        {
            double previous = 1.0;
            for (int level = 1; level <= ShapeLevel.Last; level++)
            {
                double rate = RandomPlayWinRate(level, 3000);
                Assert.That(rate, Is.LessThanOrEqualTo(previous), $"Level {level} is easier than level {level - 1}");
                // No step should cut the careless win rate by more than about a fifth of all games.
                Assert.That(previous - rate, Is.LessThan(.2), $"Level {level} is a difficulty spike");
                previous = rate;
            }

            Assert.That(previous, Is.GreaterThan(.15), "The last level should still be winnable by luck now and then");
        }

        [Test]
        public void TheTrayCannotFillBeforeBadgeDay()
        {
            for (int level = 1; level < 4; level++)
                Assert.That(RandomPlayWinRate(level, 500), Is.EqualTo(1.0), $"Level {level} can be lost");
            Assert.That(RandomPlayWinRate(4, 3000), Is.LessThan(1.0));
        }

        [Test]
        public void HiddenTilesArriveOneStepAtATime()
        {
            Assert.That(HiddenTiles(ShapeLevel.StackUp), Is.Zero);
            Assert.That(HiddenTiles(ShapeLevel.Pyramid), Is.Zero);
            Assert.That(HiddenTiles(ShapeLevel.HiddenOne), Is.EqualTo(1));
            Assert.That(HiddenTiles(ShapeLevel.DoubleDeck), Is.EqualTo(2));
            Assert.That(HiddenTiles(ShapeLevel.Star), Is.GreaterThan(2));
        }

        [Test]
        public void StackUpStartsWithItsThreeCapsAndOneCornerOpen()
        {
            PackingBoard board = Board(ShapeLevel.StackUp);
            int open = 0;
            for (int tile = 0; tile < board.TileCount; tile++)
                if (board.CanSelect(tile))
                    open++;

            Assert.That(board.TileCount, Is.EqualTo(12));
            Assert.That(open, Is.EqualTo(4));
        }

        private static int HiddenTiles(int level)
        {
            int hidden = 0;
            foreach (ShapeTile tile in ShapeLevel.Create(level))
                if (tile.FullCover >= 0)
                    hidden++;
            return hidden;
        }
    }
}
