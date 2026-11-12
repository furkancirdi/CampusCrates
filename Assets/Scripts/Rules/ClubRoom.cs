using System;
using System.Text;

namespace CampusCrates
{
    public readonly struct DecorItem
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Price;

        public DecorItem(string id, string name, int price)
        {
            Id = id;
            Name = name;
            Price = price;
        }
    }

    /// <summary>
    /// The student club room the player decorates with coins. Each spot is bought once,
    /// in any order; after that its style can be swapped for free.
    /// </summary>
    public sealed class ClubRoom
    {
        public const int Styles = 3;
        private const int NotOwned = -1;

        public static readonly DecorItem[] Catalog =
        {
            new DecorItem("rug", "Cozy Rug", 80),
            new DecorItem("pennants", "Club Pennants", 100),
            new DecorItem("chalkboard", "Chalkboard", 120),
            new DecorItem("plant", "Leafy Plant", 140),
            new DecorItem("lamp", "Reading Lamp", 160),
            new DecorItem("bookshelf", "Bookshelf", 200),
            new DecorItem("desk", "Study Desk", 240)
        };

        private readonly int[] styles = new int[Catalog.Length];

        public ClubRoom(string saved = null)
        {
            for (int index = 0; index < styles.Length; index++)
                styles[index] = NotOwned;
            if (string.IsNullOrEmpty(saved))
                return;

            string[] parts = saved.Split(',');
            for (int index = 0; index < styles.Length && index < parts.Length; index++)
                if (int.TryParse(parts[index], out int style) && style >= 0 && style < Styles)
                    styles[index] = style;
        }

        public int OwnedCount
        {
            get
            {
                int count = 0;
                foreach (int style in styles)
                    if (style != NotOwned)
                        count++;
                return count;
            }
        }

        public bool IsComplete => OwnedCount == Catalog.Length;

        public bool IsOwned(int item) => Valid(item) && styles[item] != NotOwned;

        public int StyleOf(int item) => Valid(item) ? styles[item] : NotOwned;

        public bool TryBuy(int item, int style, CoinWallet wallet)
        {
            if (!Valid(item) || IsOwned(item) || style < 0 || style >= Styles || wallet == null)
                return false;
            if (!wallet.TrySpend(Catalog[item].Price))
                return false;

            styles[item] = style;
            return true;
        }

        public bool SetStyle(int item, int style)
        {
            if (!IsOwned(item) || style < 0 || style >= Styles)
                return false;
            styles[item] = style;
            return true;
        }

        /// <summary>True when the wallet can pay for at least one spot that is still empty.</summary>
        public bool HasAffordable(CoinWallet wallet)
        {
            for (int index = 0; index < Catalog.Length; index++)
                if (!IsOwned(index) && wallet.CanAfford(Catalog[index].Price))
                    return true;
            return false;
        }

        public string Save()
        {
            var text = new StringBuilder();
            for (int index = 0; index < styles.Length; index++)
            {
                if (index > 0)
                    text.Append(',');
                text.Append(styles[index]);
            }
            return text.ToString();
        }

        private static bool Valid(int item) => item >= 0 && item < Catalog.Length;
    }
}
