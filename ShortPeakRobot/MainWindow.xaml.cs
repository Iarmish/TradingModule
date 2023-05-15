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
using ShortPeakRobot.Data;
using System.Linq;
using System.Threading;
using ShortPeakRobot.Robots.DTO;
using System.Globalization;
using Symbol = ShortPeakRobot.Market.Models.Symbol;
using ShortPeakRobot.Market.Models.ApiModels;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace ShortPeakRobot
{

    public partial class MainWindow : Window
    {

        private ChartData chartData;

        WebCallResult<IEnumerable<BinanceFuturesUsdtTrade>> trades;


        public MainWindow()
        {
            InitializeComponent();

            MarketServices.candleChartDwaw = CandleChartDwaw;

            chartData = new ChartData();

            //BinanceApi.SetKeys("VY75xh1L9t0Ac5ICLtEkjGcH5qyW99RuwkqLouK0qdnGfm3YZCxUVJfGPapPPJ4T",
            //    "ihEt9zyZgJzzNAxqXIFYtbNy5FdlwNWYXrWDmNDAQdHX7oomVnbLMtIJfxkLYYqE");

            //invest
            //BinanceApi.SetKeys("RIYpUuLyDlmxIyQhXx1YzBGl69GeURpIerPN70waLRn5O8kqTRGvvVwYRaygDYBt",
            //    "gdwNFak9F9MoGrVX8MZNLgSaSmbJPLGB3vPj07YlHIzHSGBTFgeA2qwxS5or1uQK");

            //invest two
            //BinanceApi.SetKeys("9ik6i9QXcN2gTscLHeJ35sOPULvDaeFvliL9jSOCA8wbHZY3WN9CU8kbAVKryLQ4",
            //    "scR2dKL0ckAXwpkiI4wqduqYCl19vcvOeaL5gosHGSHb0eTIz8etF6NK9L40hsdR");

            //invest read only
            //BinanceApi.SetKeys("aRLgrQdQkprx2cu5534Gsh3ZP1ksv3IhZs7JBsVdOcWrWwsWOzU0qAH1zeKXzrqb",
            //    "X0ewrjdRBMgOMjuQdFJ9mUP6VaI6mHqXPj368siQyBhi2AXI6BYYCWie299qCEfQ");
        }

        IniManager ini = new();

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
            MarketData.Info.Message += "dfgsdfg \n";
            MarketData.Info.IsMessageActive = true;



            //var position = await BinanceApi.client.UsdFuturesApi.Account.GetPositionInformationAsync("ETHUSDT");
            //var balances = await BinanceApi.client.UsdFuturesApi.Account.GetBalancesAsync();
            //var balances = await BinanceApi.client.UsdFuturesApi.Account.GetBalancesAsync();


            //var trades2 = await BinanceApi.client.UsdFuturesApi.Trading.GetUserTradesAsync("SOLUSDT", startTime: new DateTime(2023,04,21));

            //var orders = await BinanceApi.client.UsdFuturesApi.Trading.GetOrdersAsync("ETHUSDT",
            //    startTime: new DateTime(2023, 04, 18));



            //var order = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
            //        symbol: "ETHUSDT",
            //        orderId: 8389765593151522832);

            //var placedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
            //      symbol: "CHZUSDT",
            //      side: OrderSide.Buy,
            //      type: FuturesOrderType.StopMarket,
            //      quantity: 1m,
            //      stopPrice: 0.2m,
            //      timeInForce: TimeInForce.GoodTillCanceled
            //      );








        }


        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginResponse loginResponse = await ApiServices.Login(TbLogin.Text, TbPass.Text);
            BtnLogin.IsEnabled = false;

            if (loginResponse.success)
            {
                ApiServices.SetTokens(loginResponse);
                MarketData.Info.ClientId = loginResponse.data.client_id;
                RunApp();
                StackLogin.Visibility = Visibility.Collapsed;
            }
            else
            {
                MarketData.Info.Message += "Ошибка авторизации" + "\n";
                MarketData.Info.IsMessageActive = true;
            }

        }

        private async void RunApp()
        {
            var apiKey = ini.GetPrivateString("apiKey", "value");
            var apiSecret = ini.GetPrivateString("apiSecret", "value");
            BinanceApi.SetKeys(apiKey, apiSecret);

            CbSymbol.ItemsSource = SymbolInitialization.list;
            CbTradeSymbol.ItemsSource = SymbolInitialization.list;
            CbOrderSymbol.ItemsSource = SymbolInitialization.list;
            CbTF.ItemsSource = TimeFrameInitialization.list;
            StackInfo.DataContext = MarketData.Info;
            GridMessage.DataContext = MarketData.Info;
            StackRobotProfit.DataContext = MarketData.Info;
            StackRobotProfit2.DataContext = MarketData.Info;

            TBDepo.Text = ini.GetPrivateString("deposit", "value");
            MarketData.Info.Deposit = (int)decimal.Parse(TBDepo.Text.Replace(',', '.'), CultureInfo.InvariantCulture);


            await MarketServices.GetBalanceUSDTAsync();



            var startSessionChunks = ini.GetPrivateString("startSessionDate", "value").Split('.');
            var startSessionDate = new DateTime(Convert.ToInt32(startSessionChunks[2]), Convert.ToInt32(startSessionChunks[1]), Convert.ToInt32(startSessionChunks[0]));
            MarketData.Info.StartSessionDate = new DateTime(startSessionDate.Year, startSessionDate.Month, startSessionDate.Day, 0, 0, 0, DateTimeKind.Utc);

            //------------
            var startStatisticPeriodChunks = ini.GetPrivateString("startStatisticPeriod", "value").Split('.');
            var startStatisticPeriodDate = new DateTime(Convert.ToInt32(startStatisticPeriodChunks[2]), Convert.ToInt32(startStatisticPeriodChunks[1]), Convert.ToInt32(startStatisticPeriodChunks[0]));
            MarketData.Info.StartStatisticPeriod = new DateTime(startStatisticPeriodDate.Year, startStatisticPeriodDate.Month, startStatisticPeriodDate.Day, 0, 0, 0, DateTimeKind.Utc);
            TBStartStatisticPeriod.Text = startStatisticPeriodDate.ToString("dd.MM.yyyy");

            //------------
            var endStatisticPeriodChunks = ini.GetPrivateString("endStatisticPeriod", "value").Split('.');
            var endStatisticPeriodDate = new DateTime(Convert.ToInt32(endStatisticPeriodChunks[2]), Convert.ToInt32(endStatisticPeriodChunks[1]), Convert.ToInt32(endStatisticPeriodChunks[0]));
            MarketData.Info.EndStatisticPeriod = new DateTime(endStatisticPeriodDate.Year, endStatisticPeriodDate.Month, endStatisticPeriodDate.Day, 0, 0, 0, DateTimeKind.Utc);
            TBEndStatisticPeriod.Text = endStatisticPeriodDate.ToString("dd.MM.yyyy");

            MarketServices.GetSessioProfit();
            MarketServices.GetPeriodProfit();
            //------- Инициализация роботов  -----
            foreach (var robot in RobotVM.robots)//добавление id роботов в алгоритмы роботов
            {
                robot.BaseSettings.LoadSettings(robot.Index);
                var RobotStateResponse = await JsonDataServices.LoadRobotStateAsync(robot.Index);
                if (RobotStateResponse.success)
                {
                    robot.RobotState = RobotStateResponse.data;
                }
                else
                {
                    //обработать
                    robot.RobotState = new RobotState { RobotId = robot.Id };
                }
                //-----не сохраненные в в базу данные
                robot.RobotTradesUnsaved = await JsonDataServices.LoadRobotTradesAsync(robot.Id);
                robot.RobotOrdersUnsaved = await JsonDataServices.LoadRobotOrdersAsync(robot.Id);
                robot.RobotDealsUnsaved = await JsonDataServices.LoadRobotDealsAsync(robot.Id);
                robot.RobotLogsUnsaved = await JsonDataServices.LoadRobotLogsAsync(robot.Id);
                //-----
                await robot.SetRobotData();
            }


            //инициализация словоря цен и свечей
            foreach (string symbol in SymbolInitialization.list)
            {

                MarketData.CandleDictionary.Add(symbol, new Dictionary<int, List<Candle>>());

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



            //подписка на получение данных с биржы
            MarketData.MarketManager.ActivateSubscribe();
            MarketData.MarketManager.ActivateUserDataStream();

            //инициализация перврго робота для отображение в UI
            //Task.Run(() =>
            //{
            //    Thread.Sleep(3000);
            //    Dispatcher.Invoke(() =>
            //    {

            //        //-------------------

            //        RobotVM.robots[0].Selected = true;
            //        MarketData.Info.SelectedRobotIndex = 0;
            //        MonitorRobotName.Text = RobotVM.robots[0].Name;
            //        DataProcessor.SetCellsVM(MarketData.Info.SelectedRobotIndex);
            //        Grid_baseSettings.DataContext = RobotVM.robots[0].BaseSettings;
            //        LoadSettings.IsEnabled = true;
            //        SaveSettingsToFile.IsEnabled = true;
            //    });

            //});

            //MarketServices.GetRobotData(0);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

            ApiServices.httpClient.DefaultRequestHeaders.Accept.Clear();
            ApiServices.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            ApiServices.httpClient.BaseAddress = new Uri("https://api.nero-trade.ai/");

            var login = ini.GetPrivateString("login", "value");
            var password = ini.GetPrivateString("password", "value");

            TbLogin.Text = login;
            TbPass.Text = password;



            //string original = "secret message";
            //byte[] encrypted;
            //string decrypted;

            //var key = GetKey("keyd");
            //var four = GetIV("fourd");


            //using (Aes aes = Aes.Create())
            //{
                
            //    // Encrypt the string
            //    encrypted = MarketServices.EncryptStringToBytes(original, key, four);
            //    Console.WriteLine("Encrypted: {0}", Convert.ToBase64String(encrypted));

               
            //}
           

            //using (Aes aes = Aes.Create())
            //{


            //    // Decrypt the bytes
            //    decrypted = MarketServices.DecryptStringFromBytes(encrypted, key, four);
            //    Console.WriteLine("Decrypted: {0}", decrypted);
            //}

            
        }


        private static byte[] GetIV(string ivSecret)
        {
            using MD5 md5 = MD5.Create();
            return md5.ComputeHash(Encoding.UTF8.GetBytes(ivSecret));
        }
        private static byte[] GetKey(string key)
        {
            using SHA256 sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
        }

        private void DayMonthState(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var index = Convert.ToInt32(((TextBlock)sender).Text);


            RobotVM.robots[MarketData.Info.SelectedRobotIndex].BaseSettings.AllowedDayMonth[index - 1] =
                !RobotVM.robots[MarketData.Info.SelectedRobotIndex].BaseSettings.AllowedDayMonth[index - 1];

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


            RobotVM.robots[MarketData.Info.SelectedRobotIndex].BaseSettings.AllowedDayWeek[index - 1] =
                !RobotVM.robots[MarketData.Info.SelectedRobotIndex].BaseSettings.AllowedDayWeek[index - 1];

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


            RobotVM.robots[MarketData.Info.SelectedRobotIndex].BaseSettings.AllowedHours[index] =
                !RobotVM.robots[MarketData.Info.SelectedRobotIndex].BaseSettings.AllowedHours[index];

            if (((TextBlock)sender).Background == Brushes.LightGreen)
            {
                ((TextBlock)sender).Background = Brushes.LightSalmon;
            }
            else
            {
                ((TextBlock)sender).Background = Brushes.LightGreen;
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MarketData.Info.ClientId != 0)
            {
                foreach (var robot in RobotVM.robots)
                {
                    robot.BaseSettings.SaveSettings(robot.Id, robot.BaseSettings);
                }
                ini.WritePrivateString("startStatisticPeriod", "value", TBStartStatisticPeriod.Text);
                ini.WritePrivateString("endStatisticPeriod", "value", TBEndStatisticPeriod.Text);
            }
            //
            BinanceSocket.client.UnsubscribeAllAsync();


            if (MessageBox.Show("Подтвердите закрытие программы", "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "No")
            {
                e.Cancel = true;
            }
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
            var robotIndex = MarketData.Info.SelectedRobotIndex;
            RobotVM.robots[robotIndex].BaseSettings.AllowSell =
                !RobotVM.robots[robotIndex].BaseSettings.AllowSell;
        }

        private void BT_AllowBuy_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var robotIndex = MarketData.Info.SelectedRobotIndex;
            RobotVM.robots[robotIndex].BaseSettings.AllowBuy =
                !RobotVM.robots[robotIndex].BaseSettings.AllowBuy;
        }

        private void BT_Revers_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var robotIndex = MarketData.Info.SelectedRobotIndex;
            RobotVM.robots[robotIndex].BaseSettings.Revers =
                !RobotVM.robots[robotIndex].BaseSettings.Revers;
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
            if (MessageBox.Show("Подтвердите запуск всех роботов", "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {
                var robotIndexes = RobotVM.robots.Where(x => x.IsActivated).Select(x => x.Index).ToList();
                MarketData.MarketManager.RobotsRun(robotIndexes);

            }


        }

        private void ChangeState(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (MessageBox.Show("Подтвердите изменение статуса робота", "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {
                var stack = (StackPanel)((TextBlock)sender).Parent;
                int robotIndex = Convert.ToInt32(((TextBlock)stack.Children[0]).Text);

                if (!RobotVM.robots[robotIndex].IsRun)
                {
                    MarketData.MarketManager.RobotsRun(new List<int> { robotIndex });
                }
                else
                {
                    MarketData.MarketManager.RobotsStop(new List<int> { robotIndex });
                }

            }
        }

        private async void BtnCloseAll_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Подтвердите аварийную остановку торговли и закрытие всех позиций", "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {
                var robotIndexes = RobotVM.robots.Select(x => x.Index).ToList();
                MarketData.MarketManager.RobotsStop(robotIndexes);

                //закрываем все позиции
                foreach (var symbol in SymbolInitialization.list)
                {
                    //снимаем ордера
                    var cancelAllOrders = await BinanceApi.client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);

                    MarketServices.CloseSymbolPositionAsync(symbol);
                }

            }

        }

        private void RobotSelect_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var stackPanel = (StackPanel)((TextBlock)sender).Parent;

            var robotIndex = Convert.ToInt32(((TextBlock)stackPanel.Children[0]).Text);

            foreach (var robot in RobotVM.robots)
            {
                if (robot.Index != robotIndex)
                {
                    robot.Selected = false;
                }
                else
                {
                    robot.Selected = true;
                    robot.Command = RobotCommands.SetRobotInfo;
                }
            }

            MarketData.Info.SelectedRobotIndex = robotIndex;
            MonitorRobotName.Text = RobotVM.robots[robotIndex].Name;
            DataProcessor.SetCellsVM(MarketData.Info.SelectedRobotIndex);
            Grid_baseSettings.DataContext = RobotVM.robots[robotIndex].BaseSettings;
            //------------- robot data VM
            MarketServices.GetRobotMarketData(robotIndex);

        }

        private void BtnCloseSymbolPosition_Click(object sender, RoutedEventArgs e)
        {
            var stack = (StackPanel)((Button)sender).Parent;
            string symbol = ((TextBlock)stack.Children[0]).Text;
            if (MessageBox.Show("Подтвердите аварийное закрытие всех позиций на " + symbol, "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {

                MarketServices.CloseSymbolPositionAsync(symbol);

            }
        }



        private void BtnCloseRobot_Click(object sender, RoutedEventArgs e)//остановка робота и закр все ордера и позиции 
        {
            if (MessageBox.Show("Подтвердите аварийную остановку робота и закрытие всех позиций робота", "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {
                var stack = (StackPanel)((Button)sender).Parent;
                int robotIndex = Convert.ToInt32(((TextBlock)stack.Children[0]).Text);

                Task.Run(() => { RobotVM.robots[robotIndex].CloseRobotPosition(); });

            }
        }




        private void BtnRobotSell_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Подтвердите продажу", "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {
                var qty = decimal.Parse(TBChangeRobotQty.Text.Replace(',', '.'), CultureInfo.InvariantCulture);

                RobotVM.robots[MarketData.Info.SelectedRobotIndex].ChangeRobotPosition(OrderSide.Sell, qty);

            }
        }

        private void BtnRobotBuy_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Подтвердите покупку", "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {
                var qty = decimal.Parse(TBChangeRobotQty.Text.Replace(',', '.'), CultureInfo.InvariantCulture);

                RobotVM.robots[MarketData.Info.SelectedRobotIndex].ChangeRobotPosition(OrderSide.Buy, qty);
            }

        }



        private void BtnRobotCloseOrders_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Подтвердите снятие всех ордеров робота", "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {
                Task.Run(() =>
                {
                    MarketData.OpenOrders.ForEach(o =>
                    {
                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = MarketData.Info.SelectedRobotIndex,
                            Symbol = RobotVM.robots[MarketData.Info.SelectedRobotIndex].Symbol,
                            robotOrderType = RobotOrderType.OrderId,
                            robotRequestType = RobotRequestType.CancelOrder,
                            OrderId = o.OrderId
                        });
                    });


                    RobotVM.robots[MarketData.Info.SelectedRobotIndex].ResetRobotData();
                    RobotVM.robots[MarketData.Info.SelectedRobotIndex].RobotState.TakeProfitOrderId = 0;
                    RobotVM.robots[MarketData.Info.SelectedRobotIndex].RobotState.StopLossOrderId = 0;
                    RobotVM.robots[MarketData.Info.SelectedRobotIndex].RobotState.SignalSellOrderId = 0;
                    RobotVM.robots[MarketData.Info.SelectedRobotIndex].RobotState.SignalBuyOrderId = 0;
                });

            }

            //MessageBox.Show("All orders canceled.");

        }

        private async void BtnSymbolTrades_Click(object sender, RoutedEventArgs e)
        {
            var symbol = CbTradeSymbol.Text;


            var trades = await BinanceApi.client.UsdFuturesApi.Trading.GetUserTradesAsync(symbol,
                startTime: MarketData.Info.StartStatisticPeriod);

            BinTradeVM.trades.Clear();
            BinTradeVM.AddRange(BinTradeDTO.TradesDTO(trades));
        }

        private async void BtnSymbolOrders_Click(object sender, RoutedEventArgs e)
        {
            var symbol = CbTradeSymbol.Text;

            var orders = await BinanceApi.client.UsdFuturesApi.Trading.GetOrdersAsync(symbol,
                startTime: MarketData.Info.StartStatisticPeriod);

            BinOrderVM.orders.Clear();
            BinOrderVM.AddRange(BinOrderDTO.OrdersDTO(orders));
        }

        private void BtnRobotControl_Click(object sender, RoutedEventArgs e)
        {
            var robotStateWindow = new RobotControl(MarketData.Info.SelectedRobotIndex);
            robotStateWindow.Owner = Application.Current.MainWindow;

            robotStateWindow.Show();
        }

        private void BtnCancelSymbolOrders_Click(object sender, RoutedEventArgs e)
        {
            var stack = (StackPanel)((Button)sender).Parent;
            string symbol = ((TextBlock)stack.Children[0]).Text;
            if (MessageBox.Show("Подтвердите аварийное снятие всех ордеров на " + symbol, "Binance Robot", MessageBoxButton.YesNo, MessageBoxImage.Question).ToString() == "Yes")
            {
                BinanceApi.client.UsdFuturesApi.Trading.CancelAllOrdersAsync(symbol);
            }
        }

        private void BtnRobotInfo_Click(object sender, RoutedEventArgs e)
        {
            var robotStateWindow = new RobotDebugInfo();
            robotStateWindow.Owner = Application.Current.MainWindow;

            robotStateWindow.Show();
        }

        private void TBinfo_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var TB = (TextBlock)sender;
            if (TB.Foreground == Brushes.Gray)
            {
                TB.Foreground = Brushes.Green;
                MarketData.LogTypeFilter.Add((int)LogType.Info);
            }
            else
            {
                TB.Foreground = Brushes.Gray;
                MarketData.LogTypeFilter = MarketData.LogTypeFilter.Where(x => x != (int)LogType.Info).ToList();
            }
        }

        private void TBerror_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var TB = (TextBlock)sender;
            if (TB.Foreground == Brushes.Gray)
            {
                TB.Foreground = Brushes.Green;
                MarketData.LogTypeFilter.Add((int)LogType.Error);
            }
            else
            {
                TB.Foreground = Brushes.Gray;
                MarketData.LogTypeFilter = MarketData.LogTypeFilter.Where(x => x != (int)LogType.Error).ToList();
            }
        }

        private void TBrobotstate_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var TB = (TextBlock)sender;
            if (TB.Foreground == Brushes.Gray)
            {
                TB.Foreground = Brushes.Green;
                MarketData.LogTypeFilter.Add((int)LogType.RobotState);
            }
            else
            {
                TB.Foreground = Brushes.Gray;
                MarketData.LogTypeFilter = MarketData.LogTypeFilter.Where(x => x != (int)LogType.RobotState).ToList();
            }
        }

        private void Tbupdateorder_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var TB = (TextBlock)sender;
            if (TB.Foreground == Brushes.Gray)
            {
                TB.Foreground = Brushes.Green;
                MarketData.LogTypeFilter.Add((int)LogType.UpdateOrder);
            }
            else
            {
                TB.Foreground = Brushes.Gray;
                MarketData.LogTypeFilter = MarketData.LogTypeFilter.Where(x => x != (int)LogType.UpdateOrder).ToList();
            }
        }

        private void BtnSetDepo_Click(object sender, RoutedEventArgs e)
        {
            //MarketData.Info.Deposit = (int)(decimal.Parse(TBDepo.Text.Replace(',', '.'), CultureInfo.InvariantCulture));
            //ini.WritePrivateString("deposit", "value", TBDepo.Text);
        }

        private void startUserData_Click(object sender, RoutedEventArgs e)
        {
            MarketServices.StartUserDataStream();
        }

        private void stopUserData_Click(object sender, RoutedEventArgs e)
        {
            MarketServices.StopUserDataStream();
        }

        private void BtnSaveOrder_Click(object sender, RoutedEventArgs e)
        {
            var stack = (StackPanel)((Button)sender).Parent;
            var TradeId = Convert.ToInt64(((TextBlock)stack.Children[0]).Text);

            var trade = RobotTradeVM.trades.Where(x => x.Id == TradeId).First();
            MarketData.MarketManager.UpdateRobotTrade(trade);
        }

        private void BtnPeriodDate_Click(object sender, RoutedEventArgs e)
        {
            var startStatisticPeriodChunks = TBStartStatisticPeriod.Text.Split('.');
            var startStatisticPeriodDate = new DateTime(Convert.ToInt32(startStatisticPeriodChunks[2]), Convert.ToInt32(startStatisticPeriodChunks[1]), Convert.ToInt32(startStatisticPeriodChunks[0]));
            MarketData.Info.StartStatisticPeriod = new DateTime(startStatisticPeriodDate.Year, startStatisticPeriodDate.Month, startStatisticPeriodDate.Day, 0, 0, 0, DateTimeKind.Utc);
            TBStartStatisticPeriod.Text = startStatisticPeriodDate.ToString("dd.MM.yyyy");

            //------------
            var endStatisticPeriodChunks = TBEndStatisticPeriod.Text.Split('.');
            var endStatisticPeriodDate = new DateTime(Convert.ToInt32(endStatisticPeriodChunks[2]), Convert.ToInt32(endStatisticPeriodChunks[1]), Convert.ToInt32(endStatisticPeriodChunks[0]));
            MarketData.Info.EndStatisticPeriod = new DateTime(endStatisticPeriodDate.Year, endStatisticPeriodDate.Month, endStatisticPeriodDate.Day, 0, 0, 0, DateTimeKind.Utc);
            TBEndStatisticPeriod.Text = endStatisticPeriodDate.ToString("dd.MM.yyyy");

            MarketServices.GetPeriodProfit();

            MarketServices.GetBalanceUSDTAsync();
        }

        private void BtnCloseMessage_Click(object sender, RoutedEventArgs e)
        {
            MarketData.Info.IsMessageActive = false;
        }

        private void BtnClearMessage_Click(object sender, RoutedEventArgs e)
        {
            MarketData.Info.Message = string.Empty;
        }
    }
}
