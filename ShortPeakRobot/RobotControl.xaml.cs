using Binance.Net.Enums;
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
        int RobotId { get; set; }
        public RobotControl(int robotId)
        {
            InitializeComponent();
            RobotId= robotId;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TBSignalBuyID.Text = RobotVM.robots[RobotId].RobotState.SignalBuyOrderId.ToString();
            TBSignalSellID.Text = RobotVM.robots[RobotId].RobotState.SignalSellOrderId.ToString();
            TBTakeProfitID.Text = RobotVM.robots[RobotId].RobotState.TakeProfitOrderId.ToString();
            TBStopLossID.Text = RobotVM.robots[RobotId].RobotState.StopLossOrderId.ToString();
            TBPosition.Text = RobotVM.robots[RobotId].RobotState.Position.ToString();
            TBOpenPositionPrice.Text = RobotVM.robots[RobotId].RobotState.OpenPositionPrice.ToString();
        }

        private void BtnSaveState_Click(object sender, RoutedEventArgs e)
        {
            RobotVM.robots[RobotId].RobotState.SignalBuyOrderId = long.Parse(TBSignalBuyID.Text); 
            RobotVM.robots[RobotId].RobotState.SignalSellOrderId = long.Parse(TBSignalSellID.Text);
            RobotVM.robots[RobotId].RobotState.TakeProfitOrderId = long.Parse(TBTakeProfitID.Text);
            RobotVM.robots[RobotId].RobotState.StopLossOrderId = long.Parse(TBStopLossID.Text);
            
            RobotVM.robots[RobotId].RobotState.Position = decimal.Parse(TBPosition.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
            RobotVM.robots[RobotId].Position = RobotVM.robots[RobotId].RobotState.Position;

            RobotVM.robots[RobotId].RobotState.OpenPositionPrice = decimal.Parse(TBOpenPositionPrice.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
                        
            MessageBox.Show("Изменения сохранены.");
        }

        private void BtnResetSLTP_Click(object sender, RoutedEventArgs e)
        {
            

            RobotVM.robots[RobotId].RobotState.SignalBuyOrderId = long.Parse(TBSignalBuyID.Text);
            RobotVM.robots[RobotId].RobotState.SignalSellOrderId = long.Parse(TBSignalSellID.Text);
            RobotVM.robots[RobotId].RobotState.TakeProfitOrderId = long.Parse(TBTakeProfitID.Text);
            RobotVM.robots[RobotId].RobotState.StopLossOrderId = long.Parse(TBStopLossID.Text);
            
            RobotVM.robots[RobotId].RobotState.Position = decimal.Parse(TBPosition.Text.Replace(',', '.'), CultureInfo.InvariantCulture);
            RobotVM.robots[RobotId].Position = RobotVM.robots[RobotId].RobotState.Position;

            RobotVM.robots[RobotId].RobotState.OpenPositionPrice = decimal.Parse(TBOpenPositionPrice.Text.Replace(',', '.'), CultureInfo.InvariantCulture);


            if (RobotVM.robots[RobotId].RobotState.Position == 0)
            {
                MessageBox.Show("Позиция не открыта!");
                return;
            }

            var side = OrderSide.Buy;
            if (RobotVM.robots[RobotId].RobotState.Position < 0)
            {
                side = OrderSide.Sell;
            }

            RobotVM.robots[RobotId].SetSLTP(side, Math.Abs(RobotVM.robots[RobotId].RobotState.Position),
                RobotVM.robots[RobotId].RobotState.OpenPositionPrice);
        }

        private void BtnCloseSignalBuy_Click(object sender, RoutedEventArgs e)
        {
            var orderId = Convert.ToInt64(TBSignalBuyID.Text);
            if (orderId == 0)
            {
                return;
            }
            RobotVM.robots[RobotId].CancelOrderByIdAsync(orderId, "Cancel SignalBuy Order");
            RobotVM.robots[RobotId].RobotState.SignalBuyOrderId = 0;

            TBSignalBuyID.Text = "0";
        }

        private void BtnCloseSignalSell_Click(object sender, RoutedEventArgs e)
        {
            var orderId = Convert.ToInt64(TBSignalSellID.Text);
            if (orderId == 0)
            {
                return;
            }
            RobotVM.robots[RobotId].CancelOrderByIdAsync(orderId, "Cancel SignalSell Order");
            RobotVM.robots[RobotId].RobotState.SignalSellOrderId = 0;

            TBSignalSellID.Text = "0";
        }

        private void BtnCloseStopLoss_Click(object sender, RoutedEventArgs e)
        {
            var orderId = Convert.ToInt64(TBStopLossID.Text);
            if (orderId == 0)
            {
                return;
            }
            RobotVM.robots[RobotId].CancelOrderByIdAsync(orderId, "Cancel StopLoss Order");

            RobotVM.robots[RobotId].RobotState.StopLossOrderId = 0;

            TBStopLossID.Text = "0";
        }

        private void BtnCloseTakeProfit_Click(object sender, RoutedEventArgs e)
        {
            var orderId = Convert.ToInt64(TBTakeProfitID.Text);
            if (orderId == 0)
            {
                return;
            }
            RobotVM.robots[RobotId].CancelOrderByIdAsync(orderId, "Cancel TakeProfit Order");

            RobotVM.robots[RobotId].RobotState.TakeProfitOrderId = 0;

            TBTakeProfitID.Text = "0";
        }
    }
}
