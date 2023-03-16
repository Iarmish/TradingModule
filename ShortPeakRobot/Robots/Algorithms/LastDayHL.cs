﻿using Binance.Net.Enums;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Threading;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class LastDayHL
    {

        private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }


        //----------------------------------------------
        private List<DayPeaks> Peaks = new List<DayPeaks>();
        private DayPeaks CurrentPeaks = new DayPeaks();
        public int HLDaysCount { get; set; } = 2;
        private bool NeedBuyOrder { get; set; }
        private bool NeedSellOrder { get; set; }

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
                    for (int i = HLDaysCount + 1; i > 1; i--)
                    {
                        var candle = candles[^i];
                        SaveDayHL(candle, currentPrice);                        
                    }
                    

                    List<decimal> peaksHigh = Peaks.Where(x => x.High != 0).Select(x=>x.High).ToList();
                    List<decimal> peaksLow = Peaks.Where(x => x.Low != 0).Select(x=>x.Low).ToList();

                    if (peaksHigh.Count > 0)
                    {
                        CurrentPeaks.High = peaksHigh.Min(x => x);
                    }

                    if (peaksLow.Count > 0)
                    {
                        CurrentPeaks.Low = peaksLow.Min(x => x);
                    }

                    for (int i = 0; i < Peaks.Count; i++)
                    {
                        if (Peaks[i].High == CurrentPeaks.High)
                        {
                            Peaks[i].High = 0;
                        }
                        if (Peaks[i].Low == CurrentPeaks.Low)
                        {
                            Peaks[i].Low = 0;
                        }
                    }
                    
                }

                //------ выставление СЛ ТП после сбоя
                 HLRobot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(HLRobot.RobotState.Position));

                //-------------
                IsReady = true;
                MarketServices.GetRobotData(RobotId);
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
                NewCandle(LastCandle, currentPrice);
            }
            //============================================
            

            //--------------- ордер по сигналу low peak

            if (CurrentPeaks.Low != 0)
            {
                var stopPrice = CurrentPeaks.Low;

                CurrentPeaks.Low = 0;

                //HLRobot.CancelOrderAsync(HLRobot.SignalSellOrder, "Cancel Signal Order");// снимаем возможный ордер по сигналу
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = RobotId,
                    Symbol = HLRobot.Symbol,
                    robotOrderType = RobotOrderType.SignalSell,
                    robotRequestType = RobotRequestType.CancelOrder
                });

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

                //var PlacedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                //    symbol: HLRobot.Symbol,
                //    side: OrderSide.Sell,
                //    type: FuturesOrderType.StopMarket,
                //    quantity: HLRobot.BaseSettings.Volume,
                //    timeInForce: TimeInForce.GoodTillCanceled,
                //    stopPrice: stopPrice);

                //if (PlacedOrder.Success)//проверка выставления ордера по сигналу
                //{
                //    HLRobot.SignalSellOrder = RobotOrderDTO.DTO(PlacedOrder, RobotId);
                //    HLRobot.RobotState.SignalSellOrderId = HLRobot.SignalSellOrder.OrderId;
                //    RobotServices.SaveState(RobotId, HLRobot.RobotState);

                //    RobotServices.SaveOrder(RobotId, HLRobot.SignalSellOrder, "Place Signal Order");
                //    MarketServices.GetRobotData(RobotId);//for IU
                //}
                //else
                //{
                //    HLRobot.Log(LogType.Error, "Signal order Sell errror" + PlacedOrder.Error.ToString());
                //}

                

            }

            //--------------- ордер по сигналу High peak
            if (CurrentPeaks.High != 0)
            {
                var stopPrice = CurrentPeaks.High;
                CurrentPeaks.High = 0;

                //HLRobot.CancelOrderAsync(HLRobot.SignalBuyOrder, "Cancel Signal Order");// снимаем возможный ордер по сигналу
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = RobotId,
                    Symbol = HLRobot.Symbol,
                    robotOrderType = RobotOrderType.SignalBuy,
                    robotRequestType = RobotRequestType.CancelOrder
                });

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

        private void SaveDayHL(Candle candle, decimal currentPrice)
        {
            var dayPeaks = new DayPeaks { High = candle.HighPrice, Low = candle.LowPrice };

            if (currentPrice > dayPeaks.High)
            {
                dayPeaks.High = 0;
            }

            if (currentPrice < dayPeaks.Low)
            {
                dayPeaks.Low = 0;
            }

            


            if (Peaks.Count < HLDaysCount)
            {
                Peaks.Add(dayPeaks);
            }
            else
            {
                Peaks.RemoveAt(0);
                Peaks.Add(dayPeaks);
            }
        }

        public void SetRobotInfo()
        {
            if (MarketData.Info.SelectedRobotId == RobotId)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();

                    RobotInfoVM.AddParam("LastCandle", LastCandle.OpenTime.ToString("HH:mm"));
                });
            }
        }


        private void NewCandle(Candle candle, decimal currentPrice)
        {
            SaveDayHL( candle, currentPrice);

            List<decimal> peaksHigh = Peaks.Where(x => x.High != 0).Select(x => x.High).ToList();
            List<decimal> peaksLow = Peaks.Where(x => x.Low != 0).Select(x => x.Low).ToList();

            if (peaksHigh.Count > 0)
            {
                CurrentPeaks.High = peaksHigh.Min(x => x);
            }

            if (peaksLow.Count > 0)
            {
                CurrentPeaks.Low = peaksLow.Min(x => x);
            }

            for (int i = 0; i < Peaks.Count; i++)
            {
                if (Peaks[i].High == CurrentPeaks.High)
                {
                    Peaks[i].High = 0;
                }
                if (Peaks[i].Low == CurrentPeaks.Low)
                {
                    Peaks[i].Low = 0;
                }
            }

            SetRobotInfo();//to UI
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
