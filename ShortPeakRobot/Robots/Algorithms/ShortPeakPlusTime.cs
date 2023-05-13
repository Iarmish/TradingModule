using Binance.Net.Enums;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels;
using ShortPeakRobot.Robots.Algorithms.Services;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class ShortPeakPlusTime
    {
        private Peak lowPeak = new Peak();
        private Peak highPeak = new Peak();

        private ShortPeakModel HighShortPeak = new ShortPeakModel();
        private ShortPeakModel LowShortPeak = new ShortPeakModel();
        //private HighPeak HighPeak { get; set; } = new HighPeak();
        //private LowPeak LowPeak { get; set; } = new LowPeak();

        //private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }
        public bool NeedChartAnalyse { get; set; }

        public int Param1 { get; set; } = 3;//количество свечей после которых пик считается действительным
        public int Param2 { get; set; }
        public int Param3 { get; set; }
        public int Param4 { get; set; }

        //----------------------------------------------
        public ShortPeakPlusTime(int robotId, int robotIndex)
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
            var carrentCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1];
            var LastCompletedCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^2];

            robot.SetCurrentPrifit(currentPrice);

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;

                await robot.SetRobotData();

                var candlesAnalyse = CandlesAnalyse.Required;

               


                candlesAnalyse = RobotStateProcessor.CheckStateAsync(robot.RobotState, RobotIndex);


                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    robot.RobotState = new();
                    await robot.SetRobotData();

                    ChartAnalyse();

                }


            }

            if (!robot.IsReady)
            {
                return;
            }



            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(robot.BaseSettings.TimeFrame + 10) < carrentCendle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (carrentCendle.CloseTime - LastCandleTime.AddSeconds(robot.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(state: robot.RobotState, RobotIndex);

                robot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
            }
            LastCandleTime = carrentCendle.CloseTime;
            //-----------
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
            }

            if (LastCandle.CloseTime < LastCompletedCendle.CloseTime)//новая свечка
            {
                LastCandle = LastCompletedCendle;
                ChartAnalyse();
            }
            //----------- анализ графика после закрытия сделки ------------------------------
            if (!NeedChartAnalyse && robot.Position != 0)
            {
                NeedChartAnalyse = true;
            }
            if (NeedChartAnalyse && robot.Position == 0)
            {
                NeedChartAnalyse = false;
                ChartAnalyse();
            }


            //---------------- скидываем пики
            if ((lowPeak.Volume != 0 && robot.Position != 0) ||
            (robot.SignalSellOrder.OrderId != 0 && !robot.CheckTradingStatus(carrentCendle.OpenTime)))
            {
                //снимаем ордер по сигналу если торговля запрещена 
                CancelSignalSellOrder();

                lowPeak = new();
            }


            if ((highPeak.Volume != 0 && robot.Position != 0) ||
                (robot.SignalBuyOrder.OrderId != 0 && !robot.CheckTradingStatus(carrentCendle.OpenTime)))
            {
                //снимаем ордер по сигналу если торговля запрещена                 
                CancelSignalBuyOrder();

                highPeak = new();
            }

            //--------------- ордер по сигналу low peak
            var signalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, lowPeak.Volume,
                        RobotOrderType.SignalSell, FuturesOrderType.StopMarket);

            if (lowPeak.Volume != 0 && currentPrice > signalSellPrice)
            {
                lowPeak = new();//скидываем пики при открытии сделки                
                CancelSignalSellOrder();

                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    StartDealOrderId = 0,
                    Symbol = robot.Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = robot.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = signalSellPrice,
                    robotOrderType = RobotOrderType.SignalSell,
                    robotRequestType = RobotRequestType.PlaceOrder
                });



            }

            //--------------- ордер по сигналу High peak
            var signalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, highPeak.Volume,
                        RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);

            if (highPeak.Volume != 0 && currentPrice < signalBuyPrice)
            {
                highPeak = new();//скидываем пики при открытии сделки
                CancelSignalBuyOrder();


                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    StartDealOrderId = 0,
                    Symbol = robot.Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = robot.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = signalBuyPrice,
                    robotOrderType = RobotOrderType.SignalBuy,
                    robotRequestType = RobotRequestType.PlaceOrder
                });





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
            highPeak = new();
            lowPeak = new();

            foreach (var candle in candlesForCheck)
            {
                var lastHighPeak = ShortPeakAnalyse.HighPeakAnalyse(HighShortPeak, candle);
                var lastLowPeak = ShortPeakAnalyse.LowPeakAnalyse(LowShortPeak, candle);
                // сохраняем пики
                if (lastHighPeak.Volume != 0 && candle.HighPrice < lastHighPeak.Volume)
                {
                    highPeak = lastHighPeak;
                }
                if (lastLowPeak.Volume != 0 && candle.LowPrice  > lastLowPeak.Volume)
                {
                    lowPeak = lastLowPeak;
                }
                // скидываем пики c учетом смещения 
                if (highPeak.Volume != 0 && candle.HighPrice - robot.BaseSettings.OffsetPercent >= highPeak.Volume)
                {
                    highPeak = new();                    
                }
                if (lowPeak.Volume != 0 && candle.LowPrice + robot.BaseSettings.OffsetPercent <= lowPeak.Volume)
                {
                    
                    lowPeak = new();                    
                }

            }
            //-------- скидываем пики по последней свечке -------------------------------
            var awaitDateHighPeak = highPeak.Date.AddSeconds(robot.BaseSettings.TimeFrame * Param1);
            var awaitDateLowPeak = lowPeak.Date.AddSeconds(robot.BaseSettings.TimeFrame * Param1);

            if (highPeak.Volume != 0)
            {
                if (carrentCendle.HighPrice - robot.BaseSettings.OffsetPercent >= highPeak.Volume ||
                    carrentCendle.OpenTime <= awaitDateHighPeak)
                {                    
                    highPeak = new();
                }
            }

            if (lowPeak.Volume != 0)
            {
                if (carrentCendle.LowPrice + robot.BaseSettings.OffsetPercent <= lowPeak.Volume ||
                    carrentCendle.OpenTime <= awaitDateLowPeak)
                {                    
                    lowPeak = new();
                }

            }

            var SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, lowPeak.Volume,
                        RobotOrderType.SignalSell, FuturesOrderType.StopMarket);
            if (robot.SignalSellOrder.StopPrice != null &&  robot.SignalSellOrder.StopPrice == SignalSellPrice)
            {
                //если ордер уже выставлен по этому пику, то скидываем пик (для сокращения операций с биржей)
                lowPeak = new();
            }
            else
            {
                CancelSignalSellOrder();
            }


            var SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, highPeak.Volume,
                        RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);
            if (robot.SignalBuyOrder.StopPrice != null && robot.SignalBuyOrder.StopPrice == SignalBuyPrice)
            {
                //если ордер уже выставлен по этому пику, то скидываем пик (для сокращения операций с биржей)
                highPeak = new();
            }
            else
            {
                CancelSignalBuyOrder();
            }
        }

        public void SetRobotInfo()
        {
            if (MarketData.Info.SelectedRobotIndex == RobotIndex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();

                    RobotInfoVM.AddParam("HighPeak_Peak", highPeak.Volume.ToString());
                    RobotInfoVM.AddParam("HighPeak_Date", highPeak.Date.ToString());
                    RobotInfoVM.AddParam("", "");

                    RobotInfoVM.AddParam("LowPeak_Peak", lowPeak.Volume.ToString());
                    RobotInfoVM.AddParam("LowPeak_Date", lowPeak.Date.ToString());

                    RobotInfoVM.AddParam("", "");
                    RobotInfoVM.AddParam("LastCandle", LastCandle.OpenTime.ToString("HH:mm"));
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
