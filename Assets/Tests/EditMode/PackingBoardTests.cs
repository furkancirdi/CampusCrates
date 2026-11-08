using NUnit.Framework;
using static CampusCrates.SupplyKind;

namespace CampusCrates.Tests
{
    public sealed class PackingBoardTests
    {
        [Test]
        public void ReplayingOldLevelsDoesNotRepeatDeliveriesOrDowngradePlay()
        {
            var progress = new CampusProgress(2);
            Assert.That(progress.UnlockedLevel, Is.EqualTo(3));
            Assert.That(progress.Deliveries, Is.EqualTo(2));
            Assert.That(progress.Complete(1), Is.False);
            Assert.That(progress.Complete(4), Is.False);
            Assert.That(progress.UnlockedLevel, Is.EqualTo(3));
            Assert.That(progress.Deliveries, Is.EqualTo(2));

            Assert.That(progress.Complete(3), Is.True);
            Assert.That(progress.Deliveries, Is.EqualTo(3));
            Assert.That(progress.Complete(4), Is.True);
            Assert.That(progress.Deliveries, Is.EqualTo(3));
            Assert.That(progress.UnlockedLevel, Is.EqualTo(5));
        }

        [Test]
        public void FinalCompletionKeepsTheLastLevelReplayable()
        {
            var progress = new CampusProgress(ShapeLevel.Last - 1);
            Assert.That(progress.Complete(ShapeLevel.Last), Is.True);
            Assert.That(progress.UnlockedLevel, Is.EqualTo(ShapeLevel.Last));
            Assert.That(progress.Complete(ShapeLevel.Last), Is.False);
        }

        [Test]
        public void HintRequiresTwoMatchingSuppliesAndAnUncoveredThird()
        {
            var board = new PackingBoard(OpeningLevel.Create(2));
            Play(board, 0);
            Assert.That(board.FindMatchHint(), Is.EqualTo(-1));
            Play(board, 1);
            Assert.That(board.FindMatchHint(), Is.EqualTo(-1));

            Play(board, 2);
            Assert.That(board.FindMatchHint(), Is.EqualTo(5));
            Assert.That(board.Tray, Is.EqualTo(new[] { Notebook, Notebook, Pencil }));
            Play(board, 2);
            Assert.That(board.FindMatchHint(), Is.EqualTo(-1));
        }

        [Test]
        public void HintFindsAMatchingTileInASpatialLevel()
        {
            var board = new PackingBoard(ShapeLevel.Create(ShapeLevel.First));
            PlayTiles(board, 5, 17);
            Assert.That(board.FindMatchHint(), Is.EqualTo(0));
        }

        [Test]
        public void PyramidCapReleasesFourTilesAndResetCoversThemAgain()
        {
            var level = ShapeLevel.Create(ShapeLevel.First);
            var board = new PackingBoard(level);
            int[] covered = { 1, 2, 3, 4 };
            foreach (int tile in covered)
                Assert.That(board.TrySelect(tile), Is.False);

            Assert.That(board.TrySelect(15), Is.True);
            foreach (int tile in covered)
                Assert.That(board.CanSelect(tile), Is.True);

            board.Reset();
            foreach (int tile in covered)
                Assert.That(board.CanSelect(tile), Is.False);
        }

        [Test]
        public void StarTileWaitsForAllCoversAndHidesTheExactOverlap()
        {
            var level = ShapeLevel.Create(ShapeLevel.Last);
            var board = new PackingBoard(level);
            Assert.That(board.IsPresent(0), Is.True);
            Assert.That(level[0].FullCover, Is.EqualTo(15));
            Assert.That(board.TrySelect(0), Is.False);
            Assert.That(board.TrySelect(15), Is.True);
            Assert.That(board.CanSelect(0), Is.True);

            Assert.That(board.TrySelect(20), Is.True);
            Assert.That(board.TrySelect(19), Is.True);
            Assert.That(board.TrySelect(8), Is.False);
            Assert.That(board.TrySelect(18), Is.True);
            Assert.That(board.CanSelect(8), Is.True);
        }

