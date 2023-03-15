using System;

namespace ShortPeakRobot.Data
{
    public class RobotLog
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public long RobotId { get; set; } = -1;
        public long ClientId { get; set; } = -1;
        public int Type { get; set; }
        public string Message { get; set; }
    }
}
