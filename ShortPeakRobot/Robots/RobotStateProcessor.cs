using Binance.Net.Enums;
using CryptoExchange.Net.CommonObjects;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.Robots.Models;
using ShortPeakRobot.ViewModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots
{
    public static class RobotStateProcessor
    {


        public static  CandlesAnalyse CheckStateAsync(RobotState state, int robotIndex,
            RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, RobotOrder stopLossOrder, RobotOrder takeProfitOrder)
        {

            RobotVM.robots[robotIndex].IsReady = false;
            //----------------------------------
            var stateCase = GetStateCase(signalHighPeakOrder, signalLowPeakOrder, stopLossOrder, takeProfitOrder);
            RobotVM.robots[robotIndex].Log(LogType.Info, "stateCase " + stateCase.ToString());

            switch (stateCase)
            {
                case StateCase.Normal:
                    RobotVM.robots[robotIndex].IsReady = true; return CandlesAnalyse.Required;
                case StateCase.FilledOneSignalOrder:
                    var analyse = CheckSLTPStutus(state, signalHighPeakOrder, signalLowPeakOrder, robotIndex);
                    if (analyse == CandlesAnalyse.SellSLTP || analyse == CandlesAnalyse.BuySLTP)
                    {
                        RobotVM.robots[robotIndex].IsReady = true; return analyse;
                    }
                    RobotStateProcessor.FilledOneSignalOrderReaction(state, signalHighPeakOrder, signalLowPeakOrder, robotIndex);//IsReady true
                    return CandlesAnalyse.Required;
                case StateCase.FilledTwoSignalOrder:
                    RobotStateProcessor.FilledTwoSignalOrderReaction(robotIndex);//IsReady true
                    return CandlesAnalyse.Required;
                case StateCase.FilledOneSLPTOrder:
                    RobotStateProcessor.FilledOneSLPTOrderReaction(stopLossOrder, takeProfitOrder, robotIndex);//IsReady true
                    return CandlesAnalyse.Required;
                case StateCase.FilledTwoSLPTOrder:
                    RobotStateProcessor.FilledTwoSLPTOrderReaction(state, robotIndex);//IsReady true
                    return CandlesAnalyse.Required;
                case StateCase.PartiallyFilled:
                    RobotStateProcessor.PartiallyFilledReaction(state, robotIndex);
                    return CandlesAnalyse.NotRequired;
                case StateCase.PlacedSignalOrders:
                    RobotVM.robots[robotIndex].IsReady = true;
                    
                    return CandlesAnalyse.Required;
                case StateCase.PlacedSLTPOrders:
                    RobotVM.robots[robotIndex].IsReady = true;
                    
                    return CandlesAnalyse.NotRequired;


                default:
                    
                    return CandlesAnalyse.Required;
            }


        }

        private static CandlesAnalyse CheckSLTPStutus(RobotState state, RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, int robotIndex)
        {
            CandlesAnalyse candlesAnalyse = CandlesAnalyse.Required;

            if (signalHighPeakOrder.Status != -1 && signalHighPeakOrder.Status == (int)OrderStatus.Filled)
            {
                BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(RobotVM.robots[robotIndex].Symbol, signalLowPeakOrder.OrderId);

                candlesAnalyse = CandlesAnalyse.BuySLTP;

                var newCandles = MarketData.CandleDictionary[RobotVM.robots[robotIndex].Symbol]
                    [RobotVM.robots[robotIndex].BaseSettings.TimeFrame]
                    .Where(x => x.CloseTime > signalHighPeakOrder.PlacedTime).ToList();

                newCandles.ForEach(x =>
                {
                    if (x.HighPrice >= signalHighPeakOrder.StopPrice + RobotVM.robots[robotIndex].BaseSettings.TakeProfitPercent)
                    {
                        candlesAnalyse = CandlesAnalyse.Required;
                    }

                    if (x.LowPrice <= signalHighPeakOrder.StopPrice - RobotVM.robots[robotIndex].BaseSettings.StopLossPercent)
                    {
                        candlesAnalyse = CandlesAnalyse.Required;
                    }
                });
            }

            if (signalLowPeakOrder.Status != -1 && signalLowPeakOrder.Status == (int)OrderStatus.Filled)
            {
                BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(RobotVM.robots[robotIndex].Symbol, signalHighPeakOrder.OrderId);

                candlesAnalyse = CandlesAnalyse.SellSLTP;

                var newCandles = MarketData.CandleDictionary[RobotVM.robots[robotIndex].Symbol]
                    [RobotVM.robots[robotIndex].BaseSettings.TimeFrame]
                    .Where(x => x.CloseTime > signalLowPeakOrder.PlacedTime).ToList();

                newCandles.ForEach(x =>
                {
                    if (x.LowPrice <= signalLowPeakOrder.StopPrice - RobotVM.robots[robotIndex].BaseSettings.TakeProfitPercent)
                    {
                        candlesAnalyse = CandlesAnalyse.Required;
                    }

                    if (x.HighPrice >= signalLowPeakOrder.StopPrice + RobotVM.robots[robotIndex].BaseSettings.StopLossPercent)
                    {
                        candlesAnalyse = CandlesAnalyse.Required;
                    }
                });
            }

            //--------------------------
            
            

                    

            return candlesAnalyse;
        }

        public static StateCase GetStateCase(RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, RobotOrder stopLossOrder, RobotOrder takeProfitOrder)
        {

            //------------------- signal cases ----------
            if (signalHighPeakOrder.Status != -1 && signalLowPeakOrder.Status == -1)//один ордер по сигналу
            {
                if (signalHighPeakOrder.Status == (int)OrderStatus.Filled)
                {
                    return StateCase.FilledOneSignalOrder;
                }

                if (signalHighPeakOrder.Status == (int)OrderStatus.New)
                {
                    return StateCase.PlacedSignalOrders;
                }

                if (signalHighPeakOrder.Status == (int)OrderStatus.PartiallyFilled)
                {
                    return StateCase.PartiallyFilled;
                }

            }
            //----
            if (signalHighPeakOrder.Status == -1 && signalLowPeakOrder.Status != -1)//один ордер по сигналу
            {
                if (signalLowPeakOrder.Status == (int)OrderStatus.Filled)
                {
                    return StateCase.FilledOneSignalOrder;
                }

                if (signalLowPeakOrder.Status == (int)OrderStatus.PartiallyFilled)
                {
                    return StateCase.PartiallyFilled;
                }

                if (signalLowPeakOrder.Status == (int)OrderStatus.New)
                {
                    return StateCase.PlacedSignalOrders;
                }
            }
            if (signalHighPeakOrder.Status != -1 && signalLowPeakOrder.Status != -1)// два ордера по сигналу
            {
                if (signalLowPeakOrder.Status == (int)OrderStatus.PartiallyFilled || signalHighPeakOrder.Status == (int)OrderStatus.PartiallyFilled)
                {
                    return StateCase.PartiallyFilled;
                }

                if ((signalHighPeakOrder.Status == (int)OrderStatus.Filled && signalLowPeakOrder.Status != (int)OrderStatus.Filled) ||
                    (signalHighPeakOrder.Status != (int)OrderStatus.Filled && signalLowPeakOrder.Status == (int)OrderStatus.Filled))
                {
                    return StateCase.FilledOneSignalOrder;
                }

                if (signalHighPeakOrder.Status == (int)OrderStatus.Filled && signalLowPeakOrder.Status == (int)OrderStatus.Filled)
                {
                    return StateCase.FilledTwoSignalOrder;
                }

                if (signalHighPeakOrder.Status == (int)OrderStatus.New && signalLowPeakOrder.Status == (int)OrderStatus.New)
                {
                    return StateCase.PlacedSignalOrders;
                }
            }

            //------------------- SLTP cases ----------

            //if (stopLossOrder.Status != -1 && takeProfitOrder.Status == -1)
            //{
            //    if (stopLossOrder.Status == (int)OrderStatus.Filled)
            //    {
            //        return StateCase.FilledOneSLPTOrder;
            //    }

            //    if (stopLossOrder.Status == (int)OrderStatus.PartiallyFilled)
            //    {
            //        return StateCase.PartiallyFilled;
            //    }
            //}
            //if (stopLossOrder == null && takeProfitOrder != null)
            //{
            //    if (takeProfitOrder.Status == (int)OrderStatus.Filled)
            //    {
            //        return StateCase.FilledOneSLPTOrder;
            //    }

            //    if (takeProfitOrder.Status == (int)OrderStatus.PartiallyFilled)
            //    {
            //        return StateCase.PartiallyFilled;
            //    }
            //}
            //------------------
            if (stopLossOrder.Status != -1 && takeProfitOrder.Status != -1)
            {
                if (takeProfitOrder.Status == (int)OrderStatus.PartiallyFilled)
                {
                    return StateCase.PartiallyFilled;
                }

                if ((stopLossOrder.Status == (int)OrderStatus.Filled && takeProfitOrder.Status != (int)OrderStatus.Filled) ||
                    (stopLossOrder.Status != (int)OrderStatus.Filled && takeProfitOrder.Status == (int)OrderStatus.Filled))
                {
                    return StateCase.FilledOneSLPTOrder;
                }

                if (stopLossOrder.Status == (int)OrderStatus.Filled && takeProfitOrder.Status == (int)OrderStatus.Filled)
                {
                    return StateCase.FilledTwoSLPTOrder;
                }

                if (stopLossOrder.Status == (int)OrderStatus.New && takeProfitOrder.Status == (int)OrderStatus.New)
                {
                    return StateCase.PlacedSLTPOrders;
                }
            }

            return StateCase.Normal;
        }




        public async static Task PlacedSignalOrdersReaction(RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, int robotIndex)
        {
            if (signalLowPeakOrder.Status != -1 && signalLowPeakOrder.Status == (int)OrderStatus.New)
            {
                var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                                   symbol: RobotVM.robots[robotIndex].Symbol,
                                   orderId: signalLowPeakOrder.OrderId);

                if (cancelResult.Success)
                {
                    RobotVM.robots[robotIndex].Log(LogType.RobotState, "PlacedSignalOrdersReaction cansel other signal order after reconnect");
                    
                    RobotVM.robots[robotIndex].SignalSellOrder = new();
                    RobotVM.robots[robotIndex].RobotState.SignalSellOrderId = 0;
                }
                else
                {
                    RobotVM.robots[robotIndex].Log(LogType.Error, "PlacedSignalOrdersReaction cancel order Error " + cancelResult.Error.ToString());
                }
            }

            if (signalHighPeakOrder.Status != -1 && signalHighPeakOrder.Status == (int)OrderStatus.New)
            {
                var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                                   symbol: RobotVM.robots[robotIndex].Symbol,
                                   orderId: signalHighPeakOrder.OrderId);

                if (cancelResult.Success)
                {
                    RobotVM.robots[robotIndex].Log(LogType.RobotState, "PlacedSignalOrdersReaction cansel other signal order after reconnect");
                    
                }
                else
                {
                    RobotVM.robots[robotIndex].Log(LogType.Error, "PlacedSignalOrdersReaction cancel order Error " + cancelResult.Error.ToString());
                }
            }

        }

        public async static void FilledOneSignalOrderReaction(RobotState state, RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, int robotIndex)
        {
            var error = false;
            //закрываем позицию
            if (signalHighPeakOrder.Status != -1 && signalHighPeakOrder.Status == (int)OrderStatus.Filled)
            {
                var placeOrderResult = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: RobotVM.robots[robotIndex].Symbol,
                side: OrderSide.Sell,
                type: FuturesOrderType.Market,
                quantity: signalHighPeakOrder.Quantity);

                if (placeOrderResult.Success)
                {
                    RobotVM.robots[robotIndex].Log(LogType.RobotState, "FilledOneSignalOrderReaction close position after reconnect");
                    RobotVM.robots[robotIndex].SignalBuyOrder = new();
                    RobotVM.robots[robotIndex].RobotState.SignalBuyOrderId = 0;
                    RobotVM.robots[robotIndex].RobotState.Position = 0;
                }
                else
                {
                    RobotVM.robots[robotIndex].Log(LogType.Error, "FilledOneSignalOrderReaction place order Error " + placeOrderResult.Error.ToString());
                    error = true;
                }
                //
                if (signalLowPeakOrder.Status != -1)
                {
                    var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                                       symbol: RobotVM.robots[robotIndex].Symbol,
                                       orderId: signalLowPeakOrder.OrderId);

                    if (cancelResult.Success)
                    {
                        RobotVM.robots[robotIndex].Log(LogType.RobotState, "FilledOneSignalOrderReaction cansel other signal order after reconnect");
                        RobotVM.robots[robotIndex].SignalSellOrder = new();
                        RobotVM.robots[robotIndex].RobotState.SignalSellOrderId = 0;
                        
                    }
                    else
                    {
                        RobotVM.robots[robotIndex].Log(LogType.Error, "FilledOneSignalOrderReaction cancel order Error " + cancelResult.Error.ToString());
                        error = true;
                    }
                }

            }
            //-------------------------------
            if (signalLowPeakOrder.Status != -1 && signalLowPeakOrder.Status == (int)OrderStatus.Filled)
            {
                var placeOrderResult = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: RobotVM.robots[robotIndex].Symbol,
                side: OrderSide.Buy,
                type: FuturesOrderType.Market,
                quantity: signalLowPeakOrder.Quantity);

                if (placeOrderResult.Success)
                {
                    RobotVM.robots[robotIndex].Log(LogType.RobotState, "FilledOneSignalOrderReaction after reconnect");
                    RobotVM.robots[robotIndex].SignalSellOrder = new();
                    RobotVM.robots[robotIndex].RobotState.SignalSellOrderId = 0;
                    RobotVM.robots[robotIndex].RobotState.Position = 0;
                }
                else
                {
                    RobotVM.robots[robotIndex].Log(LogType.Error, "FilledOneSignalOrderReaction place order Error " + placeOrderResult.Error.ToString());
                    error = true;
                }


                if (signalHighPeakOrder.Status != -1)
                {
                    var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                                       symbol: RobotVM.robots[robotIndex].Symbol,
                                       orderId: signalHighPeakOrder.OrderId);

                    if (cancelResult.Success)
                    {
                        RobotVM.robots[robotIndex].Log(LogType.RobotState, "FilledOneSignalOrderReaction cansel other signal order after reconnect");
                        RobotVM.robots[robotIndex].SignalBuyOrder = new();
                        RobotVM.robots[robotIndex].RobotState.SignalBuyOrderId = 0;
                        
                    }
                    else
                    {
                        RobotVM.robots[robotIndex].Log(LogType.Error, "FilledOneSignalOrderReaction cancel order Error " + cancelResult.Error.ToString());
                        error = true;
                    }
                }

            }

            if (!error)
            {
                RobotVM.robots[robotIndex].IsReady = true;
            }

        }

        public static void FilledTwoSignalOrderReaction(int robotIndex)
        {
            RobotVM.robots[robotIndex].Log(LogType.RobotState, "FilledTwoSignalOrderReaction  ");
            RobotVM.robots[robotIndex].IsReady = true;
        }

        public async static void FilledOneSLPTOrderReaction(RobotOrder stopLossOrder, RobotOrder takeProfitOrder, int robotIndex)
        {
            var error = false;
            //cancel second order
            if (stopLossOrder.Status == (int)OrderStatus.Filled)
            {

                var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                   symbol: RobotVM.robots[robotIndex].Symbol,
                   orderId: takeProfitOrder.OrderId);

                if (cancelResult.Success)
                {
                    RobotVM.robots[robotIndex].Log(LogType.RobotState, "FilledOneSLPTOrderReaction cansel SLTP order after reconnect");
                    RobotVM.robots[robotIndex].ResetRobotStateOrders();                   
                    RobotVM.robots[robotIndex].RobotState = new();
                }
                else
                {
                    RobotVM.robots[robotIndex].Log(LogType.Error, "FilledOneSLPTOrderReaction Error " + cancelResult.Error.ToString());
                    error = true;
                }
            }

            if (takeProfitOrder.Status == (int)OrderStatus.Filled)
            {
                var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                   symbol: RobotVM.robots[robotIndex].Symbol,
                   orderId: stopLossOrder.OrderId);

                if (cancelResult.Success)
                {
                    RobotVM.robots[robotIndex].Log(LogType.RobotState, "FilledOneSLPTOrderReaction cansel SLTP order after reconnect");
                    RobotVM.robots[robotIndex].ResetRobotStateOrders();
                    RobotVM.robots[robotIndex].RobotState = new();                   
                }
                else
                {
                    RobotVM.robots[robotIndex].Log(LogType.Error, "FilledOneSLPTOrderReaction Error " + cancelResult.Error.ToString());
                    error = true;
                }
            }


            if (!error)
            {
                RobotVM.robots[robotIndex].IsReady = true;
            }



        }


        public async static void FilledTwoSLPTOrderReaction(RobotState state, int robotIndex)
        {
            //закрываем позицию
            if (state.Position > 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = robotIndex,
                    Symbol = RobotVM.robots[robotIndex].Symbol,
                    Side = (int)OrderSide.Buy,
                    OrderType = (int)FuturesOrderType.Market,
                    Quantity = Math.Abs(state.Position),
                    Price = 0,
                    StopPrice = 0,
                    robotOrderType = RobotOrderType.ClosePosition,
                    robotRequestType = RobotRequestType.PlaceOrder
                });

               
            }

            if (state.Position < 0)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = robotIndex,
                    Symbol = RobotVM.robots[robotIndex].Symbol,
                    Side = (int)OrderSide.Sell,
                    OrderType = (int)FuturesOrderType.Market,
                    Quantity = Math.Abs(state.Position),
                    Price = 0,
                    StopPrice = 0,
                    robotOrderType = RobotOrderType.ClosePosition,
                    robotRequestType = RobotRequestType.PlaceOrder
                });

               
            }

            RobotVM.robots[robotIndex].IsReady = true;


        }

        public async static void PartiallyFilledReaction(RobotState state, int robotIndex)
        {

        }
    }
}
