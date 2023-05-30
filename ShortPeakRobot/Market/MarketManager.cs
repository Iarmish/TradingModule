using Binance.Infrastructure.Constants;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Robots;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.Socket;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market
{
    public class MarketManager
    {

        public Dictionary<string, Subscribe> subscribes { get; set; }
        public List<CallResult<UpdateSubscription>> updateSubscriptions { get; set; } = new List<CallResult<UpdateSubscription>>();

        private DateTime ListenKeyTimeUpdate = DateTime.UtcNow;

        private List<BinanceRequest> RequestQueue { get; set; } = new List<BinanceRequest>();
        private List<BinanceRequest> FailRequestQueue { get; set; } = new List<BinanceRequest>();

        private object Locker = new object();
        private object FailRequestLocker = new object();
        private object UserDataSubcription { get; set; }

        public WebCallResult<string> ListenKey;

        public int orderCount { get; set; }

        public MarketManager()
        {
            subscribes = new Dictionary<string, Subscribe>();

            foreach (var symbol in SymbolInitialization.list)
            {
                subscribes.Add(symbol, new Subscribe());
            }

            Task.Run(() => Queue());
            Task.Run(() => BinanceFailRequestQueue());
            //Task.Run(() => TotalProfitMonitor());// дневной СЛ ТП

            //--

        }

        private void TotalProfitMonitor()
        {
            while (true)
            {
                if ( DateTime.UtcNow.Day != MarketData.DayStatus.CurrentDay)
                {
                    MarketData.DayStatus.CurrentDay = DateTime.UtcNow.Day;
                    MarketServices.GetLastDayProfit();
                    if (MarketData.DayStatus.IsSLTPTaken)
                    {
                        MarketData.DayStatus.IsSLTPTaken = false;
                        RobotsRun();
                        MarketData.Info.Message += "Роботы запущены после остановки при достижении порога СЛТП" + "\n";
                        MarketData.Info.IsMessageActive = true;
                    }
                }


                var totalCurrentProfit = MarketData.Info.DayProfit;
                foreach (var robot in RobotVM.robots)
                {
                    if (robot.Position != 0)
                    {
                        totalCurrentProfit += robot.Profit - robot.Commission;
                    }
                }
                MarketData.Info.TotalCurrentProfit = totalCurrentProfit;

                if (totalCurrentProfit > MarketData.DayStatus.DayTP || totalCurrentProfit < MarketData.DayStatus.DaySL)
                {
                    MarketData.DayStatus.IsSLTPTaken = true;
                    CloseRobotsPosition();
                    MarketData.Info.Message += "Роботы остановлены при достижении порога СЛТП. Результаты дня: "+ totalCurrentProfit + "\n";
                    MarketData.Info.IsMessageActive = true;
                }

                Thread.Sleep(400);
            }
        }
        private void BinanceFailRequestQueue()
        {
            while (true)
            {
                BinanceFailRequestManager();
                Thread.Sleep(70);
            }
        }

        private void Queue()
        {
            while (true)
            {
                BinanceRequestManager();
                Thread.Sleep(70);
            }
        }

        public void AddRequestQueue(BinanceRequest request)
        {
            lock (Locker)
            {
                RequestQueue.Add(request);
            }
        }

        public void AddRangeRequestQueue(List<BinanceRequest> requests)
        {
            lock (Locker)
            {
                RequestQueue.AddRange(requests);
            }
        }

        private void BinanceFailRequestManager()
        {
            lock (FailRequestLocker)
            {
                if (FailRequestQueue.Count > 0)
                {

                    foreach (var req in FailRequestQueue)
                    {
                        if (req.robotRequestType == RobotRequestType.PlaceOrder)
                        {
                            req.TryCount++;
                            if (req.TryCount < 6)
                            {
                                Thread.Sleep(req.TryCount * 1000);
                                PlaceBinanceOrder(req);
                            }
                            else
                            {
                                RobotServices.ForceStopRobotAsync(req.RobotIndex);
                            }
                        }

                        if (req.robotRequestType == RobotRequestType.CancelOrder)
                        {
                            req.TryCount++;
                            if (req.TryCount < 3)
                            {
                                Thread.Sleep(req.TryCount * 1500);
                                CancelBinanceOrder(req);
                            }
                            else
                            {
                                //обработать
                                //RobotServices.ForceStopRobotAsync(req.RobotId);
                            }
                        }
                    }

                    FailRequestQueue.Clear();
                }
            }
        }

        private void BinanceRequestManager()
        {
            lock (Locker)
            {
                if (RequestQueue.Count > 0)
                {
                    foreach (var q in RequestQueue)
                    {
                        if (q.robotRequestType == RobotRequestType.PlaceOrder)
                        {
                            PlaceBinanceOrder(BinanceRequestDTO.DTO(q));
                        }
                        if (q.robotRequestType == RobotRequestType.CancelOrder)
                        {
                            CancelBinanceOrder(BinanceRequestDTO.DTO(q));
                        }

                        Thread.Sleep(100);
                    }
                    RequestQueue.Clear();
                }
            }

        }

        private async void CancelBinanceOrder(BinanceRequest q)
        {
            if (q.OrderId == 0)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LogVM.AddRange(new List<RobotLog> { new RobotLog {
                    ClientId = 0,
                    Date = DateTime.Now,
                    Message = q.robotRequestType.ToString() + " " + q.Side.ToString() + " " + q.robotOrderType.ToString() +
                    " " + q.Price,

                    } });
                });
            }

            var result = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(q.Symbol, q.OrderId);
            if (result.Success)
            {
                
            }
            else
            {
                lock (FailRequestLocker)
                {
                    FailRequestQueue.Add(q);
                }

                var orderPrice = "";
                if (q.StopPrice != 0)
                {
                    orderPrice = q.StopPrice.ToString();
                }
                else
                {
                    orderPrice = q.Price.ToString();
                }
                RobotVM.robots[q.RobotIndex].Log(LogType.Error, "try:" + q.TryCount + " Cancel order error" + " id " + q.OrderId +
                    q.robotOrderType.ToString() + " " + " price " + orderPrice + " " + result.Error.ToString());
            }
        }

        private async void PlaceBinanceOrder(BinanceRequest q)
        {
            lock (Locker)
            {
                orderCount++;
            }
            var order = await MarketServices.PlaceBinanceOrder(q.StartDealOrderId, orderCount, q.RobotIndex, q.Symbol, (OrderSide)q.Side,
                                (FuturesOrderType)q.OrderType, q.Quantity, q.Price, q.StopPrice);

            if (order.Success)
            {
                
                switch (q.robotOrderType)
                {
                    case RobotOrderType.SignalBuy:
                        RobotVM.robots[q.RobotIndex].SignalBuyOrder = RobotOrderDTO.DTO(order, q.RobotIndex, q.StartDealOrderId);
                        RobotVM.robots[q.RobotIndex].RobotState.SignalBuyOrderId = RobotVM.robots[q.RobotIndex].SignalBuyOrder.OrderId;
                        break;
                    case RobotOrderType.SignalSell:
                        RobotVM.robots[q.RobotIndex].SignalSellOrder = RobotOrderDTO.DTO(order, q.RobotIndex, q.StartDealOrderId);
                        RobotVM.robots[q.RobotIndex].RobotState.SignalSellOrderId = RobotVM.robots[q.RobotIndex].SignalSellOrder.OrderId;
                        break;
                    case RobotOrderType.StopLoss:
                        RobotVM.robots[q.RobotIndex].StopLossOrder = RobotOrderDTO.DTO(order, q.RobotIndex, q.StartDealOrderId);
                        RobotVM.robots[q.RobotIndex].RobotState.StopLossOrderId = RobotVM.robots[q.RobotIndex].StopLossOrder.OrderId;
                        break;
                    case RobotOrderType.TakeProfit:
                        RobotVM.robots[q.RobotIndex].TakeProfitOrder = RobotOrderDTO.DTO(order, q.RobotIndex, q.StartDealOrderId);
                        RobotVM.robots[q.RobotIndex].RobotState.TakeProfitOrderId = RobotVM.robots[q.RobotIndex].TakeProfitOrder.OrderId;
                        break;
                    case RobotOrderType.ClosePosition:
                        RobotVM.robots[q.RobotIndex].ResetRobotData();
                        RobotVM.robots[q.RobotIndex].RobotState = new();
                        RobotServices.GetRobotDealByOrderId(order.Data.Id, q.RobotIndex);//формируем сделку откр + закр 
                        break;
                    default:
                        break;
                }
            }
            else
            {
                lock (FailRequestLocker)
                {
                    FailRequestQueue.Add(q);
                    
                }
                MarketData.Info.Message += "Place order error. RobotID "+ RobotVM.robots[q.RobotIndex].Id + " " + order.Error.ToString() + "\n";
                MarketData.Info.IsMessageActive = true;

                var orderPrice = string.Empty;
                if (q.StopPrice != 0)
                {
                    orderPrice = q.StopPrice.ToString();
                }
                else
                {
                    orderPrice = q.Price.ToString();
                }

                RobotVM.robots[q.RobotIndex].Log(LogType.Error, "try:" + q.TryCount + " Place order error " + q.robotOrderType.ToString() + " " + OrderTypes.Types[(int)q.OrderType] + " price " + orderPrice + " " + order.Error.ToString());


            }
        }


        private void SubscribtionController()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    if (DateTime.UtcNow > ListenKeyTimeUpdate.AddMinutes(30))
                    {
                        ListenKeyTimeUpdate = DateTime.UtcNow;
                        var updateResult = await BinanceApi.client.UsdFuturesApi.Account.KeepAliveUserStreamAsync(ListenKey.Data);
                        if (updateResult.Success)
                        {
                            Log(LogType.Info, "ListenKey  Updated");
                        }
                        else
                        {
                            Log(LogType.Error, "ListenKey   Update Error");
                        }
                    }
                    Thread.Sleep(10000);
                }
            });
        }

        public async void ActivateUserDataStream()
        {
            ListenKey = await BinanceApi.client.UsdFuturesApi.Account.StartUserStreamAsync();

            if (!ListenKey.Success)
            {
                Log(LogType.Error, "ListenKey   Update Error");

                return;
            }

            SubscribtionController(); //каждые 30 мин продлеваем ключ

            UserDataSubcription = await BinanceSocket.client.UsdFuturesStreams.SubscribeToUserDataUpdatesAsync(ListenKey.Data,
                data =>
                {
                    // Handle leverage update                    
                },
                data =>
                {
                    // Handle margin update

                },
                data =>
                {
                    // Handle account balance update, caused by trading
                    foreach (var position in data.Data.UpdateData.Positions)
                    {
                        var symbolIndex = MarketServices.GetSymbolIndex(position.Symbol);
                        if (symbolIndex != -1)
                        {
                            SymbolVM.symbols[symbolIndex].Position = position.Quantity;
                        }

                    }
                    var bal = data.Data.UpdateData.Balances.FirstOrDefault();

                },
            data =>
            {
                // Handle order update
                foreach (var robot in RobotVM.robots)//каждому роботу отсылаем обновленный ордер
                {
                    if (robot.IsRun)
                    {
                        robot.NewOrderUpdate(data);
                    }


                    robot.CustomOrderUpdate(data);

                }

                Log(LogType.UpdateOrder, data.Data.UpdateData.OrderId + " " +
                                data.Data.UpdateData.Status + " " +
                                data.Data.UpdateData.Price + " " +
                                data.Data.UpdateData.StopPrice + " " +
                                data.Data.UpdateData.AveragePrice + " " +
                                data.Data.UpdateData.Side);


            },
                data =>
                {
                    // Handle listen key expired
                    Log(LogType.Info, "Handle listen key expired " + data.Data.Event);
                    MarketServices.StopAllSubscribes();
                    MarketServices.StartAllSubscribes();

                });
        }

        public async void ActivateSubscribe()
        {
            //IEnumerable<string> IEsymbols = symbols.ToArray();

            foreach (var tfDictionary in MarketData.CandleDictionary)
            {
                foreach (var candleDictionary in tfDictionary.Value)
                {
                    var subscription = await BinanceSocket.client.UsdFuturesStreams.SubscribeToKlineUpdatesAsync(
                            tfDictionary.Key, (KlineInterval)candleDictionary.Key, data => SetKline(data));

                    subscription.Data.ConnectionLost += () =>
                    {
                        Log(LogType.Info, " Connection lost, trying to reconnect..");
                    };
                    subscription.Data.ConnectionRestored += (t) =>
                    {
                        Log(LogType.Info, " Connection restored " + t.ToString());
                    };

                    updateSubscriptions.Add(subscription);
                    Thread.Sleep(30);
                }
            }
        }

        public void SetKline(DataEvent<IBinanceStreamKlineData> data)
        {
            var selectedRobot = RobotVM.robots[MarketData.Info.SelectedRobotIndex];

            var klineIinterval = data.Data.Data.Interval;
            var klineSymbol = data.Data.Symbol;
            var robotIinterval = selectedRobot.BaseSettings.TimeFrame;
            var robotSymbol = selectedRobot.Symbol;

            //рисуем график если робот выбран
            if (robotSymbol == klineSymbol && (int)klineIinterval == robotIinterval)
            {
                MarketServices.candleChartDwaw(MarketData.CandleDictionary[robotSymbol][robotIinterval], new List<RobotTrade>(),
                    selectedRobot.BaseSettings.TimeFrame);



                if (selectedRobot.BaseSettings.SLPercent)
                {
                    selectedRobot.BaseSettings.StopLossPercent =
                        Math.Round(data.Data.Data.ClosePrice / 100 * selectedRobot.BaseSettings.StopLoss, SymbolIndexes.price[robotSymbol]);
                }
                else
                {
                    selectedRobot.BaseSettings.StopLossPercent = selectedRobot.BaseSettings.StopLoss;
                }

                if (selectedRobot.BaseSettings.TPPercent)
                {
                    selectedRobot.BaseSettings.TakeProfitPercent =
                        Math.Round(data.Data.Data.ClosePrice / 100 * selectedRobot.BaseSettings.TakeProfit, SymbolIndexes.price[robotSymbol]);
                }
                else
                {
                    selectedRobot.BaseSettings.TakeProfitPercent = selectedRobot.BaseSettings.TakeProfit;
                }

                if (selectedRobot.BaseSettings.IsOffsetPercent)
                {
                    selectedRobot.BaseSettings.OffsetPercent =
                        Math.Round(data.Data.Data.ClosePrice / 100 * selectedRobot.BaseSettings.Offset, SymbolIndexes.price[robotSymbol]);
                }
                else
                {
                    selectedRobot.BaseSettings.OffsetPercent = selectedRobot.BaseSettings.Offset;
                }

            }


            if (data.Data.Data.Interval == KlineInterval.OneDay)// передаем цены в UI
            {
                SymbolVM.symbols[MarketServices.GetSymbolIndex(data.Data.Symbol)].Price = data.Data.Data.ClosePrice;
            }

            if (MarketData.CandleDictionary[data.Data.Symbol][(int)data.Data.Data.Interval].Count == 0)
            {
                Task.Run(() => BinanceApi.GetCandles(data.Data.Symbol, (int)data.Data.Data.Interval, 100));
            }
            else
            {

                if (data.Data.Data.CloseTime >
                    MarketData.CandleDictionary[data.Data.Symbol][(int)data.Data.Data.Interval][^1].CloseTime)
                {
                    MarketData.CandleDictionary[data.Data.Symbol][(int)data.Data.Data.Interval].Add(KlineDataDTO.DataToCandle(data));

                }
                else
                {
                    MarketData.CandleDictionary[data.Data.Symbol][(int)data.Data.Data.Interval][^1] = KlineDataDTO.DataToCandle(data);
                }


            }

        }

        public void RobotsRun()
        {

            foreach (var robot in RobotVM.robots)
            {
                if (robot.IsActivated)
                {
                    Task.Run(() => robot.Run());
                }

            }
        }

        public void RobotsStop()
        {
            foreach (var robot in RobotVM.robots)
            {
                if (robot.IsActivated)
                {
                    robot.Stop();
                }
            }
        }

        public void CloseRobotsPosition()
        {
            foreach (var robot in RobotVM.robots)
            {
                if (robot.IsActivated)
                {
                    robot.CloseRobotPosition();
                }
            }
        }




        public void Log(LogType type, string message)
        {
            var log = new RobotLog
            {
                RobotId = -1,
                ClientId = MarketData.Info.ClientId,
                Date = DateTime.UtcNow,
                Type = (int)type,
                Message = message
            };

            Task.Run(() => ApiServices.SaveLog(log));
        }

        public void UpdateRobotTrade(RobotTrade trade)
        {
            Task.Run(() => ApiServices.UpdateTrade(trade));
            RobotServices.SaveCustomRobotDealByOrderId(trade.StartDealOrderId, trade.OrderId, RobotServices.GetRobotIndex(trade.RobotId));
        }




    }
}
