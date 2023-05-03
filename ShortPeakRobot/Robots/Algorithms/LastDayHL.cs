using Binance.Net.Enums;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoExchange.Net.CommonObjects;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class LastDayHL
    {

        //private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }


        //----------------------------------------------
        private List<CandlePeak> HighPeaks = new List<CandlePeak>();
        private List<CandlePeak> LowPeaks = new List<CandlePeak>();
        private CandlePeak CurrentHighPeak = new CandlePeak();
        private CandlePeak CurrentLowPeak = new CandlePeak();


        public int HLDaysCount { get; set; } = 2;
        private bool NeedPeaksAnalyse { get; set; }
        private Robot RobotInstance { get; set; }

        //private decimal SignalBuyPrice { get; set; }
        //private decimal SignalSellPrice { get; set; }

        //----------------------------------------------
        public LastDayHL(int robotId, int robotIndex)
        {
            RobotId = robotId;
            RobotIndex = robotIndex;
        }

        public async void NewTick(RobotCommands command)
        {
            RobotInstance = RobotVM.robots[RobotIndex];

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

            var currentPrice = MarketData.CandleDictionary[RobotInstance.Symbol][RobotInstance.BaseSettings.TimeFrame][^1].ClosePrice;

            var carrentCendle = MarketData.CandleDictionary[RobotInstance.Symbol][RobotInstance.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[RobotInstance.Symbol][RobotInstance.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[RobotInstance.Symbol][RobotInstance.BaseSettings.TimeFrame][^2];
            SetCurrentPrifit(currentPrice);

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
                var candlesAnalyse = CandlesAnalyse.Required;

                //проверка состояния предыдущей сессии 
                RobotInstance.RobotState = RobotServices.LoadStateAsync(RobotIndex);
                await RobotInstance.SetRobotOrders();


                candlesAnalyse = RobotStateProcessor.CheckStateAsync(RobotInstance.RobotState, RobotIndex,
                    RobotInstance.SignalBuyOrder, RobotInstance.SignalSellOrder, RobotInstance.StopLossOrder, RobotInstance.TakeProfitOrder);

                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    RobotInstance.RobotState = new();
                    await RobotInstance.SetRobotOrders();

                    PeaksAnalyse(candles);

                }

                //------ выставление СЛ ТП после сбоя
                RobotInstance.SetSLTPAfterFail(candlesAnalyse, Math.Abs(RobotInstance.RobotState.Position), RobotInstance.SignalBuyOrder.OrderId, RobotInstance.SignalSellOrder.OrderId);

                //-------------                

            }

            if (!RobotInstance.IsReady)
            {
                return;
            }


            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(RobotInstance.BaseSettings.TimeFrame) < carrentCendle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (carrentCendle.CloseTime - LastCandleTime.AddSeconds(RobotInstance.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(state: RobotInstance.RobotState, RobotIndex,
                    RobotInstance.SignalBuyOrder, RobotInstance.SignalSellOrder, RobotInstance.StopLossOrder, RobotInstance.TakeProfitOrder);
                //------ выставление СЛ ТП после сбоя
                RobotInstance.SetSLTPAfterFail(candlesAnalyse, Math.Abs(RobotInstance.RobotState.Position), RobotInstance.SignalBuyOrder.OrderId, RobotInstance.SignalSellOrder.OrderId);

                RobotInstance.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
            }
            LastCandleTime = carrentCendle.CloseTime;

            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
            }

            if (LastCandle.CloseTime < LastCompletedCendle.CloseTime)//новая свечка
            {
                LastCandle = LastCompletedCendle;
                SetRobotInfo();

                if (RobotInstance.Position == 0)
                {
                    CancelSignalOrders();
                    PeaksAnalyse(candles);
                }

            }

            //------------------- Проверка на выход за пределы СЛ ТП
            //Task.Run(() => HLRobot.CheckSLTPCross(currentPrice));


            //----------- анализ графика после закрытия сделки ------------------------------
            if (!NeedPeaksAnalyse && RobotInstance.Position != 0)
            {
                NeedPeaksAnalyse = true;
            }

            if (NeedPeaksAnalyse && RobotInstance.Position == 0)
            {
                NeedPeaksAnalyse = false;
                PeaksAnalyse(candles);
            }

            //--------------- ордер по сигналу low peak
            if (CurrentLowPeak.Price != 0)
            {
                if (RobotInstance.Position == 0)
                {
                    var stopPrice = CurrentLowPeak.Price;

                    CurrentLowPeak.Price = 0;

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        StartDealOrderId = 0,
                        Symbol = RobotInstance.Symbol,
                        Side = (int)OrderSide.Sell,
                        OrderType = (int)FuturesOrderType.StopMarket,
                        Quantity = RobotInstance.BaseSettings.Volume,
                        Price = 0,
                        StopPrice = stopPrice,
                        robotOrderType = RobotOrderType.SignalSell,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });
                }



            }

            //--------------- ордер по сигналу High peak            
            if (CurrentHighPeak.Price != 0)
            {
                if (RobotInstance.Position == 0)
                {
                    var stopPrice = CurrentHighPeak.Price;
                    CurrentHighPeak.Price = 0;

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        StartDealOrderId = 0,
                        Symbol = RobotInstance.Symbol,
                        Side = (int)OrderSide.Buy,
                        OrderType = (int)FuturesOrderType.StopMarket,
                        Quantity = RobotInstance.BaseSettings.Volume,
                        Price = 0,
                        StopPrice = stopPrice,
                        robotOrderType = RobotOrderType.SignalBuy,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });
                }


            }




        }

        private void GetCurrenLowPeak(decimal offset)
        {

            decimal currentLowPeak = 0;
            //-------------------------            
            foreach (var peak in LowPeaks)
            {
                if (!peak.Taken)
                {
                    if (currentLowPeak == 0)
                    {
                        currentLowPeak = peak.Price;
                    }
                    else
                    {
                        if (peak.Price > currentLowPeak)
                        {
                            currentLowPeak = peak.Price;
                        }
                    }

                }
            }

            if (currentLowPeak != 0)
            {
                for (int i = 0; i < LowPeaks.Count; i++)
                {
                    if (LowPeaks[i].Price == currentLowPeak)
                    {
                        LowPeaks[i].Taken = true;
                    }
                }
            }

            if (currentLowPeak != 0)
            {
                CurrentLowPeak.Price = currentLowPeak - offset;
            }
            else
            {
                CurrentLowPeak.Price = 0;
            }
        }

        private void GetCurrenHighPeak(decimal offset)
        {
            decimal currentHighPeak = 0;
            //-------------------------
            foreach (var peak in HighPeaks)
            {
                if (!peak.Taken)
                {
                    if (currentHighPeak == 0)
                    {
                        currentHighPeak = peak.Price;
                    }
                    else
                    {
                        if (peak.Price < currentHighPeak)
                        {
                            currentHighPeak = peak.Price;
                        }
                    }

                }
            }

            if (currentHighPeak != 0)
            {
                for (int i = 0; i < HighPeaks.Count; i++)
                {
                    if (HighPeaks[i].Price == currentHighPeak)
                    {
                        HighPeaks[i].Taken = true;
                    }
                }
            }

            if (currentHighPeak != 0)
            {
                CurrentHighPeak.Price = currentHighPeak + offset;
            }
            else
            {
                CurrentHighPeak.Price = 0;
            }

        }

        private void SaveDayHL(Candle newCandle)
        {
            var highPeak = new CandlePeak { Price = newCandle.HighPrice, Taken = false };
            var lowPeak = new CandlePeak { Price = newCandle.LowPrice, Taken = false };

            //----------------------------------------
            if (HighPeaks.Count < HLDaysCount)
            {
                HighPeaks.Add(highPeak);
            }
            else
            {
                HighPeaks.RemoveAt(0);
                HighPeaks.Add(highPeak);
            }

            if (LowPeaks.Count < HLDaysCount)
            {
                LowPeaks.Add(lowPeak);
            }
            else
            {
                LowPeaks.RemoveAt(0);
                LowPeaks.Add(lowPeak);
            }
            //


        }



        public void SetRobotInfo()
        {
            if (MarketData.Info.SelectedRobotIndex == RobotIndex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();

                    RobotInfoVM.AddParam("CurrentHighPeak", CurrentHighPeak.Price.ToString());
                    RobotInfoVM.AddParam("CurrentLowPeak", CurrentLowPeak.Price.ToString());

                    foreach (var item in HighPeaks)
                    {
                        RobotInfoVM.AddParam("HighPeaks", item.Price + " " + item.Taken.ToString());
                    }
                    foreach (var item in LowPeaks)
                    {
                        RobotInfoVM.AddParam("LowPeaks", item.Price + " " + item.Taken.ToString());
                    }
                });
            }
        }



        private void PeaksAnalyse(List<Candle> candles)
        {
            RobotInstance = RobotVM.robots[RobotIndex];

            CancelSignalOrders();
            //----------------

            for (int i = HLDaysCount + 1; i > 1; i--)
            {
                var candle = candles[^i];

                SaveDayHL(candle);
            }

            var currentCandle = candles[^1];

            // high
            for (int i = 0; i < HighPeaks.Count; i++)
            {
                if (i > 0)
                {
                    for (int n = 0; n < i; n++)
                    {
                        if (HighPeaks[i].Price > HighPeaks[n].Price + RobotInstance.BaseSettings.OffsetPercent)
                        {
                            HighPeaks[n].Taken = true;
                        }
                    }
                }


                if (currentCandle.HighPrice > HighPeaks[i].Price + RobotInstance.BaseSettings.OffsetPercent)
                {
                    HighPeaks[i].Taken = true;
                }
            }

            //----- low 
            for (int i = 0; i < LowPeaks.Count; i++)
            {
                if (i > 0)
                {
                    for (int n = 0; n < i; n++)
                    {
                        if (LowPeaks[i].Price < LowPeaks[n].Price - RobotInstance.BaseSettings.OffsetPercent)
                        {
                            LowPeaks[n].Taken = true;
                        }
                    }
                }


                if (currentCandle.LowPrice < LowPeaks[i].Price - RobotInstance.BaseSettings.OffsetPercent)
                {
                    LowPeaks[i].Taken = true;
                }
            }

            GetCurrenHighPeak(RobotInstance.BaseSettings.OffsetPercent);
            GetCurrenLowPeak(RobotInstance.BaseSettings.OffsetPercent);

        }


        private void CancelSignalOrders()
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
            }

            if (Robot.SignalBuyOrder.OrderId != 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    Symbol = Robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = Robot.SignalBuyOrder.OrderId,
                    OrderType = Robot.SignalSellOrder.Type
                });
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
