using Binance.Infrastructure.Constants;
using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Interfaces.Clients;
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using static ShortPeakRobot.MainWindow;

namespace ShortPeakRobot.Market
{
    public class MarketManager
    {
        public ApplicationDbContext _context { get; set; }
        public Dictionary<string, Subscribe> subscribes { get; set; }
        public List<CallResult<UpdateSubscription>> updateSubscriptions { get; set; } = new List<CallResult<UpdateSubscription>>();

        private DateTime ListenKeyTimeUpdate = DateTime.UtcNow;

        //private List<BinanceRequest> queue = new List<BinanceRequest>();

        private List<BinanceRequest> RequestQueue { get; set; } = new List<BinanceRequest>();

        private object Locker = new object();
        private object UserDataSubcription { get; set; }

        public WebCallResult<string> ListenKey;
        public MarketManager()
        {
            _context = new ApplicationDbContext();

            subscribes = new Dictionary<string, Subscribe>();

            foreach (var symbol in SymbolInitialization.list)
            {
                subscribes.Add(symbol, new Subscribe());
            }

            Task.Run(() => Queue());


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
                            PlaceBinanceOrder(q);
                        }
                        if (q.robotRequestType == RobotRequestType.CancelOrder)
                        {
                            CancelBinanceOrder(q);
                        }

