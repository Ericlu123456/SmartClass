using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using smartClass.Services;

namespace smartClass.Windows
{
    public partial class ExamClockWindow : Window
    {
        private DispatcherTimer _timer;

        public ExamClockWindow()
        {
            InitializeComponent();

            // 点击任意位置关闭
            MouseDown += (s, e) => Close();
            KeyDown += (s, e) => Close();

            // 定时刷新 (60ms = ~16fps，翻页动画流畅)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(60);
            _timer.Tick += Timer_Tick;
            _timer.Start();

            // 入场动画
            Loaded += (s, e) =>
            {
                try
                {
                    var fade = new System.Windows.Media.Animation.DoubleAnimation(0, 1,
                        TimeSpan.FromMilliseconds(300));
                    BeginAnimation(OpacityProperty, fade);
                }
                catch { }
            };

            Closed += (s, e) =>
            {
                try { _timer?.Stop(); }
                catch { }
            };
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var now = DateTime.Now;
                var hh = now.ToString("HH");
                var mm = now.ToString("mm");
                var ss = now.ToString("ss");

                // 逐位更新（只更新变化的位，自动触发翻页动画）
                DigH0.SetChar(hh[0].ToString());
                DigH1.SetChar(hh[1].ToString());
                DigM0.SetChar(mm[0].ToString());
                DigM1.SetChar(mm[1].ToString());
                DigS0.SetChar(ss[0].ToString());
                DigS1.SetChar(ss[1].ToString());

                // 冒号闪烁（每秒切换）
                bool colonVisible = now.Second % 2 == 0;
                Colon1.Foreground = colonVisible
                    ? new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x77))
                    : new System.Windows.Media.SolidColorBrush(
                        System.Windows.Media.Color.FromRgb(0x22, 0x22, 0x2A));
                Colon2.Foreground = Colon1.Foreground;
            }
            catch (Exception ex)
            {
                LogService.Log(ex, "ExamClockWindow 刷新失败");
            }
        }
    }
}
