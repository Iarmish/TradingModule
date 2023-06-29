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
    public class MedianaLD
    {
        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }

                

        public bool NeedChartAnalyse { get; set; }
        public bool IsChartAnalyzing { get; set; }



        private decimal SignalSellPrice { get; set; }
        private decimal SignalBuyPrice { get; set; }

        private bool IsSignalSellOrderPlaced { get; set; }
        private bool IsSignalBuyOrderPlaced { get; set; }

        decimal medianaLD = 0;
        decimal medianaPosition = 0;


        //-----------------------------
        public MedianaLD(int robotId, int robotIndex)
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

            var lastDayCandle = MarketData.CandleDictionary[robot.Symbol][86400][^2];

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

                

                IsSignalSellOrderPlaced = false;
                IsSignalBuyOrderPlaced = false;

                medianaLD = Math.Round((lastDayCandle.HighPrice + lastDayCandle.LowPrice) / 2, SymbolIndexes.price[robot.Symbol]);
                if (currentPrice > medianaLD)
                {
                    medianaPosition = -1;
                }
                else
                {
                    medianaPosition = 1;
                }



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

            //новый день 
            if (lastDayCandle.OpenTime.AddDays(1).Day < carrentCendle.OpenTime.Day)
            {
                medianaLD = Math.Round((lastDayCandle.HighPrice + lastDayCandle.LowPrice) / 2, SymbolIndexes.price[robot.Symbol]);
                if (currentPrice > medianaLD)
                {
                    medianaPosition = -1;
                }
                else
                {
                    medianaPosition = 1;
                }
            }

            //------новая свечка-----------------            
            //if (LastCandle.CloseTime < LastCompletedCendle.CloseTime)
            //{
            //    IsChartAnalyzing = true;
                
            //    Thread.Sleep(4000);

            //    IsSignalSellOrderPlaced = false;
            //    IsSignalBuyOrderPlaced = false;

            //    CancelSignalSellOrder();
            //    CancelSignalBuyOrder();

            //    GetBollingerCandles();
            //    CalculateBollinger();

            //    SetRobotInfo();


            //    LastCandle = LastCompletedCendle;
            //    IsChartAnalyzing = false;
            //}

            //----------- анализ графика после закрытия сделки ------------------------------
            

            

            //------------------- выставляем ордера ---------------------
            if (robot.Position == 0 && robot.CheckTradingStatus(carrentCendle.OpenTime))
            {
                
                //-------- sell ----------------
                //------ выставляем ордера 
                if (robot.BaseSettings.Revers)
                {
                    if (medianaPosition == 1 && !IsSignalSellOrderPlaced)
                    {
                        medianaPosition = 0;
                        IsSignalSellOrderPlaced = true;

                        SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, medianaLD,
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
                    if (medianaPosition == 1 && !IsSignalBuyOrderPlaced)
                    {
                        medianaPosition = 0;
                        IsSignalBuyOrderPlaced = true;

                        SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, medianaLD,
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
                    if (medianaPosition == -1 && !IsSignalBuyOrderPlaced)
                    {
                        medianaPosition = 0;
                        IsSignalBuyOrderPlaced = true;

                        SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, medianaLD,
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
                    if (medianaPosition == -1 && !IsSignalSellOrderPlaced)
                    {
                        medianaPosition = 0;
                        IsSignalSellOrderPlaced = true;

                        SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, medianaLD,
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



        }




        //--------------------------------------

        public void SetRobotInfo()
        {
            
            var robot = RobotVM.robots[RobotIndex];
            if (MarketData.Info.SelectedRobotIndex == RobotIndex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();

                    // RobotInfoVM.AddParam("VWAP.Position", VWAPStatus.ToString());
                    //RobotInfoVM.AddParam("VWAP.Date", MarketData.VWAPs[^1].Date.ToString());
                    RobotInfoVM.AddParam("medianaPosition ", medianaPosition.ToString());
                    RobotInfoVM.AddParam("mediana" ,medianaLD.ToString());

                    RobotInfoVM.AddParam("IsSignalBuyOrderPlaced", IsSignalBuyOrderPlaced.ToString());
                    RobotInfoVM.AddParam("IsSignalSellOrderPlaced", IsSignalSellOrderPlaced.ToString());
                });
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
