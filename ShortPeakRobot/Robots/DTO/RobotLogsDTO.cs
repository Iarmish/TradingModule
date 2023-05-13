using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.CommonObjects;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models.ApiDataModels;
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

        public static RobotLog DTO(ApiLogModel log)
        {
            DateTime.TryParse(log.date, out DateTime date);

            return new RobotLog
            {
                Id = log.id,
                ClientId = log.client_id,
                Date = date,
                Message = log.message,
                RobotId = log.robot_id,
                Type = log.type
            };
        }
    }
}
