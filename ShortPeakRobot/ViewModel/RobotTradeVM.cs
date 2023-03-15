using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.ViewModel
{
    
        public class RobotTradeVM
        {
            public static ObservableCollection<RobotTrade> trades
            {
                get; set;
            } = new ObservableCollection<RobotTrade>();


            public RobotTradeVM()
            {

            }
            public static void AddRange(List<RobotTrade> orders)
            {
                orders.ForEach(order => RobotTradeVM.trades.Add(order));
            }
        }
    
}
