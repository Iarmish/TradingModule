using CryptoExchange.Net.CommonObjects;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models.ApiDataModels;
using ShortPeakRobot.Robots.Algorithms.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.DTO
{
    public static class RobotDealModelDTO
    {
        public static RobotDealModel DTO(RobotDeal deal)
        {


            var slip = Math.Abs(deal.OpenPrice - deal.OpenOrderPrice) + Math.Abs(deal.ClosePrice - deal.CloseOrderPrice);

            return new RobotDealModel
            {
                ClientId = deal.ClientId,
                CloseOrderId = deal.CloseOrderId,
                CloseOrderPrice = deal.CloseOrderPrice,
                ClosePrice = deal.ClosePrice,
                CloseTime = deal.CloseTime,
                Fee = deal.Fee,
                Id = deal.Id,
                OpenOrderId = deal.OpenOrderId,
                OpenOrderPrice = deal.OpenOrderPrice,
                OpenPrice = deal.OpenPrice,
                OpenTime = deal.OpenTime,
                Quantity = deal.Quantity,
                Result = deal.Result,
                RobotId = deal.RobotId,
                Side = deal.Side,
                StartDeposit = deal.StartDeposit,
                Symbol = deal.Symbol,
                Slip = slip
            };
        }


        public static RobotDealModel DTO(ApiDealModel deal)
        {


            var slip = Math.Abs(deal.open_price - deal.open_order_price) + Math.Abs(deal.close_price - deal.close_order_price);

            DateTime.TryParse(deal.close_time, out DateTime CloseTime);
            DateTime.TryParse(deal.open_time, out DateTime OpenTime);

            return new RobotDealModel
            {
                ClientId = deal.client_id,
                CloseOrderId = deal.close_order_id,
                CloseOrderPrice = deal.close_order_price,
                ClosePrice = deal.close_price,
                CloseTime = CloseTime,
                Fee = deal.fee,
                Id = deal.id,
                OpenOrderId = deal.open_order_id,
                OpenOrderPrice = deal.open_order_price,
                OpenPrice = deal.open_price,
                OpenTime = OpenTime,
                Quantity = deal.quantity,
                Result = deal.result,
                RobotId = deal.robot_id,
                Side = deal.side,
                StartDeposit = deal.start_deposit,
                Symbol = deal.symbol,
                Slip = slip
            };
        }

        public static List<RobotDealModel> DTO(List<RobotDeal> deals)
        {

            var newDeals = new List<RobotDealModel>();

            foreach (var deal in deals)
            {
                if (deal.CloseOrderPrice == 0)
                {
                    deal.CloseOrderPrice = deal.ClosePrice;
                }
                var slip = Math.Abs(deal.OpenPrice - deal.OpenOrderPrice) + Math.Abs(deal.ClosePrice - deal.CloseOrderPrice);

                newDeals.Add(new RobotDealModel
                {
                    ClientId = deal.ClientId,
                    CloseOrderId = deal.CloseOrderId,
                    CloseOrderPrice = deal.CloseOrderPrice,
                    ClosePrice = deal.ClosePrice,
                    CloseTime = deal.CloseTime,
                    Fee = deal.Fee,
                    Id = deal.Id,
                    OpenOrderId = deal.OpenOrderId,
                    OpenOrderPrice = deal.OpenOrderPrice,
                    OpenPrice = deal.OpenPrice,
                    OpenTime = deal.OpenTime,
                    Quantity = deal.Quantity,
                    Result = deal.Result,
                    RobotId = deal.RobotId,
                    Side = deal.Side,
                    StartDeposit = deal.StartDeposit,
                    Symbol = deal.Symbol,
                    Slip = slip
                });
            }

            return newDeals;
        }
    }
}
