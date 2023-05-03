using ShortPeakRobot.Market.Models;
using ShortPeakRobot.Robots.Algorithms.Models.ShortPeakModels.SL3;
using System;

namespace ShortPeakRobot.Robots.Algorithms.Services
{
    public static class SL3Services
    {
        public static void SetHighDataPeaksSL3(PeakDataSL3 highData, decimal newHighPeak, decimal newLowPeak, decimal TP, DateTime date)
        {
            if (newHighPeak != 0)
            {
                if (highData.OppositePeak == 0)
                {
                    if (highData.FirstPeak != 0 && highData.SecondPeak != 0 && highData.ThirdPeak != 0)
                    {
                        highData.FirstPeak = highData.SecondPeak;
                        highData.SecondPeak = highData.ThirdPeak;
                        highData.ThirdPeak = newHighPeak;

                        highData.FirstPeakDate = highData.SecondPeakDate;
                        highData.SecondPeakDate = highData.ThirdPeakDate;
                        highData.ThirdPeakDate = date;
                        return ;
                    }
                    if (highData.FirstPeak != 0 && highData.SecondPeak != 0 && highData.ThirdPeak == 0)
                    {
                        highData.ThirdPeak = newHighPeak;
                        highData.ThirdPeakDate = date;
                        return ;
                    }
                    if (highData.FirstPeak != 0 && highData.SecondPeak == 0)
                    {
                        highData.SecondPeak = newHighPeak;
                        highData.SecondPeakDate = date;
                        return ;
                    }
                    if (highData.FirstPeak == 0)
                    {
                        highData.FirstPeak = newHighPeak;
                        highData.FirstPeakDate = date;
                        return ;
                    }
                }
            }

            if (newLowPeak != 0)
            {
                if (highData.ThirdPeak != 0 && newLowPeak < highData.ThirdPeak - TP)
                {
                    highData.OppositePeak = newLowPeak;
                    highData.OppositePeakDate = date;
                }
            }
            return ;
        }



        public static void SetLowDataPeaksSL3( PeakDataSL3 lowData, decimal newHighPeak, decimal newLowPeak, decimal TP, DateTime date)
        {
            if (newLowPeak != 0)
            {
                if (lowData.OppositePeak == 0)
                {
                    if (lowData.FirstPeak != 0 && lowData.SecondPeak != 0 && lowData.ThirdPeak != 0)
                    {
                        lowData.FirstPeak = lowData.SecondPeak;
                        lowData.SecondPeak = lowData.ThirdPeak;
                        lowData.ThirdPeak = newLowPeak;

                        lowData.FirstPeakDate = lowData.SecondPeakDate;
                        lowData.SecondPeakDate = lowData.ThirdPeakDate;
                        lowData.ThirdPeakDate = date;
                        return ;
                    }
                    if (lowData.FirstPeak != 0 && lowData.SecondPeak != 0 && lowData.ThirdPeak == 0)
                    {
                        lowData.ThirdPeak = newLowPeak;
                        lowData.ThirdPeakDate = date;
                        return ;
                    }
                    if (lowData.FirstPeak != 0 && lowData.SecondPeak == 0)
                    {
                        lowData.SecondPeak = newLowPeak;
                        lowData.SecondPeakDate = date;
                        return ;
                    }
                    if (lowData.FirstPeak == 0)
                    {
                        lowData.FirstPeak = newLowPeak;
                        lowData.FirstPeakDate = date;
                        return ;
                    }
                }

            }

            if (newHighPeak != 0)
            {
                if (lowData.ThirdPeak != 0 && newHighPeak > lowData.ThirdPeak + TP)
                {
                    lowData.OppositePeak = newHighPeak;
                    lowData.OppositePeakDate = date;
                }
            }

            return ;
        }

        //------------------------------------------------------

