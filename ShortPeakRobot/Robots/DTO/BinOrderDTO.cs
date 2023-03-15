using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.DTO
{
    public class BinOrderDTO
    {
        public static List<BinOrder> OrdersDTO(WebCallResult<IEnumerable<BinanceFuturesOrder>> orders)
        {
            var binOrders = new List<BinOrder>();



            orders.Data.ToList().ForEach(order =>
            {
                binOrders.Add(new BinOrder
                {
                    OrderId = order.Id,
                    Symbol = order.Symbol,
                    Side = (int)order.Side,
                    Status = (int)order.Status,
                    PositionSide = (int)order.PositionSide,
                    
                    Price = order.Price,
                    StopPrice= order.StopPrice,                    
                    Quantity = order.Quantity,
                    CreateTime = order.CreateTime,
                    UpdateTime= order.UpdateTime,
                    Type = (int)order.Type
                });
            });

            return binOrders;
        }
    }
}
