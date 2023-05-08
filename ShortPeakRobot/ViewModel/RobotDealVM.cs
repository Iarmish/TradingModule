using ShortPeakRobot.Data;
using ShortPeakRobot.Robots.Algorithms.Models;
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
        public static ObservableCollection<RobotDealModel> deals
        {
            get; set;
        } = new ObservableCollection<RobotDealModel>();


        public RobotDealVM()
        {

        }
        public static void AddRange(List<RobotDealModel> deals)
        {
            deals.ForEach(deal => RobotDealVM.deals.Add(deal));
        }
    }
}
