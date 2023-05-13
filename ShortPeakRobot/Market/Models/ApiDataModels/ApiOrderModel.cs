using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiDataModels
{
    public class ApiOrderModel
    {
        public long id { get; set; }
        public long order_id { get; set; }
        public long client_id { get; set; }
        public int robot_id { get; set; }
        public int side { get; set; }        
        public int type { get; set; }//FuturesOrderType
        public decimal quantity { get; set; }        
        public decimal price { get; set; }
        public decimal stop_price { get; set; }        
        public int status { get; set; }        
        public string symbol { get; set; }
        public long start_deal_order_id { get; set; }
        public decimal start_deposit { get; set; }       
        public decimal price_last_filled_trade { get; set; }        
        public string description { get; set; }
        public string placed_time { get; set; }

        public ApiOrderModel()
        {
            order_id = 0;
            client_id = 0;
            start_deal_order_id = 0;
            start_deposit = 0;
            robot_id = 0;
            symbol = string.Empty;
            side = 0;
            type = 0;
            quantity = 0;
            price = 0;
            price_last_filled_trade = 0;
            stop_price = 0;
            status = 0;
            description = string.Empty;
            placed_time = string.Empty;
        }

        public ApiOrderModel(RobotOrder order)
        {
            order_id = order.OrderId;
            client_id = order.ClientId;
            start_deal_order_id = order.StartDealOrderId;
            start_deposit = order.StartDeposit;
            robot_id = order.RobotId;
            symbol = order.Symbol;
            side = order.Side;
            type = order.Type;
            quantity = order.Quantity;
            price = order.Price;
            price_last_filled_trade = order.PriceLastFilledTrade;
            stop_price = (decimal)order.StopPrice;
            status = order.Status;
            description = order.Description;
            placed_time = order.PlacedTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");                        
        }
    }
    
}
