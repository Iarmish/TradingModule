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

        private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }


        //----------------------------------------------
        private List<CandlePeak> HighPeaks = new List<CandlePeak>();
        private List<CandlePeak> LowPeaks = new List<CandlePeak>();
        private CandlePeak CurrentHighPeak = new CandlePeak();
        private CandlePeak CurrentLowPeak = new CandlePeak();


        public int HLDaysCount { get; set; } = 2;
        private bool NeedPeaksAnalyse { get; set; }


        //private decimal SignalBuyPrice { get; set; }
        //private decimal SignalSellPrice { get; set; }

        //----------------------------------------------
        public LastDayHL(int robotId)
        {
            RobotId = robotId;
        }

        public async void NewTick(RobotCommands command)
        {
            var HLRobot = RobotVM.robots[RobotId];

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

            var currentPrice = MarketData.CandleDictionary[HLRobot.Symbol][HLRobot.BaseSettings.TimeFrame][^1].ClosePrice;

            var carrentCendle = MarketData.CandleDictionary[HLRobot.Symbol][HLRobot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[HLRobot.Symbol][HLRobot.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[HLRobot.Symbol][HLRobot.BaseSettings.TimeFrame][^2];
            SetCurrentPrifit(currentPrice);

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
                var candlesAnalyse = CandlesAnalyse.Required;

                //проверка состояния предыдущей сессии 
                HLRobot.RobotState = RobotServices.LoadStateAsync(RobotId);
                await HLRobot.ResetRobotState();


                candlesAnalyse = RobotStateProcessor.CheckStateAsync(HLRobot.RobotState, RobotId,
                    HLRobot.SignalBuyOrder, HLRobot.SignalSellOrder, HLRobot.StopLossOrder, HLRobot.TakeProfitOrder);

                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    PeaksAnalyse(candles);

                }

                //------ выставление СЛ ТП после сбоя
                HLRobot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(HLRobot.RobotState.Position));

                //-------------
                IsReady = true;

            }

            if (!IsReady)
            {
                return;
            }


            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(HLRobot.BaseSettings.TimeFrame) < carrentCendle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (carrentCendle.CloseTime - LastCandleTime.AddSeconds(HLRobot.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(state: HLRobot.RobotState, robotId: RobotId,
                    HLRobot.SignalBuyOrder, HLRobot.SignalSellOrder, HLRobot.StopLossOrder, HLRobot.TakeProfitOrder);
                //------ выставление СЛ ТП после сбоя
                HLRobot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(HLRobot.RobotState.Position));

                HLRobot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
            }
            LastCandleTime = carrentCendle.CloseTime;

            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
            }

            if (LastCandle.CloseTime < LastCompletedCendle.CloseTime)//новая свечка
            {
                LastCandle = LastCompletedCendle;

                if (HLRobot.Position == 0)
                {
                    CancelSignalOrders();
                    PeaksAnalyse(candles);
                }




            }
            //============================================
            //------------------- Проверка на выход за пределы СЛ ТП
            //Task.Run(() => HLRobot.CheckSLTPCross(currentPrice));


            //----------- анализ графика после закрытия сделки ------------------------------
            if (!NeedPeaksAnalyse && HLRobot.Position != 0)
            {
                NeedPeaksAnalyse = true;
            }

            if (NeedPeaksAnalyse && HLRobot.Position == 0)
            {
                NeedPeaksAnalyse = false;

                PeaksAnalyse(candles);   
            }
            SetRobotInfo();
            //--------------- ордер по сигналу low peak
            if (CurrentLowPeak.Price != 0)
            {
                if (HLRobot.Position == 0)
                {
                    var stopPrice = CurrentLowPeak.Price;

                    CurrentLowPeak.Price = 0;

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = HLRobot.Symbol,
                        Side = (int)OrderSide.Sell,
                        OrderType = (int)FuturesOrderType.StopMarket,
                        Quantity = HLRobot.BaseSettings.Volume,
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
                if (HLRobot.Position == 0)
                {
                    var stopPrice = CurrentHighPeak.Price;
                    CurrentHighPeak.Price = 0;

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = HLRobot.Symbol,
                        Side = (int)OrderSide.Buy,
                        OrderType = (int)FuturesOrderType.StopMarket,
                        Quantity = HLRobot.BaseSettings.Volume,
                        Price = 0,
                        StopPrice = stopPrice,
                        robotOrderType = RobotOrderType.SignalBuy,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });
                }
                

            }

           


        }

        private void GetCurrenLowPeak()
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

            CurrentLowPeak.Price = currentLowPeak;

        }

        private void GetCurrenHighPeak()
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


            CurrentHighPeak.Price = currentHighPeak;

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
            if (MarketData.Info.SelectedRobotId == RobotId)
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

        private void CheckPeacks(decimal currentPrice)
        {
            // high
            for (int i = 0; i < HighPeaks.Count; i++)
            {
                if (currentPrice > HighPeaks[i].Price)
                {
                    HighPeaks[i].Taken = true;
                }
            }

            // low
            for (int i = 0; i < LowPeaks.Count; i++)
            {
                if (currentPrice < LowPeaks[i].Price)
                {
                    LowPeaks[i].Taken = true;
                }
            }
        }

        private void PeaksAnalyse(List<Candle> candles)
        {
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
                        if (HighPeaks[i].Price > HighPeaks[n].Price)
                        {
                            HighPeaks[n].Taken = true;
                        }
                    }
                }


                if (currentCandle.HighPrice > HighPeaks[i].Price)
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
                        if (LowPeaks[i].Price < LowPeaks[n].Price)
                        {
                            LowPeaks[n].Taken = true;
                        }
                    }
                }


                if (currentCandle.LowPrice < LowPeaks[i].Price)
                {
                    LowPeaks[i].Taken = true;
                }
            }

            GetCurrenHighPeak();
            GetCurrenLowPeak();

        }


        private void CancelSignalOrders()
        {
            var HLRobot = RobotVM.robots[RobotId];
            // снимаем ордера по сигналам
            if (HLRobot.SignalSellOrder.OrderId != 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = RobotId,
                    Symbol = HLRobot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = HLRobot.SignalSellOrder.OrderId
                });
            }

            if (HLRobot.SignalBuyOrder.OrderId != 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = RobotId,
                    Symbol = HLRobot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = HLRobot.SignalBuyOrder.OrderId
                });
            }
        }

        private void SetCurrentPrifit(decimal price)
        {
            var HLRobot = RobotVM.robots[RobotId];

            if (HLRobot.Position > 0)
            {
                HLRobot.Profit = price - HLRobot.OpenPositionPrice;
                return;
            }

            if (HLRobot.Position < 0)
            {
                HLRobot.Profit = HLRobot.OpenPositionPrice - price;
                return;
            }

            HLRobot.Profit = 0;
        }
    }
}
