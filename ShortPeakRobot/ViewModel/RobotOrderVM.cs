using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.ViewModel
{
    public class RobotOrderVM
    {
        public static ObservableCollection<RobotOrder> orders
        {
            get; set;
        } = new ObservableCollection<RobotOrder>();


        public RobotOrderVM()
        {

        }
        public static void AddRange(List<RobotOrder> orders)
        {
            
            orders.ForEach(order => RobotOrderVM.orders.Add(order));
        }
    }
}
