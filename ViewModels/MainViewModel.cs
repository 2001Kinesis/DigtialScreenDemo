using DigtialScreen.Base;
using DigtialScreen.Models;
using LiveCharts;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace DigtialScreen.ViewModels
{
    public class MainViewModel : NotifyBase
    {
        public SeriesCollection StateSeries { get; set; }
        public ChartValues<ObservableValue> YieldValue1 { get; set; }
        public ChartValues<ObservableValue> YieldValue2 { get; set; }
        public SeriesCollection BarChartSeries { get; set; }

        public List<CompareItemModel> WorkerCompareList { get; set; }

        public List<CompareItemModel> QualityList { get; set; }

        public ObservableCollection<string> Alarms { get; set; }
        private string _currentYeild = "123456";
        public string CurrentYeild
        {
            get { return _currentYeild; }
            set { SetProperty(ref _currentYeild, value); }
        }

        private DateTime _nowDateTime = DateTime.Now;
        public DateTime NowDateTime
        {
            get { return _nowDateTime; }
            set { SetProperty(ref _nowDateTime, value); }
        }

        private string _timeCnFormat = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss");
        public string TimeCnFormat
        {
            get { return _timeCnFormat; }
            set { SetProperty(ref _timeCnFormat, value); }
        }

        public int FinishedRate { get; set; } = 80;

        public List<BadItemModel> BadScatter { get; set; }

        Random random = new Random();
        CancellationTokenSource cts = new CancellationTokenSource();
        Task task = null;

        public MainViewModel()
        {
            #region 饼状图数据初始化
            StateSeries = new SeriesCollection();
            StateSeries.Add(new PieSeries()
            {
                Title = "测试中",
                Values = new ChartValues<double>(new double[] { 0.533 }),
                Fill = new SolidColorBrush(Color.FromArgb(255, 43, 182, 254))
            });
            StateSeries.Add(new PieSeries()
            {
                Title = "测试通过",
                Values = new ChartValues<double>(new double[] { 0.2 }),
                Fill = new SolidColorBrush(Colors.Green)
            });
            StateSeries.Add(new PieSeries()
            {
                Title = "测试失败",
                Values = new ChartValues<double>(new double[] { 0.167 }),
                Fill = new SolidColorBrush(Colors.Red)
            });
            StateSeries.Add(new PieSeries()
            {
                Title = "待测试",
                Values = new ChartValues<double>(new double[] { 0.1 }),
                Fill = new SolidColorBrush(Color.FromArgb(255, 144, 150, 191))
            });
            #endregion

            #region 测试人员绩效初始化
            string[] Employs = new string[] { "测试员A", "测试员B", "测试员C", "测试员D" };
            WorkerCompareList = new List<CompareItemModel>();
            foreach (var e in Employs)
            {
                WorkerCompareList.Add(new CompareItemModel()
                {
                    Name = e,
                    PlanValue = random.Next(100, 200),
                    FinishedValue = random.Next(50, 200),
                });
            }
            #endregion

            #region 报警信息
            Alarms = new ObservableCollection<string>();
            #endregion

            #region 产量初始化
            YieldValue1 = new ChartValues<ObservableValue>();
            YieldValue2 = new ChartValues<ObservableValue>();
            for (int i = 0; i < 12; i++)
            {
                YieldValue1.Add(new ObservableValue(random.Next(20, 300)));
                YieldValue2.Add(new ObservableValue(random.Next(20, 300)));
            }
            #endregion

            #region 测试不良指标初始化
            BadScatter = new List<BadItemModel>();
            string[] BadNames = new string[] { "密封件损坏", "密封胶失效", "接口变形", "装配不当", "材料缺陷", "压力泄漏", "温度异常", "振动超标" };
            for (int i = 0; i < BadNames.Length; i++)
            {
                BadScatter.Add(new BadItemModel() { Title = BadNames[i], Size = 180 - 20 * i, Value = 0.9 - 0.1 * i });
            }
            #endregion

            #region 测试质量控制
            string[] quality = new string[] { "气压测试", "液压测试", "真空测试", "氦检仪", "湿度测试", "温度测试", "振动测试", "压力循环", "老化测试", "综合测试" };
            QualityList = new List<CompareItemModel>();
            foreach (var q in quality)
            {
                QualityList.Add(new CompareItemModel()
                {
                    Name = q,
                    PlanValue = random.Next(100, 200),
                    FinishedValue = random.Next(50, 200),
                });
            }
            #endregion

            #region 柱形图初始化
            BarChartSeries = new SeriesCollection();
            BarChartSeries.Add(new ColumnSeries
            {
                Title = "关键密封测试",
                Values = new ChartValues<int> { 423, 512, 378, 625, 489, 567, 712 }
            });
            BarChartSeries.Add(new ColumnSeries
            {
                Title = "常规密封测试",
                Values = new ChartValues<int> { 321, 425, 487, 398, 542, 413, 521 }
            });
            #endregion

            #region 时间初始化
            NowDateTime = DateTime.Now;
            StartClock();
            #endregion

            TcpClient tcpClient = new TcpClient();
            try
            {
                tcpClient.Connect("127.0.0.1", 502);
                Modbus.Device.ModbusIpMaster maser = Modbus.Device.ModbusIpMaster.CreateIp(tcpClient);
                task = Task.Run(async () =>
                {
                    while (!cts.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                        ushort[] values = maser.ReadHoldingRegisters(1, 0, 1);
                        //确保在主线程上更新UI绑定的属性
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentYeild = values[0].ToString("000000");
                            //添加报警
                            Alarms.Insert(0, "1234");
                            if (Alarms.Count > 6)
                            {
                                Alarms.RemoveAt(Alarms.Count - 1);
                            }
                        });
                    }
                }, cts.Token);
            }
            catch (Exception)
            {
                return;
            }

        }

        private async void StartClock()
        {
            while (!cts.IsCancellationRequested)
            {
                // 在UI线程上更新
                Application.Current.Dispatcher.Invoke(() =>
                {
                    NowDateTime = DateTime.Now;
                    TimeCnFormat = DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss");
                });

                await Task.Delay(1000); // 等待1秒
            }
        }

        public void Dispose()
        {
            cts.Cancel();
            Task.WaitAny(task);
        }
    }
}
