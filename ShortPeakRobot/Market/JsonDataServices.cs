using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models.ApiModels;
using ShortPeakRobot.Robots;
using ShortPeakRobot.ViewModel;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ShortPeakRobot.Market
{
    public static class JsonDataServices
    {
        public static List<RobotOrder> RobotOrders = new List<RobotOrder>();
        public static List<RobotTrade> RobotTrades = new List<RobotTrade>();
        public static List<RobotDeal> RobotDeals = new List<RobotDeal>();
        public static List<RobotLog> RobotLogs = new List<RobotLog>();

        //---  Order -------------------
        public static async Task SaveRobotOrderAsync(RobotOrder order)
        {
            RobotOrders.Add(order);
            string fileName = "Reserve/RobotOrders.json";

            if (!Directory.Exists("Reserve"))
            {
                Directory.CreateDirectory("Reserve");
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(fs, RobotOrders);
            }

        }
        //-------Trade -------------
        public static async Task SaveRobotTradeAsync(RobotTrade trade)
        {
            RobotTrades.Add(trade);
            string fileName = "Reserve/RobotTrades.json";

            if (!Directory.Exists("Reserve"))
            {
                Directory.CreateDirectory("Reserve");
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(fs, RobotTrades);
            }

        }
        //------- Deal -------------
        public static async Task SaveRobotDealAsync(RobotDeal deal)
        {
            RobotDeals.Add(deal);
            string fileName = "Reserve/RobotDeals.json";

            if (!Directory.Exists("Reserve"))
            {
                Directory.CreateDirectory("Reserve");
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(fs, RobotDeals);
            }

        }
        //------- Log  -------------
        public static async Task SaveRobotLogAsync(RobotLog log)
        {
            RobotLogs.Add(log);
            string fileName = "Reserve/RobotLogs.json";

            if (!Directory.Exists("Reserve"))
            {
                Directory.CreateDirectory("Reserve");
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(fs, RobotLogs);
            }

        }
        //------- State -------------
        public static async Task SaveRobotStateAsync(int robotId, RobotState state)
        {

            string fileName = "Reserve/RobotStates/RobotState_" + robotId + ".json";

            if (!Directory.Exists("Reserve/RobotStates"))
            {
                Directory.CreateDirectory("Reserve/RobotStates");
            }

            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(fs, state);
            }

        }

        public static async Task<RobotStateResponse> LoadRobotStateAsync(int robotIndex)
        {
            var robotStateResponse = new RobotStateResponse();

            var robotId = RobotServices.GetRobotId(robotIndex);
            robotStateResponse.data.RobotId = robotId;

            var fileName = "Reserve/RobotStates/RobotState_" + robotId + ".json";
            if (Directory.Exists("Reserve/RobotStates/") && File.Exists(fileName))
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    var state = await JsonSerializer.DeserializeAsync<RobotState>(fs);

                    if (state != null)
                    {
                        robotStateResponse.data = state;
                        robotStateResponse.success = true;
                    }
                }
            }

            return robotStateResponse;

        }


    }
}
