using ScottPlot;
using Skender.Stock.Indicators;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace AUTO_TRADER
{
    public partial class Controller : Window
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int FreeConsole();

        private Client client = new Client("76418a1997412737c4efb3db6681a662641b5575", "IX.D.NASDAQ.IFS.IP");
        private DateTime lastDay = DateTime.Now;
        private double[] histogram_arr;
        private double[] EMA100_arr;
        private OHLC lastPrice;



        private List<Quote> quotes285;
        public Controller()
        {
            InitializeComponent();
            AllocConsole();
            Loaded += new RoutedEventHandler(Window_Loaded);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client.connect();
            client.updateJsonFile();
            quotes285 = client.getDateFromJson().ToList<Quote>();

            plotData.Plot.Title("EMA200 Indicator");
            plotMACD.Plot.Title("MACD Indicator");
            showMACD();
            showPrices();
            checkIndicators();

            plotData.Render();
            plotMACD.Render();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromHours(1);
            timer.Tick += loop;
            timer.Start();
        }
        public void showMACD()
        {
            var MACD = quotes285.GetMacd(12, 26, 9).ToList();

            List<double> MACD_list = new List<double>();
            List<double> signal_list = new List<double>();
            List<DateTime> date_list = new List<DateTime>();

            for (int i = 0; i < MACD.Count; i++)
            {
                if (MACD[i].Macd != null && MACD[i].Signal != null)
                {
                    MACD_list.Add((double)MACD[i].Macd);
                    signal_list.Add((double)MACD[i].Signal);
                    date_list.Add(MACD[i].Date);
                }
            }
            double[] histogram = new double[MACD_list.Count];
            for (int i = 0; i < MACD_list.Count; i++)
            {
                histogram[i] = MACD_list[i] - signal_list[i];
            }

            double[] date_arr = date_list.Select(x => x.ToOADate()).ToArray();
            plotMACD.Plot.Clear();
            var bar = plotMACD.Plot.AddBar(histogram, date_arr);
            histogram_arr = histogram;
            bar.FillColor = Color.Green;
            bar.FillColorNegative = Color.Red;

            plotMACD.Plot.AddScatter(date_arr, MACD_list.ToArray(), label: "MACD", color: Color.Blue, lineWidth: 3, markerSize: 0);
            plotMACD.Plot.AddScatter(date_arr, signal_list.ToArray(), label: "Signal", color: Color.Red, lineWidth: 3, markerSize: 0);
            var MACD_tooltip = plotMACD.Plot.AddTooltip("MACD: " + Math.Round(MACD_list[MACD_list.Count - 1],2), date_arr[date_arr.Length - 1], MACD_list[MACD_list.Count - 1]);
            MACD_tooltip.Font.Color = Color.Blue;
            var Signal_tooltip = plotMACD.Plot.AddTooltip("Signal: " + Math.Round(signal_list[signal_list.Count - 1],2), date_arr[date_arr.Length - 1], signal_list[signal_list.Count - 1]);
            Signal_tooltip.Font.Color = Color.Red;
            var histogram_tooltip = plotMACD.Plot.AddTooltip("Histogram: " + Math.Round(histogram[histogram.Length - 1],2), date_arr[date_arr.Length - 1], histogram[histogram.Length - 1]);
            histogram_tooltip.Font.Color = Color.Green;

            plotMACD.Plot.XAxis.DateTimeFormat(true);
            plotMACD.Plot.Legend();
        }
        private void showPrices()
        {
            plotData.Plot.Clear();
            var prices = client.getOHCLArray(quotes285);
            lastPrice = prices[prices.Length - 1];
            plotData.Plot.AddCandlesticks(prices);
            var EMA100 = quotes285.GetEma(100).ToList();
            List<double> ema = new List<double>();
            List<DateTime> date_list = new List<DateTime>();
            for (int i = 0; i < EMA100.Count; i++)
            {
                if (EMA100[i].Ema != null)
                {
                    ema.Add((double)EMA100[i].Ema);
                    date_list.Add(EMA100[i].Date);
                }
            }
            EMA100_arr = ema.ToArray();
            double[] date_arr = date_list.Select(x => x.ToOADate()).ToArray();
            plotData.Plot.AddScatter(date_arr, ema.ToArray(),color: Color.Orange ,lineWidth: 3,label: "EMA 100", markerSize: 0);
            plotData.Plot.AddTooltip("EMA100: " + Math.Round(ema[ema.Count - 1],2), date_arr[date_arr.Length - 1], ema[ema.Count - 1]);
            plotData.Plot.XAxis.DateTimeFormat(true);
            plotData.Plot.SetAxisLimits(xMin: prices[prices.Length-1].DateTime.ToOADate()-30, xMax: prices[prices.Length-1].DateTime.ToOADate() + 10);
            plotData.Plot.Legend(true);
        }
        private void checkIndicators()
        {
            double lastDay = histogram_arr[histogram_arr.Length - 2];
            double today = histogram_arr[histogram_arr.Length - 1];
            double EMA = EMA100_arr[EMA100_arr.Length - 1];
            if (lastDay < 0 && today > 0 && lastPrice.Close > EMA)
            {
                client.openPosition("BUY", "15", "10", "GBP");
            }
            else if(lastDay > 0 && today < 0 && lastPrice.Close < EMA)
            {
                client.openPosition("SELL","15", "10", "GBP");
            }
        }
        private void loop(object sender, EventArgs e)
        {
            if (DateTime.Now.Date != lastDay.Date)
            {
                client.updateJsonFile();
                quotes285 = client.getDateFromJson().ToList<Quote>();
                showMACD();
                showPrices();
                checkIndicators();
                plotData.Render();
                plotMACD.Render();
                lastDay = DateTime.Now;
            }
        }
    }
}
