using ShortPeakRobot.Constants;
using ShortPeakRobot.ViewModel;

namespace ShortPeakRobot.Robots.Models
{
    public class CellDayWeek : BaseVM
    {
        public int RobotIndex { get; set; }        
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
                    RobotVM.robots[RobotIndex].BaseSettings.AllowedDayWeek[Index] = value;
                }
            }
        }
    }
}
