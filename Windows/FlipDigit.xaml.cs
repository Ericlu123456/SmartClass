using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace smartClass.Windows
{
    public partial class FlipDigit : System.Windows.Controls.UserControl
    {
        private string _currentChar = "0";

        public FlipDigit()
        {
            InitializeComponent();
        }

        public void SetChar(string ch, bool animate = true)
        {
            if (string.IsNullOrEmpty(ch)) ch = "0";
            if (ch.Length > 1) ch = ch[0].ToString();
            if (ch == _currentChar) return;

            if (!animate)
            {
                _currentChar = ch;
                DigitText.Text = ch;
                return;
            }

            PlayRollAnimation(ch);
        }

        private void PlayRollAnimation(string newChar)
        {
            var scale = (ScaleTransform)DigitText.RenderTransform;

            // 第一阶段：Y缩放 1→0（压扁消失）
            var squash = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(100))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            squash.Completed += (s, e) =>
            {
                // 换字
                DigitText.Text = newChar;
                _currentChar = newChar;

                // 第二阶段：Y缩放 0→1（弹回展开）
                var stretch = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, stretch);
            };
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, squash);
        }
    }
}
