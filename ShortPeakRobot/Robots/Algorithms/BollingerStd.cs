using Binance.Infrastructure.Constants;
using Binance.Net.Enums;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Market;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class BollingerStd
    {
        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }

        public List<Candle> BollingerCandles { get; set; } = new();

        private BollingerModel bollingerModel { get; set; } = new BollingerModel();

        public bool NeedChartAnalyse { get; set; }
        public bool IsChartAnalyzing { get; set; }



        private decimal SignalSellPrice { get; set; }
        private decimal SignalBuyPrice { get; set; }

        private bool IsSignalSellOrderPlaced { get; set; }
        private bool IsSignalBuyOrderPlaced { get; set; }


        //-----------------------------
        public BollingerStd(int robotId, int robotIndex)
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

                IsSignalSellOrderPlaced = false;
                IsSignalBuyOrderPlaced = false;

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

                IsSignalSellOrderPlaced = false;
                IsSignalBuyOrderPlaced = false;

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

                IsSignalSellOrderPlaced = false;
                IsSignalBuyOrderPlaced = false;



                GetBollingerCandles();
                CalculateBollinger();

                SetRobotInfo();
                IsChartAnalyzing = false;
            }

            if (BollingerCandles.Count == 0 || bollingerModel.Ema == 0)
            {
                return;
            }
            
           
            //------------------- выставляем ордера ---------------------
            if (robot.Position == 0 && robot.CheckTradingStatus(carrentCendle.OpenTime))
            {
                var UpLine = Math.Round(bollingerModel.UpLine, SymbolIndexes.price[robot.Symbol]);
                var DownLine = Math.Round(bollingerModel.DownLine, SymbolIndexes.price[robot.Symbol]);
                //-------- sell ----------------
                //------ выставляем ордера 
                if (robot.BaseSettings.Revers)
                {
                    if (bollingerModel.Position == -1 && !IsSignalSellOrderPlaced)
                    {

                        IsSignalSellOrderPlaced = true;

                        SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, UpLine,
                            RobotOrderType.SignalSell, FuturesOrderType.Limit);

                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            Side = (int)OrderSide.Sell,
                            OrderType = (int)FuturesOrderType.Limit,
                            Quantity = robot.BaseSettings.Volume,
                            Price = SignalSellPrice,
                            StopPrice = 0,
                            robotOrderType = RobotOrderType.SignalSell,
                            robotRequestType = RobotRequestType.PlaceOrder
                        });


                    }
                }
                else
                {
                    if (bollingerModel.Position == -1 && !IsSignalBuyOrderPlaced)
                    {

                        IsSignalBuyOrderPlaced = true;

                        SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, UpLine,
                            RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);

                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            Side = (int)OrderSide.Buy,
                            OrderType = (int)FuturesOrderType.StopMarket,
                            Quantity = robot.BaseSettings.Volume,
                            Price = 0,
                            StopPrice = SignalBuyPrice,
                            robotOrderType = RobotOrderType.SignalBuy,
                            robotRequestType = RobotRequestType.PlaceOrder
                        });


                    }
                }


                //-------------- buy ---------------------
                if (robot.BaseSettings.Revers)
                {
                    if (bollingerModel.Position == 1 && !IsSignalBuyOrderPlaced)
                    {

                        IsSignalBuyOrderPlaced = true;

                        SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, DownLine,
                            RobotOrderType.SignalBuy, FuturesOrderType.Limit);

                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            Side = (int)OrderSide.Buy,
                            OrderType = (int)FuturesOrderType.Limit,
                            Quantity = robot.BaseSettings.Volume,
                            Price = SignalBuyPrice,
                            StopPrice = 0,
                            robotOrderType = RobotOrderType.SignalBuy,
                            robotRequestType = RobotRequestType.PlaceOrder
                        });


                    }
                }
                else
                {
                    if (bollingerModel.Position == 1 && !IsSignalSellOrderPlaced)
                    {
                        IsSignalSellOrderPlaced = true;

                        SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, DownLine,
                            RobotOrderType.SignalSell, FuturesOrderType.StopMarket);

                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            Side = (int)OrderSide.Sell,
                            OrderType = (int)FuturesOrderType.StopMarket,
                            Quantity = robot.BaseSettings.Volume,
                            Price = 0,
                            StopPrice = SignalSellPrice,
                            robotOrderType = RobotOrderType.SignalSell,
                            robotRequestType = RobotRequestType.PlaceOrder
                        });
                    }
                }


            }
            else
            {
                if (IsSignalSellOrderPlaced || IsSignalBuyOrderPlaced)
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

                    IsSignalSellOrderPlaced = false;
                    IsSignalBuyOrderPlaced = false;


                }
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

                    RobotInfoVM.AddParam("IsSignalBuyOrderPlaced", IsSignalBuyOrderPlaced.ToString());
                    RobotInfoVM.AddParam("IsSignalSellOrderPlaced", IsSignalSellOrderPlaced.ToString());
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
