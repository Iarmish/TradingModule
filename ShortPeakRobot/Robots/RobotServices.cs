﻿using Binance.Net.Enums;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.ViewModel;
using System;
using System.Linq;
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


        public static int GetRobotId(int robotIndex)
        {
            return RobotVM.robots[robotIndex].Id;
        }

        public static int GetRobotIndex(int robotId)
        {
            return RobotVM.robots.Where(x => x.Id == robotId).Select(x => x.Index).FirstOrDefault();
        }

        public static async void ForceStopRobotAsync(int robotIndex)
        {
            var openOrders = await MarketServices.GetOpenOrders(robotIndex);

            openOrders.ForEach(o =>
            {
                MarketData.MarketManager.AddRequestQueue(new BinanceRequest
                {
                    RobotIndex = robotIndex,
                    Symbol = RobotVM.robots[robotIndex].Symbol,
                    robotOrderType = RobotOrderType.OrderId,
                    robotRequestType = RobotRequestType.CancelOrder,
                    OrderId = o.OrderId
                });
            });


            RobotVM.robots[robotIndex].ResetRobotData();
            RobotVM.robots[robotIndex].RobotState.TakeProfitOrderId = 0;
            RobotVM.robots[robotIndex].RobotState.StopLossOrderId = 0;
            RobotVM.robots[robotIndex].RobotState.SignalSellOrderId = 0;
            RobotVM.robots[robotIndex].RobotState.SignalBuyOrderId = 0;

            RobotVM.robots[robotIndex].Stop();

        }


        public static decimal GetSignalPrice(RobotOrder order)
        {
            var signalPrice = (decimal)order.StopPrice;
            if (order.StopPrice == 0)
            {
                signalPrice = order.Price;
            }
            return signalPrice;
        }

        public static void GetRobotDealByOrderId(long orderId, int robotIndex)
        {
            Task.Run(async () =>
            {
                Thread.Sleep(4000);

                var deal = new RobotDeal { CloseOrderId = orderId };

                var closeOrder = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                   symbol: RobotVM.robots[robotIndex].Symbol, orderId: orderId);
                if (closeOrder.Success)
                {
                    var arrClientOrderId = closeOrder.Data.ClientOrderId.Split(':');
                    long openDealOrderId = 0;

                    if (arrClientOrderId.Length > 2 && arrClientOrderId[0] == "robot")
                    {
                        openDealOrderId = Convert.ToInt64(arrClientOrderId[2]);
                    }
                    //---
                    var openOrder = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                        symbol: RobotVM.robots[robotIndex].Symbol, orderId: openDealOrderId);


                    if (openOrder.Success)
                    {

                        deal = RobotDealDTO.DTO(robotIndex, openOrder, closeOrder);

                    }
                    if (deal.OpenPrice == 0 || deal.ClosePrice == 0)
                    {
                        MarketData.Info.Message += "Ошибка сохранения сделки ! Robot ID - "+ RobotVM.robots[robotIndex].Id + "\n";
                        MarketData.Info.IsMessageActive = true;
                        MarketData.MarketManager.Log(LogType.Error, "Ошибка сохранения сделки ! Robot ID - " + RobotVM.robots[robotIndex].Id);
                        JsonDataServices.SaveRobotDealAsync(RobotVM.robots[robotIndex].Id, deal);
                        return;
                    }

                }


                //               


                SaveDeal(deal);
            });

        }


        public static void SaveCustomRobotDealByOrderId(long openOrderId, long closeOrderId, int robotIndex)
        {
            Task.Run(async () =>
            {
                Thread.Sleep(400);

                var closeOrder = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                   symbol: RobotVM.robots[robotIndex].Symbol, orderId: closeOrderId);

                //
                var openOrder = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                        symbol: RobotVM.robots[robotIndex].Symbol, orderId: openOrderId);

                var deal = new RobotDeal { CloseOrderId = closeOrderId };

                if (openOrder.Success && closeOrder.Success)
                {
                    deal = RobotDealDTO.DTO(robotIndex, openOrder, closeOrder);
                    deal.StartDeposit = deal.OpenPrice * deal.Quantity;
                }


                SaveDeal(deal);
            });

        }


        public static async Task<RobotOrder> GetBinOrderById(long orderId, int robotIndex)
        {

            var order = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                    symbol: RobotVM.robots[robotIndex].Symbol,
                    orderId: orderId);

            if (!order.Success)
            {
                RobotVM.robots[robotIndex].Log(LogType.Error, "Не удалось получить ордер " + orderId);
                return new RobotOrder();
            }
            else
            {
                return RobotOrderDTO.DTO(order, GetRobotId(robotIndex));
            }
        }


        public static void SaveOrder(RobotOrder order, string desc)
        {
            var index = GetRobotIndex(order.RobotId);
            lock (RobotVM.robots[index].Locker)
            {
                order.Description = desc;
                RobotVM.robots[index].RobotOrdersQueue.Add(order);
            }
        }

        public static void SaveTrade(RobotTrade trade)
        {
            var index = GetRobotIndex(trade.RobotId);
            lock (RobotVM.robots[index].Locker)
            {
                RobotVM.robots[index].RobotTradesQueue.Add(trade);
            }

        }
        public static void SaveDeal(RobotDeal deal)
        {
            var index = GetRobotIndex(deal.RobotId);
            lock (RobotVM.robots[index].Locker)
            {
                RobotVM.robots[index].RobotDealsQueue.Add(deal);
            }

        }
        public static decimal GetSignalPrice(decimal offset, decimal price, RobotOrderType robotOrderType, FuturesOrderType futuresOrderType)
        {
            decimal signalPrice = 0;

            if (price == 0)
            {
                return 0;
            }


            if (robotOrderType == RobotOrderType.SignalSell)
            {
                if (futuresOrderType == FuturesOrderType.StopMarket)
                {
                    signalPrice = price - offset;
                }
                if (futuresOrderType == FuturesOrderType.Limit)
                {
                    signalPrice = price + offset;
                }
            }

            if (robotOrderType == RobotOrderType.SignalBuy)
            {
                if (futuresOrderType == FuturesOrderType.StopMarket)
                {
                    signalPrice = price + offset;
                }
                if (futuresOrderType == FuturesOrderType.Limit)
                {
                    signalPrice = price - offset;
                }
            }

            return signalPrice;
        }

    }
}
