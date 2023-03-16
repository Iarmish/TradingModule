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
        public static void SaveState(int robotId, RobotState robotState)
        {

            
                RobotVM.robots[robotId].NeedSaveState = true;

                
            




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
