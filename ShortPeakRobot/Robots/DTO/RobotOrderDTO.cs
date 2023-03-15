using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

namespace ShortPeakRobot.Robots.DTO
{
    public static class RobotOrderDTO
    {
        


        public static RobotOrder DTO(WebCallResult<BinanceFuturesOrder> order, int robotId)
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

        public static RobotOrder DTO(WebCallResult<BinanceFuturesPlacedOrder> order, int robotId)
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



        public static RobotOrder DTO(RobotOrder order)
        {
            return new RobotOrder
            {
                OrderId = order.OrderId,
                ClientId = order.ClientId,
                RobotId= order.RobotId,
                Symbol = order.Symbol,
                Side = (int)order.Side,
                Type = (int)order.Type,
                Quantity = order.Quantity,
                Price = order.Price,
                StopPrice = order.StopPrice,
                Status = (int)order.Status,
                Description= order.Description,
                PlacedTime = order.PlacedTime,
            };
        }

        public static List<RobotOrder> OrdersDTO(WebCallResult<IEnumerable<BinanceFuturesOrder>> orders, int robotId)
        {
            List<RobotOrder> robotOrders = new List<RobotOrder>();

            orders.Data.ToList().ForEach(order =>
            {
                robotOrders.Add(new RobotOrder
                {
                    OrderId = order.Id,
                    ClientId = RobotsInitialization.ClientId,
                    
                    Symbol = order.Symbol,
                    Side = (int)order.Side,                    
                    Type = (int)order.Type,
                    Quantity = order.Quantity,
                    Price = order.Price,
                    StopPrice = order.StopPrice,
                    Status = (int)order.Status,                    
                    PlacedTime = order.UpdateTime,
                });
            });

            return robotOrders;
        }
    }
}
