using ShortPeakRobot.API;
using ShortPeakRobot.ViewModel;
using ShortPeakRobot.Robots;
using ShortPeakRobot.Socket;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Windows.Media;
using System.Collections.Generic;
using ShortPeakRobot.Market;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.UI;
using System.Threading.Tasks;
using ShortPeakRobot.Constants;
using Binance.Net.Enums;
using CryptoExchange.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using ShortPeakRobot.Robots.Models;
using ShortPeakRobot.Data;
using System.Linq;
using System.Collections;
using System.Threading;
using ShortPeakRobot.Robots.DTO;
using System.Globalization;

namespace ShortPeakRobot
{

    public partial class MainWindow : Window
    {

        private ChartData chartData;

        //public MarketManager marketManager;
        WebCallResult<IEnumerable<BinanceFuturesUsdtTrade>> trades;


        public MainWindow()
        {
            InitializeComponent();



            MarketServices.candleChartDwaw = CandleChartDwaw;

            //marketManager = new MarketManager();
            chartData = new ChartData();

            BinanceApi.SetKeys("VY75xh1L9t0Ac5ICLtEkjGcH5qyW99RuwkqLouK0qdnGfm3YZCxUVJfGPapPPJ4T",
                "ihEt9zyZgJzzNAxqXIFYtbNy5FdlwNWYXrWDmNDAQdHX7oomVnbLMtIJfxkLYYqE");

            //invest
            //BinanceApi.SetKeys("RIYpUuLyDlmxIyQhXx1YzBGl69GeURpIerPN70waLRn5O8kqTRGvvVwYRaygDYBt",
            //    "gdwNFak9F9MoGrVX8MZNLgSaSmbJPLGB3vPj07YlHIzHSGBTFgeA2qwxS5or1uQK");

        }


        

        public delegate void CandleChartDwawDelegate(List<Candle> candles, List<RobotTrade> deals,
            int timeFrame);
        public delegate void RobotMonitorMessageDelegate(string message);

        public void CandleChartDwaw(List<Candle> candles, List<RobotTrade> trades, int timeFrame)
        {
            chartData.TimeFrame = timeFrame;
            chartData.Candles = candles;
            chartData.Trades = trades;
            if (candles.Count < 100)
            {
                chartData.CandleCnt = candles.Count;
            }
            else
            {
                chartData.CandleCnt = 100;
            }
            chartData.CandleStartIndex = candles.Count - chartData.CandleCnt;
            Dispatcher.Invoke(() => CandleChart.Draw(GridCandleChart, GridCandleChartPrice, GridCandleChartDate, chartData, 0));
        }

        private async void testAction_Click(object sender, RoutedEventArgs e)
        {
            //var position = await BinanceApi.client.UsdFuturesApi.Account.GetPositionInformationAsync("ETHUSDT");

            foreach (var symbol in SymbolInitialization.list)
            {
                //снимаем ордера
                var cancelAllOrders = await BinanceApi.client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);

                MarketServices.CloseSymbolPositionAsync(symbol);


            }


            var trades = await BinanceApi.client.UsdFuturesApi.Trading.GetUserTradesAsync("ETHUSDT",
                startTime: DateTime.UtcNow.AddDays(-1));

            var orders = await BinanceApi.client.UsdFuturesApi.Trading.GetOrdersAsync("ETHUSDT",
                startTime: DateTime.UtcNow.AddDays(-1));

        }


        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MarketServices.GetBalanceUSDTAsync();

            MessageBox.Show("Client Id " + RobotsInitialization.ClientId);

            var date = DateTime.UtcNow;
            MarketData.Info.StartSessionDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            TBSessionDate.Text = date.ToString("dd.MM.yyyy");
            MarketServices.GetSessioProfit();
            //------------

            foreach (var robot in RobotVM.robots)//добавление id роботов в алгоритмы роботов
            {
                robot.BaseSettings.LoadSettings(robot.Id);                
            }


            //инициализация словоря цен и свечей
            foreach (string symbol in SymbolInitialization.list)
            {
                //MarketData.PriceDictionary.Add(symbol, 0);
                MarketData.CandleDictionary.Add(symbol, new Dictionary<int, List<Candle>>());

                //MarketData.TradeDictionary.Add(symbol, new List<RobotTrade>());

                var symbolPosition = await MarketServices.GetSymbolPositionAsync(symbol);
                SymbolVM.symbols.Add(new Symbol { Name = symbol, Position = symbolPosition });
            }

