using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market.Models.ApiDataModels
{
    public class ApiStateModel
    {
        public long id { get; set; }

        public long robot_id { get; set; }
        public long client_id { get; set; }
        public decimal position { get; set; }
        public decimal open_position_price { get; set; }
        public long signal_buy_order_id { get; set; }
        public long signal_sell_order_id { get; set; }
        public long stop_loss_order_id { get; set; }
        public long take_profit_order_id { get; set; }


        public ApiStateModel()
        {
            id = 0;
            robot_id = 0;
            client_id = 0;
            position = 0;
            open_position_price = 0;
            signal_buy_order_id = 0;
            signal_sell_order_id = 0;
            stop_loss_order_id = 0;
            take_profit_order_id = 0;
        }

        public ApiStateModel(RobotState state ,int robotId)
        {
            id = 0;
            robot_id = robotId;
            client_id = state.ClientId;
            position = state.Position;
            open_position_price = state.OpenPositionPrice;
            signal_buy_order_id = state.SignalBuyOrderId;
            signal_sell_order_id = state.SignalSellOrderId;
            stop_loss_order_id = state.StopLossOrderId;
            take_profit_order_id = state.TakeProfitOrderId;


        }
    }
}
