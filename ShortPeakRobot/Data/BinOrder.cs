using System;

namespace ShortPeakRobot.Data
{
    public class BinOrder
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long ClientId { get; set; }
        public int RobotId { get; set; }
        public int Side { get; set; }
        public int PositionSide { get; set; }
        public int Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal AvgPrice { get; set; }
        public decimal Price { get; set; }
        public decimal? StopPrice { get; set; }
        public bool ClosePosition { get; set; }
        public int Status { get; set; } = -1;
        public decimal? ActivationPrice { get; set; }
        public int WorkingType { get; set; }
        public string Symbol { get; set; }
        public DateTime UpdateTime { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.UtcNow;

        
    }
}
