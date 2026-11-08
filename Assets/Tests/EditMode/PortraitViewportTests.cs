using NUnit.Framework;

namespace CampusCrates.Tests
{
    public sealed class PortraitViewportTests
    {
        [Test]
        public void RearTileOverlapsFrontTileWithAVisibleBandAboveIt()
        {
            for (int column = 0; column < 3; column++)
            {
                var front = StackTileLayout.Bounds(column, 0);
                var behind = StackTileLayout.Bounds(column, 1);
                Assert.That(behind.Bottom, Is.LessThan(front.Top));
                Assert.That(behind.Top - front.Top, Is.GreaterThan(.04f));
                Assert.That(behind.Left, Is.LessThan(front.Right));
            }
        }

        [Test]
        public void OffsetStacksStayInsideTheTableAndClearOfEachOther()
        {
            Assert.That(StackTileLayout.Bounds(1, 0).Bottom, Is.GreaterThan(StackTileLayout.Bounds(0, 0).Bottom));
            for (int column = 0; column < 3; column++)
            for (int depth = 0; depth < 4; depth++)
            {
                var card = StackTileLayout.Bounds(column, depth);
                Assert.That(card.Bottom, Is.GreaterThan(.39f));
                Assert.That(card.Top, Is.LessThan(.77f));
                if (column < 2)
                    for (int otherDepth = 0; otherDepth < 4; otherDepth++)
                        Assert.That(card.Right, Is.LessThan(StackTileLayout.Bounds(column + 1, otherDepth).Left));
            }
        }

        [Test]
        public void TallPhoneUsesFullWidthAndKeepsContentInsideInsets()
        {
            var area = PortraitViewport.Anchors(390, 844, 0, 34, 390, 763);

            Assert.That(area.Left, Is.Zero);
            Assert.That(area.Right, Is.EqualTo(1f));
            Assert.That(area.Bottom * 844, Is.EqualTo(34f).Within(0.01f));
            Assert.That(area.Top * 844, Is.EqualTo(797f).Within(0.01f));
        }

        [Test]
        public void WidePreviewKeepsPortraitControlsCenteredAndFullHeight()
        {
            var area = PortraitViewport.Anchors(1600, 900, 0, 0, 1600, 900);

            Assert.That(area.Left * 1600, Is.EqualTo(494f).Within(0.01f));
            Assert.That(area.Right * 1600, Is.EqualTo(1106f).Within(0.01f));
            Assert.That(area.Bottom, Is.Zero);
            Assert.That(area.Top, Is.EqualTo(1f));
        }
    }
}