            foreach (var tfDictionary in MarketData.CandleDictionary)
            {
                foreach (var tf in TimeFrameInitialization.list)
                {
                    tfDictionary.Value.Add(tf, new List<Candle>());
                }
            }

            CbSymbol.ItemsSource = SymbolInitialization.list;
            CbTradeSymbol.ItemsSource = SymbolInitialization.list;
            CbOrderSymbol.ItemsSource = SymbolInitialization.list;
            CbTF.ItemsSource = TimeFrameInitialization.list;
            StackInfo.DataContext = MarketData.Info;
            StackRobotProfit.DataContext = MarketData.Info;

            //подписка на получение данных с биржы
            MarketData.MarketManager.ActivateSubscribe();
            MarketData.MarketManager.ActivateUserDataStream();

            //инициализация перврго робота для отображение в UI
            Task.Run(() =>
            {
                Thread.Sleep(3000);
                Dispatcher.Invoke(() =>
                {

                    //-------------------

                    RobotVM.robots[0].Selected = true;
                    MarketData.Info.SelectedRobotId = 0;
                    MonitorRobotName.Text = RobotVM.robots[0].Name;
                    DataProcessor.SetCellsVM(MarketData.Info.SelectedRobotId);
                    Grid_baseSettings.DataContext = RobotVM.robots[0].BaseSettings;
                    LoadSettings.IsEnabled = true;
                    SaveSettingsToFile.IsEnabled = true;
                });

            });

