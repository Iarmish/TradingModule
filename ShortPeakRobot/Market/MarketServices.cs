using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.Robots;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static ShortPeakRobot.MainWindow;
using Binance.Net.Enums;
using CryptoExchange.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using Binance.Infrastructure.Constants;
using ShortPeakRobot.Socket;
using ShortPeakRobot.Robots.Algorithms.Models;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Markup;

namespace ShortPeakRobot.Market
{
    public static class MarketServices
    {
        public static object Locker = new object();

        public static CandleChartDwawDelegate candleChartDwaw;

        

        public async static Task<decimal> GetSymbolPositionAsync(string symbol)
        {
            decimal position = 0;

            var data = await BinanceApi.client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);


            foreach (var item in data.Data)
            {
                position += item.Quantity;
            }


            return position;
        }

        public static async void GetSessioProfit()
        {
            var clienDeals = await GetClientDeals(MarketData.Info.StartSessionDate, DateTime.UtcNow.AddDays(1));
            var profit = GetDealsProfit(clienDeals);

            foreach (var robot in RobotVM.robots)//добавление id роботов в алгоритмы роботов
            {
                var robotTrades = clienDeals.Where(x => x.RobotId == robot.Id).ToList();

                robot.SessionProfit = GetDealsProfit(robotTrades);
            }

        }
        
        public static async void GetLastDayProfit()
        {
            var dateNow = DateTime.UtcNow;
            var dateToday = new DateTime(dateNow.Year, dateNow.Month, dateNow.Day, 0, 0, 0, DateTimeKind.Utc);
            
            var clienDeals = await GetClientDeals(dateToday, dateToday.AddDays(1));
            var profit = GetDealsProfit(clienDeals);
            MarketData.Info.DayProfit = profit;
        }

        public static async void GetPeriodProfit()
        {
            var clienDeals = await GetClientDeals(MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);
            var profit = GetDealsProfit(clienDeals);

            MarketData.Info.PeriodProfit = profit;

        }


        public static decimal GetDealsProfit(List<RobotDeal> robotDeals)
        {
            decimal profit = 0;

            robotDeals.ForEach(x =>
            {
                profit += x.Result;
                profit -= x.Fee;
            });
            return profit;
        }


