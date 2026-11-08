using static CampusCrates.SupplyKind;

namespace CampusCrates
{
    public static class OpeningLevel
    {
        public static SupplyKind[][] Create() => new[]
        {
            new[] { Notebook, Pencil, Cup, Badge },
            new[] { Notebook, Cup, Badge, Pencil },
            new[] { Pencil, Notebook, Badge, Cup }
        };
    }
}
