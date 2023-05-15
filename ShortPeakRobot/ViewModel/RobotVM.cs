
using ShortPeakRobot.Constants;
using ShortPeakRobot.Market;
using ShortPeakRobot.Robots;
using ShortPeakRobot.Robots.Algorithms;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace ShortPeakRobot.ViewModel
{
    public class RobotVM : BaseVM
    {
        public static ObservableCollection<Robot> robots
        {
            get; set;
        } = new ObservableCollection<Robot>();

        public  RobotVM()
        {
            

            int cnt = 0;
            foreach (var robotIni in RobotsInitialization.Robots)
            {
                var robot = new Robot()
                {
                    Id = robotIni.Id,
                    Index = cnt,
                    Name = robotIni.Name,
                    Symbol = robotIni.Symbol,
                    BaseSettings = new BaseRobotSettings(robotIni.Id),
                    algorithm = new Algorithm(robotIni.AlgorithmName, robotIni.Id, cnt),
                    AlgorithmName = robotIni.AlgorithmName,                    
                };
                Task.Run(() => robot.RunSilentMode());

                

                robots.Add(robot);
                cnt++;
            }
        }
    }
}