        [Test]
        public void BothSpatialLevelsCanBeSolved()
        {
            var pyramid = new PackingBoard(ShapeLevel.Create(ShapeLevel.First));
            PlayTiles(pyramid, 5, 17, 13, 16, 6, 15, 14, 9, 10, 0, 2, 1, 4, 3, 11, 12, 8, 7);
            Assert.That(pyramid.CratesPacked, Is.EqualTo(6));
            Assert.That(pyramid.Status, Is.EqualTo(PackingStatus.Won));

            var star = new PackingBoard(ShapeLevel.Create(ShapeLevel.Last));
            PlayTiles(star, 1, 20, 17, 6, 16, 14, 10, 18, 2, 19, 4, 8, 3, 12, 9, 7, 5, 11, 15, 0, 13);
            Assert.That(star.CratesPacked, Is.EqualTo(7));
            Assert.That(star.Status, Is.EqualTo(PackingStatus.Won));
        }

        private static void PlayTiles(PackingBoard board, params int[] tiles)
        {
            foreach (int tile in tiles)
                Assert.That(board.TrySelect(tile), Is.True, $"Tile {tile} should be selectable");
        }

        [Test]
        public void FirstLevelHasThreeOpenNotebooksAndResetRestoresTheFirstHint()
        {
            var layout = OpeningLevel.Create(1);
            var board = new PackingBoard(layout);

            Assert.That(board.TotalCrates, Is.EqualTo(1));
            for (int column = 0; column < 3; column++)
            {
                Assert.That(OpeningLevel.NotebookHint(layout, board), Is.EqualTo((column, false)));
                Play(board, column);
            }

            Assert.That(board.Status, Is.EqualTo(PackingStatus.Won));
            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.Null);