                        Thread.Sleep(50);
                    }
                    RequestQueue.Clear();
                }


            }





        }

        private async void CancelBinanceOrder(BinanceRequest q)
        {
            long orderId = 0;
            string desc = "";
            RobotOrder order = new RobotOrder();
            switch (q.robotOrderType)
            {
                case RobotOrderType.SignalBuy:
                    orderId = RobotVM.robots[q.RobotId].SignalBuyOrder.OrderId;
                    order = RobotVM.robots[q.RobotId].SignalBuyOrder;
                    RobotVM.robots[q.RobotId].RobotState.SignalBuyOrderId = 0;
                    RobotVM.robots[q.RobotId].SignalBuyOrder = new();
                    RobotServices.SaveState(q.RobotId, RobotVM.robots[q.RobotId].RobotState);
                    desc = "Cancel SignalBuy Order";
                    break;
                case RobotOrderType.SignalSell:
                    orderId = RobotVM.robots[q.RobotId].SignalSellOrder.OrderId;
                    order = RobotVM.robots[q.RobotId].SignalSellOrder;
                    RobotVM.robots[q.RobotId].RobotState.SignalSellOrderId = 0;
                    RobotVM.robots[q.RobotId].SignalSellOrder = new();
                    RobotServices.SaveState(q.RobotId, RobotVM.robots[q.RobotId].RobotState);
                    desc = "Cancel SignalSell Order";
                    break;
                case RobotOrderType.StopLoss:
                    orderId = RobotVM.robots[q.RobotId].StopLossOrder.OrderId;
                    order = RobotVM.robots[q.RobotId].StopLossOrder;
                    RobotVM.robots[q.RobotId].RobotState.StopLossOrderId = 0;
                    RobotVM.robots[q.RobotId].StopLossOrder = new();
                    RobotServices.SaveState(q.RobotId, RobotVM.robots[q.RobotId].RobotState);
                    desc = "Cancel StopLoss Order";
                    break;
                case RobotOrderType.TakeProfit:
                    orderId = RobotVM.robots[q.RobotId].TakeProfitOrder.OrderId;
                    order = RobotVM.robots[q.RobotId].TakeProfitOrder;
                    RobotVM.robots[q.RobotId].RobotState.TakeProfitOrderId = 0;
                    RobotVM.robots[q.RobotId].TakeProfitOrder = new();
                    RobotServices.SaveState(q.RobotId, RobotVM.robots[q.RobotId].RobotState);
                    desc = "Cancel TakeProfit Order";
                    break;
                case RobotOrderType.OrderId:
                    orderId = q.OrderId;
                    order.OrderId = q.OrderId;                     
                    desc = "Cancel All Orders";
                    break;
                default:
                    break;
            }

            if (order.Status == (int)OrderStatus.New || order.Status == (int)OrderStatus.PartiallyFilled || order.Status == -1)
            {
                var result = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(q.Symbol, orderId);
                if (result.Success)
                {
                    order.Status = (int)OrderStatus.Canceled;
                    RobotServices.SaveOrder(q.RobotId, order, desc);                                       
                }
                else
                {
                    //обработать
                    Log(LogType.Error, result.Error.ToString());
                }
            }

           

        }

        private async void PlaceBinanceOrder(BinanceRequest q)
        {
            var order = await MarketServices.PlaceBinanceOrder(q.RobotId, q.Symbol, (OrderSide)q.Side,
                                (FuturesOrderType)q.OrderType, q.Quantity, q.Price, q.StopPrice);

            if (order.Success)
            {
                switch (q.robotOrderType)
                {
                    case RobotOrderType.SignalBuy:
                        RobotVM.robots[q.RobotId].SignalBuyOrder = RobotOrderDTO.DTO(order, q.RobotId);
                        RobotVM.robots[q.RobotId].RobotState.SignalBuyOrderId = RobotVM.robots[q.RobotId].SignalBuyOrder.OrderId;
                        RobotServices.SaveOrder(q.RobotId, RobotVM.robots[q.RobotId].SignalBuyOrder, "Place SignalBuy Order");
                        break;
                    case RobotOrderType.SignalSell:
                        RobotVM.robots[q.RobotId].SignalSellOrder = RobotOrderDTO.DTO(order, q.RobotId);
                        RobotVM.robots[q.RobotId].RobotState.SignalSellOrderId = RobotVM.robots[q.RobotId].SignalSellOrder.OrderId;
                        RobotServices.SaveOrder(q.RobotId, RobotVM.robots[q.RobotId].SignalSellOrder, "Place SignalSell Order");
                        break;
                    case RobotOrderType.StopLoss:
                        RobotVM.robots[q.RobotId].StopLossOrder = RobotOrderDTO.DTO(order, q.RobotId);
                        RobotVM.robots[q.RobotId].RobotState.StopLossOrderId = RobotVM.robots[q.RobotId].StopLossOrder.OrderId;
                        RobotServices.SaveOrder(q.RobotId, RobotVM.robots[q.RobotId].StopLossOrder, "Place StopLoss Order");
                        break;
                    case RobotOrderType.TakeProfit:
                        RobotVM.robots[q.RobotId].TakeProfitOrder = RobotOrderDTO.DTO(order, q.RobotId);
                        RobotVM.robots[q.RobotId].RobotState.TakeProfitOrderId = RobotVM.robots[q.RobotId].TakeProfitOrder.OrderId;
                        RobotServices.SaveOrder(q.RobotId, RobotVM.robots[q.RobotId].TakeProfitOrder, "Place TakeProfit Order");
                        break;
                    default:
                        break;
                }

                RobotServices.SaveState(q.RobotId, RobotVM.robots[q.RobotId].RobotState);
                MarketServices.GetRobotData(q.RobotId);//for IU
            }
            else
            {
                RobotVM.robots[q.RobotId].Log(LogType.Error, "Place order error" + order.Error.ToString());
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
                        SymbolVM.symbols[MarketServices.GetSymbolIndex(position.Symbol)].Position = position.Quantity;
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
                                data.Data.UpdateData.Side);


            },
                data =>
                {
                    // Handle listen key expired
                    Log(LogType.Info, "Handle listen key expired " + data.Data.Event);
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
                    Thread.Sleep(50);
                }
            }
        }

        public void SetKline(DataEvent<IBinanceStreamKlineData> data)
        {
            var selectedRobot = RobotVM.robots[MarketData.Info.SelectedRobotId];

            var klineIinterval = data.Data.Data.Interval;
            var klineSymbol = data.Data.Symbol;
            var robotIinterval = selectedRobot.BaseSettings.TimeFrame;
            var robotSymbol = selectedRobot.Symbol;

            //рисуем график если робот выбран
            if (robotSymbol == klineSymbol && (int)klineIinterval == robotIinterval)
            {
                MarketServices.candleChartDwaw(MarketData.CandleDictionary[robotSymbol][robotIinterval], new List<RobotTrade>(),
                    selectedRobot.BaseSettings.TimeFrame);

                if (selectedRobot.BaseSettings.IsVariableLot)
                {
                    selectedRobot.BaseSettings.Volume =
                        Math.Round(selectedRobot.BaseSettings.Deposit / data.Data.Data.ClosePrice, SymbolIndexes.lot[robotSymbol]);

                    selectedRobot.BaseSettings.CurrentDeposit = selectedRobot.BaseSettings.Deposit + selectedRobot.BaseSettings.Profit;
                }

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

            }


            if (data.Data.Data.Interval == KlineInterval.OneMinute)// передаем цены в UI
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


        public void RobotsRun(List<int> robotIds)
        {
            foreach (var id in robotIds)
            {
                Task.Run(() => RobotVM.robots[id].Run());
            }
        }

        public void RobotsStop(List<int> robotIds)
        {
            foreach (var id in robotIds)
            {
                RobotVM.robots[id].Stop();
            }
        }

        public bool CheckRobotsState()
        {
            if (RobotVM.robots.Where(x => x.IsRun).Count() > 0)
            {
                return true;
            }
            return false;
        }

        //private async void GetTrades()
        //{
        //    foreach (var symbol in SymbolInitialization.list)
        //    {
        //        var trades = await BinanceApi.client.UsdFuturesApi.Trading.GetUserTradesAsync(symbol,
        //                                startTime: DateTime.UtcNow.AddDays(-1));
        //        if (trades.Success)
        //        {
        //            MarketData.TradeDictionary[symbol].AddRange(BinTradeDTO.TradesDTO(trades));
        //        }
        //        else
        //        {
        //            //обработать
        //        }
        //    }
        //}

        //private async void GetOrders()
        //{
        //    foreach (var symbol in SymbolInitialization.list)
        //    {
        //        var orders = await BinanceApi.client.UsdFuturesApi.Trading.GetOrdersAsync(symbol,
        //                startTime: DateTime.UtcNow.AddDays(-1));
        //        if (orders.Success)
        //        {
        //            MarketData.OrderDictionary[symbol].AddRange(BinOrderDTO.OrdersDTO(orders));
        //        }
        //        else
        //        {
        //            //обработать
        //        }
        //    }
        //}

        public void Log(LogType type, string message)
        {

            var log = new RobotLog
            {
                RobotId = -1,
                ClientId = RobotsInitialization.ClientId,
                Date = DateTime.UtcNow,
                Type = (int)type,
                Message = message
            };


            _context.RobotLogs.Add(log);
            _context.SaveChangesAsync();

            App.Current.Dispatcher.Invoke(() => LogVM.logs.Add(log));


        }
    }
}
