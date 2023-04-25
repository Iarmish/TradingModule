
using ShortPeakRobot.Constants;
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


        public RobotVM()
        {
            int cnt = 0;
            foreach (var robotIni in RobotsInitialization.dictionary)
            {
                var robot = new Robot()
                {
                    Id = robotIni.Value.Id,
                    Index = cnt,
                    Name = robotIni.Value.Name,
                    Symbol = robotIni.Value.Symbol,
                    BaseSettings = new BaseRobotSettings(robotIni.Value.Id),
                    algorithm = new Algorithm(robotIni.Value.AlgorithmName, robotIni.Value.Id),
                    AlgorithmName = robotIni.Value.AlgorithmName
                };
                Task.Run(() => robot.RunSilentMode());

                robots.Add(robot);
                cnt++;
            }
        }
    }
}
