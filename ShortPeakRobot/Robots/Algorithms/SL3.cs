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
using ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels.SL3;
using ShortPeakRobot.Robots.Algorithms.Services;
using Microsoft.Extensions.Logging;
using CryptoExchange.Net.CommonObjects;

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
            var Robot = RobotVM.robots[RobotIndex];

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

            var currentPrice = MarketData.CandleDictionary[Robot.Symbol][Robot.BaseSettings.TimeFrame][^1].ClosePrice;

            var currentCandle = MarketData.CandleDictionary[Robot.Symbol][Robot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[Robot.Symbol][Robot.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[Robot.Symbol][Robot.BaseSettings.TimeFrame][^2];
            SetCurrentPrifit(currentPrice);

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
                var candlesAnalyse = CandlesAnalyse.Required;

                //проверка состояния предыдущей сессии 
                Robot.RobotState = RobotServices.LoadStateAsync(RobotIndex);
                await Robot.SetRobotOrders();


                candlesAnalyse = RobotStateProcessor.CheckStateAsync(Robot.RobotState, RobotIndex,
                    Robot.SignalBuyOrder, Robot.SignalSellOrder, Robot.StopLossOrder, Robot.TakeProfitOrder);


                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    Robot.RobotState = new();
                    await Robot.SetRobotOrders();

                    ChartAnalyse();

                }

                //------ выставление СЛ ТП после сбоя

                Robot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(Robot.RobotState.Position), Robot.SignalBuyOrder.OrderId, Robot.SignalSellOrder.OrderId);


                //-------------
                //IsReady = true;

            }

            if (!Robot.IsReady)
            {
                return;
            }



            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(Robot.BaseSettings.TimeFrame) < currentCandle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (currentCandle.CloseTime - LastCandleTime.AddSeconds(Robot.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(state: Robot.RobotState, RobotIndex,
                    Robot.SignalBuyOrder, Robot.SignalSellOrder, Robot.StopLossOrder, Robot.TakeProfitOrder);
                //------ выставление СЛ ТП после сбоя
                Robot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(Robot.RobotState.Position), Robot.SignalBuyOrder.OrderId, Robot.SignalSellOrder.OrderId);

                Robot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
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
            if (!NeedChartAnalyse && Robot.Position != 0)
            {
                NeedChartAnalyse = true;
            }
            if (NeedChartAnalyse && Robot.Position == 0)
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
                if (Robot.Position == 0)
                {
                    CancelSignalBuyOrder();

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        StartDealOrderId = 0,
                        Symbol = Robot.Symbol,
                        Side = (int)OrderSide.Buy,
                        OrderType = (int)FuturesOrderType.StopMarket,
                        Quantity = Robot.BaseSettings.Volume,
                        Price = 0,
                        StopPrice = HighLevels.CurrentLevel - Robot.BaseSettings.TakeProfitPercent,
                        robotOrderType = RobotOrderType.SignalBuy,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });
                }
                HighLevels.CurrentLevel = 0;
            }
            //--------------- ордер по сигналу Low 
            if (LowLevels.CurrentLevel != 0)
            {
                if (Robot.Position == 0)
                {
                    CancelSignalSellOrder();

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        StartDealOrderId = 0,
                        Symbol = Robot.Symbol,
                        Side = (int)OrderSide.Sell,
                        OrderType = (int)FuturesOrderType.StopMarket,
                        Quantity = Robot.BaseSettings.Volume,
                        Price = 0,
                        StopPrice = LowLevels.CurrentLevel + Robot.BaseSettings.TakeProfitPercent,
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

            var Robot = RobotVM.robots[RobotIndex];
            var currentCandle = MarketData.CandleDictionary[Robot.Symbol][Robot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[Robot.Symbol][Robot.BaseSettings.TimeFrame];

            var candlesForCheck = candles.Where(x => x.OpenTime != currentCandle.OpenTime).ToList();

            foreach (var candle in candlesForCheck)//цыкл без последней незакрытой свечи
            {
                SL3Services.CheckCrossHighDataPeaksSL3(HighData, HighLevels, candle);//проверяем пробой HighData High Peaks
                SL3Services.CheckCrossLowDataPeaksSL3(LowData, LowLevels, candle);
                //----------------]
                SL3Services.CheckCrossHighLevelsSL3(HighLevels, candle.HighPrice, Robot.BaseSettings.TakeProfitPercent);
                SL3Services.GetCurrentHighLevelSL3(HighLevels);
                SL3Services.CheckCrossLowLevelsSL3(LowLevels, candle.LowPrice, Robot.BaseSettings.TakeProfitPercent);
                SL3Services.GetCurrentLowLevelSL3(LowLevels);
                //----------------
                decimal lowPeak = ShortPeakAnalyse.LowPeakAnalyse(LowShortPeak, candle.LowPrice);
                decimal highPeak = ShortPeakAnalyse.HighPeakAnalyse(HighShortPeak, candle.HighPrice);
                //
                if (HighData.ThirdPeak != 0)
                {
                    if (lowPeak != 0)
                    {
                         SL3Services.SetHighDataPeaksSL3(HighData, 0, lowPeak, Robot.BaseSettings.TakeProfitPercent, candle.OpenTime); //						
                    }
                }

                if (highPeak != 0)
                {
                     SL3Services.SetHighDataPeaksSL3(HighData, highPeak, 0, Robot.BaseSettings.TakeProfitPercent, candle.OpenTime); //first highPeak                                                                                         
                }
                //-----------------------------------------------------------					
                if (LowData.ThirdPeak != 0)
                {
                    if (highPeak != 0)
                    {
                       SL3Services.SetLowDataPeaksSL3(LowData, highPeak, 0, Robot.BaseSettings.TakeProfitPercent, candle.OpenTime); //							
                    }
                }

                if (lowPeak != 0)
                {
                     SL3Services.SetLowDataPeaksSL3(LowData, 0, lowPeak, Robot.BaseSettings.TakeProfitPercent, candle.OpenTime); //first highPeak                                                                                     
                }
                //-----------------------------------------------------------
                SL3Services.CheckLiveTimeLevelsSL3(LowLevels, HighLevels, candle.OpenTime, 50);
                //-------------------------------------               

            }
            //анализ незакрытой свечи
            SL3Services.CheckCrossHighLevelsSL3(HighLevels, currentCandle.HighPrice, Robot.BaseSettings.TakeProfitPercent);
            SL3Services.GetCurrentHighLevelSL3(HighLevels);
            SL3Services.CheckCrossLowLevelsSL3(LowLevels, currentCandle.LowPrice, Robot.BaseSettings.TakeProfitPercent);
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
            var Robot = RobotVM.robots[RobotIndex];           

            if (Robot.SignalBuyOrder.OrderId != 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    Symbol = Robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = Robot.SignalBuyOrder.OrderId,
                    OrderType = Robot.SignalBuyOrder.Type
                });
                Robot.RobotState.SignalBuyOrderId = 0;
                Robot.SignalBuyOrder = new();
            }

        }

        private void CancelSignalSellOrder()
        {
            var Robot = RobotVM.robots[RobotIndex];
            // снимаем ордера по сигналам
            if (Robot.SignalSellOrder.OrderId != 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    Symbol = Robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = Robot.SignalSellOrder.OrderId,
                    OrderType = Robot.SignalSellOrder.Type
                });
                Robot.RobotState.SignalSellOrderId = 0;
                Robot.SignalSellOrder = new();
            }            
        }


        private void SetCurrentPrifit(decimal price)
        {
            var Robot = RobotVM.robots[RobotIndex];

            if (Robot.Position > 0)
            {
                Robot.Profit = price - Robot.OpenPositionPrice;
                return;
            }

            if (Robot.Position < 0)
            {
                Robot.Profit = Robot.OpenPositionPrice - price;
                return;
            }

            Robot.Profit = 0;
        }
    }
}
