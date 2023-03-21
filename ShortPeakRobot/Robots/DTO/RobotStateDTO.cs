using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Sockets;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.DTO
{
    public static class RobotStateDTO
    {
        public static RobotState DTO(RobotState state)
        {
            return new RobotState
            {
                ClientId = state.ClientId,
                RobotId = state.RobotId,
                OpenPositionPrice = state.OpenPositionPrice,
                Position = state.Position,
                SignalBuyOrderId = state.SignalBuyOrderId,
                SignalSellOrderId = state.SignalSellOrderId,
                StopLossOrderId = state.StopLossOrderId,
                TakeProfitOrderId = state.TakeProfitOrderId,
            };
        }
    }
}
