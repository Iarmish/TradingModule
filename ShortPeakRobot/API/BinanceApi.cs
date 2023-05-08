using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces.Clients;
using Binance.Net.Objects;
using Binance.Net.Objects.Models.Futures;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Market;
using ShortPeakRobot.Market.Models;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace ShortPeakRobot.API
{
    public static class BinanceApi
    {
        public static BinanceClient client = new BinanceClient();
        public static void SetKeys(string ApiKey, string ApiSecret)
        {
            client.SetApiCredentials(new ApiCredentials(ApiKey, ApiSecret));
        }

        public async static void GetCandles(string symbol, int timeframe, int candleQty)
        {
            KlineInterval klineInterval = (KlineInterval)timeframe;
            var result = await client.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, klineInterval, null, null, candleQty);
            var candles = result.Data.Select(x => new Candle
            {
                OpenPrice = x.OpenPrice,
                HighPrice = x.HighPrice,
                LowPrice = x.LowPrice,
                ClosePrice = x.ClosePrice,
                OpenTime = x.OpenTime,
                CloseTime = x.CloseTime,
                Symbol = RobotVM.robots[0].Symbol,
                Volume = x.Volume
            }).ToList();

            if (MarketData.CandleDictionary[symbol][timeframe].Count == 0)
            {
                MarketData.CandleDictionary[symbol][timeframe].AddRange(candles);
            }
            //candles.RemoveAt(candles.Count - 1);

            //return candles;
        }





        //---------------------------------------
        public async static Task<WebCallResult<BinanceFuturesPlacedOrder>> PlaceOrder(string symbol, OrderSide side,
            FuturesOrderType type, decimal volume, decimal price, TimeInForce timeInForce, decimal stopPrice = 0)
        {
            if (type == FuturesOrderType.Stop)
            {
                return await client.UsdFuturesApi.Trading.PlaceOrderAsync(
               symbol, side, type, volume, price, timeInForce: timeInForce, stopPrice: stopPrice);
            }

            if (type == FuturesOrderType.Market)
            {
                return await client.UsdFuturesApi.Trading.PlaceOrderAsync(symbol, side, type, volume);
            }
            return await client.UsdFuturesApi.Trading.PlaceOrderAsync(
               symbol, side, type, volume, price, timeInForce: timeInForce);
        }
        //--------------------------
        public async static Task<WebCallResult<BinanceFuturesPlacedOrder>> PlaceStopLoss(string symbol, OrderSide side,
            FuturesOrderType type, decimal volume, decimal price, TimeInForce timeInForce, decimal stopPrice = 0)
        {

            PositionSide positionSide;
            OrderSide sltpSide;
            decimal TPstopPrice;
            decimal SLstopPrice;

            if (side == OrderSide.Sell)
            {
                TPstopPrice = price - 0.6m;
                SLstopPrice = price + 0.6m;
                sltpSide = OrderSide.Buy;
                positionSide = PositionSide.Short;
            }
            else
            {
                TPstopPrice = price + 0.6m;
                SLstopPrice = price - 0.6m;
                sltpSide = OrderSide.Sell;
                positionSide = PositionSide.Long;
            }


            var result = await client.UsdFuturesApi.Trading.PlaceOrderAsync(
               symbol: symbol,
               side: side,
               type: type,
               quantity: volume,
               price: price,
               orderResponseType: OrderResponseType.Result,
               positionSide: positionSide,
               workingType: WorkingType.Mark,
               timeInForce: timeInForce
               );

            if (result.Success)
            {

                // take profit
                var tpOrderResult = await client.UsdFuturesApi.Trading.PlaceOrderAsync(
                                        symbol: symbol,
                                        side: sltpSide,
                                        type: FuturesOrderType.TakeProfitMarket,
                                        quantity: volume,
                                        stopPrice: TPstopPrice, // take profit price
                                        orderResponseType: OrderResponseType.Result,
                                        positionSide: positionSide,
                                        workingType: WorkingType.Mark,
                                        timeInForce: TimeInForce.GoodTillCanceled,
                                        newClientOrderId: "654",
                                        closePosition: true // add this
                                    );

                // stop loss
                var slOrderResultTask = client.UsdFuturesApi.Trading.PlaceOrderAsync(
                                     symbol: symbol,
                                     side: sltpSide,
                                     type: FuturesOrderType.StopMarket,
                                     quantity: 1,
                                     stopPrice: SLstopPrice, // stop loss price
                                     orderResponseType: OrderResponseType.Result,
                                     positionSide: positionSide,
                                     workingType: WorkingType.Mark,
                                     timeInForce: TimeInForce.GoodTillCanceled,
                                     closePosition: true // add this
                                 );
            }

            return result;
        }
    }
}
