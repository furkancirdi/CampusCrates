using System;
using static CampusCrates.SupplyKind;

namespace CampusCrates
{
    public static class OpeningLevel
    {
        public const int Count = 5;

        public static SupplyKind[][] Create(int level) => level switch
        {
            1 => new[]
            {
                new[] { Notebook },
                new[] { Notebook },
                new[] { Notebook }
            },
            2 => new[]
            {
                new[] { Notebook, Pencil },
                new[] { Notebook, Pencil },
                new[] { Pencil, Notebook }
            },
            3 => new[]
            {
                new[] { Notebook, Pencil, Cup },
                new[] { Notebook, Cup, Pencil },
                new[] { Pencil, Notebook, Cup }
            },
            // Badges arrive in a forgiving order: most careless routes still finish.
            4 => new[]
            {
                new[] { Cup, Badge, Badge, Notebook },
                new[] { Badge, Pencil, Pencil, Notebook },
                new[] { Pencil, Cup, Notebook, Cup }
            },
            5 => new[]
            {
                new[] { Notebook, Pencil, Cup, Badge },
                new[] { Notebook, Cup, Badge, Pencil },
                new[] { Pencil, Notebook, Badge, Cup }
            },
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };

        public static (int Column, bool IsCovered)? NotebookHint(SupplyKind[][] layout, PackingBoard board)
        {
            if (board.CratesPacked > 0 || board.Status != PackingStatus.Playing)
                return null;

            int nearestColumn = -1;
            int nearestDepth = int.MaxValue;
            for (int column = 0; column < layout.Length; column++)
            {
                int front = board.FrontIndex(column);
                int notebook = Array.IndexOf(layout[column], Notebook, front);
                if (notebook < 0 || notebook - front >= nearestDepth)
                    continue;

                nearestColumn = column;
                nearestDepth = notebook - front;
            }

            return nearestColumn < 0 ? null : (nearestColumn, nearestDepth > 0);
        }
    }
}
