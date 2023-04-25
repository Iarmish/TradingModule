using ShortPeakRobot.API;
using ShortPeakRobot.Constants;
using ShortPeakRobot.Data;
using ShortPeakRobot.Robots.DTO;
using ShortPeakRobot.Robots;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static ShortPeakRobot.MainWindow;
using Binance.Net.Enums;
using Microsoft.Extensions.Options;
using CryptoExchange.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using Binance.Infrastructure.Constants;
using System.Windows.Markup;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Sockets;
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
            MarketData.Info.SessionProfit = 0;


            var clienDeals = GetClientDeals(MarketData.Info.StartSessionDate);
            var profit = GetDealsProfit(clienDeals);
            MarketData.Info.SessionProfit = profit;

            foreach (var robot in RobotVM.robots)//добавление id роботов в алгоритмы роботов
            {
                var robotTrades = clienDeals.Where(x => x.RobotId == robot.Id).ToList();

                robot.BaseSettings.Profit = MarketServices.GetDealsProfit(robotTrades);
            }

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

        public static decimal GetTradesProfit2(List<RobotTrade> robotTrades)
        {
            decimal profit = 0;

            robotTrades.ForEach(x =>
            {
                profit += x.RealizedPnl;
                profit -= x.Fee;
            });
            return profit;
        }

        public static void GetRobotData(int robotId)
        {
            if (MarketData.Info.SelectedRobotId == robotId)
            {
                var robotOrders = GetRobotOrders(robotId, MarketData.Info.StartSessionDate);
                var robotTrades = GetRobotTrades(robotId, MarketData.Info.StartSessionDate);
                var robotDeals = GetRobotDeals(robotId, MarketData.Info.StartSessionDate);
                var robotLogs = GetRobotLogs(robotId, MarketData.Info.StartSessionDate);
                MarketData.Info.SessionRobotProfit = GetDealsProfit(robotDeals);
                RobotVM.robots[robotId].BaseSettings.Profit = MarketData.Info.SessionRobotProfit;





                GetOpenOrders(robotId);

                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotOrderVM.orders.Clear();
                    RobotOrderVM.AddRange(robotOrders);

                    RobotTradeVM.trades.Clear();
                    RobotTradeVM.AddRange(robotTrades);

                    RobotDealVM.deals.Clear();
                    RobotDealVM.AddRange(robotDeals);

                    LogVM.logs.Clear();
                    LogVM.AddRange(robotLogs);
                });
                //MarketData.OpenOrders = RobotOrderDTO.OrdersDTO(
                //await BinanceApi.client.UsdFuturesApi.Trading.GetOpenOrdersAsync(RobotVM.robots[robotId].Symbol), robotId);

            }


        }


        public static List<RobotLog> GetRobotLogs(int robotId, DateTime startDate)
        {
            var robotLogs = new List<RobotLog>();



            try
            {
                lock (Locker)
                {
                    robotLogs = _context.RobotLogs.Where(x => x.ClientId == RobotsInitialization.ClientId && x.Date > startDate &&
                                   x.RobotId == robotId && x.Date > MarketData.Info.StartSessionDate && MarketData.LogTypeFilter.Contains(x.Type)).ToList();
                }
            }
            catch (Exception)
            {
                throw;
            }

            return robotLogs;
        }

        public async static Task<List<RobotOrder>> GetOpenOrders(int robotId)
        {
            var orders = RobotOrderDTO.OrdersDTO(
            await BinanceApi.client.UsdFuturesApi.Trading.GetOpenOrdersAsync(RobotVM.robots[robotId].Symbol), robotId);

            MarketData.OpenOrders = orders;
            RobotVM.robots[robotId].Orders = MarketData.OpenOrders.Count;
            return orders;

        }

        public static async Task GetBalanceUSDTAsync()
        {
            var balances = await BinanceApi.client.UsdFuturesApi.Account.GetBalancesAsync();
            foreach (var item in balances.Data)
            {
                if (item.Asset == "USDT")
                {
                    MarketData.BalanceUSDT = item;
                    MarketData.Info.BalanceUSDT = Math.Round(item.AvailableBalance, 2);
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

        public static List<RobotOrder> GetRobotOrders(int robotId, DateTime startDate)
        {
            var robotOrders = new List<RobotOrder>();
            lock (Locker)
            {
                robotOrders = _context.RobotOrders
                   .Where(x => x.PlacedTime > startDate && x.RobotId == robotId && x.ClientId == RobotsInitialization.ClientId)
                   .OrderBy(x => x.PlacedTime).ToList();

            }


            //var ids = GetRobotOrderIds(robotId, startDate);
            //var binOrders = await BinanceApi.client.UsdFuturesApi.Trading.GetOrdersAsync(RobotVM.robots[robotId].Symbol,
            //    startTime: startDate);
            //var robotOrders = RobotOrderDTO.OrdersDTO(binOrders, robotId);

            //robotOrders = robotOrders.Where(x => ids.Contains(x.OrderId)).ToList();

            return robotOrders;
        }

        public static List<RobotDeal> GetClientDeals(DateTime startDate)
        {
            var robotDeals = new List<RobotDeal>();
            lock (Locker)
            {
                robotDeals = _context.RobotDeals
                   .Where(x => x.OpenTime > startDate && x.ClientId == RobotsInitialization.ClientId).ToList();

            }

            //robotTrades = robotTrades.Where(x => ids.Contains(x.OrderId)).ToList();



            return robotDeals;
        }

        public static List<RobotTrade> GetClientTrades(DateTime startDate)
        {
            var robotTrades = new List<RobotTrade>();
            lock (Locker)
            {
                robotTrades = _context.RobotTrades
                   .Where(x => x.Timestamp > startDate && x.ClientId == RobotsInitialization.ClientId).ToList();

            }

            //robotTrades = robotTrades.Where(x => ids.Contains(x.OrderId)).ToList();



            return robotTrades;
        }

        public static List<RobotTrade> GetRobotTrades(int robotId, DateTime startDate)
        {
            var robotTrades = new List<RobotTrade>();
            lock (Locker)
            {
                robotTrades = _context.RobotTrades
                   .Where(x => x.Timestamp > startDate && x.RobotId == robotId && x.ClientId == RobotsInitialization.ClientId)
                   .OrderBy(x => x.Timestamp).ToList();
            }

            return robotTrades;
        }
         public static List<RobotDeal> GetRobotDeals(int robotId, DateTime startDate)
        {
            var robotDeals = new List<RobotDeal>();
            lock (Locker)
            {
                robotDeals = _context.RobotDeals
                   .Where(x => x.OpenTime > startDate && x.RobotId == robotId && x.ClientId == RobotsInitialization.ClientId)
                   .OrderBy(x => x.OpenTime).ToList();
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


        public async static Task<WebCallResult<BinanceFuturesPlacedOrder>> PlaceBinanceOrder(long startDealOrderId, int orderCount, int robotId, 
            string symbol, OrderSide side, FuturesOrderType orderType, decimal quantity, decimal price = 0, decimal stopPrice = 0)
        {
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
                    SetRobotVariableLot(robot.Id, price);
                }
            }

        }

        public static void SetRobotVariableLot(int robotId, decimal price)
        {
            

            if (RobotVM.robots[robotId].BaseSettings.IsVariableLot)
            {
                var robotPartDepo = MarketData.Info.Deposit / 100 * RobotVM.robots[robotId].BaseSettings.Deposit;
                RobotVM.robots[robotId].BaseSettings.CurrentDeposit = robotPartDepo + RobotVM.robots[robotId].BaseSettings.Profit;
                //variable lot
                RobotVM.robots[robotId].BaseSettings.Volume =
                    Math.Round(RobotVM.robots[robotId].BaseSettings.CurrentDeposit / price, SymbolIndexes.lot[RobotVM.robots[robotId].Symbol]);

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
