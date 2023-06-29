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
        
        private string _Message { get; set; }
        public string Message
        {
            get { return _Message; }
            set
            {
                if (_Message != value)
                {
                    _Message = value;
                    OnPropertyChanged("Message");
                }
            }
        }

        private bool _IsMessageActive { get; set; } = false;
        public bool IsMessageActive
        {
            get { return _IsMessageActive; }
            set
            {
                if (_IsMessageActive != value)
                {
                    _IsMessageActive = value;
                    OnPropertyChanged("IsMessageActive");
                }
            }
        }



        private decimal _TotalCurrentProfit { get; set; }
        public decimal TotalCurrentProfit
        {
            get { return _TotalCurrentProfit; }
            set
            {
                if (_TotalCurrentProfit != value)
                {
                    _TotalCurrentProfit = value;
                    OnPropertyChanged("TotalCurrentProfit");
                }
            }
        }

        public int SelectedRobotIndex { get; set; }
        public decimal Deposit { get; set; }
        public int ClientId { get; set; }
        public bool IsApiKeysValid { get; set; }
        public string AppInstanceKey { get; set; }

        public decimal DayProfit { get; set; }
        public int ServerTimeOffsetMinutes { get; set; }
    }
}
