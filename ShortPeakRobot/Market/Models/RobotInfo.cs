using CryptoExchange.Net.CommonObjects;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models
{
    public class RobotInfo : BaseVM
    {
        private string _Title { get; set; }
        public string Title
        {
            get { return _Title; }
            set
            {
                if (_Title != value)
                {
                    _Title = value;
                    OnPropertyChanged("Symbol");
                }
            }
        }
        private string _Value { get; set; }
        public string Value
        {
            get { return _Value; }
            set
            {
                if (_Value != value)
                {
                    _Value = value;
                    OnPropertyChanged("Value");
                }
            }
        }
        //public List<string> Params { get; set; } = new List<string>();
    }
}
