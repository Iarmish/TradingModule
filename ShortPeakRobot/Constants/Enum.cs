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

    //public enum Symbols
    //{
    //    ETHUSDT,
    //    BTCUSDT
    //}





}
