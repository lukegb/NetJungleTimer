using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace NetJungleTimer
{
    class JungleTimer
    {
        MainWindow parent;
        Image timerImg;
        Label timerLabel;
        int countdown;
        DateTime beganCountdown;
        DateTime endCountdown;
        bool spinning = false;
        Brush bgBrush;
        bool triggeredAtThirty = false;
        String netMessage;
        KeyboardManager.KMKey myHotkey;
        bool flashingAndIsRed = false;

        private Color DEFAULT_BRUSH_COLOR = (Color)((new ColorConverter()).ConvertFrom("#aa000000"));
        private Color THIRTY_SECONDS_BRUSH_COLOR = (Color)((new ColorConverter()).ConvertFrom("#aaff0000"));


        public JungleTimer(MainWindow parent, Image timerImg, int countdown, String netMessage, KeyboardManager.KMKey myHotkey)
        {
            this.parent = parent;
            this.netMessage = netMessage;
            this.myHotkey = myHotkey;
            parent.keyboardManager.ListenToKey(myHotkey);

            var bc = new BrushConverter();
            bgBrush = (Brush)bc.ConvertFrom("#aa000000");

            this.countdown = countdown;
            this.timerImg = timerImg;

            timerLabel = new Label();
            timerLabel.Margin = timerImg.Margin;
            timerLabel.Width = timerImg.Width;
            timerLabel.Height = timerImg.Height;
            timerLabel.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            timerLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            timerLabel.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
            timerLabel.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            timerLabel.Content = countdown.ToString();
            timerLabel.Foreground = Brushes.Yellow;
            timerLabel.Background = bgBrush;
            timerLabel.Visibility = System.Windows.Visibility.Hidden;
            Grid parentGrid = ((Grid)timerImg.Parent);
            parentGrid.Children.Add(timerLabel);
            Grid.SetRow(timerLabel, Grid.GetRow(timerImg));
            Grid.SetColumn(timerLabel, Grid.GetColumn(timerImg));
        }

        public void StartCountdown(int time)
        {
            beganCountdown = DateTime.Now;
            timerLabel.Visibility = System.Windows.Visibility.Visible;
            timerLabel.Background = new SolidColorBrush(DEFAULT_BRUSH_COLOR);
            endCountdown = beganCountdown.AddSeconds(time);
            spinning = true;
            triggeredAtThirty = false;
        }

        public void SyncCountdown(int time)
        {
            if (time == 0)
                EndCountdown();
            else
            {
                if (!spinning)
                {
                    StartCountdown(time);
                    if (time <= 30)
                    {
                        triggeredAtThirty = true;
                        timerLabel.Background = new SolidColorBrush(THIRTY_SECONDS_BRUSH_COLOR);
                    }
                }
                else
                {
                    endCountdown = DateTime.Now.AddSeconds(time);
                }
            }
        }

        public void EndCountdown()
        {
            spinning = false;
            timerLabel.Visibility = System.Windows.Visibility.Hidden;
        }

        public void UpdateComponent()
        {
            if (!spinning)
                return;

            DateTime now = DateTime.Now;

            if (endCountdown.CompareTo(now) <= 0)
            {
                EndCountdown();
                return;
            }

            TimeSpan remaining = endCountdown.Subtract(now);

            if (!triggeredAtThirty && remaining.TotalSeconds < 31)
            {
                triggeredAtThirty = true;
                TriggerThirtySecondAnimation();
            }
            else if (remaining.TotalSeconds < 25)
            {
                if (flashingAndIsRed)
                {
                    timerLabel.Background = new SolidColorBrush(DEFAULT_BRUSH_COLOR);
                }
                else
                {
                    timerLabel.Background = new SolidColorBrush(THIRTY_SECONDS_BRUSH_COLOR);
                }
                flashingAndIsRed = !flashingAndIsRed;
            }

            timerLabel.Content = Math.Round(remaining.TotalSeconds).ToString();
        }

        public void TriggerThirtySecondAnimation()
        {
            ColorAnimation bgStrokeAnimation = new ColorAnimation(THIRTY_SECONDS_BRUSH_COLOR, new System.Windows.Duration(TimeSpan.FromMilliseconds(500)));
            bgStrokeAnimation.AutoReverse = false;
            //bgStrokeAnimation.BeginAnimation(timerLabel.Background, bgStrokeAnimation);

            Storyboard s = new Storyboard();
            s.Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(500));
            s.Children.Add(bgStrokeAnimation);

            Storyboard.SetTarget(bgStrokeAnimation, timerLabel);
            Storyboard.SetTargetProperty(bgStrokeAnimation, new System.Windows.PropertyPath("Background.Color"));

            s.Begin();
        }

        public void GotMessage(String message)
        {
            String[] message_split = message.Split(new char[] { ' ' });
            if (message_split.Length != 3)
                return;

            if (message_split[1] == netMessage)
            {
                SyncCountdown(int.Parse(message_split[2]));
            }
        }

        internal void GotKey(KeyboardManager.KMKey hotKey)
        {
            if (hotKey.Equals(myHotkey))
            {
                // yay
                parent.NetBroadcast("JUNGLETIMER " + netMessage + " " + countdown.ToString());
                StartCountdown(countdown);
            }
        }

        internal void SyncData()
        {
            parent.NetBroadcast("JUNGLETIMER " + netMessage + " " + Math.Min(countdown, Math.Max(0, Math.Round(endCountdown.Subtract(DateTime.Now).TotalSeconds))));
        }
    }
}
