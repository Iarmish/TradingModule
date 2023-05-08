using Microsoft.Win32;
using ShortPeakRobot.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace ShortPeakRobot.Robots
{
    [Serializable]
    public class BaseRobotSettings : BaseVM
    {
        public int RobotId { get; set; }
        public int _TimeFrame { get; set; }
        public int TimeFrame
        {
            get { return _TimeFrame; }
            set
            {
                if (_TimeFrame != value)
                {
                    _TimeFrame = value;
                    OnPropertyChanged("TimeFrame");
                }
            }
        }


        public string _Simbol { get; set; }
        public string Simbol
        {
            get { return _Simbol; }
            set
            {
                if (_Simbol != value)
                {
                    _Simbol = value;
                    OnPropertyChanged("Simbol");
                    RobotVM.robots[RobotServices.GetRobotIndex( RobotId)].Symbol = _Simbol;
                }
            }
        }

        public decimal _StopLoss { get; set; }
        public decimal StopLoss
        {
            get { return _StopLoss; }
            set
            {
                if (_StopLoss != value)
                {
                    _StopLoss = value;
                    OnPropertyChanged("StopLoss");
                }
            }
        }
        
        public decimal _TakeProfit { get; set; }
        public decimal TakeProfit
        {
            get { return _TakeProfit; }
            set
            {
                if (_TakeProfit != value)
                {
                    _TakeProfit = value;
                    OnPropertyChanged("TakeProfit");
                }
            }
        }

        
        public decimal _StopLossPercent { get; set; }
        public decimal StopLossPercent
        {
            get { return _StopLossPercent; }
            set
            {
                if (_StopLossPercent != value)
                {
                    _StopLossPercent = value;
                    OnPropertyChanged("StopLossPercent");
                }
            }
        }
        
        public decimal _TakeProfitPercent { get; set; }
        public decimal TakeProfitPercent
        {
            get { return _TakeProfitPercent; }
            set
            {
                if (_TakeProfitPercent != value)
                {
                    _TakeProfitPercent = value;
                    OnPropertyChanged("TakeProfitPercent");
                }
            }
        }


        public decimal _Offset { get; set; }
        public decimal Offset
        {
            get { return _Offset; }
            set
            {
                if (_Offset != value)
                {
                    _Offset = value;
                    OnPropertyChanged("Offset");
                }
            }
        }

        public decimal _OffsetPercent { get; set; }
        public decimal OffsetPercent
        {
            get { return _OffsetPercent; }
            set
            {
                if (_OffsetPercent != value)
                {
                    _OffsetPercent = value;
                    OnPropertyChanged("OffsetPercent");
                }
            }
        }

        public decimal _Slip { get; set; }
        public decimal Slip
        {
            get { return _Slip; }
            set
            {
                if (_Slip != value)
                {
                    _Slip = value;
                    OnPropertyChanged("Slip");
                }
            }
        }


        public bool _AllowSell { get; set; }
        public bool AllowSell
        {
            get { return _AllowSell; }
            set
            {
                if (_AllowSell != value)
                {
                    _AllowSell = value;
                    OnPropertyChanged("AllowSell");
                }
            }
        }


        public bool _AllowBuy { get; set; }
        public bool AllowBuy
        {
            get { return _AllowBuy; }
            set
            {
                if (_AllowBuy != value)
                {
                    _AllowBuy = value;
                    OnPropertyChanged("AllowBuy");
                }
            }
        }


        public bool _Revers { get; set; }
        public bool Revers
        {
            get { return _Revers; }
            set
            {
                if (_Revers != value)
                {
                    _Revers = value;
                    OnPropertyChanged("Revers");
                }
            }
        }


        public List<bool> AllowedHours { get; set; }
        public List<bool> AllowedDayMonth { get; set; }
        public List<bool> AllowedDayWeek { get; set; }
        public bool IsActivated { get; set; }

        //public decimal CurrentLot { get; set; } 
        //public decimal VariableVolume { get; set; } 
        public decimal _Volume { get; set; } 
        public decimal Volume
        {
            get { return _Volume; }
            set
            {
                if (_Volume != value)
                {
                    _Volume = value;
                    OnPropertyChanged("Volume");
                }
            }
        }
        public decimal _Deposit { get; set; } 
        public decimal Deposit
        {
            get { return _Deposit; }
            set
            {
                if (_Deposit != value)
                {
                    _Deposit = value;
                    OnPropertyChanged("Deposit");
                }
            }
        }

        public decimal _CurrentDeposit { get; set; } 
        public decimal CurrentDeposit
        {
            get { return _CurrentDeposit; }
            set
            {
                if (_CurrentDeposit != value)
                {
                    _CurrentDeposit = value;
                    OnPropertyChanged("CurrentDeposit");
                }
            }
        }

        public bool _IsVariableLot { get; set; }
        public bool IsVariableLot
        {
            get { return _IsVariableLot; }
            set
            {
                if (_IsVariableLot != value)
                {
                    _IsVariableLot = value;
                    OnPropertyChanged("IsVariableLot");

                }
            }
        }

        public bool _SLPercent { get; set; }
        public bool SLPercent
        {
            get { return _SLPercent; }
            set
            {
                if (_SLPercent != value)
                {
                    _SLPercent = value;
                    OnPropertyChanged("SLPercent");

                }
            }
        }
        
        public bool _TPPercent { get; set; }
        public bool TPPercent
        {
            get { return _TPPercent; }
            set
            {
                if (_TPPercent != value)
                {
                    _TPPercent = value;
                    OnPropertyChanged("TPPercent");

                }
            }
        }
         public bool _IsOffsetPercent { get; set; }
        public bool IsOffsetPercent
        {
            get { return _IsOffsetPercent; }
            set
            {
                if (_IsOffsetPercent != value)
                {
                    _IsOffsetPercent = value;
                    OnPropertyChanged("IsOffsetPercent");

                }
            }
        }


        public decimal Profit { get; set; }
        

        public BaseRobotSettings(int robotId)
        {
            AllowedHours = new List<bool>();
            AllowedDayMonth = new List<bool>();
            AllowedDayWeek = new List<bool>();

            for (int i = 0; i < 24; i++)
            {
                AllowedHours.Add(true);
            }

            for (int i = 0; i < 31; i++)
            {
                AllowedDayMonth.Add(true);
            }

            for (int i = 0; i < 35; i++)
            {
                AllowedDayWeek.Add(true);
            }
            RobotId = robotId;
        }


        public async void SaveSettings(int robotId, BaseRobotSettings baseRobotSettings)
        {
            string fileName = robotId + "/" + robotId + ".json";

            if (!Directory.Exists(robotId.ToString()))
            {
                Directory.CreateDirectory(robotId.ToString());
            }


            using (FileStream fs = new FileStream(fileName, FileMode.Create))
            {
                await JsonSerializer.SerializeAsync(fs, baseRobotSettings);
                //MessageBox.Show("Конфигурация сохранена в файл " + fileName);
            }
        }


        public async void SaveSettingsToFile(int robotId, BaseRobotSettings baseRobotSettings)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text files(*.json)|*.json|All files(*.*)|*.*";

            if (saveFileDialog.ShowDialog() == true)
            {
                string fileName = saveFileDialog.FileName;

                if (!Directory.Exists(robotId.ToString()))
                {
                    Directory.CreateDirectory(robotId.ToString());
                }


                using (FileStream fs = new FileStream(fileName, FileMode.Create))
                {
                    await JsonSerializer.SerializeAsync(fs, baseRobotSettings);
                    //MessageBox.Show("Конфигурация сохранена в файл " + fileName);
                }
            }
           


        }

        public async void LoadSettings(int robotIndex)
        {
            var robotId = RobotServices.GetRobotId(robotIndex);
            var fileName = robotId + "/" + robotId + ".json";
            if (Directory.Exists(robotId.ToString()) && File.Exists(fileName))
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open))
                {
                    var settings = await JsonSerializer.DeserializeAsync<BaseRobotSettings>(fs);

                    if (settings != null)
                    {
                        RobotVM.robots[robotIndex].BaseSettings = settings;
                        RobotVM.robots[robotIndex].IsActivated = settings.IsActivated;
                    }
                }
            }
        }

        public async void LoadSettingsFromFile(int robotIndex)
        {
            var robotId = RobotServices.GetRobotId(robotIndex);
            OpenFileDialog dialog = new OpenFileDialog()
            {
                CheckFileExists = false,
                CheckPathExists = true,
                Multiselect = false,
                Title = "Выберите файл"
            };
            if (dialog.ShowDialog() == true)
            {
                using (FileStream fs = new FileStream(dialog.FileName, FileMode.Open))
                {


                    var settings = await JsonSerializer.DeserializeAsync<BaseRobotSettings>(fs);

                    if (settings != null)
                    {
                        if (settings.RobotId != robotId)
                        {
                            MessageBox.Show("Id робота не соответствует загружаемым настройкам");
                            return;
                        }
                        RobotVM.robots[robotIndex].BaseSettings = settings;
                        DataProcessor.SetCellsVM(robotIndex);
                    }

                }
            }


        }

    }
}
