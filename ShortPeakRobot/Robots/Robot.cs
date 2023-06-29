using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots.Algorithms;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots
{
    public class Robot : BaseVM
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public string AlgorithmName { get; set; }

        public List<RobotLog> RobotLogsQueue { get; set; } = new List<RobotLog>();
        public List<RobotLog> RobotLogsUnsaved { get; set; } = new List<RobotLog>();

        public List<RobotOrder> RobotOrdersQueue { get; set; } = new List<RobotOrder>();
        public List<RobotOrder> RobotOrdersUnsaved { get; set; } = new List<RobotOrder>();

        public List<RobotTrade> RobotTradesQueue { get; set; } = new List<RobotTrade>();
        public List<RobotTrade> RobotTradesUnsaved { get; set; } = new List<RobotTrade>();

        public List<RobotDeal> RobotDealsQueue { get; set; } = new List<RobotDeal>();
        public List<RobotDeal> RobotDealsUnsaved { get; set; } = new List<RobotDeal>();



        public RobotCommands Command { get; set; } = RobotCommands.Nothing;

        public object Locker = new object();

        public bool IsReady { get; set; }//выполнены все проверки перед запуском робота




        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set
            {
                if (_Selected != value)
                {
                    _Selected = value;
                    OnPropertyChanged("Selected");
                }
            }
        }

        private decimal _Position;
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

        public decimal SessionProfit { get; set; }


        private decimal _Profit;
        public decimal Profit
        {
            get { return _Profit; }
            set
            {
                if (_Profit != value)
                {
                    _Profit = value;
                    OnPropertyChanged("Profit");
                }
            }
        }

        private decimal _Commission;
        public decimal Commission
        {
            get { return _Commission; }
            set
            {
                if (_Commission != value)
                {
                    _Commission = value;
                    OnPropertyChanged("Commission");
                }
            }
        }

        private int _Orders;
        public int Orders
        {
            get { return _Orders; }
            set
            {
                if (_Orders != value)
                {
                    _Orders = value;
                    OnPropertyChanged("Orders");
                }
            }
        }

        private bool _IsRun;
        public bool IsRun
        {
            get { return _IsRun; }
            set
            {
                if (_IsRun != value)
                {
                    _IsRun = value;
                    OnPropertyChanged("IsRun");
                }
            }
        }

        private bool _IsActivated;
        public bool IsActivated
        {
            get { return _IsActivated; }
            set
            {
                if (_IsActivated != value)
                {
                    _IsActivated = value;
                    OnPropertyChanged("IsActivated");
                }
            }
        }




        private string _Symbol;
        public string Symbol
        {
            get { return _Symbol; }
            set
            {
                if (_Symbol != value)
                {
                    _Symbol = value;
                    OnPropertyChanged("Symbol");
                }
            }
        }


        public BaseRobotSettings BaseSettings { get; set; }
        public Algorithm algorithm { get; set; }

        //----------------------------------------
        public RobotOrder SignalSellOrder { get; set; } = new RobotOrder();
        public RobotOrder SignalBuyOrder { get; set; } = new RobotOrder();
        public RobotOrder StopLossOrder { get; set; } = new RobotOrder();
        public RobotOrder TakeProfitOrder { get; set; } = new RobotOrder();
        public decimal OpenPositionPrice { get; set; }

        public RobotState RobotState { get; set; } = new RobotState();
        public RobotState LastRobotState { get; set; } = new RobotState();
        //=====================================
        public void CustomOrderUpdate(DataEvent<BinanceFuturesStreamOrderUpdate> data)
        {
            var arrClientOrderId = data.Data.UpdateData.ClientOrderId.Split(':');
            //сохраняем ордер
            if (arrClientOrderId.Length > 2 && arrClientOrderId[0] == "robot")
            {
                var robotId = Convert.ToInt32(arrClientOrderId[1]);

                if (robotId == Id)
                {
                    var order = RobotOrderDTO.DTO(data, robotId);
                    order.StartDeposit = MarketData.Info.Deposit / 100 * BaseSettings.Deposit;
                    RobotServices.SaveOrder(order, "");//to Db

                }
            }
            //сохраняем сделки
            if (data.Data.UpdateData.Status == OrderStatus.Filled || data.Data.UpdateData.Status == OrderStatus.PartiallyFilled)
            {
                if (arrClientOrderId.Length > 2 && arrClientOrderId[0] == "robot")
                {
                    var robotId = Convert.ToInt32(arrClientOrderId[1]);
                    var startDealOrderId = Convert.ToInt64(arrClientOrderId[2]);

                    if (robotId == Id)
                    {
                        var trade = RobotTradeDTO.DTO(data, robotId, startDealOrderId);
                        trade.StartDeposit = MarketData.Info.Deposit / 100 * BaseSettings.Deposit;
                        RobotServices.SaveTrade(trade);//to Db
                    }
                }
            }
        }

        public void NewOrderUpdate(DataEvent<BinanceFuturesStreamOrderUpdate> data)
        {

            //----------------------Signal order status
            if (data.Data.UpdateData.OrderId == SignalSellOrder.OrderId &&
                data.Data.UpdateData.Status == OrderStatus.Filled)
            {
                if (SignalBuyOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = Index,
                        Symbol = Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = SignalBuyOrder.OrderId,
                        OrderType = SignalBuyOrder.Type
                    });
                    RobotState.SignalBuyOrderId = 0;
                    SignalBuyOrder = new();
                }

                var volume = data.Data.UpdateData.Quantity;
                var startDealOrderId = data.Data.UpdateData.OrderId;

                Task.Run(() =>// выставляем СЛ ТП
                {
                    var signalPrice = (decimal)SignalSellOrder.StopPrice;
                    if ((decimal)SignalSellOrder.StopPrice == 0)
                    { signalPrice = SignalSellOrder.Price; }
                    SetSLTP(OrderSide.Sell, volume, signalPrice, startDealOrderId);
                });
                return;
            }
            //-----------------------
            if (data.Data.UpdateData.OrderId == SignalBuyOrder.OrderId &&
                data.Data.UpdateData.Status == OrderStatus.Filled)
            {
                if (SignalSellOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = Index,
                        Symbol = Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = SignalSellOrder.OrderId,
                        OrderType = SignalSellOrder.Type
                    });
                    RobotState.SignalSellOrderId = 0;
                    SignalSellOrder = new();
                }


                var volume = data.Data.UpdateData.Quantity;
                var startDealOrderId = data.Data.UpdateData.OrderId;

                Task.Run(() =>
                {
                    var signalPrice = (decimal)SignalBuyOrder.StopPrice;
                    if ((decimal)SignalBuyOrder.StopPrice == 0)
                    { signalPrice = SignalBuyOrder.Price; }
                    SetSLTP(OrderSide.Buy, volume, signalPrice, startDealOrderId);
                });
                return;
            }

            //------------------------stoploss status check
            if (data.Data.UpdateData.OrderId == StopLossOrder.OrderId &&
                data.Data.UpdateData.Status == OrderStatus.Filled)
            {
                Position = 0;


                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = TakeProfitOrder.OrderId,
                    OrderType = TakeProfitOrder.Type
                });

                TakeProfitOrder = new();
                RobotState = new();
                //формируем и сохраняем в базу сделку открытие + закрытие
                RobotServices.GetRobotDealByOrderId(data.Data.UpdateData.OrderId, Index);
                
                return;
            }

            //takeprofit status check
            if (data.Data.UpdateData.OrderId == TakeProfitOrder.OrderId &&
                data.Data.UpdateData.Status == OrderStatus.Filled)
            {
                Position = 0;


                //CancelOrderAsync(StopLossOrder, "Cancel StopLoss Order");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = StopLossOrder.OrderId,
                    OrderType = StopLossOrder.Type
                });

                StopLossOrder = new();
                RobotState = new();
                RobotServices.GetRobotDealByOrderId(data.Data.UpdateData.OrderId, Index);

                return;
            }

            //Partial takeprofit status check
            if (data.Data.UpdateData.OrderId == TakeProfitOrder.OrderId &&
                data.Data.UpdateData.Status == OrderStatus.PartiallyFilled)
            {
                if (data.Data.UpdateData.Side == OrderSide.Sell)
                {
                    Position = data.Data.UpdateData.Quantity;
                }
                else
                {
                    Position = -data.Data.UpdateData.Quantity;
                }

                RobotState.Position = Position;

                //if (Position == 0)
                //{
                //    RobotServices.GetRobotDealByOrderId(data.Data.UpdateData.OrderId, Id);
                //}


                //CancelOrderAsync(StopLossOrder, "Reset StopLoss Order");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = StopLossOrder.OrderId,
                    OrderType = StopLossOrder.Type
                });



                SetPartialStopLoss(data);


                return;
            }
        }


        public void Run()
        {
            Log(LogType.Info, Name + " Робот запущен.");
            Command = RobotCommands.ResetCandleAnalyse;

            IsRun = true;


            while (IsRun) // цыкл  для основных функций робота
            {
                switch (AlgorithmName)
                {
                    case "ShortPeak": ((ShortPeak)algorithm.Algo).NewTick(Command); break;
                    case "VWAPHL": ((VWAPHL)algorithm.Algo).NewTick(Command); break;
                    case "LastDayHL": ((LastDayHL)algorithm.Algo).NewTick(Command); break;
                    case "LastDayHL10": ((LastDayHL10)algorithm.Algo).NewTick(Command); break;
                    case "SL3": ((SL3)algorithm.Algo).NewTick(Command); break;
                    case "ShortPeakPlusTime": ((ShortPeakPlusTime)algorithm.Algo).NewTick(Command); break;
                    case "BollEma": ((BollingerEma)algorithm.Algo).NewTick(Command); break;
                    case "BollStd": ((BollingerStd)algorithm.Algo).NewTick(Command); break;
                    case "MedianaLD": ((MedianaLD)algorithm.Algo).NewTick(Command); break;
                }

                if (Command != RobotCommands.Nothing)
                {
                    Command = RobotCommands.Nothing;
                }
                Thread.Sleep(50);
            }


            Log(LogType.Info, Name + " Робот остановлен.");

        }

        public void Stop()
        {
            IsRun = false;
        }

        public void RunSilentMode()
        {
            while (true) // цыкл  для основных функций робота
            {
                DbManager();
                Thread.Sleep(1500);
            }
        }

        public void CheckSLTPCross(decimal price)
        {



        }

        private void DbManager()
        {


            //сохраняем состояние (локально + DB)
            if (!RobotServices.CompareState(RobotState, LastRobotState))
            {
                LastRobotState = RobotStateDTO.DTO(RobotState);

                Task.Run(async () =>
                {
                    await JsonDataServices.SaveRobotStateAsync(Id, LastRobotState);
                    var response = await ApiServices.UpdateRobotStateAsync(Id, LastRobotState);
                    Thread.Sleep(100);
                });

            }



            if (RobotLogsQueue.Count > 0)
            {
                var RobotLogsTemp = new List<RobotLog>();

                lock (Locker)
                {
                    foreach (var log in RobotLogsQueue)
                    {
                        RobotLogsTemp.Add(RobotLogsDTO.DTO(log));
                    }

                    RobotLogsQueue.Clear();
                }

                Task.Run(async () =>
                {
                    var flagApiError = false;
                    foreach (var log in RobotLogsTemp)
                    {
                        var response = await ApiServices.SaveLog(log);

                        if (!response.success)
                        {
                            RobotLogsUnsaved.Add(log);
                            flagApiError = true;
                        }
                        Thread.Sleep(200);
                    }
                    if (flagApiError)
                    {
                        await JsonDataServices.SaveRobotLogsAsync(Id, RobotLogsUnsaved);
                    }
                    else
                    {
                        var RobotLogsUnsavedTemp = new List<RobotLog>();
                        foreach (var log in RobotLogsUnsaved)
                        {
                            var response = await ApiServices.SaveLog(log);
                            if (!response.success)
                            {
                                RobotLogsUnsavedTemp.Add(log);
                            }
                            Thread.Sleep(100);
                        }
                        RobotLogsUnsaved = RobotLogsUnsavedTemp;
                        await JsonDataServices.SaveRobotLogsAsync(Id, RobotLogsUnsaved);
                    }
                    Thread.Sleep(500);
                    MarketServices.GetRobotMarketData(Index);

                });

            }


            if (RobotOrdersQueue.Count > 0)
            {
                var RobotOrdersTemp = new List<RobotOrder>();

                lock (Locker)
                {
                    foreach (var order in RobotOrdersQueue)
                    {
                        RobotOrdersTemp.Add(RobotOrderDTO.DTO(order));
                    }

                    RobotOrdersQueue.Clear();
                }

                var flagApiError = false;
                Task.Run(async () =>
                {
                    foreach (var order in RobotOrdersTemp)
                    {
                        var response = await ApiServices.SaveOrder(order);

                        if (!response.success)
                        {
                            RobotOrdersUnsaved.Add(order);
                            flagApiError = true;
                        }
                        Thread.Sleep(200);
                    }
                    if (flagApiError)
                    {
                        await JsonDataServices.SaveRobotOrdersAsync(Id, RobotOrdersUnsaved);
                    }
                    else
                    {
                        var RobotOrdersUnsavedTemp = new List<RobotOrder>();
                        foreach (var order in RobotOrdersUnsaved)
                        {
                            var response = await ApiServices.SaveOrder(order);
                            if (!response.success)
                            {
                                RobotOrdersUnsavedTemp.Add(order);
                            }
                            Thread.Sleep(100);
                        }
                        RobotOrdersUnsaved = RobotOrdersUnsavedTemp;
                        await JsonDataServices.SaveRobotOrdersAsync(Id, RobotOrdersUnsaved);
                    }
                    Thread.Sleep(500);
                    MarketServices.GetRobotMarketData(Index);
                });


            }

            //-----------------------------------------------------
            if (RobotTradesQueue.Count > 0)
            {
                var RobotTradesTemp = new List<RobotTrade>();

                lock (Locker)
                {
                    foreach (var trade in RobotTradesQueue)
                    {
                        RobotTradesTemp.Add(RobotTradeDTO.DTO(trade));
                    }

                    RobotTradesQueue.Clear();
                }

                Task.Run(async () =>
                {
                    var flagApiError = false;
                    foreach (var trade in RobotTradesTemp)
                    {
                        var response = await ApiServices.SaveTrade(trade);

                        if (!response.success)
                        {
                            RobotTradesUnsaved.Add(trade);
                            flagApiError = true;
                        }
                        Thread.Sleep(200);
                    }
                    if (flagApiError)
                    {
                        await JsonDataServices.SaveRobotTradesAsync(Id, RobotTradesUnsaved);
                    }
                    else
                    {
                        var RobotTradesUnsavedTemp = new List<RobotTrade>();
                        foreach (var trade in RobotTradesUnsaved)
                        {
                            var response = await ApiServices.SaveTrade(trade);
                            if (!response.success)
                            {
                                RobotTradesUnsavedTemp.Add(trade);
                            }
                            Thread.Sleep(100);
                        }
                        RobotTradesUnsaved = RobotTradesUnsavedTemp;
                        await JsonDataServices.SaveRobotTradesAsync(Id, RobotTradesUnsaved);
                    }
                    Thread.Sleep(500);
                    MarketServices.GetRobotMarketData(Index);

                });
            }


            if (RobotDealsQueue.Count > 0)
            {
                var RobotDealsTemp = new List<RobotDeal>();

                lock (Locker)
                {
                    foreach (var deal in RobotDealsQueue)
                    {
                        RobotDealsTemp.Add(RobotDealDTO.DTO(deal));
                    }

                    RobotDealsQueue.Clear();
                }

                Task.Run(async () =>
                {
                    var flagApiError = false;
                    foreach (var deal in RobotDealsTemp)
                    {
                        var response = await ApiServices.SaveDeal(deal);

                        if (!response.success)
                        {
                            RobotDealsUnsaved.Add(deal);
                            flagApiError = true;
                        }
                        Thread.Sleep(200);
                    }
                    if (flagApiError)
                    {
                        await JsonDataServices.SaveRobotDealsAsync(Id, RobotDealsUnsaved);
                    }
                    else
                    {
                        var RobotDealsUnsavedTemp = new List<RobotDeal>();
                        foreach (var deal in RobotDealsUnsaved)
                        {
                            var response = await ApiServices.SaveDeal(deal);
                            if (!response.success)
                            {
                                RobotDealsUnsavedTemp.Add(deal);
                            }
                            Thread.Sleep(100);
                        }
                        RobotDealsUnsaved = RobotDealsUnsavedTemp;
                        await JsonDataServices.SaveRobotDealsAsync(Id, RobotDealsUnsaved);
                    }
                    Thread.Sleep(500);
                    MarketServices.GetRobotMarketData(Index);
                    MarketServices.GetLastDayProfit();
                });
            }




        }


        public  void CloseRobotPosition()
        {

            if (Position > 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    StartDealOrderId = TakeProfitOrder.StartDealOrderId,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.Market,
                    Quantity = Math.Abs(Position),
                    Price = 0,
                    StopPrice = 0,
                    robotOrderType = RobotOrderType.ClosePosition,
                    robotRequestType = RobotRequestType.PlaceOrder
                });
            }

            if (Position < 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    StartDealOrderId = TakeProfitOrder.StartDealOrderId,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.Market,
                    Quantity = Math.Abs(Position),
                    Price = 0,
                    StopPrice = 0,
                    robotOrderType = RobotOrderType.ClosePosition,
                    robotRequestType = RobotRequestType.PlaceOrder
                });
            }

            Position = 0;
            Profit = 0;
            Commission = 0;

            RobotState.Position = 0;
            RobotState.OpenPositionPrice = 0;

            CancelRobotOrdersAsync();


        }

        public async void ChangeRobotPosition(OrderSide side, decimal qty)
        {
            var plasedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                   symbol: Symbol,
                   side: side,
                   type: FuturesOrderType.Market,
                   quantity: qty,
                   newClientOrderId: "robot:" + Id + ":" +
                       new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).ToUnixTimeSeconds());

            if (plasedOrder.Success)
            {
                if (side == OrderSide.Buy)
                {
                    Position += qty;
                }
                else
                {
                    Position -= qty;
                }
            }
            else
            {
                //обработать
                Log(LogType.Error, "Change position errror" + plasedOrder.Error.ToString());
            }

        }


        public void Log(LogType type, string message)
        {

            var log = new RobotLog
            {
                RobotId = Id,
                ClientId = MarketData.Info.ClientId,
                Date = DateTime.UtcNow,
                Type = (int)type,
                Message = message
            };
            lock (Locker)
            {
                RobotLogsQueue.Add(log);
            }


        }


        public void SetSLTP(OrderSide side, decimal volume, decimal signalPrice, long startDealOrderId)
        {
            if (side == OrderSide.Sell)// СЛ ТП на продажу
            {
                Position = -volume;
                RobotState.Position = -volume;
                //--------StopLoss
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    StartDealOrderId = startDealOrderId,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = volume,
                    Price = 0,
                    StopPrice = signalPrice + BaseSettings.StopLossPercent,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.PlaceOrder
                });


                //--------TakeProfit
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    StartDealOrderId = startDealOrderId,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.Limit,
                    Quantity = volume,
                    Price = signalPrice - BaseSettings.TakeProfitPercent,
                    StopPrice = 0,
                    robotOrderType = RobotOrderType.TakeProfit,
                    robotRequestType = RobotRequestType.PlaceOrder
                });

                if ((decimal)SignalSellOrder.StopPrice == 0)
                {
                    OpenPositionPrice = (decimal)SignalSellOrder.Price;
                }
                else
                {
                    OpenPositionPrice = (decimal)SignalSellOrder.StopPrice;
                }
                RobotState.OpenPositionPrice = OpenPositionPrice;
            }
            else // СЛ ТП на покупку
            {
                Position = volume;
                RobotState.Position = volume;

                //--------StopLoss
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    StartDealOrderId = startDealOrderId,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = volume,
                    Price = 0,
                    StopPrice = signalPrice - BaseSettings.StopLossPercent,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.PlaceOrder
                });

                //--------TakeProfit
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    StartDealOrderId = startDealOrderId,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.Limit,
                    Quantity = volume,
                    Price = signalPrice + BaseSettings.TakeProfitPercent,
                    StopPrice = 0,
                    robotOrderType = RobotOrderType.TakeProfit,
                    robotRequestType = RobotRequestType.PlaceOrder
                });

                if ((decimal)SignalBuyOrder.StopPrice == 0)
                {
                    OpenPositionPrice = (decimal)SignalBuyOrder.Price;
                }
                else
                {
                    OpenPositionPrice = (decimal)SignalBuyOrder.StopPrice;
                }
                RobotState.OpenPositionPrice = OpenPositionPrice;
            }
            SignalSellOrder = new();
            RobotState.SignalSellOrderId = 0;
            SignalBuyOrder = new();
            RobotState.SignalBuyOrderId = 0;




        }



        public async void CancelOrderByIdAsync(long orderId, string desc)
        {

            var result = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(Symbol, orderId);

            if (!result.Success)
            {
                //обработать
                Log(LogType.Error, desc);
            }



        }

        private void SetPartialStopLoss(DataEvent<BinanceFuturesStreamOrderUpdate> data)
        {
            if (data.Data.UpdateData.Side == OrderSide.Buy)
            {

                //--------StopLoss
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = data.Data.UpdateData.Quantity,
                    Price = data.Data.UpdateData.Price,
                    StopPrice = data.Data.UpdateData.StopPrice,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.PlaceOrder
                });


            }
            else
            {


                //--------StopLoss
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = data.Data.UpdateData.Quantity,
                    Price = data.Data.UpdateData.Price,
                    StopPrice = data.Data.UpdateData.StopPrice,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.PlaceOrder
                });

            }
        }

        public async void CancelRobotOrdersAsync()
        {
            if (SignalBuyOrder.Status == (int)OrderStatus.New ||
                SignalBuyOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = SignalBuyOrder.OrderId,
                    OrderType = SignalBuyOrder.Type
                });
            }

            if (SignalSellOrder.Status == (int)OrderStatus.New ||
                SignalSellOrder.Status == (int)OrderStatus.PartiallyFilled)
            {

                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = SignalSellOrder.OrderId,
                    OrderType = SignalSellOrder.Type
                });
            }

            if (StopLossOrder.Status == (int)OrderStatus.New ||
                StopLossOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = StopLossOrder.OrderId,
                    OrderType = StopLossOrder.Type
                });
            }

            if (TakeProfitOrder.Status == (int)OrderStatus.New ||
                TakeProfitOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = Index,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = TakeProfitOrder.OrderId,
                    OrderType = TakeProfitOrder.Type
                });
            }
            RobotState = new();
            await SetRobotData();


            IsRun = false;//robot stop

        }

        public async Task SetRobotData()
        {
            Position = RobotState.Position;

            OpenPositionPrice = RobotState.OpenPositionPrice;

            if (RobotState.SignalBuyOrderId != 0)
            {
                SignalBuyOrder = await RobotServices.GetBinOrderById(RobotState.SignalBuyOrderId, Index);
            }
            else
            {
                SignalBuyOrder = new();
            }


            if (RobotState.SignalSellOrderId != 0)
            {
                SignalSellOrder = await RobotServices.GetBinOrderById(RobotState.SignalSellOrderId, Index);
            }
            else
            {
                SignalSellOrder = new();
            }


            if (RobotState.StopLossOrderId != 0)
            {
                StopLossOrder = await RobotServices.GetBinOrderById(RobotState.StopLossOrderId, Index);
            }
            else
            {
                StopLossOrder = new();
            }


            if (RobotState.TakeProfitOrderId != 0)
            {
                TakeProfitOrder = await RobotServices.GetBinOrderById(RobotState.TakeProfitOrderId, Index);
            }
            else
            {
                TakeProfitOrder = new();
            }


        }

        public void ResetRobotData()
        {
            SignalBuyOrder = new RobotOrder();
            SignalSellOrder = new RobotOrder();
            StopLossOrder = new RobotOrder();
            TakeProfitOrder = new RobotOrder();
            Position = 0;
            OpenPositionPrice = 0;
        }

        //public void SetSLTPAfterFail(CandlesAnalyse candlesAnalyse, decimal volume, long signalBuyOrderId, long signalSellOrderId)
        //{
        //    Task.Run(() =>// выставляем СЛ ТП
        //    {
        //        if (candlesAnalyse == CandlesAnalyse.BuySLTP)
        //        {
        //            var signalPrice = (decimal)SignalBuyOrder.StopPrice;
        //            if ((decimal)SignalBuyOrder.StopPrice == 0)
        //            { signalPrice = SignalBuyOrder.Price; }
        //            SetSLTP(OrderSide.Buy, volume, signalPrice, signalBuyOrderId);

        //        }

        //        if (candlesAnalyse == CandlesAnalyse.SellSLTP)
        //        {
        //            var signalPrice = (decimal)SignalSellOrder.StopPrice;
        //            if ((decimal)SignalSellOrder.StopPrice == 0)
        //            { signalPrice = SignalSellOrder.Price; }
        //            SetSLTP(OrderSide.Sell, volume, signalPrice, signalSellOrderId);

        //        }
        //    });
        //}


        public bool CheckTradingStatus(DateTime date)
        {
            //time filter
            if (!BaseSettings.AllowedDayMonth[date.Day - 1])
            {
                return false;
            }


            if (!BaseSettings.AllowedDayWeek[(int)date.DayOfWeek])
            {
                return false;
            }


            if (!BaseSettings.AllowedHours[date.Hour])
            {
                return false;
            }

            return true;
        }

        public void SetCurrentPrifit(decimal price)
        {
            if (OpenPositionPrice != 0)
            {
                Commission = OpenPositionPrice / 100 * 0.04m * Math.Abs(Position);
                Commission += price / 100 * 0.04m * Math.Abs(Position);
            }
            else
            {
                Commission = 0;
            }

            if (Position > 0)
            {
                Profit = (price - OpenPositionPrice) * Math.Abs(Position);
                return;
            }

            if (Position < 0)
            {
                Profit = (OpenPositionPrice - price) * Math.Abs(Position);
                return;
            }

            

            Profit = 0;
        }




    }


}
