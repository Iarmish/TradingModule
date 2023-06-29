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
        //public static List<RobotOrder> RobotOrders = new List<RobotOrder>();
        //public static List<RobotTrade> RobotTrades = new List<RobotTrade>();
        public static List<RobotDeal> RobotDeals = new List<RobotDeal>();
        public static List<RobotLog> RobotLogs = new List<RobotLog>();

        //---  Order -------------------
        public static async Task SaveRobotOrdersAsync(int robotId, List<RobotOrder> orders)
        {
            string fileName = "Reserve/RobotOrders/RobotOrders_" + robotId + ".json";
            if (orders.Count == 0)
            {
                try
                {
                    File.Delete(fileName);
                    return;
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "File delete error " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                    return;
                }
            }
            //

            if (!Directory.Exists("Reserve/RobotOrders"))
            {
                Directory.CreateDirectory("Reserve/RobotOrders");
            }
            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    if (orders.Count > 0)
                    {
                        await JsonSerializer.SerializeAsync(fs, orders);
                    }
                }
            }
            catch (System.Exception error)
            {
                MarketData.Info.Message += "Save order " + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

        }

        public static async Task<List<RobotOrder>> LoadRobotOrdersAsync(int robotId)
        {
            var robotOrders = new List<RobotOrder>();

            var fileName = "Reserve/RobotOrders/RobotOrders_" + robotId + ".json";
            if (Directory.Exists("Reserve/RobotOrders/") && File.Exists(fileName))
            {
                try
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        var orders = await JsonSerializer.DeserializeAsync<List<RobotOrder>>(fs);

                        if (orders != null)
                        {
                            robotOrders = orders;
                        }
                    }
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "Load orders " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                }
            }

            return robotOrders;

        }
        //-------Trade -------------
        public static async Task SaveRobotTradesAsync(int robotId, List<RobotTrade> trades)
        {

            string fileName = "Reserve/RobotTrades/RobotTrades_" + robotId + ".json";
            if (trades.Count == 0)
            {
                try
                {
                    File.Delete(fileName);
                    return;
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "File delete error " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                    return;
                }
            }

            if (!Directory.Exists("Reserve/RobotTrades"))
            {
                Directory.CreateDirectory("Reserve/RobotTrades");
            }

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    if (trades.Count > 0)
                    {
                        await JsonSerializer.SerializeAsync(fs, trades);
                    }
                }

            }
            catch (System.Exception error)
            {
                MarketData.Info.Message += "Save trades " + robotId + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

        }

        public static async Task<List<RobotTrade>> LoadRobotTradesAsync(int robotId)
        {
            var robotTrades = new List<RobotTrade>();

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
                            robotTrades = trades;
                        }
                    }
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "Load trades " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                }
            }

            return robotTrades;

        }
        //------- Deal -------------
        public static async Task SaveRobotDealsAsync(int robotId, List<RobotDeal> deals)
        {

            string fileName = "Reserve/RobotDeals/RobotDeals_" + robotId + ".json";
            if (deals.Count == 0)
            {
                try
                {
                    File.Delete(fileName);
                    return;
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "File delete error " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                    return;
                }
            }

            if (!Directory.Exists("Reserve/RobotDeals"))
            {
                Directory.CreateDirectory("Reserve/RobotDeals");
            }

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    if (deals.Count > 0)
                    {
                        await JsonSerializer.SerializeAsync(fs, deals);
                    }
                }

            }
            catch (System.Exception error)
            {
                MarketData.Info.Message += "Save deals " + robotId + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

        }

        public static async Task<List<RobotDeal>> LoadRobotDealsAsync(int robotId)
        {
            var robotDeals = new List<RobotDeal>();

            var fileName = "Reserve/RobotDeals/RobotDeals_" + robotId + ".json";
            if (Directory.Exists("Reserve/RobotDeals/") && File.Exists(fileName))
            {
                try
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        var deals = await JsonSerializer.DeserializeAsync<List<RobotDeal>>(fs);

                        if (deals != null)
                        {
                            robotDeals = deals;
                        }
                    }
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "Load deals " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                }
            }

            return robotDeals;

        }
        //------- Log  -------------
        public static async Task SaveRobotLogsAsync(int robotId, List<RobotLog> logs)
        {

            string fileName = "Reserve/RobotLogs/RobotLogs_" + robotId + ".json";
            if (logs.Count == 0)
            {
                try
                {
                    File.Delete(fileName);
                    return;
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "File delete error " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                    return;
                }
            }

            if (!Directory.Exists("Reserve/RobotLogs"))
            {
                Directory.CreateDirectory("Reserve/RobotLogs");
            }

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    if (logs.Count > 0)
                    {
                        await JsonSerializer.SerializeAsync(fs, logs);
                    }
                }

            }
            catch (System.Exception error)
            {
                MarketData.Info.Message += "Save logs " + robotId + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

        }

        public static async Task<List<RobotLog>> LoadRobotLogsAsync(int robotId)
        {
            var robotLogs = new List<RobotLog>();

            var fileName = "Reserve/RobotLogs/RobotLogs_" + robotId + ".json";
            if (Directory.Exists("Reserve/RobotLogs/") && File.Exists(fileName))
            {
                try
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Open))
                    {
                        var logs = await JsonSerializer.DeserializeAsync<List<RobotLog>>(fs);

                        if (logs != null)
                        {
                            robotLogs = logs;
                        }
                    }
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "Load logs " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                }
            }

            return robotLogs;

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
                MarketData.Info.Message += "Save state " + robotId + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

        }

        public static async Task<RobotStateResponse> LoadRobotStateAsync(int robotIndex)
        {
            var robotStateResponse = new RobotStateResponse();

            var robotId = RobotServices.GetRobotId(robotIndex);
            robotStateResponse.json_data.RobotId = robotId;

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
                            robotStateResponse.json_data = state;
                            robotStateResponse.success = true;
                        }
                    }
                }
                catch (System.Exception error)
                {
                    MarketData.Info.Message += "Save trades " + robotId + " " + error.Message + "\n";
                    MarketData.Info.IsMessageActive = true;
                }
            }

            return robotStateResponse;

        }

        public static async void SaveRobotDealAsync(int robotId, RobotDeal deal)
        {

            string fileName = "Reserve/RobotDeals/ErrorRobotDealSave_" + robotId + ".json";

            if (!Directory.Exists("Reserve/RobotDeals"))
            {
                Directory.CreateDirectory("Reserve/RobotDeals");
            }

            try
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(fs, deal);
                }

            }
            catch (System.Exception error)
            {
                MarketData.Info.Message += "Save deals " + robotId + " " + error.Message + "\n";
                MarketData.Info.IsMessageActive = true;
            }

        }


    }
}
