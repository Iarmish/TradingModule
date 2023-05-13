using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiDataModels
{
    public class ApiTradeModel
    {
        public long id { get; set; }
        public long order_id { get; set; }
        public long client_id { get; set; }
        public long start_deal_order_id { get; set; }
        public decimal start_deposit { get; set; }
        public int robot_id { get; set; }
        public string symbol { get; set; } = "";
        public int side { get; set; }
        public int position_side { get; set; }
        public bool buyer { get; set; }
        public decimal price { get; set; }
        public decimal price_last_filled_trade { get; set; }
        public decimal quantity { get; set; }
        public decimal realized_pnl { get; set; }
        public string timestamp { get; set; }
        public decimal fee { get; set; }

        public ApiTradeModel()
        {            
            order_id = 0;
            client_id = 0;
            start_deal_order_id = 0;
            start_deposit = 0;
            robot_id = 0;
            symbol = string.Empty;
            side = 0;
            position_side = 0;
            buyer = false;
            price = 0;
            price_last_filled_trade = 0;
            quantity = 0;
            realized_pnl = 0;
            timestamp = string.Empty;
            fee = 0;
        }


        public ApiTradeModel(RobotTrade trade)
        {
            order_id = trade.OrderId;
            client_id = trade.ClientId;
            start_deal_order_id = trade.StartDealOrderId;
            start_deposit = trade.StartDeposit;
            robot_id = trade.RobotId;
            symbol = trade.Symbol;
            side = trade.Side;
            position_side = trade.PositionSide;
            buyer = trade.Buyer;
            price = trade.Price;
            price_last_filled_trade = trade.PriceLastFilledTrade;
            quantity = trade.Quantity;
            realized_pnl = trade.RealizedPnl;
            timestamp = trade.Timestamp.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
            fee = trade.Fee;
        }
    }
}
