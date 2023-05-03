
using Newtonsoft.Json.Linq;
using ShortPeakRobot.Robots.Models;
using ShortPeakRobot.ViewModel;
using System.Windows.Media;

namespace ShortPeakRobot.Robots
{
    public static class DataProcessor
    {
        public static void SetCellsVM(int robotIndex)
        {
            //var robotId = RobotServices.GetRobotId(robotIndex);
            CellDayMonthVM.cells.Clear();
            for (int i = 1; i <= 31; i++)
            {
                CellDayMonthVM.cells.Add(new CellDayMonth() { Index = i, RobotIndex = robotIndex, State = RobotVM.robots[robotIndex].BaseSettings.AllowedDayMonth[i - 1] });

            }

            CellDayWeekVM.cells.Clear();
            for (int i = 1; i <= 35; i++)
            {
                CellDayWeekVM.cells.Add(new CellDayWeek() { Index = i, RobotIndex = robotIndex, State = RobotVM.robots[robotIndex].BaseSettings.AllowedDayWeek[i - 1] });

            }

            CellHourVM.cells.Clear();
            for (int i = 0; i < 24; i++)
            {
                CellHourVM.cells.Add(new CellHour() { Index = i, RobotIndex = robotIndex, State = RobotVM.robots[robotIndex].BaseSettings.AllowedHours[i] });

            }
            
        }


        public static SolidColorBrush Convert(bool state)
        {
            if (!state)
            {
                return Brushes.LightSalmon;
            }
            else
            {
                return Brushes.LightGreen;
            }
        }
    }
}
