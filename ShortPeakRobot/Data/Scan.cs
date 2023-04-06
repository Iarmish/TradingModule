using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Data
{
    public class Scan
    {
        public long Id { get; set; }
        public int ClientId { get; set; }
        public string Algorithm { get; set; }
        public string Stock { get; set; }
        public string TimeFrame { get; set; }
        public string Date1 { get; set; }
        public string Date2 { get; set; }
        public string Deposit { get; set; }
        public string Commission { get; set; }
        public string TradeHours { get; set; } = "";
        public bool FlagSell { get; set; }
        public bool FlagBuy { get; set; }
        public bool FlagReverse { get; set; }
        public bool VariableLot { get; set; }
        public bool IsSlPercent { get; set; }
        public bool IsTpPercent { get; set; }
        public string SL1 { get; set; }
        public string SL2 { get; set; }
        public string TP1 { get; set; }
        public string TP2 { get; set; }
        public string StepSl { get; set; }
        public string StepTp { get; set; }
    }
}
