using ShortPeakRobot.Robots.Models;
using System.Collections.ObjectModel;

namespace ShortPeakRobot.ViewModel
{
    public class CellHourVM
    {
        public static ObservableCollection<CellHour> cells
        {
            get; set;
        } = new ObservableCollection<CellHour>();


        public CellHourVM()
        {

        }
    }
}
