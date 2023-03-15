using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Models
{
    public class ViewSetting : BaseVM
    {

        private double _TimeFrame;
        public double TimeFrame
        {
            get { return _TimeFrame; }
            set
            {
                if (_TimeFrame != value)
                {
                    _TimeFrame = value;
                    OnPropertyChanged("price");
                }
            }
        }

        
        public string Simbol { get; set; } = "ETHUSDT";
        public decimal StopLoss { get; set; } = 1;
        public decimal TakeProfit { get; set; } = 1;
        public int Offset { get; set; } = 50;
        public int Slip { get; set; } = 10;
        public bool AllowSell { get; set; }
        public bool AllowBuy { get; set; }
        public bool Revers { get; set; }
        
        public decimal Volume { get; set; } = 0.01m;
    }
}
