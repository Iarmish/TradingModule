using Binance.Net.Enums;
using CryptoExchange.Net.Objects;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ShortPeakRobot
{
    /// <summary>
    /// Логика взаимодействия для RobotControl.xaml
    /// </summary>
    public partial class RobotControl : Window
    {
        int RobotIindex { get; set; }
        public RobotControl(int robotIndex)
        {
            InitializeComponent();
            RobotIindex = robotIndex;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TBSignalBuyID.Text = RobotVM.robots[RobotIindex].RobotState.SignalBuyOrderId.ToString();
            TBSignalSellID.Text = RobotVM.robots[RobotIindex].RobotState.SignalSellOrderId.ToString();
            TBTakeProfitID.Text = RobotVM.robots[RobotIindex].RobotState.TakeProfitOrderId.ToString();
            TBStopLossID.Text = RobotVM.robots[RobotIindex].RobotState.StopLossOrderId.ToString();
            TBPosition.Text = RobotVM.robots[RobotIindex].RobotState.Position.ToString();
            TBOpenPositionPrice.Text = RobotVM.robots[RobotIindex].RobotState.OpenPositionPrice.ToString();
        }

        private void BtnSaveState_Click(object sender, RoutedEventArgs e)
        {
            var robot = RobotVM.robots[RobotIindex];

            if (!long.TryParse(TBSignalBuyID.Text, out var signalBuy))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!long.TryParse(TBSignalSellID.Text, out var signalSell))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!long.TryParse(TBTakeProfitID.Text, out var takeProfit))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!long.TryParse(TBStopLossID.Text, out var stopLoss))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!decimal.TryParse(TBPosition.Text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var position))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!decimal.TryParse(TBOpenPositionPrice.Text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var positionPrice))
            { MessageBox.Show("Введено не корректное значение! "); return; }


            robot.RobotState.SignalBuyOrderId = signalBuy;
            robot.RobotState.SignalSellOrderId = signalSell;

            robot.RobotState.TakeProfitOrderId = takeProfit;
            robot.RobotState.StopLossOrderId = stopLoss;

            robot.RobotState.Position = position;
            robot.Position = robot.RobotState.Position;

            robot.RobotState.OpenPositionPrice = positionPrice;
            robot.OpenPositionPrice = positionPrice;

            MessageBox.Show("Изменения сохранены.");
        }

        private async void BtnResetSLTP_Click(object sender, RoutedEventArgs e)
        {
            var robot = RobotVM.robots[RobotIindex];

            if (!long.TryParse(TBSignalBuyID.Text, out var signalBuy))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!long.TryParse(TBSignalSellID.Text, out var signalSell))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!long.TryParse(TBTakeProfitID.Text, out var takeProfit))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!long.TryParse(TBStopLossID.Text, out var stopLoss))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!decimal.TryParse(TBPosition.Text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var position))
            { MessageBox.Show("Введено не корректное значение! "); return; }

            if (!decimal.TryParse(TBOpenPositionPrice.Text.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var positionPrice))
            { MessageBox.Show("Введено не корректное значение! "); return; }


            robot.RobotState.SignalBuyOrderId = signalBuy;
            robot.RobotState.SignalSellOrderId = signalSell;

            robot.RobotState.TakeProfitOrderId = takeProfit;
            robot.RobotState.StopLossOrderId = stopLoss;

            robot.RobotState.Position = position;
            robot.Position = robot.RobotState.Position;

            robot.RobotState.OpenPositionPrice = positionPrice;

            await robot.SetRobotData();//синхронизируем данные по текущим ордера с биржей


            long signalOrderId = 0;
            OrderSide side = new();

            if (robot.RobotState.Position > 0)
            {
                if (robot.SignalBuyOrder.OrderId != 0)
                {
                    signalOrderId = robot.SignalBuyOrder.OrderId;
                }
                else
                {
                    signalOrderId = robot.RobotState.SignalBuyOrderId;
                }
                side = OrderSide.Buy;
            }

            if (robot.RobotState.Position < 0)
            {
                if (robot.SignalSellOrder.OrderId != 0)
                {
                    signalOrderId = robot.SignalSellOrder.OrderId;
                }
                else
                {
                    signalOrderId = robot.RobotState.SignalSellOrderId;
                }
                side = OrderSide.Sell;
            }

            var signalOrder = await RobotServices.GetBinOrderById(signalOrderId, RobotServices.GetRobotIndex(RobotIindex));
            //-----
            if (signalOrder.OrderId == 0)
            {
                MarketData.Info.Message += "Не найден ордер id" + signalOrder.OrderId + "\n";
                MarketData.Info.IsMessageActive = true;
               
                return;
            }
            else
            {
                if (side == OrderSide.Buy)
                {
                    robot.SignalBuyOrder = signalOrder;
                }
                else
                {
                    robot.SignalSellOrder = signalOrder;
                }
            }


            robot.SetSLTP(side, Math.Abs(robot.RobotState.Position), robot.RobotState.OpenPositionPrice, signalOrderId);
        }

        private void BtnCloseSignalBuy_Click(object sender, RoutedEventArgs e)
        {
            var orderId = Convert.ToInt64(TBSignalBuyID.Text);
            if (orderId == 0)
            {
                return;
            }
            RobotVM.robots[RobotIindex].CancelOrderByIdAsync(orderId, "Cancel SignalBuy Order");
            RobotVM.robots[RobotIindex].RobotState.SignalBuyOrderId = 0;

            TBSignalBuyID.Text = "0";
        }

        private void BtnCloseSignalSell_Click(object sender, RoutedEventArgs e)
        {
            var orderId = Convert.ToInt64(TBSignalSellID.Text);
            if (orderId == 0)
            {
                return;
            }
            RobotVM.robots[RobotIindex].CancelOrderByIdAsync(orderId, "Cancel SignalSell Order");
            RobotVM.robots[RobotIindex].RobotState.SignalSellOrderId = 0;

            TBSignalSellID.Text = "0";
        }

        private void BtnCloseStopLoss_Click(object sender, RoutedEventArgs e)
        {
            var orderId = Convert.ToInt64(TBStopLossID.Text);
            if (orderId == 0)
            {
                return;
            }
            RobotVM.robots[RobotIindex].CancelOrderByIdAsync(orderId, "Cancel StopLoss Order");

            RobotVM.robots[RobotIindex].RobotState.StopLossOrderId = 0;

            TBStopLossID.Text = "0";
        }

        private void BtnCloseTakeProfit_Click(object sender, RoutedEventArgs e)
        {
            var orderId = Convert.ToInt64(TBTakeProfitID.Text);
            if (orderId == 0)
            {
                return;
            }
            RobotVM.robots[RobotIindex].CancelOrderByIdAsync(orderId, "Cancel TakeProfit Order");

            RobotVM.robots[RobotIindex].RobotState.TakeProfitOrderId = 0;

            TBTakeProfitID.Text = "0";
        }
    }
}
