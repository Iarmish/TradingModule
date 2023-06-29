using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using ShortPeakRobot.ViewModel;
using Binance.Net.Enums;
using ShortPeakRobot.Market;

namespace ShortPeakRobot.UI
{
    public static class CandleChart
    {
        public static void Draw(Grid Grid_candles, Grid Grid_price, Grid Grid_time, ChartData chartData, int y_zoom_pix)
        {
            if (chartData.Candles.Count == 0) { return; }

            Grid_candles.Children.Clear();
            Grid_price.Children.Clear();
            Grid_time.Children.Clear();


            int candleLastIndex = chartData.CandleStartIndex + chartData.CandleCnt;

            double chartHigh = -1, chartLow = -1;

            int lastTimeLine = 0;

            double chartHeight = Grid_candles.ActualHeight;
            double chartWidth = Grid_candles.ActualWidth;

            // ---------------- вычисляем макс и мин значения цен в видимой части графика
            for (int i = chartData.CandleStartIndex; i < candleLastIndex; i++)
            {
                if (i >= chartData.Candles.Count) { break; }
                if (i >= chartData.CandleStartIndex && i <= candleLastIndex)
                {
                    if (chartHigh == -1)
                    {
                        chartHigh = (double)chartData.Candles[i].HighPrice;
                        chartLow = (double)chartData.Candles[i].LowPrice;
                    }
                    if ((double)chartData.Candles[i].HighPrice > chartHigh)
                    {
                        chartHigh = (double)chartData.Candles[i].HighPrice;
                    }
                    if ((double)chartData.Candles[i].LowPrice < chartLow && chartData.Candles[i].LowPrice != -1)
                    {
                        chartLow = (double)chartData.Candles[i].LowPrice;
                    }

                }

            }
            //------------- шаг цены ------------------------------
            double setkaStart = ((chartLow - (y_zoom_pix / 2)) / 10) * 10, setkaEnd = (((chartHigh + (y_zoom_pix / 2)) / 10) * 10), setkaStep = 1;

            double y_point = chartHeight / (setkaEnd - setkaStart);

            while (setkaStep * y_point < 30 )
            {
                setkaStep += 5;
            }

            //-------------------------------------
            for (double i_pr = setkaEnd - setkaStart; i_pr >= 0; i_pr -= setkaStep)
            {
                Line line_pr = new Line();
                line_pr.X1 = 0; line_pr.X2 = 7;
                line_pr.Y1 = (double)(i_pr * y_point); line_pr.Y2 = (double)(i_pr * y_point);
                line_pr.Stroke = Brushes.Gray;
                Grid_price.Children.Add(line_pr);

                Label lab_pr = new Label();
                lab_pr.Margin = new Thickness(5, (double)(i_pr * y_point) - 13, 0, 0);
                //lab_pr.Foreground = Brushes.White;
                lab_pr.Content = setkaEnd - i_pr;
                Grid_price.Children.Add(lab_pr);

                Line line_setka_y = new Line();
                line_setka_y.X1 = 0; line_setka_y.X2 = chartWidth;
                line_setka_y.Y1 = (double)(i_pr * y_point); line_setka_y.Y2 = i_pr * y_point;
                line_setka_y.Stroke = Brushes.LightGray;
                line_setka_y.StrokeDashArray = new DoubleCollection() { 2 };
                Grid_candles.Children.Add(line_setka_y);

                
            }

            double candleWidth = (chartWidth / chartData.CandleCnt) * 0.8;
            double candleSpace = chartWidth / chartData.CandleCnt;





            //double y_add_step = (y_zoom_pix / 2) / y_point;
            //--------дата 


            
            //-------------------------------------------------------
            for (int i_cdl = chartData.CandleStartIndex; i_cdl <= candleLastIndex; i_cdl++) //----- цикл по свечам
            {
                //orders


                //---------------------

                if (i_cdl >= chartData.Candles.Count) { break; }
                double x = (i_cdl - chartData.CandleStartIndex) * candleSpace;// -- -1 для смещения координат х на 1 свечу
                double y = 0, y_vwap = 0;
                double y_hi = ((setkaEnd - (double)chartData.Candles[i_cdl].HighPrice) * y_point),
                       y_low = ((setkaEnd - (double)chartData.Candles[i_cdl].LowPrice) * y_point);
                if (i_cdl == chartData.CandleStartIndex)
                {

                }
                //------ параметры свечи
                int type_candle = 0;
                double heigth_candle = 0;
                if (chartData.Candles[i_cdl].OpenPrice - chartData.Candles[i_cdl].ClosePrice > 0)
                {
                    type_candle = 1;
                    heigth_candle = ((double)chartData.Candles[i_cdl].OpenPrice - (double)chartData.Candles[i_cdl].ClosePrice) * y_point;
                    y = ((setkaEnd - (double)chartData.Candles[i_cdl].OpenPrice) * y_point);

                }
                else
                {
                    type_candle = 2;
                    heigth_candle = ((double)chartData.Candles[i_cdl].ClosePrice - (double)chartData.Candles[i_cdl].OpenPrice) * y_point;
                    y = ((setkaEnd - (double)chartData.Candles[i_cdl].ClosePrice) * y_point);

                }
                //y_vwap = ((setkaEnd - candles[i_cdl].vwap) * y_point);
                //--------------------------------------------------
                //string st_day, st_month, st_hour, st_min, st_year;

                //--------------------- шкала времени -----------------------
                var date = chartData.Candles[i_cdl].OpenTime;
                double x_t = ((i_cdl - chartData.CandleStartIndex - 1) * candleSpace) ;// -- -1 для смещения координат х на 1 свечу

                if (lastTimeLine + candleSpace > 90 || i_cdl == chartData.CandleStartIndex)
                {
                    lastTimeLine = 0;
                    Line line_t = new Line();
                    line_t.X1 = x  + (candleWidth / 2); line_t.X2 = x +  (candleWidth / 2);
                    line_t.Y1 = Grid_time.ActualHeight; line_t.Y2 = 0;
                    line_t.Stroke = Brushes.Gray;
                    Grid_time.Children.Add(line_t);

                    Label lab2 = new Label();
                    lab2.Margin = new Thickness(x + (candleWidth / 2), Grid_time.ActualHeight - 23, 0, 0);
                    //lab2.Foreground = Brushes.White;
                    lab2.Content = chartData.Candles[i_cdl].OpenTime.ToString("HH:mm");
                    Grid_time.Children.Add(lab2);



                    Line line_setka2 = new Line();
                    line_setka2.X1 = x + (candleWidth / 2); line_setka2.X2 = x + (candleWidth / 2);
                    line_setka2.Y1 = 0; line_setka2.Y2 = Grid_candles.ActualHeight;
                    line_setka2.Stroke = Brushes.LightGray;
                    //line_setka2.StrokeThickness = 1;
                    line_setka2.StrokeDashArray = new DoubleCollection() { 2 };


                    Grid_candles.Children.Add(line_setka2);

                }
                else
                {
                    lastTimeLine += Convert.ToInt32(candleSpace);
                }
                

                //---------------- рисуем свечу
                Line line = new Line();
                line.X1 = x + (candleWidth / 2); line.X2 = x + (candleWidth / 2);
                line.Y1 = y_hi; line.Y2 = y_low; line.Stroke = Brushes.Black;
                Grid_candles.Children.Add(line);
                line.SetValue(Grid.ColumnProperty, 1);

                Rectangle rect = new Rectangle();
                rect.Width = candleWidth; rect.Height = heigth_candle;
                rect.Margin = new Thickness(x, y, 0, 0);
                rect.HorizontalAlignment = HorizontalAlignment.Left;
                rect.VerticalAlignment = VerticalAlignment.Top;

                if (type_candle == 1) { rect.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 136, 189)); }
                if (type_candle == 2) { rect.Fill = Brushes.White; }

