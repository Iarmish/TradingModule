﻿using System;

namespace ShortPeakRobot.Data
{
    public class RobotOrder
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long ClientId { get; set; }
        public int RobotId { get; set; }
        public long StartDealOrderId { get; set; }
        public decimal StartDeposit { get; set; }
        public string Symbol { get; set; }
        public int Side { get; set; }
        public int Type { get; set; } = -1; //FuturesOrderType
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal PriceLastFilledTrade { get; set; }
        public decimal? StopPrice { get; set; }
        public int Status { get; set; } = -1;
        public string? Description { get; set; }
        public DateTime PlacedTime { get; set; }
    }




}
