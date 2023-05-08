using Binance.Net.Enums;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.ViewModel;
using System;
using System.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class ShortPeak
    {
        private HighPeak HighPeak { get; set; } = new HighPeak();
        private LowPeak LowPeak { get; set; } = new LowPeak();

        //private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }
        public bool NeedChartAnalyse { get; set; }

        private Robot RobotInstance { get; set; }

        //----------------------------------------------
        public ShortPeak(int robotId, int robotIndex)
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
            //var candles = MarketData.CandleDictionary[Robot.Symbol][Robot.BaseSettings.TimeFrame];
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

                    ChartAnalyse();

                }

                //------ выставление СЛ ТП после сбоя

                RobotInstance.SetSLTPAfterFail(candlesAnalyse, Math.Abs(RobotInstance.RobotState.Position), RobotInstance.SignalBuyOrder.OrderId, RobotInstance.SignalSellOrder.OrderId);


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
            //-----------
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
            if (!NeedChartAnalyse && RobotInstance.Position != 0)
            {
                NeedChartAnalyse = true;
            }
            if (NeedChartAnalyse && RobotInstance.Position == 0)
            {
                NeedChartAnalyse = false;
                ChartAnalyse();
            }


            //---------------- скидываем пики
            if ((LowPeak.Peak != 0 && RobotInstance.Position != 0) ||
            (RobotInstance.SignalSellOrder.OrderId != 0 && !RobotInstance.CheckTradingStatus(carrentCendle.OpenTime)))
            {
                if (RobotInstance.SignalSellOrder.OrderId != 0)//снимаем ордер по сигналу если торговля запрещена 
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = RobotInstance.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = RobotInstance.SignalSellOrder.OrderId,
                        OrderType = RobotInstance.SignalSellOrder.Type
                    });
                    RobotInstance.RobotState.SignalSellOrderId = 0;
                    RobotInstance.SignalSellOrder = new();

                }
                LowPeak.Peak = 0;
            }


            if ((HighPeak.Peak != 0 && RobotInstance.Position != 0) ||
                (RobotInstance.SignalBuyOrder.OrderId != 0 && !RobotInstance.CheckTradingStatus(carrentCendle.OpenTime)))
            {
                if (RobotInstance.SignalBuyOrder.OrderId != 0)//снимаем ордер по сигналу если торговля запрещена 
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = RobotInstance.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = RobotInstance.SignalBuyOrder.OrderId,
                        OrderType = RobotInstance.SignalSellOrder.Type
                    });
                    RobotInstance.RobotState.SignalBuyOrderId = 0;
                    RobotInstance.SignalBuyOrder = new();

                }
                HighPeak.Peak = 0;
            }

            //--------------- ордер по сигналу low peak
            var signalSellPrice = RobotServices.GetSignalPrice(RobotInstance.BaseSettings.OffsetPercent, LowPeak.Peak,
                        RobotOrderType.SignalSell, FuturesOrderType.StopMarket);
            
            if (LowPeak.Peak != 0 && currentPrice  > signalSellPrice)
            {
                

                //LowPeak.Peak + RobotInstance.BaseSettings.OffsetPercent;
                LowPeak.Peak = 0;//скидываем пики при открытии сделки
                if (RobotInstance.SignalSellOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = RobotInstance.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = RobotInstance.SignalSellOrder.OrderId,
                        OrderType = RobotInstance.SignalSellOrder.Type
                    });
                }
                RobotInstance.RobotState.SignalSellOrderId = 0;
                RobotInstance.SignalSellOrder = new();



                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    StartDealOrderId = 0,
                    Symbol = RobotInstance.Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = RobotInstance.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = signalSellPrice,
                    robotOrderType = RobotOrderType.SignalSell,
                    robotRequestType = RobotRequestType.PlaceOrder
                });



            }

            //--------------- ордер по сигналу High peak
            var signalBuyPrice = RobotServices.GetSignalPrice(RobotInstance.BaseSettings.OffsetPercent, HighPeak.Peak,
                        RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);

            if (HighPeak.Peak != 0 && currentPrice < signalBuyPrice)
            {                
                HighPeak.Peak = 0;//скидываем пики при открытии сделки

                if (RobotInstance.SignalBuyOrder.OrderId != 0)
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = RobotInstance.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = RobotInstance.SignalBuyOrder.OrderId,
                        OrderType = RobotInstance.SignalSellOrder.Type
                    });
                }
                RobotInstance.RobotState.SignalBuyOrderId = 0;
                RobotInstance.SignalBuyOrder = new();


                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = RobotIndex,
                    StartDealOrderId = 0,
                    Symbol = RobotInstance.Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.StopMarket,
                    Quantity = RobotInstance.BaseSettings.Volume,
                    Price = 0,
                    StopPrice = signalBuyPrice,
                    robotOrderType = RobotOrderType.SignalBuy,
                    robotRequestType = RobotRequestType.PlaceOrder
                });





            }

        }


        public void ChartAnalyse()
        {
            var SpRobot = RobotVM.robots[RobotIndex];
            var carrentCendle = MarketData.CandleDictionary[SpRobot.Symbol][SpRobot.BaseSettings.TimeFrame][^1];
            var candles = MarketData.CandleDictionary[SpRobot.Symbol][SpRobot.BaseSettings.TimeFrame];

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
                if (candle.HighPrice - RobotInstance.BaseSettings.OffsetPercent < HighPeak.SecondCandle)
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
                if (candle.LowPrice + RobotInstance.BaseSettings.OffsetPercent > LowPeak.SecondCandle)
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
            if (HighPeak.Peak != 0 && candle.HighPrice - RobotInstance.BaseSettings.OffsetPercent >= HighPeak.Peak)
                HighPeak.Peak = 0;
            if (LowPeak.Peak != 0 && candle.LowPrice + RobotInstance.BaseSettings.OffsetPercent <= LowPeak.Peak)
                LowPeak.Peak = 0;

            SetRobotInfo();//to UI
        }

        private void SetCurrentPrifit(decimal price)
        {
            RobotInstance = RobotVM.robots[RobotIndex];

            if (RobotInstance.Position > 0)
            {
                RobotInstance.Profit = price - RobotInstance.OpenPositionPrice;
                return;
            }

            if (RobotInstance.Position < 0)
            {
                RobotInstance.Profit = RobotInstance.OpenPositionPrice - price;
                return;
            }

            RobotInstance.Profit = 0;
        }



    }
}
