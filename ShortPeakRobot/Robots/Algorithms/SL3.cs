using Binance.Net.Enums;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Market;
using ShortPeakRobot.ViewModel;
using System;
using System.Linq;
using ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels;
using ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels.SL3;
using ShortPeakRobot.Robots.Algorithms.Services;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class SL3
    {
        
        private ShortPeakModel HighShortPeak { get; set; } = new ShortPeakModel();
        private ShortPeakModel LowShortPeak { get; set; } = new ShortPeakModel();


        private PeakDataSL3 HighData { get; set; } = new PeakDataSL3();
        private PeakDataSL3 LowData { get; set; } = new PeakDataSL3();


        private LevelsSL3 HighLevels { get; set; } = new LevelsSL3();
        private LevelsSL3 LowLevels { get; set; } = new LevelsSL3();




        //private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }
        public bool NeedChartAnalyse { get; set; }



        //----------------------------------------------
        public SL3(int robotId, int robotIndex)
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

            var currentCandle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^2];
            SetCurrentPrifit(currentPrice);

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
                var candlesAnalyse = CandlesAnalyse.Required;
               
                await robot.SetRobotData();


                candlesAnalyse = RobotStateProcessor.CheckStateAsync(robot.RobotState, RobotIndex);


                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    robot.RobotState = new();
                    await robot.SetRobotData();

                    ChartAnalyse();

                }


                //-------------
                //IsReady = true;

            }

            if (!robot.IsReady)
            {
                return;
            }



            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(robot.BaseSettings.TimeFrame) < currentCandle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (currentCandle.CloseTime - LastCandleTime.AddSeconds(robot.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(state: robot.RobotState, RobotIndex);

                robot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
            }
            LastCandleTime = currentCandle.CloseTime;

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


            //---------------- 
            SL3Services.CheckCrossHighDataPeaksSL3(HighData, HighLevels, currentCandle);//проверяем пробой HighData High Peaks
            SL3Services.CheckCrossLowDataPeaksSL3(LowData, LowLevels, currentCandle);

            //if (Robot.Position != 0)
            //{
            //    SL3Services.CheckCrossHighLevelsSL3(HighLevels, currentCandle.HighPrice, Robot.BaseSettings.TakeProfitPercent);
            //    SL3Services.CheckCrossLowLevelsSL3(LowLevels, currentCandle.LowPrice, Robot.BaseSettings.TakeProfitPercent);
            //}

            //--------------- ордер по сигналу High 
            if (HighLevels.CurrentLevel != 0)
            {
                if (robot.Position == 0)
                {
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
                        StopPrice = HighLevels.CurrentLevel - robot.BaseSettings.TakeProfitPercent,
                        robotOrderType = RobotOrderType.SignalBuy,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });
                }
                HighLevels.CurrentLevel = 0;
            }
            //--------------- ордер по сигналу Low 
            if (LowLevels.CurrentLevel != 0)
            {
                if (robot.Position == 0)
                {
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
                        StopPrice = LowLevels.CurrentLevel + robot.BaseSettings.TakeProfitPercent,
                        robotOrderType = RobotOrderType.SignalSell,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });
                }
                LowLevels.CurrentLevel = 0;
            }



        }


        public void ChartAnalyse()
        {
            CancelSignalBuyOrder();
            CancelSignalSellOrder();

            HighData = new PeakDataSL3();
            HighLevels = new LevelsSL3();
            LowData =new PeakDataSL3();
            LowLevels = new LevelsSL3();

            var robot = RobotVM.robots[RobotIndex];
            var currentCandle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame];

            var candlesForCheck = candles.Where(x => x.OpenTime != currentCandle.OpenTime).ToList();

            foreach (var candle in candlesForCheck)//цыкл без последней незакрытой свечи
            {
                SL3Services.CheckCrossHighDataPeaksSL3(HighData, HighLevels, candle);//проверяем пробой HighData High Peaks
                SL3Services.CheckCrossLowDataPeaksSL3(LowData, LowLevels, candle);
                //----------------]
                SL3Services.CheckCrossHighLevelsSL3(HighLevels, candle.HighPrice, robot.BaseSettings.TakeProfitPercent);
                SL3Services.GetCurrentHighLevelSL3(HighLevels);
                SL3Services.CheckCrossLowLevelsSL3(LowLevels, candle.LowPrice, robot.BaseSettings.TakeProfitPercent);
                SL3Services.GetCurrentLowLevelSL3(LowLevels);
                //----------------
                Peak lowPeak = ShortPeakAnalyse.LowPeakAnalyse(LowShortPeak, candle);
                Peak highPeak = ShortPeakAnalyse.HighPeakAnalyse(HighShortPeak, candle);
                //
                if (HighData.ThirdPeak != 0)
                {
                    if (lowPeak.Volume != 0)
                    {
                         SL3Services.SetHighDataPeaksSL3(HighData, 0, lowPeak.Volume, robot.BaseSettings.TakeProfitPercent, candle.OpenTime); //						
                    }
                }

                if (highPeak.Volume != 0)
                {
                     SL3Services.SetHighDataPeaksSL3(HighData, highPeak.Volume, 0, robot.BaseSettings.TakeProfitPercent, candle.OpenTime); //first highPeak                                                                                         
                }
                //-----------------------------------------------------------					
                if (LowData.ThirdPeak != 0)
                {
                    if (highPeak.Volume != 0)
                    {
                       SL3Services.SetLowDataPeaksSL3(LowData, highPeak.Volume, 0, robot.BaseSettings.TakeProfitPercent, candle.OpenTime); //							
                    }
                }

                if (lowPeak.Volume != 0)
                {
                     SL3Services.SetLowDataPeaksSL3(LowData, 0, lowPeak.Volume, robot.BaseSettings.TakeProfitPercent, candle.OpenTime); //first highPeak                                                                                     
                }
                //-----------------------------------------------------------
                SL3Services.CheckLiveTimeLevelsSL3(LowLevels, HighLevels, candle.OpenTime, 50);
                //-------------------------------------               

            }
            //анализ незакрытой свечи
            SL3Services.CheckCrossHighLevelsSL3(HighLevels, currentCandle.HighPrice, robot.BaseSettings.TakeProfitPercent);
            SL3Services.GetCurrentHighLevelSL3(HighLevels);
            SL3Services.CheckCrossLowLevelsSL3(LowLevels, currentCandle.LowPrice, robot.BaseSettings.TakeProfitPercent);
            SL3Services.GetCurrentLowLevelSL3(LowLevels);

            SetRobotInfo();
        }

        public void SetRobotInfo()
        {
            if (MarketData.Info.SelectedRobotIndex == RobotIndex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();

                    RobotInfoVM.AddParam("HighData_First", HighData.FirstPeak.ToString() + " / " + HighData.FirstPeakDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("HighData_Second", HighData.SecondPeak.ToString() + " / " + HighData.SecondPeakDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("HighData_Third", HighData.ThirdPeak.ToString() + " / " + HighData.ThirdPeakDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("HighData_OppositePeak", HighData.OppositePeak.ToString() + " / " + HighData.OppositePeakDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam(" ", " ");

                    RobotInfoVM.AddParam("LowData_First", LowData.FirstPeak.ToString() + " / " + LowData.FirstPeakDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("LowData_Second",LowData.SecondPeak.ToString() + " / " + LowData.SecondPeakDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("LowData_Third", LowData.ThirdPeak.ToString() + " / " + LowData.ThirdPeakDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("LowData_OppositePeak", LowData.OppositePeak.ToString() + " / " + LowData.OppositePeakDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam(" ", " ");

                    RobotInfoVM.AddParam("HighLevel_First", HighLevels.FirstLevel.ToString() + " / " + HighLevels.FirstLevelDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("HighLevel_Second",HighLevels.SecondLevel.ToString() + " / " + HighLevels.SecondLevelDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("HighLevel_Third", HighLevels.ThirdLevel.ToString() + " / " + HighLevels.ThirdLevelDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("HighLevel_Current", HighLevels.CurrentLevel.ToString() + " / " + HighLevels.CurrentLevelDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam(" ", " ");

                    RobotInfoVM.AddParam("LowLevel_First", LowLevels.FirstLevel.ToString() + " / " + LowLevels.FirstLevelDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("LowLevel_Second",LowLevels.SecondLevel.ToString() + " / " + LowLevels.SecondLevelDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("LowLevel_Third", LowLevels.ThirdLevel.ToString() + " / " + LowLevels.ThirdLevelDate.ToString("dd.MM.yyyy"));
                    RobotInfoVM.AddParam("LowLevel_Current", LowLevels.CurrentLevel.ToString() + " / " + LowLevels.CurrentLevelDate.ToString("dd.MM.yyyy"));
                });
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

        private void CancelSignalSellOrder()
        {
            var robot = RobotVM.robots[RobotIndex];
            // снимаем ордера по сигналам
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


        private void SetCurrentPrifit(decimal price)
        {
            var robot = RobotVM.robots[RobotIndex];

            if (robot.Position > 0)
            {
                robot.Profit = price - robot.OpenPositionPrice;
                return;
            }

            if (robot.Position < 0)
            {
                robot.Profit = robot.OpenPositionPrice - price;
                return;
            }

            robot.Profit = 0;
        }
    }
}
