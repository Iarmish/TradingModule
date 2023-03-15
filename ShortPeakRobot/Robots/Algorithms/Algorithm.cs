using Microsoft.EntityFrameworkCore;
using ShortPeakRobot.Data;
using ShortPeakRobot.Market.Models;
using static ShortPeakRobot.MainWindow;

namespace ShortPeakRobot.Robots.Algorithms
{
    public class Algorithm
    {          
        public object Algo;
        public int RobotId { get; set; }
        public string AlgorithmName { get; set; }

        public Algorithm(string algorithm, int robotId)
        {
            RobotId = robotId;
            AlgorithmName = algorithm;

            switch (algorithm)
            {
                case "ShortPeak": Algo = new ShortPeak(robotId); break;
                case "VWAPHL": Algo = new VWAPHL(robotId); break;
                case "LastDayHL": Algo = new LastDayHL(robotId); break;
            }
            
        }

       
    }
}
