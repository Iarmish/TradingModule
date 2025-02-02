﻿using Binance.Net.Enums;
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

namespace ShortPeakRobot.Robots.Algorithms
{
    public class SL2
    {
        private HighPeak HighPeak { get; set; } = new HighPeak();
        private LowPeak LowPeak { get; set; } = new LowPeak();

        private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }
        public bool NeedChartAnalyse { get; set; }



        //----------------------------------------------
        public SL2(int robotId, int robotIndex)
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
            var candles = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^2];

            robot.SetCurrentPrifit(currentPrice);

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
                var candlesAnalyse = CandlesAnalyse.Required;
               
                await robot.SetRobotData();


                candlesAnalyse =  RobotStateProcessor.CheckStateAsync(robot.RobotState, RobotIndex);

                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    robot.RobotState = new();
                    await robot.SetRobotData();

                    ChartAnalyse();

                }

                //-------------
                IsReady = true;

            }

            if (!IsReady)
            {
                return;
            }



            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(robot.BaseSettings.TimeFrame) < carrentCendle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (carrentCendle.CloseTime - LastCandleTime.AddSeconds(robot.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse =  RobotStateProcessor.CheckStateAsync(state: robot.RobotState, RobotIndex);

                robot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
            }
            LastCandleTime = carrentCendle.CloseTime;

            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
            }

            if (LastCandle.CloseTime < LastCompletedCendle.CloseTime)//новая свечка
            {
                LastCandle = LastCompletedCendle;
                NewCandle(LastCandle);
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

            //------------------- Проверка на выход за пределы СЛ ТП
            //Task.Run(() => SpRobot.CheckSLTPCross(currentPrice));

            //---------------- скидываем пики
            if ((LowPeak.Peak != 0 && robot.Position != 0) ||
            (robot.SignalSellOrder.OrderId != 0 && !robot.CheckTradingStatus(carrentCendle.OpenTime)))
            {
                if (robot.SignalSellOrder.OrderId != 0)//снимаем ордер по сигналу если торговля запрещена 
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalSellOrder.OrderId
                    });
                    robot.RobotState.SignalSellOrderId = 0;
                    robot.SignalSellOrder = new();

                }
                LowPeak.Peak = 0;
            }


            if ((HighPeak.Peak != 0 && robot.Position != 0) ||
                (robot.SignalBuyOrder.OrderId != 0 && !robot.CheckTradingStatus(carrentCendle.OpenTime)))
            {
                if (robot.SignalBuyOrder.OrderId != 0)//снимаем ордер по сигналу если торговля запрещена 
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalBuyOrder.OrderId
                    });
                    robot.RobotState.SignalBuyOrderId = 0;
                    robot.SignalBuyOrder = new();

                }
                HighPeak.Peak = 0;
            }

            //--------------- ордер по сигналу low peak

            if (LowPeak.Peak != 0 && currentPrice > LowPeak.Peak)
            {
                var stopPrice = LowPeak.Peak;
                LowPeak.Peak = 0;//скидываем пики при открытии сделки
                if (robot.SignalSellOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalSellOrder.OrderId
                    });
                }
                robot.RobotState.SignalSellOrderId = 0;
                robot.SignalSellOrder = new();


                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    Symbol = robot.Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = robot.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = stopPrice,
                    robotOrderType = RobotOrderType.SignalSell,
                    robotRequestType = RobotRequestType.PlaceOrder
                });



            }

            //--------------- ордер по сигналу High peak
            if (HighPeak.Peak != 0 && currentPrice < HighPeak.Peak)
            {
                var stopPrice = HighPeak.Peak;
                HighPeak.Peak = 0;//скидываем пики при открытии сделки

                if (robot.SignalBuyOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalBuyOrder.OrderId
                    });
                }
                robot.RobotState.SignalBuyOrderId = 0;
                robot.SignalBuyOrder = new();



                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    Symbol = robot.Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = robot.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = stopPrice,
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

            foreach (var candle in candlesForCheck)
            {
                NewCandle(candle);
            }
            //-------- скидываем пики по последней свечке -------------------------------
            if (carrentCendle.HighPrice >= HighPeak.Peak)
            {
                HighPeak.Peak = 0;
            }
            if (carrentCendle.LowPrice <= LowPeak.Peak)
            {
                LowPeak.Peak = 0;
            }
        }

        public void SetRobotInfo()
        {
            if (MarketData.Info.SelectedRobotIndex == RobotIndex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();
                    RobotInfoVM.AddParam("HighPeak_FirstCandle", HighPeak.FirstCandle.ToString());
                    RobotInfoVM.AddParam("HighPeak_SecondCandle", HighPeak.SecondCandle.ToString());
                    RobotInfoVM.AddParam("HighPeak_Peak", HighPeak.Peak.ToString());
                    RobotInfoVM.AddParam("LowPeak_FirstCandle", LowPeak.FirstCandle.ToString());
                    RobotInfoVM.AddParam("LowPeak_SecondCandle", LowPeak.SecondCandle.ToString());
                    RobotInfoVM.AddParam("LowPeak_Peak", LowPeak.Peak.ToString());
                    RobotInfoVM.AddParam("LastCandle", LastCandle.OpenTime.ToString("HH:mm"));
                });
            }
        }


        private void NewCandle(Candle candle)
        {
            //------------инициализация high1------------
            if (HighPeak.FirstCandle == 0) { HighPeak.FirstCandle = candle.HighPrice; }
            //--------------добавление high2 или обновление high1 ---------------------
            if (HighPeak.FirstCandle != 0 && HighPeak.SecondCandle == 0 && candle.HighPrice > HighPeak.FirstCandle)
            {
                HighPeak.SecondCandle = candle.HighPrice;
            }
            if (HighPeak.FirstCandle != 0 && HighPeak.SecondCandle == 0 && candle.HighPrice < HighPeak.FirstCandle)
            {
                HighPeak.FirstCandle = candle.HighPrice;
            }
            //---------- добавление short hi или обновление high1 high2
            if (HighPeak.FirstCandle != 0 && HighPeak.SecondCandle != 0 && candle.HighPrice < HighPeak.SecondCandle)
            {
                HighPeak.Peak = HighPeak.SecondCandle;
                HighPeak.FirstCandle = candle.HighPrice;
                HighPeak.SecondCandle = 0;
            }
            if (HighPeak.FirstCandle != 0 && HighPeak.SecondCandle != 0 && candle.HighPrice > HighPeak.SecondCandle)
            {
                HighPeak.FirstCandle = HighPeak.SecondCandle;
                HighPeak.SecondCandle = candle.HighPrice;
            }

            //------------инициализация low1  candle_low------------

            if (LowPeak.FirstCandle == 0) { LowPeak.FirstCandle = candle.LowPrice; }
            //--------------добавление low2 или обновление low1 ---------------------
            if (LowPeak.FirstCandle != 0 && LowPeak.SecondCandle == 0 && candle.LowPrice < LowPeak.FirstCandle)
            {
                LowPeak.SecondCandle = candle.LowPrice;
            }
            if (LowPeak.FirstCandle != 0 && LowPeak.SecondCandle == 0 && candle.LowPrice > LowPeak.FirstCandle)
            {
                LowPeak.FirstCandle = candle.LowPrice;
            }
            //---------- добавление short low или обновление low1 low2
            if (LowPeak.FirstCandle != 0 && LowPeak.SecondCandle != 0 && candle.LowPrice > LowPeak.SecondCandle)
            {
                LowPeak.Peak = LowPeak.SecondCandle;
                LowPeak.FirstCandle = candle.LowPrice;
                LowPeak.SecondCandle = 0;
            }
            if (LowPeak.FirstCandle != 0 && LowPeak.SecondCandle != 0 && candle.LowPrice < LowPeak.SecondCandle)
            {
                LowPeak.FirstCandle = LowPeak.SecondCandle;
                LowPeak.SecondCandle = candle.LowPrice;
            }
            //------- скидываем pick
            if (HighPeak.Peak != 0 && candle.HighPrice > HighPeak.Peak)
                HighPeak.Peak = 0;
            if (LowPeak.Peak != 0 && candle.LowPrice < LowPeak.Peak)
                LowPeak.Peak = 0;

            SetRobotInfo();//to UI
        }

        //private void SetCurrentPrifit(decimal price)
        //{
        //    var robot = RobotVM.robots[RobotIndex];

        //    if (robot.Position > 0)
        //    {
        //        robot.Profit = price - robot.OpenPositionPrice;
        //        return;
        //    }

        //    if (robot.Position < 0)
        //    {
        //        robot.Profit = robot.OpenPositionPrice - price;
        //        return;
        //    }

        //    robot.Profit = 0;
        //}
    }
}
