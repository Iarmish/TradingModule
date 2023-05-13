using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models.ApiModels;
using ShortPeakRobot.Robots;
using ShortPeakRobot.ViewModel;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace ShortPeakRobot.Market
{
    public static class JsonDataServices
    {
        public static List<RobotOrder> RobotOrders = new List<RobotOrder>();
        //public static List<RobotTrade> RobotTrades = new List<RobotTrade>();
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
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(fs, RobotOrders);
                }
            }
            catch (System.Exception error)
            {
                MessageBox.Show(error.Message);
            }

        }
        //-------Trade -------------
        public static async Task SaveRobotTradesAsync(List<RobotTrade> trades)
        {
            if (trades.Count == 0)
            {
                return;
            }
            var robotId = trades[0].RobotId;            

            string fileName = "Reserve/RobotTrades/RobotTrades_"+robotId+".json";

            if (!Directory.Exists("Reserve/RobotTrades"))
            {
                Directory.CreateDirectory("Reserve/RobotTrades");
            }

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(fs, trades);
                }

            }
            catch (System.Exception error)
            {
                MessageBox.Show(error.Message);
            }

        }

        public static async Task<GetTradesResponse> LoadRobotTradeAsync(int robotIndex)
        {
            var robotTradeResponse = new GetTradesResponse();

            var robotId = RobotServices.GetRobotId(robotIndex);            

            var fileName = "Reserve/RobotTrades/RobotTrades_" + robotId + ".json";
            if (Directory.Exists("Reserve/RobotTrades/") && File.Exists(fileName))
            {
                try
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        var trades = await JsonSerializer.DeserializeAsync<List<RobotTrade>>(fs);

                        if (trades != null)
                        {                            
                            robotTradeResponse.success = true;
                        }
                    }
                }
                catch (System.Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }

            return robotTradeResponse;

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

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(fs, RobotDeals);
                }
            }
            catch (System.Exception error)
            {
                MessageBox.Show(error.Message);
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

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(fs, RobotLogs);
                }
            }
            catch (System.Exception error)
            {
                MessageBox.Show(error.Message);
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

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(fs, state);
                }

            }
            catch (System.Exception error)
            {
                MessageBox.Show(error.Message);
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
                try
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
                catch (System.Exception error)
                {
                    MessageBox.Show(error.Message);
                }
            }

            return robotStateResponse;

        }


    }
}
