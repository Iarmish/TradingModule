﻿using ShortPeakRobot.Constants;
using ShortPeakRobot.ViewModel;

namespace ShortPeakRobot.Robots.Models
{
    public class CellHour : BaseVM
    {
        public int RobotIndex { get; set; }


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
                    RobotVM.robots[RobotIndex].BaseSettings.AllowedHours[Index] = value;
                }
            }
        }
    }
}
