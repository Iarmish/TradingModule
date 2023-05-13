using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models.ApiDataModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;

namespace ShortPeakRobot.Robots.DTO
{
    public static class RobotOrderDTO
    {
        public static RobotOrder DTO(DataEvent<BinanceFuturesStreamOrderUpdate> data, int robotId)
        {
            
            long startDealOrderId = 0;
            var arrClientOrderId = data.Data.UpdateData.ClientOrderId.Split(':');

            if (arrClientOrderId.Length > 2 && arrClientOrderId[0] == "robot")
            {
                startDealOrderId = Convert.ToInt64(arrClientOrderId[2]);
            }


            return new RobotOrder
            {
                OrderId = data.Data.UpdateData.OrderId,
                ClientId = RobotsInitialization.ClientId,
                RobotId = robotId,
                StartDealOrderId = startDealOrderId,
                StopPrice = data.Data.UpdateData.StopPrice,
                Quantity = data.Data.UpdateData.Quantity,
                Price = data.Data.UpdateData.Price,
                PriceLastFilledTrade = data.Data.UpdateData.PriceLastFilledTrade,
                Status = (int)data.Data.UpdateData.Status,
                Side = (int)data.Data.UpdateData.Side,
                Type = (int)data.Data.UpdateData.Type,
                PlacedTime = data.Data.UpdateData.UpdateTime,
                Symbol = data.Data.UpdateData.Symbol,
            };
        }


        public static RobotOrder DTO(WebCallResult<BinanceFuturesOrder> order, int robotId)
        {
            long startDealOrderId = 0;
            var arrClientOrderId = order.Data.ClientOrderId.Split(':');
            if (arrClientOrderId.Length > 2 && arrClientOrderId[0] == "robot")
            {
                startDealOrderId = Convert.ToInt64(arrClientOrderId[2]);
            }


            return new RobotOrder
            {
                OrderId = order.Data.Id,
                ClientId = RobotsInitialization.ClientId,
                StartDealOrderId = startDealOrderId,
                RobotId = robotId,
                StopPrice = order.Data.StopPrice,
                Quantity = order.Data.Quantity,
                Price = order.Data.Price,
                Status = (int)order.Data.Status,
                Side = (int)order.Data.Side,
                Type = (int)order.Data.Type,
                PlacedTime = order.Data.UpdateTime,
                Symbol = order.Data.Symbol,
            };
        }

        public static RobotOrder DTO(WebCallResult<BinanceFuturesCancelOrder> order, int robotId)
        {
            return new RobotOrder
            {
                OrderId = order.Data.Id,
                ClientId = RobotsInitialization.ClientId,
                RobotId = robotId,
                StopPrice = order.Data.StopPrice,
                Quantity = order.Data.Quantity,
                Price = order.Data.Price,
                Status = (int)order.Data.Status,
                Side = (int)order.Data.Side,
                Type = (int)order.Data.Type,
                PlacedTime = order.Data.UpdateTime,
                Symbol = order.Data.Symbol,
            };
        }

        public static RobotOrder DTO(WebCallResult<BinanceFuturesPlacedOrder> order, int robotId, long startDealOrderId)
        {
            return new RobotOrder
            {
                OrderId = order.Data.Id,
                ClientId = RobotsInitialization.ClientId,
                RobotId = robotId,
                StartDealOrderId = startDealOrderId,
                StopPrice = order.Data.StopPrice,
                Quantity = order.Data.Quantity,
                Price = order.Data.Price,
                Status = (int)order.Data.Status,
                Side = (int)order.Data.Side,
                Type = (int)order.Data.Type,
                PlacedTime = order.Data.UpdateTime,
                Symbol = order.Data.Symbol,
            };
        }



        public static RobotOrder DTO(RobotOrder order)
        {
            return new RobotOrder
            {
                OrderId = order.OrderId,
                ClientId = order.ClientId,
                StartDealOrderId = order.StartDealOrderId,
                StartDeposit = order.StartDeposit,
                RobotId = order.RobotId,
                Symbol = order.Symbol,
                Side = (int)order.Side,
                Type = (int)order.Type,
                Quantity = order.Quantity,
                Price = order.Price,
                PriceLastFilledTrade = order.PriceLastFilledTrade,
                StopPrice = order.StopPrice,
                Status = (int)order.Status,
                Description = order.Description,
                PlacedTime = order.PlacedTime,
            };
        }




        public static RobotOrder DTO(ApiOrderModel order)
        {
            DateTime.TryParse(order.placed_time, out DateTime date);            

            return new RobotOrder
            {
                OrderId = order.order_id,
                ClientId = order.client_id,
                StartDealOrderId = order.start_deal_order_id,
                StartDeposit = order.start_deposit,
                RobotId = order.robot_id,
                Symbol = order.symbol,
                Side = order.side,
                Type = order.type,
                Quantity = order.quantity,
                Price = order.price,
                PriceLastFilledTrade = order.price_last_filled_trade,
                StopPrice = order.stop_price,
                Status = order.status,
                Description = order.description,
                PlacedTime = date,
                Id = order.id,
            };
        }


       

        public static List<RobotOrder> OrdersDTO(WebCallResult<IEnumerable<BinanceFuturesOrder>> orders, int robotIndex)
        {
            List<RobotOrder> robotOrders = new List<RobotOrder>();
            var robotId = RobotServices.GetRobotId(robotIndex);

            orders.Data.ToList().ForEach(order =>
            {
                var arrClientOrderId = order.ClientOrderId.Split(':');
                //сохраняем ордер
                if (arrClientOrderId.Length > 1 && arrClientOrderId[0] == "robot")
                {
                    var id = arrClientOrderId[1];

                    if (Convert.ToInt32(id) == robotId)
                    {
                        robotOrders.Add(new RobotOrder
                        {
                            OrderId = order.Id,
                            ClientId = RobotsInitialization.ClientId,
                            RobotId = -1,
                            Symbol = order.Symbol,
                            Side = (int)order.Side,
                            Type = (int)order.Type,
                            Quantity = order.Quantity,
                            Price = order.Price,
                            StopPrice = order.StopPrice,
                            Status = (int)order.Status,
                            PlacedTime = order.UpdateTime,
                        });
                    }
                }
            });

            return robotOrders;
        }
    }
}
