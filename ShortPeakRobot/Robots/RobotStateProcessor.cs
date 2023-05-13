using Binance.Net.Enums;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.ViewModel;
using System;

namespace ShortPeakRobot.Robots
{
    public static class RobotStateProcessor
    {
        public static CandlesAnalyse CheckStateAsync(RobotState state, int robotIndex)
        {
            var robot = RobotVM.robots[robotIndex];

            
            //----------------------------------
            var stateCase = GetStateCase(robotIndex);

            robot.Log(LogType.RobotState, "stateCase " + stateCase.ToString());

            switch (stateCase)
            {
                case StateCase.Normal:
                    return CandlesAnalyse.Required;

                case StateCase.FilledOneSignalOrder:
                    var sltpStutus = CheckSLTPStutus(robotIndex);
                    if (sltpStutus == SLTPStatus.Valid)
                    {
                        RobotStateProcessor.FilledOneSignalOrderReaction(robotIndex);                        
                        return CandlesAnalyse.NotRequired;
                    }
                    else
                    {
                        return CandlesAnalyse.SLTPCrossed;
                    }

                case StateCase.FilledTwoSignalOrder:
                    RobotStateProcessor.FilledTwoSignalOrderReaction(robotIndex);
                    return CandlesAnalyse.Required;

                case StateCase.FilledOneSLPTOrder:
                    RobotStateProcessor.FilledOneSLPTOrderReaction(robotIndex);
                    return CandlesAnalyse.Required;

                case StateCase.FilledTwoSLPTOrder:
                    RobotStateProcessor.FilledTwoSLPTOrderReaction(state, robotIndex);
                    return CandlesAnalyse.Required;

                case StateCase.PartiallyFilled:
                    RobotStateProcessor.PartiallyFilledReaction(state, robotIndex);
                    return CandlesAnalyse.NotRequired;

                case StateCase.PlacedSignalOrders:
                    RobotStateProcessor.PlacedSignalOrdersReaction(robotIndex);
                    return CandlesAnalyse.Required;

                case StateCase.PlacedSLTPOrders:
                    return CandlesAnalyse.NotRequired;


                default:
                    return CandlesAnalyse.Required;
            }
        }



        private static SLTPStatus CheckSLTPStutus(int robotIndex)
        {
            var robot = RobotVM.robots[robotIndex];
            SLTPStatus _SLTPStatus = SLTPStatus.Valid;
            //сигнал на покупку
            if (robot.SignalBuyOrder.Status != -1 && robot.SignalBuyOrder.Status == (int)OrderStatus.Filled)
            {
                //signal price
                var signalPrice = RobotServices.GetSignalPrice(robot.SignalBuyOrder);

                var currentCandle = MarketData.CandleDictionary[robot.Symbol]
                    [robot.BaseSettings.TimeFrame][^1];


                if (currentCandle.HighPrice >= signalPrice + robot.BaseSettings.TakeProfitPercent)
                {
                    _SLTPStatus = SLTPStatus.Crossed;
                }

                if (currentCandle.LowPrice <= signalPrice - robot.BaseSettings.StopLossPercent)
                {
                    _SLTPStatus = SLTPStatus.Crossed;
                }

            }
            //сигнал на продажу
            if (robot.SignalSellOrder.Status != -1 && robot.SignalSellOrder.Status == (int)OrderStatus.Filled)
            {
                //signal price                
                var signalPrice = RobotServices.GetSignalPrice(robot.SignalSellOrder);

                var currentCandle = MarketData.CandleDictionary[robot.Symbol]
                     [robot.BaseSettings.TimeFrame][^1];


                if (currentCandle.LowPrice <= signalPrice - robot.BaseSettings.TakeProfitPercent)
                {
                    _SLTPStatus = SLTPStatus.Crossed;
                }

                if (currentCandle.HighPrice >= signalPrice + robot.BaseSettings.StopLossPercent)
                {
                    _SLTPStatus = SLTPStatus.Crossed;
                }

            }

            //--------------------------

            return _SLTPStatus;
        }