        public static void CheckCrossHighDataPeaksSL3(PeakDataSL3 highData, LevelsSL3 highLevels, Candle candle)
        {
            if (highData.ThirdPeak != 0 && candle.HighPrice > highData.ThirdPeak)
            {
                highData.FirstPeak = 0;
                highData.SecondPeak = 0;
                highData.ThirdPeak = 0;
                highData.OppositePeak =0;
                return;
            }
            if (highData.SecondPeak != 0 && candle.HighPrice > highData.SecondPeak)
            {
                highData.FirstPeak = 0;
                highData.SecondPeak = 0;
                highData.ThirdPeak = 0;
                highData.OppositePeak = 0;
                return;
            }
            if (highData.FirstPeak != 0 && candle.HighPrice > highData.FirstPeak)
            {
                highData.FirstPeak = 0;
                highData.SecondPeak = 0;
                highData.ThirdPeak = 0;
                highData.OppositePeak = 0;
                return;
            }
            //------
            if (highData.OppositePeak != 0 && candle.LowPrice < highData.OppositePeak)
            {
                highLevels.FirstLevel = highData.FirstPeak;
                highLevels.SecondLevel = highData.SecondPeak;
                highLevels.ThirdLevel = highData.ThirdPeak;
                highLevels.FirstLevelDate = highData.FirstPeakDate;
                highLevels.SecondLevelDate = highData.SecondPeakDate;
                highLevels.ThirdLevelDate = highData.ThirdPeakDate;

                highLevels.CurrentLevel = highData.ThirdPeak;
                highLevels.CurrentLevelDate = highData.ThirdPeakDate;
                //
                highData.FirstPeak = 0;
                highData.SecondPeak = 0;
                highData.ThirdPeak = 0;
                highData.OppositePeak = 0;
            }
        }

        public static void CheckCrossLowDataPeaksSL3(PeakDataSL3 lowData, LevelsSL3 lowLevels, Candle candle)
        {
            if (lowData.ThirdPeak != 0 && candle.LowPrice < lowData.ThirdPeak)
            {
                lowData.FirstPeak = 0;
                lowData.SecondPeak = 0;
                lowData.ThirdPeak = 0;
                lowData.OppositePeak = 0;
                return;
            }
            if (lowData.SecondPeak != 0 && candle.LowPrice < lowData.SecondPeak)
            {
                lowData.FirstPeak = 0;
                lowData.SecondPeak = 0;
                lowData.ThirdPeak = 0;
                lowData.OppositePeak = 0;
                return;
            }
            if (lowData.FirstPeak != 0 && candle.LowPrice < lowData.FirstPeak)
            {
                lowData.FirstPeak = 0;
                lowData.SecondPeak = 0;
                lowData.ThirdPeak = 0;
                lowData.OppositePeak = 0;
                return;
            }
            //------
            if (lowData.OppositePeak != 0 && candle.HighPrice > lowData.OppositePeak)
            {
                lowLevels.FirstLevel = lowData.FirstPeak;
                lowLevels.SecondLevel = lowData.SecondPeak;
                lowLevels.ThirdLevel = lowData.ThirdPeak;
                lowLevels.FirstLevelDate = lowData.FirstPeakDate;
                lowLevels.SecondLevelDate = lowData.SecondPeakDate;
                lowLevels.ThirdLevelDate = lowData.ThirdPeakDate;

                lowLevels.CurrentLevel = lowData.ThirdPeak;
                lowLevels.CurrentLevelDate = lowData.ThirdPeakDate;

                lowData.FirstPeak = 0;
                lowData.SecondPeak = 0;
                lowData.ThirdPeak = 0;
                lowData.OppositePeak = 0;
            }
        }
        //----------------------------------------------------------
        public static void GetCurrentHighLevelSL3(LevelsSL3 highLevels)
        {
            if (highLevels.ThirdLevel != 0)
            {
                highLevels.CurrentLevel = highLevels.ThirdLevel;
                highLevels.CurrentLevelDate = highLevels.ThirdLevelDate;
                return;
            }
            if (highLevels.SecondLevel != 0)
            {
                highLevels.CurrentLevel = highLevels.SecondLevel;
                highLevels.CurrentLevelDate = highLevels.SecondLevelDate;
                return;
            }
            if (highLevels.FirstLevel != 0)
            {
                highLevels.CurrentLevel = highLevels.FirstLevel;
                highLevels.CurrentLevelDate = highLevels.FirstLevelDate;
                return;
            }

            highLevels.CurrentLevel = 0;
        }


