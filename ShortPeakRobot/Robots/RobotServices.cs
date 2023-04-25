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

        public static void GetRobotDealByOrderId(long orderId, int robotId)
        {
            Task.Run(async () =>
            {
                Thread.Sleep(4000);

                var closeOrder = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                   symbol: RobotVM.robots[robotId].Symbol, orderId: orderId);



                var arrClientOrderId = closeOrder.Data.ClientOrderId.Split(':');
                long openDealOrderId = 0;

                if (arrClientOrderId.Length > 2 && arrClientOrderId[0] == "robot")
                {
                    openDealOrderId = Convert.ToInt64(arrClientOrderId[2]);
                }

                //
                var openOrder = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                        symbol: RobotVM.robots[robotId].Symbol, orderId: openDealOrderId);

                var deal = new RobotDeal { CloseOrderId = orderId };

                if (openOrder.Success && closeOrder.Success)
                {
                    deal = RobotDealDTO.DTO(robotId, openOrder, closeOrder);
                }


                SaveDeal(deal);
            });

        }


        public static void SaveCustomRobotDealByOrderId(long openOrderId,long closeOrderId, int robotId)
        {
            Task.Run(async () =>
            {
                Thread.Sleep(400);

                var closeOrder = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                   symbol: RobotVM.robots[robotId].Symbol, orderId: closeOrderId);



               

                //
                var openOrder = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                        symbol: RobotVM.robots[robotId].Symbol, orderId: openOrderId);

                var deal = new RobotDeal { CloseOrderId = closeOrderId };

                if (openOrder.Success && closeOrder.Success)
                {
                    deal = RobotDealDTO.DTO(robotId, openOrder, closeOrder);
                    deal.StartDeposit = deal.OpenPrice * deal.Quantity;
                }


                SaveDeal(deal);
            });

        }


        public static async Task<RobotOrder> GetBinOrderById(long orderId, int robotId)
        {
            var order = await BinanceApi.client.UsdFuturesApi.Trading.GetOrderAsync(
                    symbol: RobotVM.robots[robotId].Symbol,
                    orderId: orderId);

            if (!order.Success)
            {
                RobotVM.robots[robotId].Log(LogType.Error, "Не удалось получить ордер " + orderId);
                return new RobotOrder();
            }
            else
            {
                return RobotOrderDTO.DTO(order, robotId);
            }
        }


        public static void SaveOrder(RobotOrder order, string desc)
        {

            lock (RobotVM.robots[order.RobotId].Locker)
            {
                order.Description = desc;
                RobotVM.robots[order.RobotId].RobotOrdersQueue.Add(order);
            }
        }

        public static void SaveTrade(RobotTrade trade)
        {

            lock (RobotVM.robots[trade.RobotId].Locker)
            {
                RobotVM.robots[trade.RobotId].RobotTradesQueue.Add(trade);
            }

        }
        public static void SaveDeal(RobotDeal deal)
        {

            lock (RobotVM.robots[deal.RobotId].Locker)
            {
                RobotVM.robots[deal.RobotId].RobotDealQueue.Add(deal);
            }

        }

    }
}
