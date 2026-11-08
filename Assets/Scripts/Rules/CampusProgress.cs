using System;

namespace CampusCrates
{
    public sealed class CampusProgress
    {
        public const int DeliveriesNeeded = 3;
        public int HighestCompleted { get; private set; }
        public int UnlockedLevel => Math.Min(HighestCompleted + 1, ShapeLevel.Last);
        public int Deliveries => Math.Min(HighestCompleted, DeliveriesNeeded);

        public CampusProgress(int completed)
        {
            HighestCompleted = Math.Max(0, Math.Min(completed, ShapeLevel.Last));
        }

        public bool Complete(int level)
        {
            if (level != HighestCompleted + 1 || level > ShapeLevel.Last)
                return false;

            HighestCompleted = level;
            return true;
        }
    }
}
