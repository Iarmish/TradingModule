using ShortPeakRobot.ViewModel;

namespace ShortPeakRobot.Market.Models
{
    public class Symbol : BaseVM
    {
        public string Name { get; set; }


        private decimal _Price { get; set; }
        public decimal Price
        {
            get { return _Price; }
            set
            {
                if (_Price != value)
                {
                    _Price = value;
                    OnPropertyChanged("Price");
                    MarketServices.SetRobotLotBySymbol(Name, value);
                }
            }
        }


        private decimal _Position { get; set; }
        public decimal Position
        {
            get { return _Position; }
            set
            {
                if (_Position != value)
                {
                    _Position = value;
                    OnPropertyChanged("Position");
                }
            }
        }
    }
}
