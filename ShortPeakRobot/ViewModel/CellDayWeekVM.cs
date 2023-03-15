using ShortPeakRobot.Robots.Models;
using System.Collections.ObjectModel;

namespace ShortPeakRobot.ViewModel
{
    public class CellDayWeekVM
    {
        public static ObservableCollection<CellDayWeek> cells
        {
            get; set;
        } = new ObservableCollection<CellDayWeek>();


        public CellDayWeekVM()
        {

        }
    }
}
