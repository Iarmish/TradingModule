using ShortPeakRobot.Robots;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
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
    /// Логика взаимодействия для RobotListWindow.xaml
    /// </summary>
    public partial class RobotListWindow : Window
    {
        public RobotListWindow()
        {
            InitializeComponent();
        }

        public void Draw_map_robot()
        {
            int x_ind = 0, y_ind = 0, width = 80, height = 20;
            foreach (var robot in RobotVM.robots)
                        {
                TextBlock text_robo = new TextBlock();
                text_robo.Text = robot.Name;
                text_robo.HorizontalAlignment = HorizontalAlignment.Left;
                text_robo.VerticalAlignment = VerticalAlignment.Top;
                text_robo.ToolTip = robot.Index.ToString();
                text_robo.MouseDown += Text_robo_MouseDown;
                if (robot.IsActivated)
                    text_robo.Background = Brushes.LightGreen;
                else
                    text_robo.Background = Brushes.LightSalmon;

                text_robo.Margin = new Thickness(x_ind * (width + 5), y_ind * (height + 5), 0, 0);
                text_robo.Height = height; text_robo.Width = width;
                text_robo.Padding = new Thickness(2);
                RobotList.Children.Add(text_robo);
                x_ind++;
                if (x_ind > 8)
                {
                    x_ind = 0;
                    y_ind++;
                }
            }
        }

        private void Text_robo_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (((TextBlock)sender).Background == Brushes.LightGreen)
            {
                ((TextBlock)sender).Background = Brushes.LightSalmon;
                RobotVM.robots[Convert.ToInt32(((TextBlock)sender).ToolTip)].IsActivated = false;
                RobotVM.robots[Convert.ToInt32(((TextBlock)sender).ToolTip)].BaseSettings.IsActivated = false;
            }
            else
            {
                ((TextBlock)sender).Background = Brushes.LightGreen;
                RobotVM.robots[Convert.ToInt32(((TextBlock)sender).ToolTip)].IsActivated = true;
                RobotVM.robots[Convert.ToInt32(((TextBlock)sender).ToolTip)].BaseSettings.IsActivated = true;
            }
        }
    }
}
