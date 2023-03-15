using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.ViewModel
{
    public class LogVM
    {
        public static ObservableCollection<RobotLog> logs
        {
            get; set;
        } = new ObservableCollection<RobotLog>();


        public LogVM()
        {

        }

        public static void AddRange(List<RobotLog> logs)
        {
            logs.ForEach(log => LogVM.logs.Add(log));
        }
    }
}
