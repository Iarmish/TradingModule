using Binance.Net.Enums;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels;
using ShortPeakRobot.Robots.Algorithms.Services;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class ShortPeak
    {
        //private Peak lowPeak = new Peak();
        //private Peak highPeak = new Peak();

        private Peak currentLowPeak = new Peak();
        private Peak currentHighPeak = new Peak();

        private RobotData robotData = new RobotData();
        


        private ShortPeakModel HighShortPeak = new ShortPeakModel();
        private ShortPeakModel LowShortPeak = new ShortPeakModel();


        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }
        public bool IsPositionOpen { get; set; }

        public int Param1 { get; set; }
        public int Param2 { get; set; }
        public int Param3 { get; set; }
        public int Param4 { get; set; }



        //----------------------------------------------
        public ShortPeak(int robotId, int robotIndex)
        {
            RobotId = robotId;
            RobotIndex = robotIndex;
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
                case RobotCommands.CloseRobotPosition:
                    //CloseRobotPositionAsync();
                    break;
                case RobotCommands.ResetCandleAnalyse:
                    LastCandle = new();
                    break;
                default:
                    break;
            }

            var currentPrice = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1].ClosePrice;
            var currentCandle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame];
            var lastCompletedCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^2];

            var currentServerTime = DateTime.UtcNow.AddMinutes(-MarketData.Info.ServerTimeOffsetMinutes);
            bool isTradingAllowed = robot.CheckTradingStatus(currentServerTime);

            if (candles.Count == 0)
            {
                return;
            }

            var currentTime = DateTime.UtcNow;

            robot.SetCurrentPrifit(currentPrice);

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = lastCompletedCendle;
                LastTime = currentTime;

                await robot.SetRobotData();

                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(robot.RobotState, RobotIndex);

                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    robot.RobotState = new();
                    await robot.SetRobotData();

                    robotData = new();
                    

                    ChartAnalyse();
                }
                if (candlesAnalyse == CandlesAnalyse.SLTPCrossed)
                {
                    robot.Log(LogType.Error, " Пересечение СЛ или ТП во время отсутствия связи!");
                    RobotServices.ForceStopRobotAsync(RobotIndex);
                }


                //-------------                
                robot.IsReady = true;
            }

            if (!robot.IsReady)
            {
                return;
            }



            //проверка на разрыв связи             
            if (LastTime.AddSeconds(30) < currentTime)
            {
                robot.IsReady = false;
                LastCandle = new();

                var lostTime = (currentTime - LastTime.AddSeconds(30)).TotalSeconds;
                robot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
            }
            LastTime = currentTime;
            //-----------новая свечка           
            if (LastCandle.CloseTime < lastCompletedCendle.CloseTime)
            {
                LastCandle = lastCompletedCendle;
                NewCandle(lastCompletedCendle);
                SetRobotInfo();                
            }
            //----------- Действия при открытии позиции 
            if (!IsPositionOpen && robot.Position != 0)
            {
                IsPositionOpen = true;                 
                DropCurrentPeaks();// скидываем currentPeaks
                robotData = new();
                
            }
            // действия при закрытии позции 
            if (IsPositionOpen && robot.Position == 0)
            {
                IsPositionOpen = false;
            }
            //-------------------------------------------
            
            //снимаем ордер по сигналу если торговля запрещена 
            if (robot.SignalSellOrder.OrderId != 0 && !isTradingAllowed)
            {
                CancelSignalLowOrder();
                robotData.IsSellOrderPlaced = false;
                robotData.SignalSellPrice = 0;
            }
            if (robot.SignalBuyOrder.OrderId != 0 && !isTradingAllowed)
            {
                CancelSignalHighOrder();
                robotData.IsBuyOrderPlaced = false;
                robotData.SignalBuyPrice = 0;
            }
            //отслеживаем пробой пика 
            if (currentHighPeak.Volume != 0 && currentCandle.HighPrice - robot.BaseSettings.OffsetPercent >= currentHighPeak.Volume)
            {
                currentHighPeak = new();
            }
            if (currentLowPeak.Volume != 0 && currentCandle.LowPrice + robot.BaseSettings.OffsetPercent <= currentLowPeak.Volume)
            {
                currentLowPeak = new();
            }


            //================================== ордер по сигналу low peak
            var currentSignalLowPrice = 0m;

            if (currentLowPeak.Volume != 0)
            {
                currentSignalLowPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, currentLowPeak.Volume,
                       RobotOrderType.SignalSell, FuturesOrderType.StopMarket);
            }

            //снятие ордера при замене
            if (robot.Position == 0  && robotData.IsSellOrderPlaced && currentSignalLowPrice != robotData.SignalSellPrice)
            {
                CancelSignalLowOrder();
                robotData.IsSellOrderPlaced = false;
                robotData.SignalSellPrice = 0;
            }
            //выставление ордера
            if (isTradingAllowed && robot.Position == 0 && currentLowPeak.Volume != 0 && !robotData.IsSellOrderPlaced )
            {
                robotData.IsSellOrderPlaced = true;                
                robotData.SignalSellPrice = currentSignalLowPrice;

                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    StartDealOrderId = 0,
                    Symbol = robot.Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = robot.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = currentSignalLowPrice,
                    robotOrderType = RobotOrderType.SignalSell,
                    robotRequestType = RobotRequestType.PlaceOrder
                });
            }

            //================================== ордер по сигналу High peak
            var currentSignalHighPrice = 0m;

            if (currentHighPeak.Volume != 0)
            {
                currentSignalHighPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, currentHighPeak.Volume,
                       RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);
            }

            //снятие ордера при замене
            if (robot.Position == 0 && robotData.IsBuyOrderPlaced && currentSignalHighPrice != robotData.SignalBuyPrice)
            {
                CancelSignalHighOrder();
                robotData.IsBuyOrderPlaced = false;
                robotData.SignalBuyPrice = 0;
            }
            //выставление ордера
            if (isTradingAllowed && robot.Position == 0 && currentHighPeak.Volume != 0 && !robotData.IsBuyOrderPlaced )
            {
                robotData.IsBuyOrderPlaced = true;
                robotData.SignalBuyPrice = currentSignalHighPrice;

                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    StartDealOrderId = 0,
                    Symbol = robot.Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = robot.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = currentSignalHighPrice,
                    robotOrderType = RobotOrderType.SignalBuy,
                    robotRequestType = RobotRequestType.PlaceOrder
                });
            }



        }



        public void DropCurrentPeaks()
        {
            var robot = RobotVM.robots[RobotIndex];

            if (robot.Position != 0)
            {
                if (robot.Position > 0)
                {
                    if (!robot.BaseSettings.Revers)
                    {
                        currentHighPeak = new();
                    }
                    else
                    {
                        currentLowPeak = new();
                    }
                }
                if (robot.Position < 0)
                {
                    if (!robot.BaseSettings.Revers)
                    {
                        currentLowPeak = new();
                    }
                    else
                    {
                        currentHighPeak = new();
                    }
                }

            }
        }
        public void NewCandle(Candle newCandle)
        {
            var robot = RobotVM.robots[RobotIndex];

            var lastHighPeak = ShortPeakAnalyse.HighPeakAnalyse(HighShortPeak, newCandle);
            var lastLowPeak = ShortPeakAnalyse.LowPeakAnalyse(LowShortPeak, newCandle);

            if (lastHighPeak.Volume != 0)
            {
                currentHighPeak = lastHighPeak;
            }
            if (lastLowPeak.Volume != 0)
            {
                currentLowPeak = lastLowPeak;
            }

            // скидываем пики c учетом смещения 
            if (currentHighPeak.Volume != 0 && newCandle.ClosePrice - robot.BaseSettings.OffsetPercent >= currentHighPeak.Volume)
            {
                currentHighPeak = new();
            }

            if (currentLowPeak.Volume != 0 && newCandle.ClosePrice + robot.BaseSettings.OffsetPercent <= currentLowPeak.Volume)
            {
                currentLowPeak = new();
            }



        }

        public void ChartAnalyse()
        {
            var robot = RobotVM.robots[RobotIndex];
            var carrentCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame];

            var candlesForCheck = candles.Where(x => x.OpenTime != carrentCendle.OpenTime).ToList();

            HighShortPeak = new();
            LowShortPeak = new();
            currentLowPeak = new();
            currentHighPeak = new();


            foreach (var candle in candlesForCheck)
            {
                var lastHighPeak = ShortPeakAnalyse.HighPeakAnalyse(HighShortPeak, candle);
                var lastLowPeak = ShortPeakAnalyse.LowPeakAnalyse(LowShortPeak, candle);
                // сохраняем пики
                if (lastHighPeak.Volume != 0 && candle.HighPrice < lastHighPeak.Volume)
                {
                    currentHighPeak = lastHighPeak;
                }
                if (lastLowPeak.Volume != 0 && candle.LowPrice > lastLowPeak.Volume)
                {
                    currentLowPeak = lastLowPeak;
                }
                // скидываем пики c учетом смещения 
                if (currentHighPeak.Volume != 0 && candle.ClosePrice - robot.BaseSettings.OffsetPercent >= currentHighPeak.Volume)
                {
                    currentHighPeak = new();
                }
                if (currentLowPeak.Volume != 0 && candle.ClosePrice + robot.BaseSettings.OffsetPercent <= currentLowPeak.Volume)
                {
                    currentLowPeak = new();
                }
            }
            //-------- скидываем пики по последней свечке -------------------------------

            if (currentHighPeak.Volume != 0)
            {
                if (carrentCendle.HighPrice - robot.BaseSettings.OffsetPercent >= currentHighPeak.Volume)
                {
                    currentHighPeak = new();
                }
            }

            if (currentLowPeak.Volume != 0)
            {
                if (carrentCendle.LowPrice + robot.BaseSettings.OffsetPercent <= currentLowPeak.Volume)
                {
                    currentLowPeak = new();
                }

            }

            ///------------------------------
            var SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, currentLowPeak.Volume,
                        RobotOrderType.SignalSell, FuturesOrderType.StopMarket);
            if (robot.SignalSellOrder.StopPrice != null && robot.SignalSellOrder.StopPrice == SignalSellPrice)
            {
                //если ордер уже выставлен по этому пику, то скидываем пик (для сокращения операций с биржей)				
                currentLowPeak = new();
            }
            else
            {
                CancelSignalLowOrder();
            }


            var SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, currentHighPeak.Volume,
                        RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);
            if (robot.SignalBuyOrder.StopPrice != null && robot.SignalBuyOrder.StopPrice == SignalBuyPrice)
            {
                //если ордер уже выставлен по этому пику, то скидываем пик (для сокращения операций с биржей)
                currentHighPeak = new();
            }
            else
            {
                CancelSignalHighOrder();
            }
        }

        public void SetRobotInfo()
        {
            if (MarketData.Info.SelectedRobotIndex == RobotIndex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();

                    RobotInfoVM.AddParam("HighPeak_Peak", currentHighPeak.Volume.ToString());
                    RobotInfoVM.AddParam("HighPeak_Date", currentHighPeak.Date.ToString());
                    RobotInfoVM.AddParam("", "");

                    RobotInfoVM.AddParam("LowPeak_Peak", currentLowPeak.Volume.ToString());
                    RobotInfoVM.AddParam("LowPeak_Date", currentLowPeak.Date.ToString());

                    RobotInfoVM.AddParam("", "");
                    RobotInfoVM.AddParam("IsHighPeakOrderPlaced", robotData.IsBuyOrderPlaced.ToString());
                    RobotInfoVM.AddParam("IsLowPeakOrderPlaced", robotData.IsSellOrderPlaced.ToString());
                    
                    RobotInfoVM.AddParam("", "");
                    RobotInfoVM.AddParam("SignalHighPrice", robotData.SignalBuyPrice.ToString());
                    RobotInfoVM.AddParam("SignalLowPrice", robotData.SignalSellPrice.ToString());

                    RobotInfoVM.AddParam("", "");
                    RobotInfoVM.AddParam("LastCandle", LastCandle.OpenTime.ToString("HH:mm"));
                });
            }
        }




        private void CancelSignalLowOrder()
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
        private void CancelSignalHighOrder()
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