            MarketServices.GetRobotData(0);





        }


        private void DayMonthState(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var index = Convert.ToInt32(((TextBlock)sender).Text);


            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowedDayMonth[index - 1] =
                !RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowedDayMonth[index - 1];

            if (((TextBlock)sender).Background == Brushes.LightGreen)
            {
                ((TextBlock)sender).Background = Brushes.LightSalmon;

            }
            else
            {
                ((TextBlock)sender).Background = Brushes.LightGreen;

            }

        }

        private void DayWeekState(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var index = Convert.ToInt32(((TextBlock)sender).Text);


            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowedDayWeek[index - 1] =
                !RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowedDayWeek[index - 1];

            if (((TextBlock)sender).Background == Brushes.LightGreen)
            {
                ((TextBlock)sender).Background = Brushes.LightSalmon;

            }
            else
            {
                ((TextBlock)sender).Background = Brushes.LightGreen;

            }

        }

        private void HourState(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var index = Convert.ToInt32(((TextBlock)sender).Text);


            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowedHours[index] =
                !RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowedHours[index];

            if (((TextBlock)sender).Background == Brushes.LightGreen)
            {
                ((TextBlock)sender).Background = Brushes.LightSalmon;
            }
            else
            {
                ((TextBlock)sender).Background = Brushes.LightGreen;
            }

        }



        private void LoadSettings_Click(object sender, RoutedEventArgs e)
        {
            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.LoadSettingsFromFile(MarketData.Info.SelectedRobotId);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (var robot in RobotVM.robots)
            {
                robot.BaseSettings.SaveSettings(robot.Id, robot.BaseSettings);
            }

            BinanceSocket.client.UnsubscribeAllAsync();
        }

        private void SaveSettingsToFile_Click(object sender, RoutedEventArgs e)
        {
            var settings = RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings;
            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.SaveSettingsToFile(MarketData.Info.SelectedRobotId, settings);
        }

        private void BtnRobotList_Click(object sender, RoutedEventArgs e)
        {
            RobotListWindow robotList = new RobotListWindow();
            robotList.Owner = Application.Current.MainWindow;

            robotList.Show();
            robotList.Draw_map_robot();
        }

        private void BT_AllowSell_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowSell =
                !RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowSell;
        }

        private void BT_AllowBuy_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowBuy =
                !RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.AllowBuy;
        }

        private void BT_Revers_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.Revers =
                !RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.Revers;
        }



        //private async void GridCandleChart_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    if (chartData.Candles.Count == 0) { return; }

        //    double space_candle = GridCandleChart.ActualWidth / chartData.CandleCnt;
        //    chartData.MousePosition = e.GetPosition(Wind);



        //    while (Convert.ToString(e.LeftButton) == "Pressed")
        //    {
        //        var mouse_pos_now = e.GetPosition(Wind);
        //        if (mouse_pos_now.X > chartData.MousePosition.X + space_candle || mouse_pos_now.X < chartData.MousePosition.X - space_candle)
        //        {


        //            chartData.IndexMove = Convert.ToInt32((chartData.MousePosition.X - mouse_pos_now.X) / space_candle);



        //            if (chartData.CandleStartIndex + chartData.CandleCnt <= chartData.Candles.Count && chartData.CandleStartIndex >= 0)
        //            {
        //                chartData.CandleStartIndex += chartData.IndexMove;
        //                chartData.MousePosition = mouse_pos_now;

        //                if (chartData.CandleStartIndex + chartData.CandleCnt >= chartData.Candles.Count)
        //                {
        //                    chartData.CandleStartIndex = chartData.Candles.Count - chartData.CandleCnt;
        //                }

        //                if (chartData.CandleStartIndex < 0) { chartData.CandleStartIndex = 0; }
        //                CandleChart.Draw(GridCandleChart, chartData, 0);
        //            }



        //        }
        //        await Task.Delay(50);
        //    }
        //}

       

        

        private void TB_RobotsRun_Click(object sender, RoutedEventArgs e)
        {
            var robotIds = RobotVM.robots.Where(x => x.IsActivated).Select(x => x.Id).ToList();
            MarketData.MarketManager.RobotsRun(robotIds);

            //var date = DateTime.UtcNow;
            //MarketData.Info.StartSessionDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);
            MarketData.Info.IsSessionRun = true;
        }

        private void ChangeState(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var stack = (StackPanel)((TextBlock)sender).Parent;
            int robotId = Convert.ToInt32(((TextBlock)stack.Children[0]).Text);

            if (!RobotVM.robots[robotId].IsRun)
            {
                if (!MarketData.MarketManager.CheckRobotsState())
                {
                    MarketData.Info.IsSessionRun = true;
                }

                MarketData.MarketManager.RobotsRun(new List<int> { robotId });

            }
            else
            {
                MarketData.MarketManager.RobotsStop(new List<int> { robotId });

                if (!MarketData.MarketManager.CheckRobotsState())
                {
                    MarketData.Info.IsSessionRun = false;
                }
            }
        }

        private async void BtnCloseAll_Click(object sender, RoutedEventArgs e)
        {
            var robotIds = RobotVM.robots.Select(x => x.Id).ToList();
            MarketData.MarketManager.RobotsStop(robotIds);

            MarketData.Info.IsSessionRun = false;


            //закрываем все позиции
            foreach (var symbol in SymbolInitialization.list)
            {
                //снимаем ордера
                var cancelAllOrders = await BinanceApi.client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);

                MarketServices.CloseSymbolPositionAsync(symbol);


            }

        }

        private async void RobotSelect_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var stackPanel = (StackPanel)((TextBlock)sender).Parent;

            var robotId = Convert.ToInt32(((TextBlock)stackPanel.Children[0]).Text);

            foreach (var robot in RobotVM.robots)
            {
                if (robot.Id != robotId)
                {
                    robot.Selected = false;
                }
                else
                {
                    robot.Selected = true;
                }
            }

            MarketData.Info.SelectedRobotId = robotId;
            MonitorRobotName.Text = RobotVM.robots[robotId].Name;
            DataProcessor.SetCellsVM(MarketData.Info.SelectedRobotId);
            Grid_baseSettings.DataContext = RobotVM.robots[robotId].BaseSettings;
            //------------- robot data VM
            MarketServices.GetRobotData(robotId);



        }

        private void BtnCloseSymbolPosition_Click(object sender, RoutedEventArgs e)
        {
            var stack = (StackPanel)((Button)sender).Parent;
            string symbol = ((TextBlock)stack.Children[0]).Text;

            MarketServices.CloseSymbolPositionAsync(symbol);
        }



       

       

        private void BtnCloseRobot_Click(object sender, RoutedEventArgs e)
        {
            var stack = (StackPanel)((Button)sender).Parent;
            int robotId = Convert.ToInt32(((TextBlock)stack.Children[0]).Text);

            Task.Run(() => { RobotVM.robots[robotId].CloseRobotPosition(); });

        }

        private async void BtnDepoCalc_Click(object sender, RoutedEventArgs e)
        {
            var profit = 0m;
            
            

            var trades =  MarketServices.GetRobotTrades(MarketData.Info.SelectedRobotId, MarketData.Info.StartSessionDate);

            trades.ForEach(trade =>
            {
                profit += trade.RealizedPnl;
                profit -= trade.Fee;
            });

            RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.CurrentDeposit =
                RobotVM.robots[MarketData.Info.SelectedRobotId].BaseSettings.Deposit + Math.Round(profit, 2);

        }

       

        

        private void BtnRobotSell_Click(object sender, RoutedEventArgs e)
        {
            var qty = decimal.Parse(TBChangeRobotQty.Text.Replace(',', '.'), CultureInfo.InvariantCulture);

            RobotVM.robots[MarketData.Info.SelectedRobotId].ChangeRobotPosition(OrderSide.Sell, qty);
        }

        private void BtnRobotBuy_Click(object sender, RoutedEventArgs e)
        {
            var qty = decimal.Parse(TBChangeRobotQty.Text.Replace(',', '.'), CultureInfo.InvariantCulture);

            RobotVM.robots[MarketData.Info.SelectedRobotId].ChangeRobotPosition(OrderSide.Buy, qty);
        }

        private void BtnSetSessionDate_Click(object sender, RoutedEventArgs e)
        {
            
            var dateChunks = TBSessionDate.Text.Split('.');
            var date = new DateTime(Convert.ToInt32(dateChunks[2]), Convert.ToInt32(dateChunks[1]), Convert.ToInt32(dateChunks[0]));
            var dateUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc);

            MarketData.Info.StartSessionDate = dateUtc;

            MarketServices.GetSessioProfit();
            
        }

        private  void BtnRobotCloseOrders_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() =>
            {
                MarketData.OpenOrders.ForEach(o =>
                {
                    //RobotVM.robots[MarketData.Info.SelectedRobotId].CancelOrderAsync(o, "Cancel robot orders");
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = MarketData.Info.SelectedRobotId,
                        Symbol = RobotVM.robots[MarketData.Info.SelectedRobotId].Symbol,
                        robotOrderType = RobotOrderType.OrderId,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = o.OrderId
                    });
                });


                RobotVM.robots[MarketData.Info.SelectedRobotId].ResetRobotStateOrders();
                RobotVM.robots[MarketData.Info.SelectedRobotId].RobotState.TakeProfitOrderId = 0;
                RobotVM.robots[MarketData.Info.SelectedRobotId].RobotState.StopLossOrderId = 0;
                RobotVM.robots[MarketData.Info.SelectedRobotId].RobotState.SignalSellOrderId = 0;
                RobotVM.robots[MarketData.Info.SelectedRobotId].RobotState.SignalBuyOrderId = 0;
                RobotServices.SaveState(MarketData.Info.SelectedRobotId,
                    RobotVM.robots[MarketData.Info.SelectedRobotId].RobotState);
            });
            MessageBox.Show("All orders canceled.");

        }

        private async void BtnSymbolTrades_Click(object sender, RoutedEventArgs e)
        {
            var symbol = CbTradeSymbol.Text;

            var trades = await BinanceApi.client.UsdFuturesApi.Trading.GetUserTradesAsync(symbol,
                startTime: MarketData.Info.StartSessionDate);

            BinTradeVM.trades.Clear();
            BinTradeVM.AddRange(BinTradeDTO.TradesDTO(trades));
        }

        private async void BtnSymbolOrders_Click(object sender, RoutedEventArgs e)
        {
            var symbol = CbTradeSymbol.Text;

            var orders = await BinanceApi.client.UsdFuturesApi.Trading.GetOrdersAsync(symbol,
                startTime: MarketData.Info.StartSessionDate);

            BinOrderVM.orders.Clear();
            BinOrderVM.AddRange(BinOrderDTO.OrdersDTO(orders));
        }

        private void BtnRobotControl_Click(object sender, RoutedEventArgs e)
        {
            var robotStateWindow = new RobotControl(MarketData.Info.SelectedRobotId);
            robotStateWindow.Owner = Application.Current.MainWindow;

            robotStateWindow.Show();
        }
    }
}
