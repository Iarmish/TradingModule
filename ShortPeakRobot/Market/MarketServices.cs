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


            var clientTrades = GetClientTrades(MarketData.Info.StartSessionDate);
            var profit = GetTradesProfit(clientTrades);
            MarketData.Info.SessionProfit = profit;

            foreach (var robot in RobotVM.robots)//добавление id роботов в алгоритмы роботов
            {
               var robotTrades = clientTrades.Where(x => x.RobotId == robot.Id).ToList();
                 
                robot.BaseSettings.Profit = MarketServices.GetTradesProfit(robotTrades);
            }

        }
        

        public static decimal GetTradesProfit(List<RobotTrade> robotTrades)
        {
            decimal profit = 0;

            robotTrades.ForEach(x =>
            {
                profit += x.RealizedPnl;
                profit -= x.Fee;
            });
            return profit;
        }

        public async static void GetRobotData(int robotId)
        {
            if (MarketData.Info.SelectedRobotId == robotId)
            {
                var robotOrders = await GetRobotOrders(robotId, MarketData.Info.StartSessionDate);
                var robotTrades = GetRobotTrades(robotId, MarketData.Info.StartSessionDate);
                MarketData.Info.SessionRobotProfit = GetTradesProfit(robotTrades);
                RobotVM.robots[robotId].BaseSettings.Profit = MarketData.Info.SessionRobotProfit;

                var robotLogs = new List<RobotLog>();
                try
                {
                    lock (Locker)
                    {
                        robotLogs = _context.RobotLogs.Where(x => x.ClientId == RobotsInitialization.ClientId &&
                                       x.RobotId == robotId && x.Date > MarketData.Info.StartSessionDate).ToList();
                    }
                }
                catch (Exception)
                {

                    throw;
                }



                MarketData.OpenOrders = robotOrders.Where(x => x.Status == (int)OrderStatus.New).ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    RobotOrderVM.orders.Clear();
                    RobotOrderVM.AddRange(robotOrders);

                    RobotTradeVM.trades.Clear();
                    RobotTradeVM.AddRange(robotTrades);


                    LogVM.logs.Clear();
                    LogVM.AddRange(robotLogs);
                });
                //MarketData.OpenOrders = RobotOrderDTO.OrdersDTO(
                //await BinanceApi.client.UsdFuturesApi.Trading.GetOpenOrdersAsync(RobotVM.robots[robotId].Symbol), robotId);

            }


        }

        public static async void GetBalanceUSDTAsync()
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

        public async static Task<List<RobotOrder>> GetRobotOrders(int robotId, DateTime startDate)
        {
            //var endDate = new DateTime(DateTime.Now, DateTimeKind.Utc)


            var ids = GetRobotOrderIds(robotId, startDate);
            var binOrders = await BinanceApi.client.UsdFuturesApi.Trading.GetOrdersAsync(RobotVM.robots[robotId].Symbol,
                startTime: startDate);
            var robotOrders = RobotOrderDTO.OrdersDTO(binOrders, robotId);

            robotOrders = robotOrders.Where(x => ids.Contains(x.OrderId)).ToList();

            return robotOrders;
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
                   .Where(x => x.Timestamp > startDate && x.RobotId == robotId && x.ClientId == RobotsInitialization.ClientId).ToList();

            }

            //robotTrades = robotTrades.Where(x => ids.Contains(x.OrderId)).ToList();



            return robotTrades;
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


        public async static Task<WebCallResult<BinanceFuturesPlacedOrder>> PlaceBinanceOrder(int robotId, string symbol, OrderSide side,
            FuturesOrderType orderType, decimal quantity, decimal price = 0, decimal stopPrice = 0)
        {
            if (price != 0)
            {
                var placedOrder = await BinanceApi.client.UsdFuturesApi.Trading.PlaceOrderAsync(
                  symbol: symbol,
                  side: side,
                  type: orderType,
                  quantity: quantity,
                  price: price,
                  timeInForce: TimeInForce.GoodTillCanceled);

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
                   timeInForce: TimeInForce.GoodTillCanceled);

                return placedOrder;
            }



        }


    }
}