        public async static void GetRobotMarketData(int robotIndex)
        {
            var robotId = RobotServices.GetRobotId(robotIndex);
            var robot = RobotVM.robots[robotIndex];
            if (MarketData.Info.SelectedRobotIndex == robotIndex)
            {
                var robotOrdersRespose = await ApiServices.GetOrders(robotId, MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);
                var robotTradesRespose = await ApiServices.GetTrades(robotId, MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);
                var robotDealsRespose = await ApiServices.GetDeals(robotId, MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);
                var robotLogsRespose = await ApiServices.GetLogs(robotId, MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);


                GetOpenOrders(robotIndex);


                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotTradeVM.trades.Clear();
                    RobotOrderVM.orders.Clear();
                    RobotDealVM.deals.Clear();
                    LogVM.logs.Clear();

                    if (robotOrdersRespose.success)
                    {
                        var robotOrders = new List<RobotOrder>();
                        robotOrdersRespose.data.ForEach(order => robotOrders.Add(RobotOrderDTO.DTO(order)));
                        RobotOrderVM.AddRange(robotOrders);
                    }


                    if (robotTradesRespose.success)
                    {
                        var robotTrades = new List<RobotTrade>();
                        robotTradesRespose.data.ForEach(trade => robotTrades.Add(RobotTradeDTO.DTO(trade)));
                        RobotTradeVM.AddRange(robotTrades);
                    }
                    else
                    {
                        RobotTradeVM.AddRange(robot.RobotTradesUnsaved);
                    }


                    if (robotDealsRespose.success)
                    {
                        var robotDealsModel = new List<RobotDealModel>();
                        var robotDeals = new List<RobotDeal>();

                        robotDealsRespose.data.ForEach(deal =>
                        {
                            robotDealsModel.Add(RobotDealModelDTO.DTO(deal));
                            robotDeals.Add(RobotDealDTO.DTO(deal));

                        });
                        RobotDealVM.AddRange(robotDealsModel);

                        MarketData.Info.PeriodRobotProfit = GetDealsProfit(robotDeals);
                    }

                    if (robotLogsRespose.success)
                    {
                        var robotLogs = new List<RobotLog>();
                        robotLogsRespose.data.ForEach(log => robotLogs.Add(RobotLogsDTO.DTO(log)));
                        LogVM.AddRange(robotLogs);
                    }
                });


            }


        }




        public async static Task<List<RobotOrder>> GetOpenOrders(int robotIndex)
        {
            var orders = RobotOrderDTO.OrdersDTO(
            await BinanceApi.client.UsdFuturesApi.Trading.GetOpenOrdersAsync(RobotVM.robots[robotIndex].Symbol), robotIndex);

            MarketData.OpenOrders = orders;
            RobotVM.robots[robotIndex].Orders = MarketData.OpenOrders.Count;
            return orders;

        }

        public static async Task<bool> GetBalanceUSDTAsync()
        {
            var balances = await BinanceApi.client.UsdFuturesApi.Account.GetBalancesAsync();
            if (!balances.Success)
            {
                MarketData.Info.Message += balances.Error + "\n";
                MarketData.Info.IsMessageActive = true;
                return false;
            }
            foreach (var item in balances.Data)
            {
                if (item.Asset == "USDT")
                {
                    MarketData.Info.BalanceUSDT = Math.Round(item.WalletBalance, 2);
                }
            }
            return true;
        }

        public static int GetSymbolIndex(string symbol)
        {

            return SymbolInitialization.list.IndexOf(symbol);
        }

        public async static void CloseSymbolPositionAsync(string symbol)
        {

            var position = await BinanceApi.client.UsdFuturesApi.Account.GetPositionInformationAsync(symbol);

            var positions = position.Data.ToList();

            if (positions.Count > 0 && positions[0].Quantity != 0)
            {
                if (positions[0].Quantity > 0)
                {
                    var dd = BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                       symbol: symbol,
                       side: OrderSide.Sell,
                       type: FuturesOrderType.Market,
                       quantity: Math.Abs(positions[0].Quantity));
                }
                if (positions[0].Quantity < 0)
                {
                    var dd = BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                       symbol: symbol,
                       side: OrderSide.Buy,
                       type: FuturesOrderType.Market,
                       quantity: Math.Abs(positions[0].Quantity));
                }

            }
        }

        

        public async static Task<List<RobotDeal>> GetClientDeals(DateTime startDate, DateTime endDate)
        {
            var robotDeals = new List<RobotDeal>();// GetClientDeals

            var robotOrdersRespose = await ApiServices.GetClientDeals(startDate, endDate);

            if (robotOrdersRespose.success)
            {
                robotOrdersRespose.data.ForEach(order => robotDeals.Add(RobotDealDTO.DTO(order)));
            }
           
            return robotDeals;
        }

        

       


        public async static Task<WebCallResult<BinanceFuturesPlacedOrder>> PlaceBinanceOrder(long startDealOrderId, int orderCount, int robotIndex,
            string symbol, OrderSide side, FuturesOrderType orderType, decimal quantity, decimal price = 0, decimal stopPrice = 0)
        {
            var robotId = RobotServices.GetRobotId(robotIndex);
            if (orderType == FuturesOrderType.Market)
            {
                var placedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                  symbol: symbol,
                  side: side,
                  type: orderType,
                  quantity: quantity,
                  newClientOrderId: "robot:" + robotId + ":" + startDealOrderId + ":" + orderCount);

                return placedOrder;
            }

            if (price != 0)
            {
                var placedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                  symbol: symbol,
                  side: side,
                  type: orderType,
                  quantity: quantity,
                  price: price,
                  timeInForce: TimeInForce.GoodTillCanceled,
                  newClientOrderId: "robot:" + robotId + ":" + startDealOrderId + ":" + orderCount);

                return placedOrder;
            }
            else
            {
                var placedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                   symbol: symbol,
                   side: side,
                   type: orderType,
                   quantity: quantity,
                   stopPrice: stopPrice,
                   timeInForce: TimeInForce.GoodTillCanceled,
                   newClientOrderId: "robot:" + robotId + ":" + startDealOrderId + ":" + orderCount);

                return placedOrder;
            }



        }

        public static void SetRobotLotBySymbol(string symbol, decimal price)
        {
            foreach (Robot robot in RobotVM.robots)
            {
                if (robot.Symbol.Equals(symbol))
                {
                    SetRobotVariableLot(robot.Index, price);
                    SetRobotParamPercent(robot.Index, price);
                }
            }

        }

        public static void SetRobotVariableLot(int robotIndex, decimal price)
        {
            if (RobotVM.robots[robotIndex].BaseSettings.IsVariableLot)
            {
                var robotPartDepo = MarketData.Info.Deposit / 100 * RobotVM.robots[robotIndex].BaseSettings.Deposit;
                RobotVM.robots[robotIndex].BaseSettings.CurrentDeposit = robotPartDepo + RobotVM.robots[robotIndex].SessionProfit;
                //variable lot
                RobotVM.robots[robotIndex].BaseSettings.Volume =
                    Math.Round(RobotVM.robots[robotIndex].BaseSettings.CurrentDeposit / price, SymbolIndexes.lot[RobotVM.robots[robotIndex].Symbol]);

            }

        }

        public static void SetRobotParamPercent(int robotIndex, decimal price)
        {
            if (RobotVM.robots[robotIndex].BaseSettings.TPPercent)
            {
                RobotVM.robots[robotIndex].BaseSettings.TakeProfitPercent =
                        Math.Round(price / 100 * RobotVM.robots[robotIndex].BaseSettings.TakeProfit, SymbolIndexes.price[RobotVM.robots[robotIndex].Symbol]);                
            }

            if (RobotVM.robots[robotIndex].BaseSettings.SLPercent)
            {
                RobotVM.robots[robotIndex].BaseSettings.StopLossPercent =
                        Math.Round(price / 100 * RobotVM.robots[robotIndex].BaseSettings.StopLoss, SymbolIndexes.price[RobotVM.robots[robotIndex].Symbol]);
            }

            if (RobotVM.robots[robotIndex].BaseSettings.IsOffsetPercent)
            {
                RobotVM.robots[robotIndex].BaseSettings.OffsetPercent =
                        Math.Round(price / 100 * RobotVM.robots[robotIndex].BaseSettings.Offset, SymbolIndexes.price[RobotVM.robots[robotIndex].Symbol]);
            }
        }

        public async static void StopAllSubscribes()
        {
            await BinanceSocket.client.UnsubscribeAllAsync();
        }

        public async static void StopUserDataStream()
        {
            var key = MarketData.MarketManager.ListenKey;
            var res = await BinanceApi.client.UsdFuturesApi.Account.StopUserStreamAsync(key.Data);
        }


        public async static void StartUserDataStream()
        {
            MarketData.MarketManager.ActivateUserDataStream();
        }

        public async static void StartAllSubscribes()
        {
            MarketData.MarketManager.ActivateUserDataStream();
            MarketData.MarketManager.ActivateSubscribe();
        }


        public static byte[] EncryptStringToBytes(string plainText, byte[] key, byte[] iv)
        {
            byte[] encrypted;

            // Create an Aes object with the specified key and IV.
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                // Create a new MemoryStream object to contain the encrypted bytes.
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    // Create a CryptoStream object to perform the encryption.
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        // Encrypt the plaintext.
                        using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        encrypted = memoryStream.ToArray();
                    }
                }
            }

            return encrypted;
        }

        public static string DecryptStringFromBytes(byte[] cipherText, byte[] key, byte[] iv)
        {
            string decrypted;

            // Create an Aes object with the specified key and IV.
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                // Create a new MemoryStream object to contain the decrypted bytes.
                using (MemoryStream memoryStream = new MemoryStream(cipherText))
                {
                    // Create a CryptoStream object to perform the decryption.
                    using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        // Decrypt the ciphertext.
                        using (StreamReader streamReader = new StreamReader(cryptoStream))
                        {
                            decrypted = streamReader.ReadToEnd();
                        }
                    }
                }
            }

            return decrypted;
        }
    }
}
