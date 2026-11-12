using System;

namespace CampusCrates
{
    /// <summary>Soft currency earned by packing levels and spent on club room decor.</summary>
    public sealed class CoinWallet
    {
        public const int StartingGift = 100;
        public const int ReplayReward = 25;

        public int Balance { get; private set; }

        public CoinWallet(int balance)
        {
            Balance = Math.Max(0, balance);
        }

        /// <summary>First clears pay more for later levels; replays pay a small steady amount.</summary>
        public static int RewardFor(int level, bool firstClear)
            => firstClear ? 60 + 20 * Math.Max(1, level) : ReplayReward;

        public void Earn(int amount)
        {
            if (amount > 0)
                Balance += amount;
        }

        public bool CanAfford(int price) => price >= 0 && price <= Balance;

        public bool TrySpend(int price)
        {
            if (!CanAfford(price))
                return false;
            Balance -= price;
            return true;
        }
    }
}
