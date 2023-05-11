using CryptoExchange.Net.CommonObjects;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models
{
    public class MarketInfo : BaseVM
    {
        private DateTime _StartSessionDate { get; set; }
        public DateTime StartSessionDate
        {
            get { return _StartSessionDate; }
            set
            {
                if (_StartSessionDate != value)
                {
                    _StartSessionDate = value;
                    OnPropertyChanged("StartSessionDate");
                }
            }
        }

        public DateTime StartStatisticPeriod { get; set; }
        public DateTime EndStatisticPeriod { get; set; }
        


        public decimal _PeriodProfit { get; set; }
        public decimal PeriodProfit
        {
            get { return _PeriodProfit; }
            set
            {
                if (_PeriodProfit != value)
                {
                    _PeriodProfit = value;
                    OnPropertyChanged("PeriodProfit");
                }
            }
        }


        public decimal _PeriodRobotProfit { get; set; }
        public decimal PeriodRobotProfit
        {
            get { return _PeriodRobotProfit; }
            set
            {
                if (_PeriodRobotProfit != value)
                {
                    _PeriodRobotProfit = value;
                    OnPropertyChanged("PeriodRobotProfit");
                }
            }
        }



        private decimal _BalanceUSDT { get; set; }
        public decimal BalanceUSDT
        {
            get { return _BalanceUSDT; }
            set
            {
                if (_BalanceUSDT != value)
                {
                    _BalanceUSDT = value;
                    OnPropertyChanged("BalanceUSDT");
                }
            }
        }

        public int SelectedRobotIndex { get; set; }
        public decimal Deposit { get; set; }
        
       
    }
}
