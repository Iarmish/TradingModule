using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Constants
{

    public enum LogType
    {
        Error,
        Info,
        RobotState,
        UpdateOrder
    }

    public enum StateCase
    {
        Normal,
        FilledOneSignalOrder,
        FilledTwoSignalOrder,
        FilledOneSLPTOrder,
        FilledTwoSLPTOrder,
        PartiallyFilled,
        PlacedSignalOrders,
        PlacedSLTPOrders
    }

    public enum CandlesAnalyse
    {
        Required,
        NotRequired,
        BuySLTP,
        SellSLTP

    }

    public enum RobotCommands
    {
        Nothing,
        SetRobotInfo,
        CloseRobotPosition,
        ResetCandleAnalyse
    }

    public enum RobotOrderType
    {
        SignalBuy,
        SignalSell,
        StopLoss,
        TakeProfit,
        OrderId
    }

    public enum RobotRequestType
    {
        PlaceOrder,
        CancelOrder
    }

    public enum CandleInterval
    {
        OneSecond = 1,
        OneMinute = 60,
        FiveMinutes = 60 * 5,
        FifteenMinutes = 60 * 15,
        ThirtyMinutes = 60 * 30,
        OneHour = 60 * 60,
        FourHour = 60 * 60 * 4,
        OneDay = 60 * 60 * 24,
        OneWeek = 60 * 60 * 24 * 7,
        OneMonth = 60 * 60 * 24 * 30
    }

    //public enum Symbols
    //{
    //    ETHUSDT,
    //    BTCUSDT
    //}

    public enum RobotFutOrderType
    {
        Limit,
        Market,
        Stop,
        StopMarket,
        TakeProfit,
        TakeProfitMarket,
        TrailingStopMarket,
        Liquidation
    }



}