            board.Reset();
            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.EqualTo((0, false)));
        }

        [Test]
        public void SecondLevelNeedsAnUncoveringMoveAndEndsWithTwoCrates()
        {
            var board = new PackingBoard(OpeningLevel.Create(2));
            Play(board, 0, 1);
            Assert.That(board.TrySelect(2, 1), Is.False);

            Play(board, 2, 2);
            Assert.That(board.Tray, Is.EqualTo(new[] { Pencil }));
            Assert.That(board.CratesPacked, Is.EqualTo(1));

            Play(board, 0, 1);
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Won));
            Assert.That(board.CratesPacked, Is.EqualTo(2));
        }

        [Test]
        public void ThirdLevelIntroducesCupsWithThreeCrates()
        {
            var board = new PackingBoard(OpeningLevel.Create(3));
            Assert.That(board.TotalCrates, Is.EqualTo(3));
            Play(board, 0, 1, 2, 2);
            Assert.That(board.Tray, Is.EqualTo(new[] { Pencil }));

            Play(board, 0, 1, 1, 0, 2);
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Won));
            Assert.That(board.CratesPacked, Is.EqualTo(3));
        }

        [Test]
        public void FourthLevelNeedsAnUncoveringMoveAndCanBeSolved()
        {
            var board = new PackingBoard(OpeningLevel.Create(4));
            Play(board, 0, 1, 2);
            Assert.That(board.Tray, Is.EqualTo(new[] { Notebook, Notebook, Pencil }));
            Assert.That(board.CratesPacked, Is.Zero);

            Play(board, 2);
            Assert.That(board.Tray, Is.EqualTo(new[] { Pencil }));
            Assert.That(board.CratesPacked, Is.EqualTo(1));

            Play(board, 0, 1, 1, 1, 0, 2, 0, 2);
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Won));
            Assert.That(board.CratesPacked, Is.EqualTo(4));
        }

        [Test]
        public void FourthLevelCanFillTheTrayByClearingStacksWithoutPlanning()
        {
            var board = new PackingBoard(OpeningLevel.Create(4));
            Play(board, 0, 0, 0, 0, 1, 1);
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Playing));

            Play(board, 1);
            Assert.That(board.Tray.Count, Is.EqualTo(7));
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Lost));
        }

        [Test]
        public void NotebookHintShowsTheCoveringTileAndStopsAtTheFirstCrate()
        {
            var layout = OpeningLevel.Create(4);
            var board = new PackingBoard(layout);

            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.EqualTo((0, false)));
            Play(board, 0);
            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.EqualTo((1, false)));
            Play(board, 1);
            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.EqualTo((2, true)));
            Play(board, 2);
            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.EqualTo((2, false)));
            Play(board, 2);
            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.Null);
        }

        [Test]
        public void NotebookHintFollowsAnAlternativeOpening()
        {
            var layout = OpeningLevel.Create(4);
            var board = new PackingBoard(layout);
            Play(board, 2, 2, 0);

            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.EqualTo((1, false)));
            Play(board, 1);
            Assert.That(OpeningLevel.NotebookHint(layout, board), Is.Null);
        }

        private static void Play(PackingBoard board, params int[] columns)
        {
            foreach (int column in columns)
                Assert.That(board.TrySelect(column, board.FrontIndex(column)), Is.True);
        }

        [Test]
        public void OnlyTheCurrentFrontCardCanEnterTheTray()
        {
            var board = new PackingBoard(new[]
            {
                new[] { Notebook, Pencil },
                new[] { Notebook, Pencil },
                new[] { Notebook, Pencil }
            });

            Assert.That(board.TrySelect(0, 1), Is.False);
            Assert.That(board.TrySelect(0, 0), Is.True);
            Assert.That(board.TrySelect(0, 0), Is.False);
            Assert.That(board.FrontIndex(0), Is.EqualTo(1));
            Assert.That(board.Tray, Is.EqualTo(new[] { Notebook }));
        }

        [Test]
        public void SeventhCardClearsANonAdjacentTripleBeforeCapacityCheck()
        {
            var board = new PackingBoard(new[]
            {
                new[] { Notebook, Pencil, Cup },
                new[] { Notebook, Badge, Pencil },
                new[] { Notebook, Pencil, Cup, Cup, Badge, Badge }
            });

            for (int column = 0; column < 2; column++)
                for (int card = 0; card < 3; card++)
                    Assert.That(board.TrySelect(column, card), Is.True);

            Assert.That(board.Tray.Count, Is.EqualTo(6));
            Assert.That(board.TrySelect(2, 0), Is.True);
            Assert.That(board.Tray, Is.EqualTo(new[] { Pencil, Cup, Badge, Pencil }));
            Assert.That(board.CratesPacked, Is.EqualTo(1));
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Playing));
        }

        [Test]
        public void SevenUnmatchedCardsEndTheAttemptAndResetRestoresIt()
        {
            var board = new PackingBoard(new[]
            {
                new[] { Notebook, Pencil, Cup },
                new[] { Notebook, Pencil, Cup },
                new[] { Badge, Notebook, Pencil, Cup, Badge, Badge }
            });

            for (int column = 0; column < 2; column++)
                for (int card = 0; card < 3; card++)
                    Assert.That(board.TrySelect(column, card), Is.True);

            Assert.That(board.TrySelect(2, 0), Is.True);
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Lost));
            Assert.That(board.TrySelect(2, 1), Is.False);
            Assert.That(board.Tray.Count, Is.EqualTo(7));

            board.Reset();

            Assert.That(board.Status, Is.EqualTo(PackingStatus.Playing));
            Assert.That(board.Tray, Is.Empty);
            Assert.That(board.TrySelect(0, 0), Is.True);
        }

        [Test]
        public void PackingTheLastTripleCompletesTheLevel()
        {
            var board = new PackingBoard(new[]
            {
                new[] { Notebook, Pencil },
                new[] { Notebook, Pencil },
                new[] { Notebook, Pencil }
            });

            for (int column = 0; column < 3; column++)
                Assert.That(board.TrySelect(column, 0), Is.True);

            Assert.That(board.Status, Is.EqualTo(PackingStatus.Playing));

            for (int column = 0; column < 3; column++)
                Assert.That(board.TrySelect(column, 1), Is.True);

            Assert.That(board.Status, Is.EqualTo(PackingStatus.Won));
            Assert.That(board.Tray, Is.Empty);
            Assert.That(board.CratesPacked, Is.EqualTo(2));
            board.Reset();
            Assert.That(board.CratesPacked, Is.Zero);
        }
    }
}
