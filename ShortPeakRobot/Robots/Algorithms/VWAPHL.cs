using Binance.Infrastructure.Constants;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class VWAPHL
    {


        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }
        public int RobotIndex { get; set; }

        public List<CandleVWAP> VWAPcandles { get; set; } = new();

        private VWAP vwap { get; set; } = new VWAP();

        private int VWAPStatus { get; set; }

        private int StartCandle { get; set; } = 3;
        private decimal DayHighPrice { get; set; }
        private decimal DayLowPrice { get; set; }
        private decimal SignalSellPrice { get; set; }
        private decimal SignalBuyPrice { get; set; }

        private bool IsSignalSellOrderPlaced { get; set; }
        private bool IsSignalBuyOrderPlaced { get; set; }

        
        //-----------------------------
        public VWAPHL(int robotId, int robotIndex)
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
                case RobotCommands.ResetCandleAnalyse:
                    LastCandle = new();
                    break;
                default:
                    break;
            }

            var currentPrice = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1].ClosePrice;

            var carrentCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^1];
            //var candles = MarketData.CandleDictionary[VwapRobot.Symbol][VwapRobot.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[robot.Symbol][robot.BaseSettings.TimeFrame][^2];

            SetCurrentPrifit(currentPrice);
            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;

                vwap = new VWAP();
                MarketData.VWAPs.Clear();

                await GetVWAPCandles();
                CalculateVWAP();
                Take_status();
                SetRobotInfo();

                var candlesAnalyse = CandlesAnalyse.Required;

                
                await robot.SetRobotData();//из robotState


                candlesAnalyse = RobotStateProcessor.CheckStateAsync(robot.RobotState, RobotIndex);

                //--------- анализ графика ------------
                //if (candlesAnalyse == CandlesAnalyse.Required)
                //{
                //    robot.RobotState = new();
                //    await robot.SetRobotData();
                //}

                //-------------              

            }


            if (!robot.IsReady)
            {
                return;
            }
            //-------------------------------------------
            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(robot.BaseSettings.TimeFrame) < carrentCendle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (carrentCendle.CloseTime - LastCandleTime.AddSeconds(robot.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(state: robot.RobotState, RobotIndex);

                robot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
            }
            LastCandleTime = carrentCendle.CloseTime;
            //-----------------------
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;
            }

            if (LastCandle.CloseTime < LastCompletedCendle.CloseTime)//новая свечка
            {

                if (LastCandle.CloseTime.Day != LastCompletedCendle.CloseTime.Day)//новый день - сброс vwap
                {
                    Thread.Sleep(500);

                    vwap = new VWAP();
                    MarketData.VWAPs.Clear();

                    await GetVWAPCandles();
                    CalculateVWAP();
                    Take_status();
                    SetRobotInfo();
                }
                else
                {
                    NewCandle(LastCompletedCendle);
                }


                LastCandle = LastCompletedCendle;

            }
            //------------------- Проверка на выход за пределы СЛ ТП
            //Task.Run(() => VwapRobot.CheckSLTPCross(currentPrice));
            //==============  пересечение vwap =======================================================
            if (VWAPStatus == -1 && currentPrice < VWAPcandles[^1].VWAP)
            {
                VWAPStatus = 0;// скидываем статус
            }

            if (VWAPStatus == 1 && currentPrice > VWAPcandles[^1].VWAP)
            {
                VWAPStatus = 0; // скидываем статус
            }
            //------------------- выставляем ордера ---------------------
            if (robot.Position == 0 && robot.CheckTradingStatus(carrentCendle.OpenTime))
            {
                var vwapVolume = Math.Round(VWAPcandles[^1].VWAP, SymbolIndexes.price[robot.Symbol]);
                //-------- sell ----------------
                //------ выставляем ордера 
                if (VWAPStatus == -1 && !IsSignalSellOrderPlaced)
                {
                    IsSignalSellOrderPlaced = true;

                    SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, vwapVolume, 
                        RobotOrderType.SignalSell, FuturesOrderType.StopMarket);

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        Side = (int)OrderSide.Sell,
                        OrderType = (int)FuturesOrderType.StopMarket,
                        Quantity = robot.BaseSettings.Volume,
                        Price = 0,
                        StopPrice = SignalSellPrice,
                        robotOrderType = RobotOrderType.SignalSell,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });


                }
                //------------ заменяем ордера ------------
                if (IsSignalSellOrderPlaced && SignalSellPrice != RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, vwapVolume,
                        RobotOrderType.SignalSell, FuturesOrderType.StopMarket))
                {
                    SignalSellPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, vwapVolume,
                        RobotOrderType.SignalSell, FuturesOrderType.StopMarket);

                    //VwapRobot.CancelOrderAsync(VwapRobot.SignalSellOrder, "Cancel Signal Sell Order");
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalSellOrder.OrderId,
                        OrderType = (int)FuturesOrderType.StopMarket
                    });

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
                        StopPrice = SignalSellPrice,
                        robotOrderType = RobotOrderType.SignalSell,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });

                    

                }

                //-------------- buy ---------------------
                if (VWAPStatus == 1 && !IsSignalBuyOrderPlaced)
                {
                    IsSignalBuyOrderPlaced = true;
                    SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, vwapVolume,
                        RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        Side = (int)OrderSide.Buy,
                        OrderType = (int)FuturesOrderType.StopMarket,
                        Quantity = robot.BaseSettings.Volume,
                        Price = 0,
                        StopPrice = SignalBuyPrice,
                        robotOrderType = RobotOrderType.SignalBuy,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });

                }
                //------------ заменяем ордера ------------
                if (IsSignalBuyOrderPlaced && SignalBuyPrice != RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, vwapVolume,
                        RobotOrderType.SignalBuy, FuturesOrderType.StopMarket))
                {
                    SignalBuyPrice = RobotServices.GetSignalPrice(robot.BaseSettings.OffsetPercent, vwapVolume,
                        RobotOrderType.SignalBuy, FuturesOrderType.StopMarket);
                    //VwapRobot.CancelOrderAsync(VwapRobot.SignalBuyOrder, "Cancel Signal Buy Order");
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalBuyOrder.OrderId,
                        OrderType = (int)FuturesOrderType.StopMarket
                    });

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
                        StopPrice = SignalBuyPrice,
                        robotOrderType = RobotOrderType.SignalBuy,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });



                }

            }
            else
            {
                if (IsSignalSellOrderPlaced || IsSignalBuyOrderPlaced)
                {                    
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalSellOrder.OrderId,
                        OrderType = (int)FuturesOrderType.StopMarket
                    });
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = RobotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalBuyOrder.OrderId,
                        OrderType = (int)FuturesOrderType.StopMarket
                    });

                    IsSignalSellOrderPlaced = false;
                    IsSignalBuyOrderPlaced = false;

                }
            }
            // ====================== Day high/low ======================
            if (currentPrice  > DayHighPrice)
            {
                 var vwapVolume = VWAPcandles[^1].VWAP;
                DayHighPrice = currentPrice;
                if (VWAPStatus == 0 && currentPrice - vwapVolume > robot.BaseSettings.OffsetPercent)
                    VWAPStatus = -1;
            }
            if (currentPrice < DayLowPrice)
            {
                var vwapVolume = VWAPcandles[^1].VWAP;
                DayLowPrice = currentPrice;
                if (VWAPStatus == 0 && vwapVolume - currentPrice > robot.BaseSettings.OffsetPercent)
                    VWAPStatus = 1;
            }


        }


        public void NewCandle(Candle candle)
        {

            var candleVWAP = CalculateCandleVWAP(CandleVWAPDTO.DTO(candle));

            VWAPcandles.Add(CandleVWAPDTO.DTO(candle, candleVWAP));

            //MarketData.VWAPs.Add(new VWAP { Date = candle.OpenTime, Volume = candleVWAP });

            SetRobotInfo();
        }

        //--------------------------------------

        public void SetRobotInfo()
        {
            var robot = RobotVM.robots[RobotIndex];
            if (MarketData.Info.SelectedRobotIndex == RobotIndex)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotInfoVM.ClearParams();
                    RobotInfoVM.AddParam("VWAPs.Count", MarketData.VWAPs.Count.ToString());

                    RobotInfoVM.AddParam("VWAP.Volume", Math.Round(VWAPcandles[^1].VWAP, SymbolIndexes.price[robot.Symbol]).ToString());
                    RobotInfoVM.AddParam("VWAP.Position", VWAPStatus.ToString());
                    //RobotInfoVM.AddParam("VWAP.Date", MarketData.VWAPs[^1].Date.ToString());
                    RobotInfoVM.AddParam("DayHighPrice", DayHighPrice.ToString());
                    RobotInfoVM.AddParam("DayLowPrice", DayLowPrice.ToString());

                });
            }
        }

        public void Take_status()
        {
            var robot = RobotVM.robots[RobotIndex];

            int candleCnt = 1;
            foreach (CandleVWAP candle in VWAPcandles)
            {
                if (candle.VWAP == 0)
                {
                    continue;
                }
                //-----
                if (DayHighPrice == 0)
                {
                    DayHighPrice = candle.HighPrice;
                    DayLowPrice = candle.LowPrice;
                }

                //-----------------------------
                if (VWAPStatus == -1 && candle.LowPrice <= candle.VWAP + robot.BaseSettings.OffsetPercent)
                {
                    VWAPStatus = 0;
                }

                if (VWAPStatus == 1 && candle.HighPrice >= candle.VWAP - robot.BaseSettings.OffsetPercent)
                {
                    VWAPStatus = 0;//////////////////////////////////////////
                }

                //------------------ hi low day 
                if (candle.HighPrice > DayHighPrice && candle.HighPrice - candle.VWAP > robot.BaseSettings.OffsetPercent)
                {
                    DayHighPrice = candle.HighPrice;
                    if (candleCnt > StartCandle)
                        VWAPStatus = -1;

                }
                if (candle.LowPrice < DayLowPrice && candle.VWAP - candle.LowPrice > robot.BaseSettings.OffsetPercent)
                {
                    DayLowPrice = candle.LowPrice;
                    if (candleCnt > StartCandle)
                        VWAPStatus = 1;
                }
                candleCnt++;

            }
            //VWAPStatus = -1;
        }




        private async Task GetVWAPCandles()
        {
            var robot = RobotVM.robots[RobotIndex];
            var result = await BinanceApi.client.UsdFuturesApi.ExchangeData.GetKlinesAsync(robot.Symbol,
                (KlineInterval)robot.BaseSettings.TimeFrame,
                limit: 1440);
            //startTime: startDate, endTime: DateTime.UtcNow);
            VWAPcandles = result.Data.Where(x=> x.OpenTime.Day == DateTime.UtcNow.Day)
                .Select(x => new CandleVWAP
            {
                OpenPrice = x.OpenPrice,
                HighPrice = x.HighPrice,
                LowPrice = x.LowPrice,
                ClosePrice = x.ClosePrice,
                OpenTime = x.OpenTime,
                CloseTime = x.CloseTime,
                Symbol = RobotVM.robots[0].Symbol,
                Volume = x.Volume
            }).ToList();

            VWAPcandles.RemoveAt(VWAPcandles.Count - 1);//удаляем незакрытую свечу
        }

        private decimal CalculateCandleVWAP(CandleVWAP candle)
        {
            decimal candleVWAP = 0;

            vwap.VolumeSum += candle.Volume;
            vwap.VwapSum += (candle.HighPrice + candle.LowPrice) / 2 * candle.Volume;
            if (vwap.VolumeSum != 0)
            {
                candleVWAP = vwap.VwapSum / vwap.VolumeSum;
            }

            else
            {
                candleVWAP = vwap.VwapSum;
            }

            return candleVWAP;
        }
        private void CalculateVWAP()
        {


            for (int i = 0; i < VWAPcandles.Count; i++)
            {

                if (DateTime.UtcNow.Day != VWAPcandles[i].OpenTime.Day)
                {
                    continue;
                }
                VWAPcandles[i].VWAP = CalculateCandleVWAP(VWAPcandles[i]);

                //MarketData.VWAPs.Add(new VWAP { Date = VWAPcandles[i].OpenTime, Volume = VWAPcandles[i].VWAP });
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
