using ShortPeakRobot.Robots;
using ShortPeakRobot.Robots.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.ViewModel
{
    public class CellDayMonthVM
    {
        public static ObservableCollection<CellDayMonth> cells
        {
            get; set;
        } = new ObservableCollection<CellDayMonth>();


        public CellDayMonthVM()
        {
           
        }
    }
}
