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
    public static class RobotLogsDTO
    {
        public static RobotLog DTO(RobotLog log)
        {
            return new RobotLog
            {
                Id = log.Id,
                ClientId = log.ClientId,
                Date = log.Date,
                Message = log.Message,
                RobotId = log.RobotId,
                Type = log.Type
            };
        }
    }
}
