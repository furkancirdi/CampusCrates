using NUnit.Framework;

namespace CampusCrates.Tests
{
    public sealed class MetaProgressTests
    {
        [Test]
        public void FirstClearsPayMoreThanReplaysAndGrowWithLevel()
        {
            Assert.That(CoinWallet.RewardFor(1, true), Is.EqualTo(80));
            Assert.That(CoinWallet.RewardFor(6, true), Is.GreaterThan(CoinWallet.RewardFor(1, true)));
            Assert.That(CoinWallet.RewardFor(6, false), Is.EqualTo(CoinWallet.ReplayReward));
        }

        [Test]
        public void WalletNeverGoesNegative()
        {
            var wallet = new CoinWallet(50);
            Assert.That(wallet.TrySpend(80), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(50));
            wallet.Earn(-30);
            Assert.That(wallet.Balance, Is.EqualTo(50));
            Assert.That(wallet.TrySpend(50), Is.True);
            Assert.That(wallet.Balance, Is.Zero);
        }

        [Test]
        public void BuyingSpendsOnceAndRestylingIsFree()
        {
            var wallet = new CoinWallet(ClubRoom.Catalog[0].Price + 10);
            var room = new ClubRoom();
            Assert.That(room.SetStyle(0, 1), Is.False);
            Assert.That(room.TryBuy(0, 2, wallet), Is.True);
            Assert.That(wallet.Balance, Is.EqualTo(10));
            Assert.That(room.TryBuy(0, 1, wallet), Is.False);
            Assert.That(room.SetStyle(0, 1), Is.True);
            Assert.That(room.StyleOf(0), Is.EqualTo(1));
            Assert.That(wallet.Balance, Is.EqualTo(10));
        }

        [Test]
        public void UnaffordableSpotStaysEmpty()
        {
            var wallet = new CoinWallet(10);
            var room = new ClubRoom();
            Assert.That(room.HasAffordable(wallet), Is.False);
            Assert.That(room.TryBuy(ClubRoom.Catalog.Length - 1, 0, wallet), Is.False);
            Assert.That(room.OwnedCount, Is.Zero);
            Assert.That(wallet.Balance, Is.EqualTo(10));
        }

        [Test]
        public void SavedRoomRestoresStylesAndIgnoresBadData()
        {
            var wallet = new CoinWallet(10000);
            var room = new ClubRoom();
            room.TryBuy(1, 2, wallet);
            room.TryBuy(4, 0, wallet);
            var restored = new ClubRoom(room.Save());
            Assert.That(restored.StyleOf(1), Is.EqualTo(2));
            Assert.That(restored.StyleOf(4), Is.EqualTo(0));
            Assert.That(restored.OwnedCount, Is.EqualTo(2));

            var broken = new ClubRoom("9,x,1");
            Assert.That(broken.IsOwned(0), Is.False);
            Assert.That(broken.IsOwned(1), Is.False);
            Assert.That(broken.StyleOf(2), Is.EqualTo(1));
        }

        [Test]
        public void AFullFirstRunAffordsMostOfTheRoom()
        {
            int earned = CoinWallet.StartingGift;
            for (int level = 1; level <= ShapeLevel.Last; level++)
                earned += CoinWallet.RewardFor(level, true);

            int total = 0;
            foreach (DecorItem item in ClubRoom.Catalog)
                total += item.Price;
            // The demo should feel rewarding but leave a reason to replay.
            Assert.That(earned, Is.LessThan(total));
            Assert.That(earned, Is.GreaterThan(total - ClubRoom.Catalog[ClubRoom.Catalog.Length - 1].Price - 1));
        }
    }
}
