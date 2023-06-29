using Binance.Infrastructure.Constants;
using Binance.Net.Enums;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Market;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShortPeakRobot.Robots.Algorithms.Models;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class BollingerEma
    {
        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }

        public List<Candle> BollingerCandles { get; set; } = new();

        private BollingerModel bollingerModel { get; set; } = new BollingerModel();

        public bool NeedChartAnalyse { get; set; }
        public bool IsChartAnalyzing { get; set; }



        private RobotData RobotData = new RobotData();


        //-----------------------------
        public BollingerEma(int robotId, int robotIndex)
        {
            RobotId = robotId;
            RobotIndex = robotIndex;
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                SetExtParams();
            });
        }


        private void SetExtParams()
        {
            RobotVM.robots[RobotIndex].BaseSettings.LableParam1 = "Period";
            RobotVM.robots[RobotIndex].BaseSettings.LableParam2 = "Deviation";
        }



        public async void NewTick(RobotCommands command)
        {
            var robot = RobotVM.robots[RobotIndex];

            switch (command)
            {
                case RobotCommands.Nothing:
                    break;
                case RobotCommands.SetRobotInfo:
                    SetRobotInfo();
                    break;
                case RobotCommands.ResetCandleAnalyse:
                    LastCandle = new();
                    break;
                default:
                    break;
            }

            var currentPrice = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1].ClosePrice;

            var carrentCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^2];

            var currentServerTime = DateTime.UtcNow.AddMinutes(-MarketData.Info.ServerTimeOffsetMinutes);
            bool isTradingAllowed = robot.CheckTradingStatus(currentServerTime);

            robot.SetCurrentPrifit(currentPrice);

            if (candles.Count == 0 || IsChartAnalyzing)
            {
                return;
            }

            var currentTime = DateTime.UtcNow;

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                IsChartAnalyzing = true;
                LastTime = currentTime;

                bollingerModel = new BollingerModel();

                RobotData = new RobotData();

                GetBollingerCandles();
                CalculateBollinger();

                SetRobotInfo();

                await robot.SetRobotData();//из robotState

                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(robot.RobotState, RobotIndex);

                if (candlesAnalyse == CandlesAnalyse.SLTPCrossed)
                {
                    robot.Log(LogType.Error, " Пересечение СЛ или ТП во время отсутствия связи!"); 
                    RobotServices.ForceStopRobotAsync(RobotIndex);
                }

                //-------------                
                robot.IsReady = true;
                IsChartAnalyzing = false;
                LastCandle = LastCompletedCendle;

            }


            if (!robot.IsReady)
            {
                return;
            }


            //-------------------------------------------
            //проверка на разрыв связи 
            if (LastTime.AddSeconds(30) < currentTime)
            {
                robot.IsReady = false;
                LastCandle = new();

                var lostTime = (currentTime - LastTime.AddSeconds(30)).TotalSeconds;
                robot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");

            }
            LastTime = currentTime;

            //------новая свечка-----------------            
            if (LastCandle.CloseTime < LastCompletedCendle.CloseTime)
            {
                IsChartAnalyzing = true;
                BollingerCandles.Clear();
                bollingerModel = new BollingerModel();

                Thread.Sleep(4000);

                RobotData = new RobotData();

                CancelSignalSellOrder();
                CancelSignalBuyOrder();

                GetBollingerCandles();
                CalculateBollinger();

                SetRobotInfo();


                LastCandle = LastCompletedCendle;
                IsChartAnalyzing = false;
            }

            //----------- анализ графика после закрытия сделки ------------------------------
            if (!NeedChartAnalyse && robot.Position != 0)
            {
                NeedChartAnalyse = true;
            }
            if (NeedChartAnalyse && robot.Position == 0)
            {
                NeedChartAnalyse = false;

                IsChartAnalyzing = true;
                bollingerModel = new BollingerModel();
                BollingerCandles.Clear();

                RobotData = new();


                GetBollingerCandles();
                CalculateBollinger();

                SetRobotInfo();
                IsChartAnalyzing = false;
            }

            if (BollingerCandles.Count == 0 || bollingerModel.Ema == 0)
            {
                return;
            }
            //==============  пересечение EMA =======================================================
            if (bollingerModel.Position == 1 && currentPrice < bollingerModel.Ema)
            {
                bollingerModel.Position = 0;
            }

            if (bollingerModel.Position == -1 && currentPrice > bollingerModel.Ema)
            {
                bollingerModel.Position = 0;
            }
            //==============  пересечение STD =======================================================
            if (currentPrice > bollingerModel.UpLine)
            {
                bollingerModel.Position = 1;
            }

            if (currentPrice < bollingerModel.DownLine)
            {
                bollingerModel.Position = -1;
            }
            //------------------- выставляем ордера ---------------------
            if (robot.Position == 0 && robot.CheckTradingStatus(carrentCendle.OpenTime))
            {
                var EMA = Math.Round(bollingerModel.Ema, SymbolIndexes.price[robot.Symbol]);
                //-------- bollinger.Position -1 ----------------
                //------ выставляем ордера 
                if (robot.BaseSettings.Revers)
                {
                    if (bollingerModel.Position == -1 && !RobotData.IsSellOrderPlaced)
                    {

                        RobotData.IsSellOrderPlaced = true;

                        RobotData.SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, EMA,
                            RobotOrderType.SignalSell, FuturesOrderType.Limit);

                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            Side = (int)OrderSide.Sell,
                            OrderType = (int)FuturesOrderType.Limit,
                            Quantity = robot.BaseSettings.Volume,
                            Price = RobotData.SignalSellPrice,
                            StopPrice = 0,
                            robotOrderType = RobotOrderType.SignalSell,
                            robotRequestType = RobotRequestType.PlaceOrder
                        });


                    }
                }
                else
                {
                    if (bollingerModel.Position == -1 && !RobotData.IsBuyOrderPlaced)
                    {

                        RobotData.IsBuyOrderPlaced = true;

                        RobotData.SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, EMA,
                            RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);

                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            Side = (int)OrderSide.Buy,
                            OrderType = (int)FuturesOrderType.StopMarket,
                            Quantity = robot.BaseSettings.Volume,
                            Price = 0,
                            StopPrice = RobotData.SignalBuyPrice,
                            robotOrderType = RobotOrderType.SignalBuy,
                            robotRequestType = RobotRequestType.PlaceOrder
                        });


                    }
                }


                ////-------- bollinger.Position 1 ----------------
                if (robot.BaseSettings.Revers)
                {
                    if (bollingerModel.Position == 1 && !RobotData.IsBuyOrderPlaced)
                    {

                        RobotData.IsBuyOrderPlaced = true;

                        RobotData.SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, EMA,
                            RobotOrderType.SignalBuy, FuturesOrderType.Limit);

                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            Side = (int)OrderSide.Buy,
                            OrderType = (int)FuturesOrderType.Limit,
                            Quantity = robot.BaseSettings.Volume,
                            Price = RobotData.SignalBuyPrice,
                            StopPrice = 0,
                            robotOrderType = RobotOrderType.SignalBuy,
                            robotRequestType = RobotRequestType.PlaceOrder
                        });


                    }
                }
                else
                {
                    if (bollingerModel.Position == 1 && !RobotData.IsSellOrderPlaced)
                    {
                        RobotData.IsSellOrderPlaced = true;

                        RobotData.SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, EMA,
                            RobotOrderType.SignalSell, FuturesOrderType.StopMarket);

                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            Side = (int)OrderSide.Sell,
                            OrderType = (int)FuturesOrderType.StopMarket,
                            Quantity = robot.BaseSettings.Volume,
                            Price = 0,
                            StopPrice = RobotData.SignalSellPrice,
                            robotOrderType = RobotOrderType.SignalSell,
                            robotRequestType = RobotRequestType.PlaceOrder
                        });
                    }
                }


            }
            else
            {
                if (RobotData.IsSellOrderPlaced || RobotData.IsBuyOrderPlaced)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalSellOrder.OrderId,
                        OrderType = (int)FuturesOrderType.StopMarket
                    });
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalBuyOrder.OrderId,
                        OrderType = (int)FuturesOrderType.StopMarket
                    });

                    RobotData.IsSellOrderPlaced = false;
                    RobotData.IsBuyOrderPlaced = false;


                }
            }

        }




        //--------------------------------------

        public void SetRobotInfo()
        {
            if (BollingerCandles.Count == 0)
            {
                return;
            }
            var robot = RobotVM.robots[RobotIndex];
            if (MarketData.Info.SelectedRobotIndex == RobotIndex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();

                    // RobotInfoVM.AddParam("VWAP.Position", VWAPStatus.ToString());
                    //RobotInfoVM.AddParam("VWAP.Date", MarketData.VWAPs[^1].Date.ToString());
                    RobotInfoVM.AddParam("Bollinger Position", bollingerModel.Position.ToString());
                    RobotInfoVM.AddParam("Bollinger EMA", bollingerModel.Ema.ToString());

                    RobotInfoVM.AddParam("IsSignalBuyOrderPlaced", RobotData.IsBuyOrderPlaced.ToString());
                    RobotInfoVM.AddParam("IsSignalSellOrderPlaced", RobotData.IsSellOrderPlaced.ToString());
                });
            }
        }


        private void GetBollingerCandles()
        {
            var robot = RobotVM.robots[RobotIndex];

            BollingerCandles = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame].TakeLast(95).ToList();
        }



        private void CalculateBollinger()
        {
            var robot = RobotVM.robots[RobotIndex];
            var period = robot.BaseSettings.Param1;
            var deviation = robot.BaseSettings.Param2;

            for (int i = 0; i < BollingerCandles.Count - 1; i++)
            {
                if (i > period)
                {
                    if (bollingerModel.Ema != 0)
                    {
                        if (BollingerCandles[i].HighPrice > bollingerModel.UpLine)
                        {
                            bollingerModel.Position = 1;
                        }

                        if (BollingerCandles[i].LowPrice < bollingerModel.DownLine)
                        {
                            bollingerModel.Position = -1;
                        }

                        if (bollingerModel.Position == 1 && BollingerCandles[i].LowPrice < bollingerModel.Ema)
                        {
                            bollingerModel.Position = 0;
                        }

                        if (bollingerModel.Position == -1 && BollingerCandles[i].HighPrice > bollingerModel.Ema)
                        {
                            bollingerModel.Position = 0;
                        }
                    }

                    //-----------------------------------------
                    bollingerModel.UpLine = 0;
                    bollingerModel.Ema = 0;
                    bollingerModel.DownLine = 0;
                    bollingerModel.Deviation = 0;
                    int cnt = 0;
                    for (int e = i - (int)period + 1; e <= i; e++)
                    {
                        bollingerModel.Ema += BollingerCandles[e].ClosePrice;
                        cnt++;
                    }
                    bollingerModel.Ema = bollingerModel.Ema / period;

                    for (int e = i - (int)period + 1; e <= i; e++)
                    {
                        bollingerModel.Deviation += (BollingerCandles[e].ClosePrice - bollingerModel.Ema) * (BollingerCandles[e].ClosePrice - bollingerModel.Ema);//возведение в квадрат
                    }
                    bollingerModel.Deviation /= period;
                    bollingerModel.Deviation = (decimal)Math.Sqrt((double)bollingerModel.Deviation);// корень квадратный

                    bollingerModel.UpLine = bollingerModel.Ema + deviation * bollingerModel.Deviation;
                    bollingerModel.DownLine = bollingerModel.Ema - deviation * bollingerModel.Deviation;

                    MarketData.CandleExtParams.Clear();
                    MarketData.CandleExtParams.Add(new CandleExtParam { Date = BollingerCandles[i].CloseTime, Param1 = bollingerModel.UpLine, Param2 = bollingerModel.Ema, Param3 = bollingerModel.DownLine });

                }
            }

            if (BollingerCandles.Count > period)
            {
                if (BollingerCandles[^1].HighPrice > bollingerModel.UpLine)
                {
                    bollingerModel.Position = 1;
                }

                if (BollingerCandles[^1].LowPrice < bollingerModel.DownLine)
                {
                    bollingerModel.Position = -1;
                }

                if (bollingerModel.Position == 1 && BollingerCandles[^1].LowPrice < bollingerModel.Ema)
                {
                    bollingerModel.Position = 0;
                }

                if (bollingerModel.Position == -1 && BollingerCandles[^1].HighPrice > bollingerModel.Ema)
                {
                    bollingerModel.Position = 0;
                }
            }
        }

        private void CancelSignalSellOrder()
        {
            var robot = RobotVM.robots[RobotIndex];

            if (robot.SignalSellOrder.OrderId != 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    Symbol = robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = robot.SignalSellOrder.OrderId,
                    OrderType = robot.SignalSellOrder.Type
                });

                robot.RobotState.SignalSellOrderId = 0;
                robot.SignalSellOrder = new();
            }

        }
        private void CancelSignalBuyOrder()
        {
            var robot = RobotVM.robots[RobotIndex];

            if (robot.SignalBuyOrder.OrderId != 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    Symbol = robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = robot.SignalBuyOrder.OrderId,
                    OrderType = robot.SignalBuyOrder.Type
                });

                robot.RobotState.SignalBuyOrderId = 0;
                robot.SignalBuyOrder = new();
            }

        }

       


    }
}
