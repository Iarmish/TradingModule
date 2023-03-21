using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Migrations;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.Robots.Models;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots
{
    public static class RobotServices
    {
        public static bool CompareState(RobotState FirstState, RobotState SecondState)
        {
            if (FirstState.OpenPositionPrice != SecondState.OpenPositionPrice) { return false; }
            if (FirstState.Position != SecondState.Position) { return false; }
            if (FirstState.SignalBuyOrderId != SecondState.SignalBuyOrderId) { return false; }
            if (FirstState.SignalSellOrderId != SecondState.SignalSellOrderId) { return false; }
            if (FirstState.StopLossOrderId != SecondState.StopLossOrderId) { return false; }
            if (FirstState.TakeProfitOrderId != SecondState.TakeProfitOrderId) { return false; }

            return true;
        }
        //public static void SaveState(int robotId, RobotState robotState)
        //{            
        //        RobotVM.robots[robotId].NeedSaveState = true;
        //}


        public static async void ForceStopRobotAsync(int robotId)
        {   
                var openOrders = await MarketServices.GetOpenOrders(robotId);

                openOrders.ForEach(o =>
                {
                    //RobotVM.robots[MarketData.Info.SelectedRobotId].CancelOrderAsync(o, "Cancel robot orders");
                    MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                    {
                        RobotId = robotId,
                        Symbol = RobotVM.robots[robotId].Symbol,
                        robotOrderType = RobotOrderType.OrderId,
                        robotRequestType = RobotRequestType.CancelOrder,
                        OrderId = o.OrderId
                    });
                });


                RobotVM.robots[robotId].ResetRobotStateOrders();
                RobotVM.robots[robotId].RobotState.TakeProfitOrderId = 0;
                RobotVM.robots[robotId].RobotState.StopLossOrderId = 0;
                RobotVM.robots[robotId].RobotState.SignalSellOrderId = 0;
                RobotVM.robots[robotId].RobotState.SignalBuyOrderId = 0;

                RobotVM.robots[robotId].Stop();
            
        }


        public static RobotState LoadStateAsync(int robotId)
        {
            lock (RobotVM.robots[robotId].Locker)
            {
                var state = RobotVM.robots[robotId]._context.RobotStates
                                .Where(x => x.ClientId == RobotsInitialization.ClientId && x.RobotId == robotId).FirstOrDefault();

                if (state != null)
                {
                    return state;
                }
                else
                {
                    state = new RobotState { RobotId = robotId };
                    RobotVM.robots[robotId]._context.RobotStates.Add(state);
                    RobotVM.robots[robotId]._context.SaveChanges();

                    return state;
                }
            }


        }



        public static async Task<RobotOrder> GetBinOrderById(long orderId, int robotId)
        {
            var order = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                    symbol: RobotVM.robots[robotId].Symbol,
                    orderId: orderId);

            if (!order.Success)
            {
                RobotVM.robots[robotId].Log(LogType.Error, "Не удалось получить ордер " + orderId);
                return null;
            }
            else
            {
                return RobotOrderDTO.DTO(order, robotId);
            }
        }


        public static void SaveOrder(int robotId, RobotOrder order, string desc)
        {

            lock (RobotVM.robots[robotId].Locker)
            {
                order.Description = desc;
                RobotVM.robots[robotId].RobotOrdersQueue.Add(order);
            }
        }

        public static void SaveTrade(RobotTrade trade, int robotId)
        {

            lock (RobotVM.robots[robotId].Locker)
            {
                RobotVM.robots[robotId].RobotTradesQueue.Add(trade);
            }

        }

    }
}
