
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Controls;

using static ShortPeakRobot.MainWindow;
using Binance.Net.Clients;
using Binance.Net.Objects;
using Binance.Net.Interfaces.Clients;
using CryptoExchange.Net.Interfaces;
using Binance.Net.Enums;

namespace ShortPeakRobot.Socket
{
    public static class BinanceSocket
    {
        public static BinanceSocketClient client = new BinanceSocketClient();


       

        public static async void Unsubscribe(BinanceSocketClient binanceSocketClient)
        {
            await binanceSocketClient.UnsubscribeAllAsync();
        }
    }
}
