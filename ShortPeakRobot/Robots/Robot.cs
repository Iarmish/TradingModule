using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots.Algorithms;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.Robots.Models;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ShortPeakRobot.Robots
{
    public class Robot : BaseVM
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AlgorithmName { get; set; }

        public List<RobotLog> RobotLogsQueue { get; set; } = new List<RobotLog>();
        public List<RobotOrder> RobotOrdersQueue { get; set; } = new List<RobotOrder>();
        public List<RobotTrade> RobotTradesQueue { get; set; } = new List<RobotTrade>();

        public ApplicationDbContext _context { get; set; } = new ApplicationDbContext();

        private RobotCommands Command { get; set; } = RobotCommands.Nothing;

        public object Locker = new object();

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
        //=====================================
        public void CustomOrderUpdate(DataEvent<BinanceFuturesStreamOrderUpdate> data)
        {

            var arrClientOrderId = data.Data.UpdateData.ClientOrderId.Split(':');
            if (data.Data.UpdateData.Status == OrderStatus.Filled || data.Data.UpdateData.Status == OrderStatus.PartiallyFilled)
            {
                if (arrClientOrderId.Length > 1 && arrClientOrderId[0] == "robot")
                {
                    var id = arrClientOrderId[1];

                    if (Convert.ToInt32(id) == Id)
                    {
                        var trade = RobotTradeDTO.DTO(data, Id);
                        RobotServices.SaveTrade(trade, Id);//to Db
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
                var trade = RobotTradeDTO.DTO(data, Id);
                RobotServices.SaveTrade(trade, Id);//to Db

                //CancelOrderAsync(SignalBuyOrder, "Cancel Signal Order");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.SignalBuy,
                    robotRequestType = RobotRequestType.CancelOrder
                });

                var volume = data.Data.UpdateData.Quantity;

                Task.Run(async () =>// выставляем СЛ ТП
                {
                    var SLTPsuccess = await SetSLTP(OrderSide.Sell, volume, (decimal)SignalSellOrder.StopPrice);
                    if (!SLTPsuccess)
                    {
                        //обработать
                        Log(LogType.Error, "SLTP errror");
                    }
                });
                return;
            }
            //-----------------------
            if (data.Data.UpdateData.OrderId == SignalBuyOrder.OrderId &&
                data.Data.UpdateData.Status == OrderStatus.Filled)
            {
                var trade = RobotTradeDTO.DTO(data, Id);
                RobotServices.SaveTrade(trade, Id);//to Db

                //CancelOrderAsync(SignalSellOrder, "Cancel Signal Order");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.SignalSell,
                    robotRequestType = RobotRequestType.CancelOrder
                });

                var volume = data.Data.UpdateData.Quantity;

                Task.Run(async () =>
                {
                    var SLTPsuccess = await SetSLTP(OrderSide.Buy, volume, (decimal)SignalBuyOrder.StopPrice);
                    if (!SLTPsuccess)
                    {
                        //обработать
                        Log(LogType.Error, "SLTP errror");
                    }
                });
                return;
            }

            //------------------------stoploss status check
            if (data.Data.UpdateData.OrderId == StopLossOrder.OrderId &&
                data.Data.UpdateData.Status == OrderStatus.Filled)
            {
                Position = 0;
                RobotState = new();
                RobotServices.SaveState(Id, RobotState);

                //CancelOrderAsync(TakeProfitOrder, "Cancel TakeProfit Order");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.TakeProfit,
                    robotRequestType = RobotRequestType.CancelOrder
                });

                var trade = RobotTradeDTO.DTO(data, Id);
                RobotServices.SaveTrade(trade, Id);//to Db

                MarketServices.GetRobotData(Id);//for UI

                return;
            }

            //takeprofit status check
            if (data.Data.UpdateData.OrderId == TakeProfitOrder.OrderId &&
                data.Data.UpdateData.Status == OrderStatus.Filled)
            {
                Position = 0;
                RobotState = new();
                RobotServices.SaveState(Id, RobotState);

                //CancelOrderAsync(StopLossOrder, "Cancel StopLoss Order");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.CancelOrder
                });

                var trade = RobotTradeDTO.DTO(data, Id);
                RobotServices.SaveTrade(trade, Id);//to Db
                MarketServices.GetRobotData(Id);//for UI

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


                //CancelOrderAsync(StopLossOrder, "Reset StopLoss Order");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.CancelOrder
                });

                var trade = RobotTradeDTO.DTO(data, Id);
                RobotServices.SaveTrade(trade, Id);//to Db

                SetPartialStopLoss(data.Data.UpdateData.Side, data.Data.UpdateData.Quantity);

                RobotServices.SaveState(Id, RobotState);
                MarketServices.GetRobotData(Id);//for UI

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
                Thread.Sleep(3000);
            }
        }

        private void DbManager()
        {
            lock (Locker)
            {
                if (RobotLogsQueue.Count > 0)
                {
                    var RobotLogsTemp = new List<RobotLog>();
                    RobotLogsQueue.ForEach(x => RobotLogsTemp.Add(RobotLogsDTO.DTO(x)));
                    RobotLogsQueue.Clear();



                    try
                    {
                        _context.RobotLogs.AddRange(RobotLogsTemp);
                        _context.SaveChanges();
                        Thread.Sleep(200);
                    }
                    catch (Exception)
                    {
                        throw;
                    }


                    if (MarketData.Info.SelectedRobotId == Id)
                    {

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            LogVM.logs.Clear();
                            LogVM.AddRange(_context.RobotLogs.Where(x => x.ClientId == RobotsInitialization.ClientId &&
                            x.RobotId == Id && x.Date > MarketData.Info.StartSessionDate).ToList());

                        });
                    }

                }


                if (RobotOrdersQueue.Count > 0)
                {
                    var RobotOrdersTemp = new List<RobotOrder>();

                    RobotOrdersQueue.ForEach(x => RobotOrdersTemp.Add(RobotOrderDTO.DTO(x)));
                    RobotOrdersQueue.Clear();


                    _context.RobotOrders.AddRange(RobotOrdersTemp);
                        _context.SaveChanges();
                        
                        Thread.Sleep(200);
                    

                    if (MarketData.Info.SelectedRobotId == Id)
                    {

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            MarketServices.GetRobotData(Id);
                        });
                    }
                }


                if (RobotTradesQueue.Count > 0)
                {
                    var RobotTradesTemp = new List<RobotTrade>();

                    RobotTradesQueue.ForEach(x => RobotTradesTemp.Add(RobotTradeDTO.DTO(x)));
                    RobotTradesQueue.Clear();


                    _context.RobotTrades.AddRange(RobotTradesQueue);
                        _context.SaveChanges();                        
                        Thread.Sleep(200);
                   


                    if (MarketData.Info.SelectedRobotId == Id)
                    {

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            MarketServices.GetRobotData(Id);
                        });
                    }
                    MarketServices.GetSessioProfit();
                }
            }
        }





        public async void CloseRobotPosition()
        {


            if (Position > 0)
            {
                
                var plasedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                       symbol: Symbol,
                       side: OrderSide.Sell,
                       type: FuturesOrderType.Market,
                       quantity: Math.Abs(Position),
                       newClientOrderId: "robot:" + Id + ":" +
                       new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).ToUnixTimeSeconds());

                RobotServices.SaveOrder(Id, RobotOrderDTO.DTO(plasedOrder, Id), "Close position");
                MarketServices.GetRobotData(Id);//for IU
            }

            if (Position < 0)
            {
                var plasedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                       symbol: Symbol,
                       side: OrderSide.Buy,
                       type: FuturesOrderType.Market,
                       quantity: Math.Abs(Position),
                       newClientOrderId: "robot:" + Id + ":" +
                       new DateTimeOffset(DateTime.UtcNow, TimeSpan.Zero).ToUnixTimeSeconds());

                RobotServices.SaveOrder(Id, RobotOrderDTO.DTO(plasedOrder, Id), "Close position");
                MarketServices.GetRobotData(Id);//for IU
            }

            Position = 0;
            Profit = 0;

            CloseRobotPositionAsync();
            //Command = RobotCommands.CloseRobotPosition;


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
                RobotServices.SaveOrder(Id, RobotOrderDTO.DTO(plasedOrder, Id), "Change position");
                MarketServices.GetRobotData(Id);//for IU

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
                ClientId = RobotsInitialization.ClientId,
                Date = DateTime.UtcNow,
                Type = (int)type,
                Message = message
            };
            lock (Locker)
            {
                RobotLogsQueue.Add(log);
            }


        }


        public async Task<bool> SetSLTP(OrderSide side, decimal volume, decimal signalPrice)
        {
            var SLTPsuccess = true;
            if (side == OrderSide.Sell)// СЛ ТП на продажу
            {
                //var signalPrice = 0m;
                //if (SignalSellOrder.StopPrice == 0) { signalPrice = SignalSellOrder.Price; }
                //else { signalPrice = (decimal)SignalSellOrder.StopPrice; }

                Position = -volume;
                RobotState.Position = -volume;
                RobotServices.SaveState(Id, RobotState);
                //--------StopLoss
                var placedStopLossOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: Symbol,
                    side: OrderSide.Buy,
                    type: FuturesOrderType.StopMarket,
                    quantity: volume,
                    stopPrice: signalPrice + BaseSettings.StopLossPercent,
                    timeInForce: TimeInForce.GoodTillCanceled);

                if (placedStopLossOrder.Success)
                {
                    StopLossOrder = RobotOrderDTO.DTO(placedStopLossOrder, Id);
                    RobotState.StopLossOrderId = StopLossOrder.OrderId;

                    RobotServices.SaveOrder(Id, StopLossOrder, "Place StopLoss Order");
                }
                else
                {
                    SLTPsuccess = false;
                    Log(LogType.Error, " StopLoss Error " + placedStopLossOrder.Error.ToString());
                }
                //--------TakeProfit
                var placedTakeProfitOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: Symbol,
                    side: OrderSide.Buy,
                    type: FuturesOrderType.Limit,
                    quantity: volume,
                    price: signalPrice - BaseSettings.TakeProfitPercent,
                    timeInForce: TimeInForce.GoodTillCanceled);

                if (placedTakeProfitOrder.Success)
                {
                    TakeProfitOrder = RobotOrderDTO.DTO(placedTakeProfitOrder, Id);
                    RobotState.TakeProfitOrderId = TakeProfitOrder.OrderId;

                    RobotServices.SaveOrder(Id, TakeProfitOrder, "Place TakeProfit Order");
                }
                else
                {
                    SLTPsuccess = false;
                    Log(LogType.Error, "TakeProfit Error " + placedTakeProfitOrder.Error.ToString());
                }

                OpenPositionPrice = (decimal)SignalSellOrder.StopPrice;
                RobotState.OpenPositionPrice = (decimal)SignalSellOrder.StopPrice;
            }
            else // СЛ ТП на покупку
            {
                //var signalPrice = 0m;
                //if (SignalBuyOrder.StopPrice == 0) { signalPrice = SignalBuyOrder.Price; }
                //else { signalPrice = (decimal)SignalBuyOrder.StopPrice; }

                Position = volume;
                RobotState.Position = volume;
                RobotServices.SaveState(Id, RobotState);
                //--------StopLoss
                var placedStopLossOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: Symbol,
                    side: OrderSide.Sell,
                    type: FuturesOrderType.StopMarket,
                    quantity: volume,
                    stopPrice: signalPrice - BaseSettings.StopLossPercent,
                    timeInForce: TimeInForce.GoodTillCanceled);

                if (placedStopLossOrder.Success)
                {
                    StopLossOrder = RobotOrderDTO.DTO(placedStopLossOrder, Id);
                    RobotState.StopLossOrderId = StopLossOrder.OrderId;

                    RobotServices.SaveOrder(Id, StopLossOrder, "Place StopLoss Order");
                }
                else
                {
                    SLTPsuccess = false;
                    Log(LogType.Error, "StopLoss Error " + placedStopLossOrder.Error.ToString());
                }
                //--------TakeProfit
                var placedTakeProfitOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: Symbol,
                    side: OrderSide.Sell,
                    type: FuturesOrderType.Limit,
                    quantity: volume,
                    price: signalPrice + BaseSettings.TakeProfitPercent,
                    timeInForce: TimeInForce.GoodTillCanceled);

                if (placedTakeProfitOrder.Success)
                {
                    TakeProfitOrder = RobotOrderDTO.DTO(placedTakeProfitOrder, Id);
                    RobotState.TakeProfitOrderId = TakeProfitOrder.OrderId;

                    RobotServices.SaveOrder(Id, TakeProfitOrder, "Place TakeProfit Order");
                }
                else
                {
                    SLTPsuccess = false;
                    Log(LogType.Error, "TakeProfit Error " + placedTakeProfitOrder.Error.ToString());
                }

                OpenPositionPrice = (decimal)SignalBuyOrder.StopPrice;
                RobotState.OpenPositionPrice = (decimal)SignalBuyOrder.StopPrice;
            }
            SignalSellOrder = new();
            RobotState.SignalSellOrderId = 0;
            SignalBuyOrder = new();
            RobotState.SignalBuyOrderId = 0;
            RobotServices.SaveState(Id, RobotState);

            MarketServices.GetRobotData(Id);
            return SLTPsuccess;
        }

        public async void CancelOrderAsync2(RobotOrder order, string desc)
        {
            if (order.Status == (int)OrderStatus.New)
            {
                var result = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(Symbol, order.OrderId);
                if (!result.Success)
                {
                    //обработать
                    Log(LogType.Error, desc);
                }
                RobotServices.SaveOrder(Id, order, desc);
            }
            MarketServices.GetRobotData(Id);
        }

        public async void CancelOrderByIdAsync(long orderId, string desc)
        {

            var result = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(Symbol, orderId);

            if (!result.Success)
            {
                //обработать
                Log(LogType.Error, desc);
            }


            MarketServices.GetRobotData(Id);
        }

        private async Task SetPartialStopLoss(OrderSide side, decimal volume)
        {
            if (side == OrderSide.Buy)
            {
                var signalPrice = 0m;
                if (SignalSellOrder.StopPrice == 0) { signalPrice = SignalSellOrder.Price; }
                else { signalPrice = (decimal)SignalSellOrder.StopPrice; }

                //--------StopLoss
                var placedStopLossOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: Symbol,
                    side: OrderSide.Buy,
                    type: FuturesOrderType.StopMarket,
                    quantity: BaseSettings.Volume,
                    stopPrice: signalPrice + BaseSettings.StopLossPercent,
                    timeInForce: TimeInForce.GoodTillCanceled);

                if (placedStopLossOrder.Success)
                {
                    StopLossOrder = RobotOrderDTO.DTO(placedStopLossOrder, Id);
                    RobotState.StopLossOrderId = StopLossOrder.OrderId;

                    RobotServices.SaveOrder(Id, StopLossOrder, "Place StopLoss Order");
                }
                else
                {
                    RobotVM.robots[Id].Log(LogType.Error, " StopLoss Error " + placedStopLossOrder.Error.ToString());
                }

            }
            else
            {
                var signalPrice = 0m;
                if (SignalBuyOrder.StopPrice == 0) { signalPrice = SignalBuyOrder.Price; }
                else { signalPrice = (decimal)SignalBuyOrder.StopPrice; }

                //--------StopLoss
                var placedStopLossOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: RobotVM.robots[Id].Symbol,
                    side: OrderSide.Sell,
                    type: FuturesOrderType.StopMarket,
                    quantity: BaseSettings.Volume,
                    stopPrice: signalPrice - BaseSettings.StopLossPercent,
                    timeInForce: TimeInForce.GoodTillCanceled);

                if (placedStopLossOrder.Success)
                {
                    StopLossOrder = RobotOrderDTO.DTO(placedStopLossOrder, Id);
                    RobotState.StopLossOrderId = StopLossOrder.OrderId;

                    RobotServices.SaveOrder(Id, StopLossOrder, "Place StopLoss Order");
                }
                else
                {
                    Log(LogType.Error, "StopLoss Error " + placedStopLossOrder.Error.ToString());
                }
            }
        }

        public async void CloseRobotPositionAsync()
        {
            if (SignalBuyOrder.Status == (int)OrderStatus.New ||
                SignalBuyOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                //CancelOrderAsync(SignalBuyOrder, "SignalHigh Close Robot Position");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.SignalBuy,
                    robotRequestType = RobotRequestType.CancelOrder
                });
            }

            if (SignalSellOrder.Status == (int)OrderStatus.New ||
                SignalSellOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                //CancelOrderAsync(SignalSellOrder, "SignalLow Close Robot Position");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.SignalSell,
                    robotRequestType = RobotRequestType.CancelOrder
                });
            }

            if (StopLossOrder.Status == (int)OrderStatus.New ||
                StopLossOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                //CancelOrderAsync(StopLossOrder, "StopLoss Close Robot Position");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.CancelOrder
                });
            }

            if (TakeProfitOrder.Status == (int)OrderStatus.New ||
                TakeProfitOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                //CancelOrderAsync(TakeProfitOrder, "TakeProfit Close Robot Position");
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotOrderType = RobotOrderType.TakeProfit,
                    robotRequestType = RobotRequestType.CancelOrder
                });
            }
            RobotState = new();
            await ResetRobotState();
            //RobotServices.SaveState(Id, RobotState);

            IsRun = false;//robot stop
            MarketServices.GetRobotData(Id);
        }

        public async Task ResetRobotState()
        {
            Position = RobotState.Position;

            OpenPositionPrice = RobotState.OpenPositionPrice;

            if (RobotState.SignalBuyOrderId != 0)
            {
                SignalBuyOrder = await RobotServices.GetBinOrderById(RobotState.SignalBuyOrderId, Id);
            }

            if (RobotState.SignalSellOrderId != 0)
            {
                SignalSellOrder = await RobotServices.GetBinOrderById(RobotState.SignalSellOrderId, Id);
            }

            if (RobotState.StopLossOrderId != 0)
            {
                StopLossOrder = await RobotServices.GetBinOrderById(RobotState.StopLossOrderId, Id);
            }

            if (RobotState.TakeProfitOrderId != 0)
            {
                TakeProfitOrder = await RobotServices.GetBinOrderById(RobotState.TakeProfitOrderId, Id);
            }


        }

        public async void ResetRobotStateOrders()
        {
            SignalBuyOrder = new RobotOrder();
            SignalSellOrder = new RobotOrder();
            StopLossOrder = new RobotOrder();
            TakeProfitOrder = new RobotOrder();


        }

        public async Task SetSLTPAfterFail(CandlesAnalyse candlesAnalyse, decimal volume)
        {
            if (candlesAnalyse == CandlesAnalyse.BuySLTP)
            {
                var SLTPsuccess = await SetSLTP(OrderSide.Buy, volume, (decimal)SignalBuyOrder.StopPrice);
                if (!SLTPsuccess)
                {
                    //обработать
                    Log(LogType.Error, "SLTP errror");
                }
            }

            if (candlesAnalyse == CandlesAnalyse.SellSLTP)
            {
                var SLTPsuccess = await SetSLTP(OrderSide.Sell, volume, (decimal)SignalSellOrder.StopPrice);
                if (!SLTPsuccess)
                {
                    //обработать
                    Log(LogType.Error, "SLTP errror");
                }
            }
        }


        public bool CheckTradingStatus(DateTime date)
        {
            //time filter
            if (!BaseSettings.AllowedDayMonth[date.Day - 1])
            {
                return false;
            }

            if (!BaseSettings.AllowedDayWeek[date.Day - 1])
            {
                //return false;
            }

            if (!BaseSettings.AllowedHours[date.Hour])
            {
                return false;
            }

            return true;
        }

        public async Task<WebCallResult<BinanceFuturesPlacedOrder>> PlaceSignalOrder(OrderSide side, FuturesOrderType type,
            decimal? price = null, decimal? stopPrice = null)
        {
            var order = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: Symbol,
                    side: side,
                    type: type,
                    quantity: BaseSettings.Volume,
                    price: price,
                    timeInForce: TimeInForce.GoodTillCanceled,
                    stopPrice: stopPrice);

            MarketServices.GetRobotData(Id);
            return order;
        }
    }


}
