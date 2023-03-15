using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Models
{
    public class RobotBaseModel// для инициализации роботов
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }        
        public string AlgorithmName { get; set; }        
    }
}
