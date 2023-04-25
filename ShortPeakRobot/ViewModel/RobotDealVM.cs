using ShortPeakRobot.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.ViewModel
{
    public class RobotDealVM
    {
        public static ObservableCollection<RobotDeal> deals
        {
            get; set;
        } = new ObservableCollection<RobotDeal>();


        public RobotDealVM()
        {

        }
        public static void AddRange(List<RobotDeal> deals)
        {
            deals.ForEach(deal => RobotDealVM.deals.Add(deal));
        }
    }
}
