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

namespace ShortPeakRobot.Robots.Algorithms
{
    public class SPTemp
    {
        
            private HighPeak HighPeak { get; set; } = new HighPeak();
            private LowPeak LowPeak { get; set; } = new LowPeak();

            public Candle LastCandle { get; set; } = new Candle();
            public DateTime LastTime { get; set; } = DateTime.UtcNow;
            public int RobotId { get; set; }
            public int RobotIndex { get; set; }
            public bool NeedChartAnalyse { get; set; }


            //----------------------------------------------
            public SPTemp(int robotId, int robotIndex)
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
                var carrentCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1];
                var candles = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame];
                var LastCompletedCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^2];

                if (candles.Count == 0)
                {
                    return;
                }

                var currentTime = DateTime.UtcNow;

                robot.SetCurrentPrifit(currentPrice);

                //Анализ графика
                if (LastCandle.OpenPrice == 0)
                {
                    LastCandle = LastCompletedCendle;
                    LastTime = currentTime;

                    await robot.SetRobotData();

                    var candlesAnalyse = RobotStateProcessor.CheckStateAsync(robot.RobotState, RobotIndex);

                    //--------- анализ графика ------------
                    if (candlesAnalyse == CandlesAnalyse.Required)
                    {
                        robot.RobotState = new();
                        await robot.SetRobotData();

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

                if (!robot.IsReady)//метод NewTick вызывается кажды 50мс - дальше не идем пока !robot.IsReady
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
                //-----------

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
                            OrderId = robot.SignalSellOrder.OrderId,
                            OrderType = robot.SignalSellOrder.Type
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
                            OrderId = robot.SignalBuyOrder.OrderId,
                            OrderType = robot.SignalSellOrder.Type
                        });
                        robot.RobotState.SignalBuyOrderId = 0;
                        robot.SignalBuyOrder = new();

                    }
                    HighPeak.Peak = 0;
                }

                //--------------- ордер по сигналу low peak
                var signalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, LowPeak.Peak,
                            RobotOrderType.SignalSell, FuturesOrderType.StopMarket);

                if (LowPeak.Peak != 0 && currentPrice > signalSellPrice)
                {


                    //LowPeak.Peak + RobotInstance.BaseSettings.OffsetPercent;
                    LowPeak.Peak = 0;//скидываем пики при открытии сделки
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
                    }
                    robot.RobotState.SignalSellOrderId = 0;
                    robot.SignalSellOrder = new();



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
                var signalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, HighPeak.Peak,
                            RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);

                if (HighPeak.Peak != 0 && currentPrice < signalBuyPrice)
                {
                    HighPeak.Peak = 0;//скидываем пики при открытии сделки

                    if (robot.SignalBuyOrder.OrderId != 0)
                    {
                        MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                        {
                            RobotIndex = RobotIndex,
                            Symbol = robot.Symbol,
                            robotRequestType = RobotRequestType.CancelOrder,
                            OrderId = robot.SignalBuyOrder.OrderId,
                            OrderType = robot.SignalSellOrder.Type
                        });
                    }
                    robot.RobotState.SignalBuyOrderId = 0;
                    robot.SignalBuyOrder = new();


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

                foreach (var candle in candlesForCheck)
                {
                    NewCandle(candle);
                }
                //-------- скидываем пики по последней свечке -------------------------------
                if (HighPeak.Peak != 0 && carrentCendle.HighPrice >= HighPeak.Peak)
                {
                    HighPeak.Peak = 0;
                }
                if (LowPeak.Peak != 0 && carrentCendle.LowPrice <= LowPeak.Peak)
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
                var robot = RobotVM.robots[RobotIndex];
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
                    if (candle.HighPrice - robot.BaseSettings.OffsetPercent < HighPeak.SecondCandle)
                    {
                        HighPeak.Peak = HighPeak.SecondCandle;
                    }
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
                    if (candle.LowPrice + robot.BaseSettings.OffsetPercent > LowPeak.SecondCandle)
                    {
                        LowPeak.Peak = LowPeak.SecondCandle;
                    }
                    LowPeak.FirstCandle = candle.LowPrice;
                    LowPeak.SecondCandle = 0;
                }
                if (LowPeak.FirstCandle != 0 && LowPeak.SecondCandle != 0 && candle.LowPrice < LowPeak.SecondCandle)
                {
                    LowPeak.FirstCandle = LowPeak.SecondCandle;
                    LowPeak.SecondCandle = candle.LowPrice;
                }
                //------- скидываем pick
                if (HighPeak.Peak != 0 && candle.HighPrice - robot.BaseSettings.OffsetPercent >= HighPeak.Peak)
                    HighPeak.Peak = 0;
                if (LowPeak.Peak != 0 && candle.LowPrice + robot.BaseSettings.OffsetPercent <= LowPeak.Peak)
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
