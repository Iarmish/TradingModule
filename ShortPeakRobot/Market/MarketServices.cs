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

namespace ShortPeakRobot.Market
{
    public static class MarketServices
    {
        public static object Locker = new object();

        public static CandleChartDwawDelegate candleChartDwaw;

        public static ApplicationDbContext _context = new ApplicationDbContext();


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

        public static void GetSessioProfit()
        {
            var clienDeals = GetClientDeals(MarketData.Info.StartSessionDate, DateTime.UtcNow);
            var profit = GetDealsProfit(clienDeals);

            foreach (var robot in RobotVM.robots)//добавление id роботов в алгоритмы роботов
            {
                var robotTrades = clienDeals.Where(x => x.RobotId == robot.Id).ToList();

                robot.SessionProfit = GetDealsProfit(robotTrades);
            }

        }

        public static void GetPeriodProfit()
        {
            var clienDeals = GetClientDeals(MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);
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
            if (MarketData.Info.SelectedRobotIndex == robotIndex)
            {
                var robotOrdersRespose = await ApiServices.GetOrders(robotId, MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);
                var robotTradesRespose = await ApiServices.GetTrades(robotId, MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);
                var robotDealsRespose = await ApiServices.GetDeals(robotId, MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);
                var robotLogsRespose = await ApiServices.GetLogs(robotId, MarketData.Info.StartStatisticPeriod, MarketData.Info.EndStatisticPeriod);

                if (robotDealsRespose.success)
                {
                    MarketData.Info.PeriodRobotProfit = GetDealsProfit(robotDealsRespose.data);
                }

                GetOpenOrders(robotIndex);


                App.Current.Dispatcher.Invoke(() =>
                {
                    if (robotOrdersRespose.success)
                    {
                        RobotOrderVM.orders.Clear();
                        RobotOrderVM.AddRange(robotOrdersRespose.data);
                    }


                    if (robotTradesRespose.success)
                    {
                        RobotTradeVM.trades.Clear();
                        RobotTradeVM.AddRange(robotTradesRespose.data);
                    }


                    if (robotDealsRespose.success)
                    {
                        RobotDealVM.deals.Clear();
                        RobotDealVM.AddRange(RobotDealModelDTO.DTO(robotDealsRespose.data));
                    }

                    if (robotLogsRespose.success)
                    {
                        LogVM.logs.Clear();
                        LogVM.AddRange(robotLogsRespose.data);
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

        public static async Task GetBalanceUSDTAsync()
        {
            var balances = await BinanceApi.client.UsdFuturesApi.Account.GetBalancesAsync();
            foreach (var item in balances.Data)
            {
                if (item.Asset == "USDT")
                {
                    MarketData.Info.BalanceUSDT = Math.Round(item.WalletBalance, 2);
                }
            }
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

        public static List<RobotOrder> GetRobotOrders(int robotId, DateTime startDate, DateTime endDate)
        {
            var robotOrders = new List<RobotOrder>();
            lock (Locker)
            {
                robotOrders = _context.RobotOrders
                   .Where(x => x.PlacedTime >= startDate && x.PlacedTime <= endDate.AddDays(1) && x.RobotId == robotId && x.ClientId == RobotsInitialization.ClientId)
                   .OrderBy(x => x.PlacedTime).AsEnumerable().TakeLast(100).ToList();

                //robotOrders = robotOrders.TakeLast(100).ToList();
            }
            return robotOrders;
        }

        public static List<RobotDeal> GetClientDeals(DateTime startDate, DateTime endDate)
        {
            var robotDeals = new List<RobotDeal>();
            lock (Locker)
            {
                robotDeals = _context.RobotDeals
                   .Where(x => x.CloseTime >= startDate && x.CloseTime <= endDate.AddDays(1) && x.ClientId == RobotsInitialization.ClientId).ToList();

            }


            return robotDeals;
        }

        public static List<RobotTrade> GetClientTrades(DateTime startDate)
        {
            var robotTrades = new List<RobotTrade>();
            lock (Locker)
            {
                robotTrades = _context.RobotTrades
                   .Where(x => x.Timestamp > startDate && x.ClientId == RobotsInitialization.ClientId)
                   .ToList();

            }

            //robotTrades = robotTrades.Where(x => ids.Contains(x.OrderId)).ToList();



            return robotTrades;
        }

        public static List<RobotTrade> GetRobotTrades(int robotId, DateTime startDate, DateTime endDate)
        {
            var robotTrades = new List<RobotTrade>();
            lock (Locker)
            {
                robotTrades = _context.RobotTrades
                   .Where(x => x.Timestamp >= startDate && x.Timestamp <= endDate.AddDays(1) && x.RobotId == robotId && x.ClientId == RobotsInitialization.ClientId)
                   .OrderBy(x => x.Timestamp).AsEnumerable().TakeLast(100).ToList();
            }

            return robotTrades;
        }
        public static List<RobotDeal> GetRobotDeals(int robotId, DateTime startDate, DateTime endDate)
        {
            var robotDeals = new List<RobotDeal>();
            lock (Locker)
            {
                robotDeals = _context.RobotDeals
                   .Where(x => x.CloseTime >= startDate && x.CloseTime <= endDate.AddDays(1) && x.RobotId == robotId && x.ClientId == RobotsInitialization.ClientId)
                   .OrderBy(x => x.OpenTime).AsEnumerable().TakeLast(200).ToList();
            }

            return robotDeals;
        }


        public static List<long> GetRobotOrderIds(int robotId, DateTime startDate)
        {
            var ids = new List<long>();

            try
            {
                lock (Locker)
                {
                    var orders = _context.RobotOrders
                        .Where(x => x.ClientId == RobotsInitialization.ClientId && x.RobotId == robotId && x.PlacedTime >= startDate);
                    ids = orders.Select(x => x.OrderId).Distinct().ToList();
                }
            }
            catch (Exception)
            {

                throw;
            }


            return ids;
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
    }
}
