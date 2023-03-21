﻿using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Migrations;
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

        //public bool NeedSaveRobotState { get; set; }


       


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
            if (arrClientOrderId.Length > 1 && arrClientOrderId[0] == "robot")
            {
                var id = arrClientOrderId[1];

                if (Convert.ToInt32(id) == Id)
                {
                    var order = RobotOrderDTO.DTO(data, Id);
                    RobotServices.SaveOrder(Id, order, "");//to Db
                   
                }
            }
            //сохраняем сделки
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
                if (SignalBuyOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = Id,
                        Symbol = Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = SignalBuyOrder.OrderId
                    });
                    RobotState.SignalBuyOrderId = 0;
                    SignalBuyOrder = new();
                }

                var volume = data.Data.UpdateData.Quantity;

                Task.Run(async () =>// выставляем СЛ ТП
                {
                    var SLTPsuccess = await SetSLTP(OrderSide.Sell, volume, (decimal)SignalSellOrder.StopPrice);                    
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
                        RobotId = Id,
                        Symbol = Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = SignalSellOrder.OrderId
                    });
                    RobotState.SignalSellOrderId = 0;
                    SignalSellOrder = new();
                }


                var volume = data.Data.UpdateData.Quantity;

                Task.Run(async () =>
                {
                    var SLTPsuccess = await SetSLTP(OrderSide.Buy, volume, (decimal)SignalBuyOrder.StopPrice);                    
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
                    RobotId = Id,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = TakeProfitOrder.OrderId
                });

                TakeProfitOrder = new();
                RobotState = new();   

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
                    RobotId = Id,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = StopLossOrder.OrderId
                });

                StopLossOrder = new();
                RobotState = new();
                

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
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = StopLossOrder.OrderId
                });



                SetPartialStopLoss(data.Data.UpdateData.Side, data.Data.UpdateData.Quantity);

                
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
                Thread.Sleep(1500);
            }
        }
            
        public void CheckSLTPCross(decimal price)
        {
            if (Position != 0 )
            {               

                if (Position > 0)
                {
                    if (price > OpenPositionPrice + BaseSettings.TakeProfitPercent || price < OpenPositionPrice - BaseSettings.StopLossPercent)
                    {
                        Thread.Sleep(2000);
                        CloseRobotPosition();
                    }
                }
                if (Position < 0)
                {
                    if (price < OpenPositionPrice - BaseSettings.TakeProfitPercent || price > OpenPositionPrice + BaseSettings.StopLossPercent)
                    {
                        Thread.Sleep(2000);
                        CloseRobotPosition();
                    }
                }
            }
            else
            {
                return;
            }

            
        }

        private void DbManager()
        {
            bool needSaveContext = false;            

            //сохраняем состояние
            if (!RobotServices.CompareState(RobotState, LastRobotState))
            {
                LastRobotState = RobotStateDTO.DTO(RobotState);
                needSaveContext = true;

                var state = _context.RobotStates
                                .Where(x => x.ClientId == RobotsInitialization.ClientId && x.RobotId == Id).FirstOrDefault();
                Thread.Sleep(30);

                if (state != null)
                {
                    state.Position = RobotState.Position;
                    state.OpenPositionPrice = RobotState.OpenPositionPrice;
                    state.SignalBuyOrderId = RobotState.SignalBuyOrderId;
                    state.SignalSellOrderId = RobotState.SignalSellOrderId;
                    state.StopLossOrderId = RobotState.StopLossOrderId;
                    state.TakeProfitOrderId = RobotState.TakeProfitOrderId;

                    _context.RobotStates.Update(state);
                }
                else
                {
                    _context.RobotStates.Add(RobotState);

                }

            }

           

            if (RobotLogsQueue.Count > 0)
            {
                needSaveContext = true;
                var RobotLogsTemp = new List<RobotLog>();

                lock (Locker)
                {
                    foreach (var log in RobotLogsQueue)
                    {
                        RobotLogsTemp.Add(RobotLogsDTO.DTO(log));
                    }

                    RobotLogsQueue.Clear();
                }

                _context.RobotLogs.AddRange(RobotLogsTemp);

            }


            if (RobotOrdersQueue.Count > 0)
            {
                needSaveContext = true;
                var RobotOrdersTemp = new List<RobotOrder>();

                lock (Locker)
                {
                    foreach (var order in RobotOrdersQueue)
                    {
                        RobotOrdersTemp.Add(RobotOrderDTO.DTO(order));
                    }

                    RobotOrdersQueue.Clear();
                }



                _context.RobotOrders.AddRange(RobotOrdersTemp);
                Task.Run(() =>
                {
                    Thread.Sleep(700);
                    MarketServices.GetRobotData(Id);
                });
            }


            if (RobotTradesQueue.Count > 0)
            {
                needSaveContext = true;
                var RobotTradesTemp = new List<RobotTrade>();

                lock (Locker)
                {
                    foreach (var trade in RobotTradesQueue)
                    {
                        RobotTradesTemp.Add(RobotTradeDTO.DTO(trade));
                    }

                    RobotTradesQueue.Clear();
                }


                _context.RobotTrades.AddRange(RobotTradesTemp);

                Task.Run(() =>
                {
                    Thread.Sleep(1200);
                    MarketServices.GetSessioProfit();

                });
            }

            if (needSaveContext)
            {
                _context.SaveChanges();
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
                Position = -volume;
                RobotState.Position = -volume;
                //--------StopLoss
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
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
                    RobotId = Id,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.Limit,
                    Quantity = volume,
                    Price = signalPrice - BaseSettings.TakeProfitPercent,
                    StopPrice = 0,
                    robotOrderType = RobotOrderType.TakeProfit,
                    robotRequestType = RobotRequestType.PlaceOrder
                });


                OpenPositionPrice = (decimal)SignalSellOrder.StopPrice;
                RobotState.OpenPositionPrice = (decimal)SignalSellOrder.StopPrice;
            }
            else // СЛ ТП на покупку
            {
                Position = volume;
                RobotState.Position = volume;

                //--------StopLoss
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
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
                    RobotId = Id,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.Limit,
                    Quantity = volume,
                    Price = signalPrice + BaseSettings.TakeProfitPercent,
                    StopPrice = 0,
                    robotOrderType = RobotOrderType.TakeProfit,
                    robotRequestType = RobotRequestType.PlaceOrder
                });

                OpenPositionPrice = (decimal)SignalBuyOrder.StopPrice;
                RobotState.OpenPositionPrice = (decimal)SignalBuyOrder.StopPrice;
            }
            SignalSellOrder = new();
            RobotState.SignalSellOrderId = 0;
            SignalBuyOrder = new();
            RobotState.SignalBuyOrderId = 0;


            
            return SLTPsuccess;
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

        private void SetPartialStopLoss(OrderSide side, decimal volume)
        {
            if (side == OrderSide.Buy)
            {
                var signalPrice = 0m;
                if (SignalSellOrder.StopPrice == 0) { signalPrice = SignalSellOrder.Price; }
                else { signalPrice = (decimal)SignalSellOrder.StopPrice; }

                //--------StopLoss
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = volume,
                    Price = 0,
                    StopPrice = signalPrice + BaseSettings.StopLossPercent,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.PlaceOrder
                });


            }
            else
            {
                var signalPrice = 0m;
                if (SignalBuyOrder.StopPrice == 0) { signalPrice = SignalBuyOrder.Price; }
                else { signalPrice = (decimal)SignalBuyOrder.StopPrice; }

                //--------StopLoss
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = volume,
                    Price = 0,
                    StopPrice = signalPrice - BaseSettings.StopLossPercent,
                    robotOrderType = RobotOrderType.StopLoss,
                    robotRequestType = RobotRequestType.PlaceOrder
                });

            }
        }

        public async void CloseRobotPositionAsync()
        {
            if (SignalBuyOrder.Status == (int)OrderStatus.New ||
                SignalBuyOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = SignalBuyOrder.OrderId
                });
            }

            if (SignalSellOrder.Status == (int)OrderStatus.New ||
                SignalSellOrder.Status == (int)OrderStatus.PartiallyFilled)
            {

                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = SignalSellOrder.OrderId
                });
            }

            if (StopLossOrder.Status == (int)OrderStatus.New ||
                StopLossOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = StopLossOrder.OrderId
                });
            }

            if (TakeProfitOrder.Status == (int)OrderStatus.New ||
                TakeProfitOrder.Status == (int)OrderStatus.PartiallyFilled)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = Id,
                    Symbol = Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = TakeProfitOrder.OrderId
                });
            }
            RobotState = new();
            await ResetRobotState();
            

            IsRun = false;//robot stop
            
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

        public void SetSLTPAfterFail(CandlesAnalyse candlesAnalyse, decimal volume)
        {
            Task.Run(async () =>// выставляем СЛ ТП
            {
                if (candlesAnalyse == CandlesAnalyse.BuySLTP)
                {
                    var SLTPsuccess = await SetSLTP(OrderSide.Buy, volume, (decimal)SignalBuyOrder.StopPrice);
                    
                }

                if (candlesAnalyse == CandlesAnalyse.SellSLTP)
                {
                    var SLTPsuccess = await SetSLTP(OrderSide.Sell, volume, (decimal)SignalSellOrder.StopPrice);
                    
                }
            });
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

        //public async Task<WebCallResult<BinanceFuturesPlacedOrder>> PlaceSignalOrder2(OrderSide side, FuturesOrderType type,
        //    decimal? price = null, decimal? stopPrice = null)
        //{
        //    var order = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
        //            symbol: Symbol,
        //            side: side,
        //            type: type,
        //            quantity: BaseSettings.Volume,
        //            price: price,
        //            timeInForce: TimeInForce.GoodTillCanceled,
        //            stopPrice: stopPrice);

        //    
        //    return order;
        //}
    }


}
