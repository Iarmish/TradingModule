using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiDataModels
{
    public class ApiDealModel
    {
        public long id { get; set; }
        public long client_id { get; set; }
        public int robot_id { get; set; }
        public long open_order_id { get; set; }
        public long close_order_id { get; set; }
        public decimal start_deposit { get; set; }
        public string symbol { get; set; }
        public int side { get; set; }
        public decimal quantity { get; set; }
        public decimal open_price { get; set; }
        public decimal close_price { get; set; }
        public decimal open_order_price { get; set; }
        public decimal close_order_price { get; set; }
        public decimal fee { get; set; }
        public decimal result { get; set; }


        public string open_time { get; set; }
        public string close_time { get; set; }

        public ApiDealModel()
        {
            client_id = 0;
            robot_id = 0;
            open_order_id = 0;
            close_order_id = 0;
            start_deposit = 0;
            symbol = string.Empty;
            side = 0;
            quantity = 0;
            open_price = 0;
            close_price = 0;
            open_order_price = 0;
            close_order_price = 0;
            fee = 0;
            result =0;
            open_time = string.Empty;
            close_time = string.Empty;
        }

        public ApiDealModel(RobotDeal deal)
        {
            client_id = deal.ClientId;
            robot_id = deal.RobotId;
            open_order_id = deal.OpenOrderId;
            close_order_id= deal.CloseOrderId;
            start_deposit = deal.StartDeposit;
            symbol = deal.Symbol;
            side = deal.Side;
            quantity = deal.Quantity;
            open_price = deal.OpenPrice;
            close_price = deal.ClosePrice;
            open_order_price = deal.OpenOrderPrice;
            close_order_price = deal.CloseOrderPrice;
            fee = deal.Fee;
            result = deal.Result;
            open_time = deal.OpenTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            close_time = deal.CloseTime.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
        }
    }

}
