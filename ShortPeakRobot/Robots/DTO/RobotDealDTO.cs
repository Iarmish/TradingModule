﻿using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market;
using ShortPeakRobot.Market.Models.ApiDataModels;
using ShortPeakRobot.Robots.Algorithms.Models;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.DTO
{
    public static class RobotDealDTO
    {
        public static RobotDeal DTO(int robotIndex, WebCallResult<BinanceFuturesOrder> openOrder, WebCallResult<BinanceFuturesOrder> closeOrder)
        {
            var robotId = RobotServices.GetRobotId(robotIndex);

            var openPrice = 0m;
            var openOrderPrice = 0m;
            var openFee = 0m;

            if (openOrder.Data.Type == FuturesOrderType.Limit)
            {
                openPrice = openOrder.Data.Price;
                openOrderPrice = openOrder.Data.Price;
                openFee = openPrice / 100 * 0.02m * openOrder.Data.Quantity;
            }
            else
            {
                openPrice = openOrder.Data.AvgPrice;
                openOrderPrice = (decimal)openOrder.Data.StopPrice;
                if (openPrice == 0) { openPrice = openOrderPrice; }
                openFee = openPrice / 100 * 0.04m * openOrder.Data.Quantity;
            }
            //
            var closePrice = 0m;
            var closeOrderPrice = 0m;
            var closeFee = 0m;
            
            if (closeOrder.Data.Type == FuturesOrderType.Limit)
            {
                closePrice = closeOrder.Data.Price;
                closeOrderPrice = closeOrder.Data.Price;
                closeFee = closePrice / 100 * 0.02m * closeOrder.Data.Quantity;
            }
            else
            {
                closePrice = closeOrder.Data.AvgPrice;
                closeOrderPrice = (decimal)closeOrder.Data.StopPrice;
                closeFee = closePrice / 100 * 0.04m * closeOrder.Data.Quantity;
            }
            //
            var result = 0m;
            if (openOrder.Data.Side == OrderSide.Buy)
            {
                result = (closePrice - openPrice) * openOrder.Data.Quantity;
            }
            else
            {
                result = (openPrice - closePrice) * openOrder.Data.Quantity;
            }
            //

            var CurrentDeposit = RobotVM.robots[robotIndex].BaseSettings.CurrentDeposit - result;


            return new RobotDeal
            {
                ClientId = MarketData.Info.ClientId,
                RobotId = robotId,
                OpenOrderId = openOrder.Data.Id,
                CloseOrderId = closeOrder.Data.Id,
                OpenPrice = openPrice,
                ClosePrice = closePrice,
                OpenOrderPrice = openOrderPrice,
                CloseOrderPrice = closeOrderPrice,
                OpenTime = openOrder.Data.UpdateTime,
                CloseTime = closeOrder.Data.UpdateTime,
                Side = (int)openOrder.Data.Side,
                Quantity = openOrder.Data.Quantity,
                Symbol = openOrder.Data.Symbol,
                Result = result,
                Fee = openFee + closeFee,
                StartDeposit = CurrentDeposit
                
            };
        }


        public static RobotDeal DTO(RobotDeal deal)
        {
            return new RobotDeal
            {
                ClientId = deal.ClientId,
                RobotId = deal.RobotId,
                Id = deal.Id,
                Fee = deal.Fee,
                StartDeposit = deal.StartDeposit,
                Side = deal.Side,
                Quantity= deal.Quantity,
                Symbol = deal.Symbol,
                Result = deal.Result,
                CloseOrderId= deal.CloseOrderId,
                ClosePrice = deal.ClosePrice,
                OpenTime = deal.OpenTime,
                CloseTime = deal.CloseTime,
                OpenOrderId = deal.OpenOrderId,
                OpenPrice = deal.OpenPrice,
                CloseOrderPrice = deal.CloseOrderPrice,
                OpenOrderPrice = deal.OpenOrderPrice,
                
            };
        }
        
        public static RobotDeal DTO(ApiDealModel deal)
        {
            var slip = Math.Abs(deal.open_price - deal.open_order_price) + Math.Abs(deal.close_price - deal.close_order_price);

            DateTime.TryParse(deal.close_time, out DateTime CloseTime);
            DateTime.TryParse(deal.open_time, out DateTime OpenTime);

            return new RobotDeal
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
            };
        }

    }
}
