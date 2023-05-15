using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Market.Models.ApiDataModels;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ShortPeakRobot.Robots.DTO
{
    public static class RobotTradeDTO
    {
        public static RobotTrade DTO(RobotTrade order)
        {
            return new RobotTrade
            {
                Id = order.Id,
                OrderId = order.OrderId,
                ClientId = order.ClientId,
                RobotId = order.RobotId,
                StartDealOrderId = order.StartDealOrderId,
                StartDeposit = order.StartDeposit,
                Symbol = order.Symbol,
                Side = order.Side,
                PositionSide = order.PositionSide,
                Buyer = order.Buyer,
                Price = order.Price, 
                PriceLastFilledTrade = order.PriceLastFilledTrade,
                Quantity = order.Quantity,
                RealizedPnl = order.RealizedPnl,
                Timestamp = order.Timestamp,
                Fee = order.Fee
            };
        }

       


        public static RobotTrade DTO(ApiTradeModel  trade)
        {
            DateTime.TryParse(trade.timestamp, out DateTime date);
            return new RobotTrade
            {
                Id = trade.id,
                OrderId = trade.order_id,
                ClientId = trade.client_id,
                RobotId = trade.robot_id,
                StartDealOrderId = trade.start_deal_order_id,
                StartDeposit = trade.start_deposit,
                Symbol = trade.symbol,
                Side = trade.side,
                PositionSide = trade.position_side,
                Buyer = trade.buyer,
                Price = trade.price,
                PriceLastFilledTrade = trade.price_last_filled_trade,
                Quantity = trade.quantity,
                RealizedPnl = trade.realized_pnl,
                Timestamp = date,
                Fee = trade.fee,
            };
        }

        public static RobotTrade DTO(DataEvent< BinanceFuturesStreamOrderUpdate> data, int robotId, long startDealOrderId)
        {
            var tradePrice = data.Data.UpdateData.Price;
            if (data.Data.UpdateData.Price == 0)
            {
                tradePrice = data.Data.UpdateData.StopPrice;
            }

            return new RobotTrade
            {
                OrderId = data.Data.UpdateData.OrderId,
                ClientId = MarketData.Info.ClientId,
                RobotId = robotId,
                StartDealOrderId= startDealOrderId,
                Symbol = data.Data.UpdateData.Symbol,
                Side = (int)data.Data.UpdateData.Side,
                Buyer = data.Data.UpdateData.BuyerIsMaker,
                PositionSide = (int)data.Data.UpdateData.PositionSide,
                Price = tradePrice,
                PriceLastFilledTrade = tradePrice,
                Quantity = data.Data.UpdateData.QuantityOfLastFilledTrade,
                Fee = data.Data.UpdateData.Fee,
                RealizedPnl = data.Data.UpdateData.RealizedProfit,
                Timestamp = data.Data.UpdateData.UpdateTime
            };
        }

        //public static RobotTrade DTO(WebCallResult<BinanceFuturesOrder> order, int robotId)
        //{
        //    return new RobotTrade
        //    {
        //        OrderId = order.Data.Id,
        //        ClientId = RobotsInitialization.ClientId,
        //        RobotId = robotId,                
        //        Symbol = order.Data.Symbol,
        //        Side = (int)order.Data.Side,
        //        PositionSide = (int)order.Data.PositionSide,                
        //        Price = order.Data.Price,                
        //        Quantity = order.Data.Quantity,                
        //        Timestamp = order.Data.UpdateTime,                
        //    };
        //}

        //public static List<RobotTrade> TradeDTO(WebCallResult<IEnumerable<BinanceFuturesUsdtTrade>> trades, int robotId)
        //{
        //    List<RobotTrade> robotTrades = new List<RobotTrade>();

        //    trades.Data.ToList().ForEach(trade =>
        //    {
        //        robotTrades.Add(new RobotTrade
        //        {
        //            OrderId = trade.OrderId,
        //            ClientId = RobotsInitialization.ClientId,
        //            RobotId = robotId,
        //            Symbol = trade.Symbol,
        //            Side = (int)trade.Side,
        //            PositionSide = (int)trade.PositionSide,
        //            Buyer = trade.Buyer,
        //            Price = trade.Price,
        //            Quantity = trade.Quantity,
        //            RealizedPnl = trade.RealizedPnl,
        //            Timestamp = trade.Timestamp,
        //            Fee = trade.Fee
        //        });
        //    });

        //    return robotTrades;
        //}
    }
}