                // date = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(candles[i_cdl].Date);               


                rect.Stroke = Brushes.Black;
                rect.ToolTip =
                    "Дата " + chartData.Candles[i_cdl].OpenTime.ToString("dd.MM.yy") + "\n" +
                    "Время " + chartData.Candles[i_cdl].OpenTime.ToString("HH:mm") + "\n" +
                    "Open " + chartData.Candles[i_cdl].OpenPrice + "\n" +
                    "High " + chartData.Candles[i_cdl].HighPrice + "\n" +
                    "Low " + chartData.Candles[i_cdl].LowPrice + "\n" +
                    "Close " + chartData.Candles[i_cdl].ClosePrice + "\n" +
                    "volume " + chartData.Candles[i_cdl].Volume + "\n"

                    ;
                if (candleLastIndex == i_cdl)
                {
                    rect.Name = "lastRect";
                }
                Grid_candles.Children.Add(rect);
                rect.SetValue(Grid.ColumnProperty, 1);



            }


            Line line_horizont = new Line();
            line_horizont.X1 = 0; line_horizont.X2 = Grid_time.ActualWidth;
            line_horizont.Y1 = 1; line_horizont.Y2 = 1;
            line_horizont.Stroke = Brushes.Gray;
            Grid_time.Children.Add(line_horizont);

            Line line_vert = new Line();
            line_vert.X1 = 1; line_vert.X2 = 1;
            line_vert.Y1 = 0; line_vert.Y2 = Grid_price.ActualHeight;
            line_vert.Stroke = Brushes.Gray;
            Grid_price.Children.Add(line_vert);

            //------------------- рисуем сделки ----

