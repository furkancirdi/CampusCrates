using NUnit.Framework;
using static CampusCrates.SupplyKind;

namespace CampusCrates.Tests
{
    public sealed class PackingBoardTests
    {
        [Test]
        public void OpeningLevelAsksForOneUncoveringMoveBeforeTheFirstTripleAndCanBeSolved()
        {
            var board = new PackingBoard(OpeningLevel.Create());
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
        public void OpeningLevelCanFillTheTrayByClearingStacksWithoutPlanning()
        {
            var board = new PackingBoard(OpeningLevel.Create());
            Play(board, 0, 0, 0, 0, 1, 1);
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Playing));

            Play(board, 1);
            Assert.That(board.Tray.Count, Is.EqualTo(7));
            Assert.That(board.Status, Is.EqualTo(PackingStatus.Lost));
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
