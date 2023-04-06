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
        public bool IsSessionRun { get; set; }


        public decimal _SessionProfit { get; set; }
        public decimal SessionProfit
        {
            get { return _SessionProfit; }
            set
            {
                if (_SessionProfit != value)
                {
                    _SessionProfit = value;
                    OnPropertyChanged("SessionProfit");
                }
            }
        }


        public decimal _SessionRobotProfit { get; set; }
        public decimal SessionRobotProfit
        {
            get { return _SessionRobotProfit; }
            set
            {
                if (_SessionRobotProfit != value)
                {
                    _SessionRobotProfit = value;
                    OnPropertyChanged("SessionRobotProfit");
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

        public int SelectedRobotId { get; set; }
        public decimal Deposit { get; set; }
        
       
    }
}
