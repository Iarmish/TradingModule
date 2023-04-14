using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Data
{
    public class Test
    {
        public long Id { get; set; }
        public int ClientId { get; set; }
        public string Algorithm { get; set; } = "";
        public string Symbol { get; set; } = "";
        public string Interval { get; set; } = "";
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public string Deposit { get; set; } = "";
        public string StopLoss { get; set; } = "";
        public string TakeProfit { get; set; } = "";
        public string Offset { get; set; } = "";
        public string Comission { get; set; } = "";
        public bool IsVariabaleLot { get; set; }
        public bool IsSlPercent { get; set; }
        public bool IsTpPercent { get; set; }
        public bool IsOffsetPercent { get; set; }
        public bool BuyAllowed { get; set; }
        public bool SellAllowed { get; set; }
        public bool IsRevers { get; set; }
        public string TradeHours { get; set; } = "";
        public decimal Profit { get; set; }
        public decimal DrawDown { get; set; }
        public decimal ProfitFactor { get; set; }
        public decimal RecoveryFactor { get; set; }
    }
}
