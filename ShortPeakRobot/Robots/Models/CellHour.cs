using ShortPeakRobot.Constants;
using ShortPeakRobot.ViewModel;

namespace ShortPeakRobot.Robots.Models
{
    public class CellHour : BaseVM
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
                    RobotVM.robots[RobotId].BaseSettings.AllowedHours[Index] = value;
                }
            }
        }
    }
}
