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
        public int RobotIndex { get; set; }
        public string AlgorithmName { get; set; }

        public Algorithm(string algorithm, int robotId, int robotIndex)
        {
            RobotId = robotId;
            RobotIndex = robotIndex;
            AlgorithmName = algorithm;

            switch (algorithm)
            {
                case "ShortPeak": Algo = new ShortPeak(robotId, robotIndex); break;
                case "VWAPHL": Algo = new VWAPHL(robotId, robotIndex); break;
                case "LastDayHL": Algo = new LastDayHL(robotId, robotIndex); break;
                case "LastDayHL10": Algo = new LastDayHL10(robotId, robotIndex); break;
                case "SL3": Algo = new SL3(robotId, robotIndex); break;
                case "ShortPeakPlusTime": Algo = new ShortPeakPlusTime(robotId, robotIndex); break;
            }
            
        }

       
    }
}
