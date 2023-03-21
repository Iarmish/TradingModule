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
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class VWAPHL
    {
        

        
        
        private bool IsReady { get; set; }

        public Candle LastCandle { get; set; } = new Candle();
        public DateTime LastCandleTime { get; set; } = DateTime.UtcNow;
        public int RobotId { get; set; }        

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


        private Robot VwapRobot { get; set; }
        //-----------------------------
        public VWAPHL(int robotId)
        {
            RobotId = robotId;
            
        }

        

        


        public async void NewTick(RobotCommands command)
        {
            VwapRobot = RobotVM.robots[RobotId];

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

            var currentPrice = MarketData.CandleDictionary[VwapRobot.Symbol][VwapRobot.BaseSettings.TimeFrame][^1].ClosePrice;

            var carrentCendle = MarketData.CandleDictionary[VwapRobot.Symbol][VwapRobot.BaseSettings.TimeFrame][^1];
            //var candles = MarketData.CandleDictionary[VwapRobot.Symbol][VwapRobot.BaseSettings.TimeFrame];
            var LastCompletedCendle = MarketData.CandleDictionary[VwapRobot.Symbol][VwapRobot.BaseSettings.TimeFrame][^2];

            //Анализ графика
            if (LastCandle.OpenPrice == 0)
            {
                LastCandle = LastCompletedCendle;

                await GetVWAPCandles();
                CalculateVWAP();
                Take_status();

                var candlesAnalyse = CandlesAnalyse.Required;

                //проверка состояния предыдущей сессии 
                VwapRobot.RobotState = RobotServices.LoadStateAsync(RobotId);
                await VwapRobot.ResetRobotState();


                candlesAnalyse = RobotStateProcessor.CheckStateAsync(VwapRobot.RobotState, RobotId,
                    VwapRobot.SignalBuyOrder, VwapRobot.SignalSellOrder, VwapRobot.StopLossOrder, VwapRobot.TakeProfitOrder);

                //--------- анализ графика ------------
                if (candlesAnalyse == CandlesAnalyse.Required)
                {
                    VwapRobot.RobotState = new();
                    await VwapRobot.ResetRobotState();                                       
                }

                //------ выставление СЛ ТП после сбоя
                 VwapRobot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(VwapRobot.RobotState.Position));

                //-------------
                IsReady = true;
               
            }


            if (!IsReady)
            {
                return;
            }
            //-------------------------------------------
            //проверка на разрыв связи 
            if (LastCandleTime.AddSeconds(VwapRobot.BaseSettings.TimeFrame) < carrentCendle.CloseTime &&
                LastCandle.OpenPrice != 0)
            {
                var lostTime = (carrentCendle.CloseTime - LastCandleTime.AddSeconds(VwapRobot.BaseSettings.TimeFrame)).TotalMinutes;
                var candlesAnalyse = RobotStateProcessor.CheckStateAsync(state: VwapRobot.RobotState, robotId: RobotId,
                    VwapRobot.StopLossOrder, VwapRobot.TakeProfitOrder, VwapRobot.StopLossOrder, VwapRobot.TakeProfitOrder);
                //------ выставление СЛ ТП после сбоя
                 VwapRobot.SetSLTPAfterFail(candlesAnalyse, Math.Abs(VwapRobot.RobotState.Position));

                VwapRobot.Log(LogType.RobotState, "отсутствие связи с сервером " + lostTime + " мин");
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
            //------------------- Проверка на выход за пределы СЛ ТП
            Task.Run(() => VwapRobot.CheckSLTPCross(currentPrice));
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
            if(VwapRobot.Position == 0 && VwapRobot.CheckTradingStatus(carrentCendle.OpenTime))
            {
                var vwap = Math.Round(VWAPcandles[^1].VWAP, SymbolIndexes.price[VwapRobot.Symbol]);
                //-------- sell ----------------
                //------ выставляем ордера 
                if (VWAPStatus == 1 && !IsSignalSellOrderPlaced)
                {
                    IsSignalSellOrderPlaced = true;
                    SignalSellPrice = vwap;

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = VwapRobot.Symbol,
                        Side = (int)OrderSide.Sell,
                        OrderType = (int)FuturesOrderType.Limit,
                        Quantity = VwapRobot.BaseSettings.Volume,
                        Price = vwap,
                        StopPrice = 0,
                        robotOrderType = RobotOrderType.SignalSell,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });

                    //var plasedOrder = await VwapRobot.PlaceSignalOrder( OrderSide.Sell, FuturesOrderType.Limit, vwap, null);
                    //if (plasedOrder.Success)
                    //{
                    //    VwapRobot.SignalSellOrder = RobotOrderDTO.DTO(plasedOrder, RobotId);
                    //    VwapRobot.RobotState.SignalSellOrderId = VwapRobot.SignalSellOrder.OrderId;
                    //    RobotServices.SaveState(RobotId, VwapRobot.RobotState);

                    //    //RobotServices.SaveOrder(RobotId, VwapRobot.SignalSellOrder, "Place Signal Sell Order");
                    //}
                    //else
                    //{                        
                    //    VwapRobot.Log(LogType.Error, " Signal Sell Error " + plasedOrder.Error.ToString());
                    //}

                }
                //------------ заменяем ордера ------------
                if (IsSignalSellOrderPlaced && SignalSellPrice != vwap)
                {
                    SignalSellPrice = vwap;

                    //VwapRobot.CancelOrderAsync(VwapRobot.SignalSellOrder, "Cancel Signal Sell Order");
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = VwapRobot.Symbol,                        
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = VwapRobot.SignalSellOrder.OrderId
                    });

                    VwapRobot.RobotState.SignalSellOrderId = 0;
                    VwapRobot.SignalSellOrder = new();

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = VwapRobot.Symbol,
                        Side = (int)OrderSide.Sell,
                        OrderType = (int)FuturesOrderType.Limit,
                        Quantity = VwapRobot.BaseSettings.Volume,
                        Price = vwap,
                        StopPrice = 0,
                        robotOrderType = RobotOrderType.SignalSell,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });

                    //var plasedOrder = await VwapRobot.PlaceSignalOrder(OrderSide.Sell, FuturesOrderType.Limit, vwap, null);
                    //if (plasedOrder.Success)
                    //{
                    //    VwapRobot.SignalSellOrder = RobotOrderDTO.DTO(plasedOrder, RobotId);
                    //    VwapRobot.RobotState.SignalSellOrderId = VwapRobot.SignalSellOrder.OrderId;
                    //    RobotServices.SaveState(RobotId, VwapRobot.RobotState);

                    //    //RobotServices.SaveOrder(RobotId, VwapRobot.SignalSellOrder, "Place Signal Sell Order");
                    //}
                    //else
                    //{
                    //    VwapRobot.Log(LogType.Error, " Signal Sell Error " + plasedOrder.Error.ToString());
                    //}


                }

                //-------------- buy ---------------------
                if (VWAPStatus == -1 && !IsSignalBuyOrderPlaced)
                {
                    IsSignalBuyOrderPlaced = true;
                    SignalBuyPrice = vwap;

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = VwapRobot.Symbol,
                        Side = (int)OrderSide.Buy,
                        OrderType = (int)FuturesOrderType.Limit,
                        Quantity = VwapRobot.BaseSettings.Volume,
                        Price = vwap,
                        StopPrice = 0,
                        robotOrderType = RobotOrderType.SignalBuy,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });

                    //var plasedOrder = await VwapRobot.PlaceSignalOrder(OrderSide.Buy, FuturesOrderType.Limit, vwap, null);
                    //if (plasedOrder.Success)
                    //{
                    //    VwapRobot.SignalBuyOrder = RobotOrderDTO.DTO(plasedOrder, RobotId);
                    //    VwapRobot.RobotState.SignalBuyOrderId = VwapRobot.SignalBuyOrder.OrderId;

                    //    //RobotServices.SaveOrder(RobotId, VwapRobot.SignalBuyOrder, "Place Signal Buy Order");
                    //}
                    //else
                    //{
                    //    VwapRobot.Log(LogType.Error, " Signal Buy Error " + plasedOrder.Error.ToString());
                    //}

                }
                //------------ заменяем ордера ------------
                if (IsSignalBuyOrderPlaced && SignalBuyPrice != vwap)
                {
                    SignalBuyPrice = vwap;
                    //VwapRobot.CancelOrderAsync(VwapRobot.SignalBuyOrder, "Cancel Signal Buy Order");
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = VwapRobot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = VwapRobot.SignalBuyOrder.OrderId
                    });

                    VwapRobot.RobotState.SignalBuyOrderId = 0;
                    VwapRobot.SignalBuyOrder = new();

                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = VwapRobot.Symbol,
                        Side = (int)OrderSide.Buy,
                        OrderType = (int)FuturesOrderType.Limit,
                        Quantity = VwapRobot.BaseSettings.Volume,
                        Price = vwap,
                        StopPrice = 0,
                        robotOrderType = RobotOrderType.SignalBuy,
                        robotRequestType = RobotRequestType.PlaceOrder
                    });

                    //var plasedOrder = await VwapRobot.PlaceSignalOrder(OrderSide.Buy, FuturesOrderType.Limit, vwap, null);
                    //if (plasedOrder.Success)
                    //{
                    //    VwapRobot.SignalBuyOrder = RobotOrderDTO.DTO(plasedOrder, RobotId);
                    //    VwapRobot.RobotState.SignalBuyOrderId = VwapRobot.SignalBuyOrder.OrderId;
                    //    RobotServices.SaveState(RobotId, VwapRobot.RobotState);

                    //    //RobotServices.SaveOrder(RobotId, VwapRobot.SignalBuyOrder, "Place Signal Buy Order");
                    //}
                    //else
                    //{
                    //    VwapRobot.Log(LogType.Error, " Signal Buy Error " + plasedOrder.Error.ToString());
                    //}


                }

            }
            else
            {
                if (IsSignalSellOrderPlaced || IsSignalBuyOrderPlaced)
                {
                    //VwapRobot.CancelOrderAsync(VwapRobot.SignalBuyOrder, "Cancel Signal Buy Order");
                    //VwapRobot.CancelOrderAsync(VwapRobot.SignalSellOrder, "Cancel Signal Sell Order");
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = VwapRobot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = VwapRobot.SignalSellOrder.OrderId
                    });
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = RobotId,
                        Symbol = VwapRobot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = VwapRobot.SignalBuyOrder.OrderId
                    });

                    IsSignalSellOrderPlaced = false;
                    IsSignalBuyOrderPlaced = false;
                    
                }
            }
            // ====================== Day high/low ======================
            if (currentPrice > DayHighPrice)
            {
                DayHighPrice = currentPrice;
                if (VWAPStatus == 0 )
                    VWAPStatus = -1;
            }
            if (currentPrice < DayLowPrice)
            {
                DayLowPrice = currentPrice;
                if (VWAPStatus == 0)
                    VWAPStatus = 1;
            }


        }


        public void NewCandle(Candle candle)
        {

            var candleVWAP = CalculateCandleVWAP(CandleVWAPDTO.DTO(candle));

            VWAPcandles.Add(CandleVWAPDTO.DTO(candle, candleVWAP));

            MarketData.VWAPs.Add(new VWAP { Date = candle.OpenTime, Volume = candleVWAP });
        }

        //--------------------------------------
        


        public void Take_status()
        {
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
                if (VWAPStatus == -1 && candle.LowPrice <= candle.VWAP)
                {
                    VWAPStatus = 0;
                }

                if (VWAPStatus == 1 && candle.HighPrice >= candle.VWAP)
                {
                    VWAPStatus = 0;//////////////////////////////////////////
                }

                //------------------ hi low day 
                if (candle.HighPrice > DayHighPrice)
                {
                    DayHighPrice = candle.HighPrice;
                    if (candleCnt > StartCandle)
                        VWAPStatus = -1;

                }
                if (candle.LowPrice < DayLowPrice)
                {
                    DayLowPrice = candle.LowPrice;
                    if (candleCnt > StartCandle)
                        VWAPStatus = 1;
                }
                candleCnt++;

            }
            //VWAPStatus = 1;
        }

        

        

        


        private async Task GetVWAPCandles()
        {
            var startDate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 0, 0, 0, DateTimeKind.Utc);
            var result = await BinanceApi.client.UsdFuturesApi.ExchangeData.GetKlinesAsync(VwapRobot.Symbol,
                (KlineInterval)VwapRobot.BaseSettings.TimeFrame,
                limit: 1440);
            //startTime: startDate, endTime: DateTime.UtcNow);
            VWAPcandles = result.Data.Select(x => new CandleVWAP
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

                MarketData.VWAPs.Add(new VWAP { Date = VWAPcandles[i].OpenTime, Volume = VWAPcandles[i].VWAP });
            }
        }

        


        

        

        
    }
}
