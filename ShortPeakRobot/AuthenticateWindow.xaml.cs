using ShortPeakRobot.API;
using ShortPeakRobot.Market;
using ShortPeakRobot.Market.Models.ApiModels;
using ShortPeakRobot.Migrations;
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
    /// Interaction logic for AuthenticateWindow.xaml
    /// </summary>
    public partial class AuthenticateWindow : Window
    {
        IniManager ini = new();

        public AuthenticateWindow()
        {
            InitializeComponent();
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            BtnLogin.IsEnabled = false;
            LoginResponse loginResponse = await ApiServices.Login(TbLogin.Text, TbPass.Text);

            if (loginResponse.success)
            {
                ApiServices.SetTokens(loginResponse);
                MarketData.Info.ClientId = loginResponse.data.client_id;                

                
                AuthWindow.Close();
            }
            else
            {
                TbAuthMessage.Text = loginResponse.message;
                BtnLogin.IsEnabled = true;
            }
            

        }

        private void AuthWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var login = ini.GetPrivateString("login", "value");
            var password = ini.GetPrivateString("password", "value");

            TbLogin.Text = login;
            TbPass.Text = password;
        }
    }
}