        public static void GetCurrentLowLevelSL3(LevelsSL3 lowLevels)
        {
            if (lowLevels.ThirdLevel != 0)
            {
                lowLevels.CurrentLevel = lowLevels.ThirdLevel;
                lowLevels.CurrentLevelDate = lowLevels.ThirdLevelDate;
                return;
            }
            if (lowLevels.SecondLevel != 0)
            {
                lowLevels.CurrentLevel = lowLevels.SecondLevel;
                lowLevels.CurrentLevelDate = lowLevels.SecondLevelDate;
                return;
            }
            if (lowLevels.FirstLevel != 0)
            {
                lowLevels.CurrentLevel = lowLevels.FirstLevel;
                lowLevels.CurrentLevelDate = lowLevels.FirstLevelDate;
                return;
            }

            lowLevels.CurrentLevel = 0;
        }
        //-----------------------------------------------
        public static void CheckCrossHighLevelsSL3(LevelsSL3 highLevels, decimal currenPrice, decimal TP)
        {
            if (currenPrice > highLevels.ThirdLevel - TP)
            {
                highLevels.ThirdLevel = 0;
                highLevels.ThirdLevelDate = new DateTime();
            }
            if (currenPrice > highLevels.SecondLevel - TP)
            {
                highLevels.SecondLevel = 0;
                highLevels.SecondLevelDate = new DateTime();
            }
            if (currenPrice > highLevels.FirstLevel - TP)
            {
                highLevels.FirstLevel = 0;
                highLevels.FirstLevelDate = new DateTime();
            }
        }

        public static void CheckCrossLowLevelsSL3(LevelsSL3 lowLevels, decimal currenPrice, decimal TP)
        {
            if (currenPrice < lowLevels.ThirdLevel + TP)
            {
                lowLevels.ThirdLevel = 0;
                lowLevels.ThirdLevelDate = new DateTime();
            }
            if (currenPrice < lowLevels.SecondLevel + TP)
            {
                lowLevels.SecondLevel = 0;
                lowLevels.SecondLevelDate = new DateTime();
            }
            if (currenPrice < lowLevels.FirstLevel + TP)
            {
                lowLevels.FirstLevel = 0;
                lowLevels.FirstLevelDate = new DateTime();
            }
        }

        //------------------------
        public static void CheckLiveTimeLevelsSL3( LevelsSL3 lowLevels,  LevelsSL3 highLevels, DateTime currentDate, int LiveTimeCandleCnt)
        {
            if (highLevels.FirstLevelDate.AddDays(LiveTimeCandleCnt) < currentDate)
            {
                highLevels.FirstLevel = 0;
                highLevels.FirstLevelDate = new DateTime();
            }
            if (highLevels.SecondLevelDate.AddDays(LiveTimeCandleCnt) < currentDate)
            {
                highLevels.SecondLevel = 0;
                highLevels.SecondLevelDate = new DateTime();
            }
            if (highLevels.ThirdLevelDate.AddDays(LiveTimeCandleCnt) < currentDate)
            {
                highLevels.ThirdLevel = 0;
                highLevels.ThirdLevelDate = new DateTime();
            }
            if (highLevels.CurrentLevelDate.AddDays(LiveTimeCandleCnt) < currentDate)
            {
                highLevels.CurrentLevel = 0;
                highLevels.CurrentLevelDate = new DateTime();
            }
            //-----------
            if (lowLevels.FirstLevelDate.AddDays(LiveTimeCandleCnt) < currentDate)
            {
                lowLevels.FirstLevel = 0;
                lowLevels.FirstLevelDate = new DateTime();
            }
            if (lowLevels.SecondLevelDate.AddDays(LiveTimeCandleCnt) < currentDate)
            {
                lowLevels.SecondLevel = 0;
                lowLevels.SecondLevelDate = new DateTime();
            }
            if (lowLevels.ThirdLevelDate.AddDays(LiveTimeCandleCnt) < currentDate)
            {
                lowLevels.ThirdLevel = 0;
                lowLevels.ThirdLevelDate = new DateTime();
            }
            if (lowLevels.CurrentLevelDate.AddDays(LiveTimeCandleCnt) < currentDate)
            {
                lowLevels.CurrentLevel = 0;
                lowLevels.CurrentLevelDate = new DateTime();
            }

        }
        //------------------------
    }
}
