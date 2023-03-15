using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Data;
using ShortPeakRobot.Robots.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ShortPeakRobot.ViewModel
{
    public class BinTradeVM
    {
        public static ObservableCollection<BinTrade> trades
        {
            get; set;
        } = new ObservableCollection<BinTrade>();


        public BinTradeVM()
        {
           
        }

        public static void AddRange(List<BinTrade> trades)
        {
            trades.ForEach(trade => BinTradeVM.trades.Add(trade));
        }

        
    }
}