        public static StateCase GetStateCase(int robotIndex)
        {
            var robot = RobotVM.robots[robotIndex];
            //------------------- signal cases ----------
            if (robot.SignalBuyOrder.Status != -1 && robot.SignalSellOrder.Status == -1)//один ордер по сигналу
            {
                if (robot.SignalBuyOrder.Status == (int)OrderStatus.Filled)
                {
                    return StateCase.FilledOneSignalOrder;
                }

                if (robot.SignalBuyOrder.Status == (int)OrderStatus.New)
                {
                    return StateCase.PlacedSignalOrders;
                }

                if (robot.SignalBuyOrder.Status == (int)OrderStatus.PartiallyFilled)
                {
                    return StateCase.PartiallyFilled;
                }

            }
            //----
            if (robot.SignalBuyOrder.Status == -1 && robot.SignalSellOrder.Status != -1)//один ордер по сигналу
            {
                if (robot.SignalSellOrder.Status == (int)OrderStatus.Filled)
                {
                    return StateCase.FilledOneSignalOrder;
                }

                if (robot.SignalSellOrder.Status == (int)OrderStatus.PartiallyFilled)
                {
                    return StateCase.PartiallyFilled;
                }

                if (robot.SignalSellOrder.Status == (int)OrderStatus.New)
                {
                    return StateCase.PlacedSignalOrders;
                }
            }
            if (robot.SignalBuyOrder.Status != -1 && robot.SignalSellOrder.Status != -1)// два ордера по сигналу
            {
                if (robot.SignalSellOrder.Status == (int)OrderStatus.PartiallyFilled || robot.SignalBuyOrder.Status == (int)OrderStatus.PartiallyFilled)
                {
                    return StateCase.PartiallyFilled;
                }

                if ((robot.SignalBuyOrder.Status == (int)OrderStatus.Filled && robot.SignalSellOrder.Status != (int)OrderStatus.Filled) ||
                    (robot.SignalBuyOrder.Status != (int)OrderStatus.Filled && robot.SignalSellOrder.Status == (int)OrderStatus.Filled))
                {
                    return StateCase.FilledOneSignalOrder;
                }

                if (robot.SignalBuyOrder.Status == (int)OrderStatus.Filled && robot.SignalSellOrder.Status == (int)OrderStatus.Filled)
                {
                    return StateCase.FilledTwoSignalOrder;
                }

                if (robot.SignalBuyOrder.Status == (int)OrderStatus.New && robot.SignalSellOrder.Status == (int)OrderStatus.New)
                {
                    return StateCase.PlacedSignalOrders;
                }
            }


            //------------------
            if (robot.StopLossOrder.Status != -1 && robot.TakeProfitOrder.Status != -1)
            {
                if (robot.TakeProfitOrder.Status == (int)OrderStatus.PartiallyFilled)
                {
                    return StateCase.PartiallyFilled;
                }

                if ((robot.StopLossOrder.Status == (int)OrderStatus.Filled && robot.TakeProfitOrder.Status != (int)OrderStatus.Filled) ||
                    (robot.StopLossOrder.Status != (int)OrderStatus.Filled && robot.TakeProfitOrder.Status == (int)OrderStatus.Filled))
                {
                    return StateCase.FilledOneSLPTOrder;
                }

                if (robot.StopLossOrder.Status == (int)OrderStatus.Filled && robot.TakeProfitOrder.Status == (int)OrderStatus.Filled)
                {
                    return StateCase.FilledTwoSLPTOrder;
                }

                if (robot.StopLossOrder.Status == (int)OrderStatus.New && robot.TakeProfitOrder.Status == (int)OrderStatus.New)
                {
                    return StateCase.PlacedSLTPOrders;
                }
            }

            return StateCase.Normal;
        }




        public static void PlacedSignalOrdersReaction(int robotIndex)
        {
            var robot = RobotVM.robots[robotIndex];
            if (robot.SignalSellOrder.Status != -1 && robot.SignalSellOrder.Status == (int)OrderStatus.New)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = robotIndex,
                    Symbol = robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = robot.SignalSellOrder.OrderId,
                    OrderType = robot.SignalSellOrder.Type
                });

            }

            if (robot.SignalBuyOrder.Status != -1 && robot.SignalBuyOrder.Status == (int)OrderStatus.New)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = robotIndex,
                    Symbol = robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = robot.SignalBuyOrder.OrderId,
                    OrderType = robot.SignalBuyOrder.Type
                });
            }

        }

        public static void FilledOneSignalOrderReaction(int robotIndex)
        {
            var robot = RobotVM.robots[robotIndex];
            //----buy----
            if (robot.SignalBuyOrder.Status != -1 && robot.SignalBuyOrder.Status == (int)OrderStatus.Filled)
            {
                if (robot.SignalSellOrder.OrderId != 0)//снимаем ордер противоположного сигнала
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = robotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalSellOrder.OrderId,
                        OrderType = robot.SignalSellOrder.Type
                    });
                }

                var signalPrice = RobotServices.GetSignalPrice(robot.SignalBuyOrder);
                robot.SetSLTP(OrderSide.Buy, robot.SignalBuyOrder.Quantity, signalPrice, robot.SignalBuyOrder.OrderId);
            }
            //---sell----------------------------
            if (robot.SignalSellOrder.Status != -1 && robot.SignalSellOrder.Status == (int)OrderStatus.Filled)
            {
                if (robot.SignalBuyOrder.OrderId != 0)//снимаем ордер противоположного сигнала
                {
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotIndex = robotIndex,
                        Symbol = robot.Symbol,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = robot.SignalBuyOrder.OrderId,
                        OrderType = robot.SignalBuyOrder.Type
                    });
                }

                var signalPrice = RobotServices.GetSignalPrice(robot.SignalSellOrder);
                robot.SetSLTP(OrderSide.Sell, robot.SignalSellOrder.Quantity, signalPrice, robot.SignalSellOrder.OrderId);
            }

        }

        public static void FilledTwoSignalOrderReaction(int robotIndex)
        {
            RobotVM.robots[robotIndex].Log(LogType.RobotState, "FilledTwoSignalOrderReaction  ");
            RobotVM.robots[robotIndex].IsReady = true;
        }

        public static void FilledOneSLPTOrderReaction(int robotIndex)
        {
            var robot = RobotVM.robots[robotIndex];

            //cancel second order
            if (robot.StopLossOrder.Status == (int)OrderStatus.Filled)
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = robotIndex,
                    Symbol = robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = robot.TakeProfitOrder.OrderId,
                    OrderType = robot.TakeProfitOrder.Type
                });


                robot.Log(LogType.RobotState, "FilledOneSLPTOrderReaction cansel SLTP order after reconnect");
                robot.ResetRobotData();
                robot.RobotState = new();

            }

            if (robot.TakeProfitOrder.Status == (int)OrderStatus.Filled)
            {

                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = robotIndex,
                    Symbol = robot.Symbol,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = robot.StopLossOrder.OrderId,
                    OrderType = robot.StopLossOrder.Type
                });               

                robot.Log(LogType.RobotState, "FilledOneSLPTOrderReaction cansel SLTP order after reconnect");
                robot.ResetRobotData();
                robot.RobotState = new();

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
