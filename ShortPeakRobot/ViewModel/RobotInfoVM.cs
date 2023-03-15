using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ShortPeakRobot.ViewModel
{
    public class RobotInfoVM
    {
        public static ObservableCollection<RobotInfo> robotParams
        {
            get; set;
        } = new ObservableCollection<RobotInfo>();


        public RobotInfoVM()
        {

        }

        public static void ClearParams()
        {
            Dispatcher.CurrentDispatcher.Invoke(() => { robotParams.Clear(); });
            
        }

        public static void AddParam(string title, string value)
        {
            RobotInfoVM.robotParams.Add(new RobotInfo { Title = title, Value = value });
        }
    }
}