            for (int i_deal = 0; i_deal < chartData.Trades.Count; i_deal++)
            {
                var candleIndex = DateTimeToIndex(chartData, chartData.Trades[i_deal].Timestamp);
                if (candleIndex == 0)
                {
                    continue;
                }
               

                if (candleIndex >= chartData.CandleStartIndex
                && candleIndex <= chartData.CandleStartIndex + chartData.CandleCnt)
                {
                    

                    double x_ = ((candleIndex - chartData.CandleStartIndex - 1) * candleSpace) + (candleWidth / 2);
                    double y_ = (setkaEnd - (double)chartData.Trades[i_deal].Price) * y_point;




                    Ellipse elipse = new Ellipse();
                    elipse.Width = 12; elipse.Height = 12;
                    elipse.StrokeThickness = 1;
                    elipse.Stroke = Brushes.Black;
                    if (chartData.Trades[i_deal].Side == (int)OrderSide.Sell) { elipse.Fill = Brushes.Red; }
                    if (chartData.Trades[i_deal].Side == (int)OrderSide.Buy) { elipse.Fill = Brushes.Green; }
                    elipse.Margin = new Thickness(x_ , y_ , 0, 0);
                    elipse.HorizontalAlignment = HorizontalAlignment.Left;
                    elipse.VerticalAlignment = VerticalAlignment.Top;
                    elipse.ToolTip = chartData.Trades[i_deal].Price;
                    Grid_candles.Children.Add(elipse);




                }
                if (candleIndex > chartData.CandleStartIndex + chartData.CandleCnt) { break; }

            }

            //рисуем VWAP 
            for (int i_deal = 0; i_deal < MarketData.CandleExtParams.Count; i_deal++)
            {
                var candleIndex = DateTimeToIndex(chartData, MarketData.CandleExtParams[i_deal].Date);
                if (candleIndex == 0)
                {
                    continue;
                }


                if (candleIndex >= chartData.CandleStartIndex
                && candleIndex <= chartData.CandleStartIndex + chartData.CandleCnt)
                {
                    double x_ = ((candleIndex - chartData.CandleStartIndex - 1) * candleSpace) + (candleWidth / 2);
                    double y1_ = (setkaEnd - (double)MarketData.CandleExtParams[i_deal].Param1) * y_point;
                    double y2_ = (setkaEnd - (double)MarketData.CandleExtParams[i_deal].Param2) * y_point;
                    double y3_ = (setkaEnd - (double)MarketData.CandleExtParams[i_deal].Param3) * y_point;

                    Ellipse elipse1 = new Ellipse();
                    elipse1.Width = 4; elipse1.Height = 4;
                    elipse1.StrokeThickness = 1;
                    elipse1.Stroke = Brushes.Black;
                    elipse1.Fill = Brushes.Red;                    
                    elipse1.Margin = new Thickness(x_, y1_, 0, 0);
                    elipse1.HorizontalAlignment = HorizontalAlignment.Left;
                    elipse1.VerticalAlignment = VerticalAlignment.Top;
                    elipse1.ToolTip = MarketData.CandleExtParams[i_deal].Param1;
                    Grid_candles.Children.Add(elipse1);

                     Ellipse elipse2 = new Ellipse();
                    elipse2.Width = 4; elipse1.Height = 4;
                    elipse2.StrokeThickness = 1;
                    elipse2.Stroke = Brushes.Black;
                    elipse2.Fill = Brushes.Red;                    
                    elipse2.Margin = new Thickness(x_, y2_, 0, 0);
                    elipse2.HorizontalAlignment = HorizontalAlignment.Left;
                    elipse2.VerticalAlignment = VerticalAlignment.Top;
                    elipse2.ToolTip = MarketData.CandleExtParams[i_deal].Param2;
                    Grid_candles.Children.Add(elipse2);

                     Ellipse elipse3 = new Ellipse();
                    elipse3.Width = 4; elipse1.Height = 4;
                    elipse3.StrokeThickness = 1;
                    elipse3.Stroke = Brushes.Black;
                    elipse3.Fill = Brushes.Red;                    
                    elipse3.Margin = new Thickness(x_, y3_, 0, 0);
                    elipse3.HorizontalAlignment = HorizontalAlignment.Left;
                    elipse3.VerticalAlignment = VerticalAlignment.Top;
                    elipse3.ToolTip = MarketData.CandleExtParams[i_deal].Param3;
                    Grid_candles.Children.Add(elipse3);

                }
                if (candleIndex > chartData.CandleStartIndex + chartData.CandleCnt) { break; }

            }
            //Рисуем ордера

            foreach (var order in MarketData.OpenOrders)
            {
                double price;
                if (order.StopPrice == null || order.StopPrice == 0)
                {
                    price = (double)order.Price;
                }
                else
                {
                    price = (double)order.StopPrice;
                }
                if (price > chartLow && price < chartHigh)
                {
                    var order_Y = (setkaEnd - price) * y_point;

                    Line line = new Line();
                    line.X1 = 0 + (candleWidth / 2); line.X2 = Grid_candles.ActualWidth;
                    line.Y1 = order_Y; line.Y2 = order_Y;

                    if (order.Side == (int)OrderSide.Buy)
                    {
                        line.Stroke = Brushes.Green;
                    }
                    else
                    {
                        line.Stroke = Brushes.Red;
                    }

                    Grid_candles.Children.Add(line);
                    line.SetValue(Grid.ColumnProperty, 1);
                }
            }
        }

        private static int DateTimeToIndex(ChartData chartData, DateTime tradeDate)
        {
            return chartData.Candles.FindIndex(x => x.CloseTime > tradeDate &&
            x.OpenTime > tradeDate.AddSeconds(-chartData.TimeFrame));
        }
    }
}
