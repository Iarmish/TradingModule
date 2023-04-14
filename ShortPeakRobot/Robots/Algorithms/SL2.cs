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
    public class SL2
    {
        private HighPeak HighPeak { get; set; } = new HighPeak();
        private LowPeak LowPeak { get; set; } = new LowPeak();

        private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public bool NeedChartAnalyse { get; set; }



        //----------------------------------------------
        public SL2(int robotId)
        {
            RobotId = robotId;
        }

        public async void NewTick(RobotCommands command)
        {
            var SpRobot = RobotVM.robots[RobotId];

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

            var currentPrice = MarketData.CandleDictionary[SpRobot.Symbol][SpRobot.BaseSettings.TimeFrame][^1].ClosePrice;

            var carrentCendle = MarketData.CandleDictionary[SpRobot.Symbol][SpRobot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[SpRobot.Symbol][SpRobot.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[SpRobot.Symbol][SpRobot.BaseSettings.TimeFrame][^2];
            SetCurrentPrifit(currentPrice);

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
                var candlesAnalyse = CandlesAnalyse.Required;

                //проверка состояния предыдущей сессии 
                SpRobot.RobotState = RobotServices.LoadStateAsync(RobotId);
                await SpRobot.ResetRobotState();


                candlesAnalyse = RobotStateProcessor.CheckStateAsync(SpRobot.RobotState, RobotId,
                    SpRobot.SignalBuyOrder, SpRobot.SignalSellOrder, SpRobot.StopLossOrder, SpRobot.TakeProfitOrder);

                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    SpRobot.RobotState = new();
                    await SpRobot.ResetRobotState();

                    ChartAnalyse();

                }

                //------ выставление СЛ ТП после сбоя
                SpRobot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(SpRobot.RobotState.Position));

                //-------------
                IsReady = true;

            }

            if (!IsReady)
            {
                return;
            }



            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(SpRobot.BaseSettings.TimeFrame) < carrentCendle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (carrentCendle.CloseTime - LastCandleTime.AddSeconds(SpRobot.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(state: SpRobot.RobotState, robotId: RobotId,
                    SpRobot.SignalBuyOrder, SpRobot.SignalSellOrder, SpRobot.StopLossOrder, SpRobot.TakeProfitOrder);
                //------ выставление СЛ ТП после сбоя
                SpRobot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(SpRobot.RobotState.Position));

                SpRobot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
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
            if (!NeedChartAnalyse && SpRobot.Position != 0)
            {
                NeedChartAnalyse = true;
            }
            if (NeedChartAnalyse && SpRobot.Position == 0)
            {
                NeedChartAnalyse = false;
                ChartAnalyse();
            }

            //------------------- Проверка на выход за пределы СЛ ТП
            //Task.Run(() => SpRobot.CheckSLTPCross(currentPrice));

            //---------------- скидываем пики
            if ((LowPeak.Peak != 0 && SpRobot.Position != 0) ||
            (SpRobot.SignalSellOrder.OrderId != 0 && !SpRobot.CheckTradingStatus(carrentCendle.OpenTime)))
            {
                if (SpRobot.SignalSellOrder.OrderId != 0)//снимаем ордер по сигналу если торговля запрещена 
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = SpRobot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = SpRobot.SignalSellOrder.OrderId
                    });
                    SpRobot.RobotState.SignalSellOrderId = 0;
                    SpRobot.SignalSellOrder = new();

                }
                LowPeak.Peak = 0;
            }


            if ((HighPeak.Peak != 0 && SpRobot.Position != 0) ||
                (SpRobot.SignalBuyOrder.OrderId != 0 && !SpRobot.CheckTradingStatus(carrentCendle.OpenTime)))
            {
                if (SpRobot.SignalBuyOrder.OrderId != 0)//снимаем ордер по сигналу если торговля запрещена 
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = SpRobot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = SpRobot.SignalBuyOrder.OrderId
                    });
                    SpRobot.RobotState.SignalBuyOrderId = 0;
                    SpRobot.SignalBuyOrder = new();

                }
                HighPeak.Peak = 0;
            }

            //--------------- ордер по сигналу low peak

            if (LowPeak.Peak != 0 && currentPrice > LowPeak.Peak)
            {
                var stopPrice = LowPeak.Peak;
                LowPeak.Peak = 0;//скидываем пики при открытии сделки
                if (SpRobot.SignalSellOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = SpRobot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = SpRobot.SignalSellOrder.OrderId
                    });
                }
                SpRobot.RobotState.SignalSellOrderId = 0;
                SpRobot.SignalSellOrder = new();


                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = RobotId,
                    Symbol = SpRobot.Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = SpRobot.BaseSettings.Volume,
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

                if (SpRobot.SignalBuyOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = SpRobot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = SpRobot.SignalBuyOrder.OrderId
                    });
                }
                SpRobot.RobotState.SignalBuyOrderId = 0;
                SpRobot.SignalBuyOrder = new();



                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotId = RobotId,
                    Symbol = SpRobot.Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = SpRobot.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = stopPrice,
                    robotOrderType = RobotOrderType.SignalBuy,
                    robotRequestType = RobotRequestType.PlaceOrder
                });





            }

        }


        public void ChartAnalyse()
        {
            var SpRobot = RobotVM.robots[RobotId];
            var carrentCendle = MarketData.CandleDictionary[SpRobot.Symbol][SpRobot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[SpRobot.Symbol][SpRobot.BaseSettings.TimeFrame];

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
            if (MarketData.Info.SelectedRobotId == RobotId)
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

        private void SetCurrentPrifit(decimal price)
        {
            var SpRobot = RobotVM.robots[RobotId];

            if (SpRobot.Position > 0)
            {
                SpRobot.Profit = price - SpRobot.OpenPositionPrice;
                return;
            }

            if (SpRobot.Position < 0)
            {
                SpRobot.Profit = SpRobot.OpenPositionPrice - price;
                return;
            }

            SpRobot.Profit = 0;
        }
    }
}
