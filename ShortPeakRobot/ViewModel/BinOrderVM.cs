using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Spot;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ShortPeakRobot.ViewModel
{
    public class BinOrderVM
    {
        public static ObservableCollection<BinOrder> orders
        {
            get; set;
        } = new ObservableCollection<BinOrder>();


        public BinOrderVM()
        {

        }

        public static void AddRange(List<BinOrder> orders)
        {
            orders.ForEach(order => BinOrderVM.orders.Add(order));
        }
        
    }
}
