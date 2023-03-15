using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPeakRobot.Robots.Models
{
    public class CellDayMonth : BaseVM
    {
        
        public int RobotId { get; set; }


        private int _Index;
        public int Index { get; set; }

        private bool _State;
        public bool State
        {
            get { return _State; }
            set
            {
                if (_State != value)
                {
                    _State = value;
                    OnPropertyChanged("State");
                    RobotVM.robots[RobotId].BaseSettings.AllowedDayMonth[Index - 1] = value;
                }
            }
        }
    }
}
