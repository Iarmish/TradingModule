using Binance.Net.Enums;
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


        public static CandlesAnalyse CheckStateAsync(RobotState state, int robotId,
            RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, RobotOrder stopLossOrder, RobotOrder takeProfitOrder)
        {


            //----------------------------------
            var stateCase = GetStateCase(signalHighPeakOrder, signalLowPeakOrder, stopLossOrder, takeProfitOrder);
            RobotVM.robots[robotId].Log(LogType.Info, "stateCase " + stateCase.ToString());

            switch (stateCase)
            {
                case StateCase.Normal:
                    return CandlesAnalyse.Required;
                case StateCase.FilledOneSignalOrder:
                    var analyse = CheckSLTPStutus(state, signalHighPeakOrder, signalLowPeakOrder, robotId);
                    if (analyse == CandlesAnalyse.SellSLTP || analyse == CandlesAnalyse.BuySLTP)
                    {
                        return analyse;
                    }
                    RobotStateProcessor.FilledOneSignalOrderReaction(state, signalHighPeakOrder, signalLowPeakOrder, robotId);
                    return CandlesAnalyse.Required;
                case StateCase.FilledTwoSignalOrder:
                    RobotStateProcessor.FilledTwoSignalOrderReaction(robotId);
                    return CandlesAnalyse.Required;
                case StateCase.FilledOneSLPTOrder:
                    RobotStateProcessor.FilledOneSLPTOrderReaction(stopLossOrder, takeProfitOrder, robotId);
                    return CandlesAnalyse.Required;
                case StateCase.FilledTwoSLPTOrder:
                    RobotStateProcessor.FilledTwoSLPTOrderReaction(state, robotId);
                    return CandlesAnalyse.Required;
                case StateCase.PartiallyFilled:
                    RobotStateProcessor.PartiallyFilledReaction(state, robotId);
                    return CandlesAnalyse.NotRequired;
                case StateCase.PlacedSignalOrders:
                    RobotStateProcessor.PlacedSignalOrdersReaction(signalHighPeakOrder, signalLowPeakOrder, robotId);
                    return CandlesAnalyse.Required;
                case StateCase.PlacedSLTPOrders:
                    //RobotStateProcessor.PlacedSLTPOrdersReaction(signalBuyOrder, signalSellOrder, robotId);
                    return CandlesAnalyse.NotRequired;


                default:
                    return CandlesAnalyse.Required;
            }


        }

        private static CandlesAnalyse CheckSLTPStutus(RobotState state, RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, int robotId)
        {
            CandlesAnalyse candlesAnalyse = CandlesAnalyse.Required;

            if (signalHighPeakOrder.Status != -1 && signalHighPeakOrder.Status == (int)OrderStatus.Filled)
            {
                BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(RobotVM.robots[robotId].Symbol, signalLowPeakOrder.OrderId);

                candlesAnalyse = CandlesAnalyse.BuySLTP;

                var newCandles = MarketData.CandleDictionary[RobotVM.robots[robotId].Symbol]
                    [RobotVM.robots[robotId].BaseSettings.TimeFrame]
                    .Where(x => x.CloseTime > signalHighPeakOrder.PlacedTime).ToList();

                newCandles.ForEach(x =>
                {
                    if (x.HighPrice >= signalHighPeakOrder.StopPrice + RobotVM.robots[robotId].BaseSettings.TakeProfitPercent)
                    {
                        candlesAnalyse = CandlesAnalyse.Required;
                    }

                    if (x.LowPrice <= signalHighPeakOrder.StopPrice - RobotVM.robots[robotId].BaseSettings.StopLossPercent)
                    {
                        candlesAnalyse = CandlesAnalyse.Required;
                    }
                });
            }

            if (signalLowPeakOrder.Status != -1 && signalLowPeakOrder.Status == (int)OrderStatus.Filled)
            {
                BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(RobotVM.robots[robotId].Symbol, signalHighPeakOrder.OrderId);

                candlesAnalyse = CandlesAnalyse.SellSLTP;

                var newCandles = MarketData.CandleDictionary[RobotVM.robots[robotId].Symbol]
                    [RobotVM.robots[robotId].BaseSettings.TimeFrame]
                    .Where(x => x.CloseTime > signalLowPeakOrder.PlacedTime).ToList();

                newCandles.ForEach(x =>
                {
                    if (x.LowPrice <= signalLowPeakOrder.StopPrice - RobotVM.robots[robotId].BaseSettings.TakeProfitPercent)
                    {
                        candlesAnalyse = CandlesAnalyse.Required;
                    }

                    if (x.HighPrice >= signalLowPeakOrder.StopPrice + RobotVM.robots[robotId].BaseSettings.StopLossPercent)
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




        public async static void PlacedSignalOrdersReaction(RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, int robotId)
        {
            if (signalLowPeakOrder.Status != -1 && signalLowPeakOrder.Status == (int)OrderStatus.New)
            {
                var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                                   symbol: RobotVM.robots[robotId].Symbol,
                                   orderId: signalLowPeakOrder.OrderId);

                if (cancelResult.Success)
                {
                    RobotVM.robots[robotId].Log(LogType.RobotState, "PlacedSignalOrdersReaction cansel other signal order after reconnect");
                    //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(cancelResult, robotId), "Cansel signal order");
                }
                else
                {
                    RobotVM.robots[robotId].Log(LogType.Error, "PlacedSignalOrdersReaction cancel order Error " + cancelResult.Error.ToString());
                }
            }

            if (signalHighPeakOrder.Status != -1 && signalHighPeakOrder.Status == (int)OrderStatus.New)
            {
                var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                                   symbol: RobotVM.robots[robotId].Symbol,
                                   orderId: signalHighPeakOrder.OrderId);

                if (cancelResult.Success)
                {
                    RobotVM.robots[robotId].Log(LogType.RobotState, "PlacedSignalOrdersReaction cansel other signal order after reconnect");
                    //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(cancelResult, robotId), "Cansel signal order");
                }
                else
                {
                    RobotVM.robots[robotId].Log(LogType.Error, "PlacedSignalOrdersReaction cancel order Error " + cancelResult.Error.ToString());
                }
            }

        }

        public async static void FilledOneSignalOrderReaction(RobotState state, RobotOrder signalHighPeakOrder, RobotOrder signalLowPeakOrder, int robotId)
        {
            //закрываем позицию
            if (signalHighPeakOrder.Status != -1 && signalHighPeakOrder.Status == (int)OrderStatus.Filled)
            {
                var placeOrderResult = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: RobotVM.robots[robotId].Symbol,
                side: OrderSide.Sell,
                type: FuturesOrderType.Market,
                quantity: signalHighPeakOrder.Quantity);

                if (placeOrderResult.Success)
                {
                    RobotVM.robots[robotId].Log(LogType.RobotState, "FilledOneSignalOrderReaction close position after reconnect");
                    //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(placeOrderResult, robotId), "Close robot position");
                }
                else
                {
                    RobotVM.robots[robotId].Log(LogType.Error, "FilledOneSignalOrderReaction place order Error " + placeOrderResult.Error.ToString());
                }

                if (signalLowPeakOrder.Status != -1)
                {
                    var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                                       symbol: RobotVM.robots[robotId].Symbol,
                                       orderId: signalLowPeakOrder.OrderId);

                    if (cancelResult.Success)
                    {
                        RobotVM.robots[robotId].Log(LogType.RobotState, "FilledOneSignalOrderReaction cansel other signal order after reconnect");
                        //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(cancelResult, robotId), "Cansel other signal order");
                    }
                    else
                    {
                        RobotVM.robots[robotId].Log(LogType.Error, "FilledOneSignalOrderReaction cancel order Error " + cancelResult.Error.ToString());
                    }
                }

            }
            //-------------------------------
            if (signalLowPeakOrder.Status != -1 && signalLowPeakOrder.Status == (int)OrderStatus.Filled)
            {
                var placeOrderResult = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: RobotVM.robots[robotId].Symbol,
                side: OrderSide.Buy,
                type: FuturesOrderType.Market,
                quantity: signalLowPeakOrder.Quantity);

                if (placeOrderResult.Success)
                {
                    RobotVM.robots[robotId].Log(LogType.RobotState, "FilledOneSignalOrderReaction after reconnect");
                    //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(placeOrderResult, robotId), "Close robot position");
                }
                else
                {
                    RobotVM.robots[robotId].Log(LogType.Error, "FilledOneSignalOrderReaction place order Error " + placeOrderResult.Error.ToString());
                }


                if (signalHighPeakOrder.Status != -1)
                {
                    var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                                       symbol: RobotVM.robots[robotId].Symbol,
                                       orderId: signalHighPeakOrder.OrderId);

                    if (cancelResult.Success)
                    {
                        RobotVM.robots[robotId].Log(LogType.RobotState, "FilledOneSignalOrderReaction cansel other signal order after reconnect");
                        //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(cancelResult, robotId), "Cansel other signal order");
                    }
                    else
                    {
                        RobotVM.robots[robotId].Log(LogType.Error, "FilledOneSignalOrderReaction cancel order Error " + cancelResult.Error.ToString());
                    }
                }

            }

        }

        public static void FilledTwoSignalOrderReaction(int robotId)
        {
            RobotVM.robots[robotId].Log(LogType.RobotState, "FilledTwoSignalOrderReaction  ");
        }

        public async static void FilledOneSLPTOrderReaction(RobotOrder stopLossOrder, RobotOrder takeProfitOrder, int robotId)
        {
            //cancel second order
            if (stopLossOrder.Status == (int)OrderStatus.Filled)
            {

                var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                   symbol: RobotVM.robots[robotId].Symbol,
                   orderId: takeProfitOrder.OrderId);

                if (cancelResult.Success)
                {
                    RobotVM.robots[robotId].Log(LogType.RobotState, "FilledOneSLPTOrderReaction cansel SLTP order after reconnect");
                    //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(cancelResult, robotId), "Cansel SLTP order");
                }
                else
                {
                    RobotVM.robots[robotId].Log(LogType.Error, "FilledOneSLPTOrderReaction Error " + cancelResult.Error.ToString());
                }
            }

            if (takeProfitOrder.Status == (int)OrderStatus.Filled)
            {
                var cancelResult = await BinanceApi.client.UsdFuturesApi.Trading.CancelOrderAsync(
                   symbol: RobotVM.robots[robotId].Symbol,
                   orderId: stopLossOrder.OrderId);

                if (cancelResult.Success)
                {
                    RobotVM.robots[robotId].Log(LogType.RobotState, "FilledOneSLPTOrderReaction cansel SLTP order after reconnect");
                    //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(cancelResult, robotId), "Cansel SLTP order");
                }
                else
                {
                    RobotVM.robots[robotId].Log(LogType.Error, "FilledOneSLPTOrderReaction Error " + cancelResult.Error.ToString());
                }
            }

        }


        public async static void FilledTwoSLPTOrderReaction(RobotState state, int robotId)
        {
            //закрываем позицию
            if (state.Position > 0)
            {
                var placeOrderResult = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: RobotVM.robots[robotId].Symbol,
                side: OrderSide.Buy,
                type: FuturesOrderType.Market,
                quantity: Math.Abs(state.Position));

                if (placeOrderResult.Success)
                {
                    RobotVM.robots[robotId].Log(LogType.RobotState, "FilledTwoSLPTOrderReaction close position after reconnect");
                    //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(placeOrderResult, robotId), "Close robot position");
                }
                else
                {
                    RobotVM.robots[robotId].Log(LogType.Error, "FilledTwoSLPTOrderReaction Error " + placeOrderResult.Error.ToString());
                }
            }

            if (state.Position < 0)
            {
                var placeOrderResult = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                symbol: RobotVM.robots[robotId].Symbol,
                side: OrderSide.Sell,
                type: FuturesOrderType.Market,
                quantity: Math.Abs(state.Position));

                if (placeOrderResult.Success)
                {
                    RobotVM.robots[robotId].Log(LogType.RobotState, "FilledTwoSLPTOrderReaction close position after reconnect");
                    //RobotServices.SaveOrder(robotId, RobotOrderDTO.DTO(placeOrderResult, robotId), "Close robot position");
                }
                else
                {
                    RobotVM.robots[robotId].Log(LogType.Error, "FilledTwoSLPTOrderReaction Error " + placeOrderResult.Error.ToString());
                }
            }


        }

        public async static void PartiallyFilledReaction(RobotState state, int robotId)
        {

        }
    }
}
