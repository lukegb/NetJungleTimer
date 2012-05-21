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
    struct NetworkedTimerContext
    {
        public Image TimerBackgroundImage { get; private set; }
        public int Countdown { get; private set; }
        public String NetworkMessage { get; private set; }
        public KeyboardManager.KMKey Hotkey { get; private set; }
        public String ChatMessageComplete { get; private set; }

        public NetworkedTimerContext(Image inTimerBackgroundImage, int inCountdown, String inNetworkMessage, KeyboardManager.KMKey inHotkey, String inChatMessageComplete) : this()
        {
            TimerBackgroundImage = inTimerBackgroundImage;
            Countdown = inCountdown;
            NetworkMessage = inNetworkMessage;
            Hotkey = inHotkey;
            ChatMessageComplete = inChatMessageComplete;
        }
    }

    internal class NetworkedTimer
    {

        MainWindow parent;
        public NetworkedTimerContext context;

        Label timerLabel;
        DateTime beganCountdown;
        DateTime endCountdown;
        bool spinning = false;
        Brush bgBrush;
        bool triggeredPreWarning = false;
        int flashingLastSecond = 0;

        private Color DEFAULT_BRUSH_COLOR = (Color)((new ColorConverter()).ConvertFrom("#aa000000"));
        private Color PRE_WARNING_BRUSH_COLOR = (Color)((new ColorConverter()).ConvertFrom("#aaff0000"));
        private const int PRE_WARNING = 30;


        internal NetworkedTimer(MainWindow parent, NetworkedTimerContext ntc)
        {
            this.parent = parent;
            this.context = ntc;
            parent.keyboardManager.ListenToKey(context.Hotkey);

            var bc = new BrushConverter();
            bgBrush = (Brush)bc.ConvertFrom("#aa000000");

            timerLabel = new Label();
            timerLabel.Margin = context.TimerBackgroundImage.Margin;
            timerLabel.Width = context.TimerBackgroundImage.Width;
            timerLabel.Height = context.TimerBackgroundImage.Height;
            timerLabel.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            timerLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            timerLabel.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
            timerLabel.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            timerLabel.Content = context.Countdown.ToString();
            timerLabel.Foreground = Brushes.Yellow;
            timerLabel.Background = bgBrush;
            timerLabel.Visibility = System.Windows.Visibility.Hidden;
            Grid parentGrid = ((Grid)context.TimerBackgroundImage.Parent);
            parentGrid.Children.Add(timerLabel);
            Grid.SetRow(timerLabel, Grid.GetRow(context.TimerBackgroundImage));
            Grid.SetColumn(timerLabel, Grid.GetColumn(context.TimerBackgroundImage));
        }

        public void StartCountdown(int time)
        {
            beganCountdown = DateTime.Now;
            timerLabel.Visibility = System.Windows.Visibility.Visible;
            timerLabel.Background = new SolidColorBrush(DEFAULT_BRUSH_COLOR);
            endCountdown = beganCountdown.AddSeconds(time);
            spinning = true;
            triggeredPreWarning = false;
        }

        public void SyncCountdown(int time)
        {
            Console.WriteLine("SYNCING COUNTDOWN");
            if (time == 0)
                EndCountdown();
            else
            {
                if (!spinning)
                {
                    StartCountdown(time);
                    if (time <= PRE_WARNING)
                    {
                        triggeredPreWarning = true;
                        timerLabel.Background = new SolidColorBrush(PRE_WARNING_BRUSH_COLOR);
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

            parent.OnTimerExpiry(this);
        }

        public void UpdateComponent(DateTime now)
        {
            if (!spinning)
                return;

            if (endCountdown.CompareTo(now) <= 0)
            {
                EndCountdown();
                return;
            }

            TimeSpan remaining = endCountdown.Subtract(now);

            if (!triggeredPreWarning && remaining.TotalSeconds < PRE_WARNING)
            {
                triggeredPreWarning = true;
                TriggerPreWarningAnimation();
                parent.OnTimerFinalCountdown(this, PRE_WARNING);
                flashingLastSecond = (int)Math.Round(remaining.TotalSeconds);
            }
            else if (remaining.TotalSeconds < PRE_WARNING - 1)
            {
                if (flashingLastSecond != (int)Math.Round(remaining.TotalSeconds))
                {
                    if ((int)Math.Round(remaining.TotalSeconds) % 2 == 0)
                    {
                        timerLabel.Background = new SolidColorBrush(DEFAULT_BRUSH_COLOR);
                    }
                    else
                    {
                        timerLabel.Background = new SolidColorBrush(PRE_WARNING_BRUSH_COLOR);
                    }
                    flashingLastSecond = (int)remaining.TotalSeconds;
                }
            }

            timerLabel.Content = Math.Round(remaining.TotalSeconds).ToString();
        }

        internal void TriggerPreWarningAnimation()
        {
            ColorAnimation bgStrokeAnimation = new ColorAnimation(PRE_WARNING_BRUSH_COLOR, new System.Windows.Duration(TimeSpan.FromMilliseconds(500)));
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

            if (message_split[1] == context.NetworkMessage)
            {
                SyncCountdown(int.Parse(message_split[2]));
            }
        }

        internal bool GotKey(KeyboardManager.KMKey hotKey)
        {
            if (hotKey.Equals(context.Hotkey))
            {
                // yay
                parent.NetBroadcast("NETTIMER " + context.NetworkMessage + " " + context.Countdown.ToString());
                StartCountdown(context.Countdown);
                return true;
            }
            return false;
        }

        internal void SyncData()
        {
            parent.NetBroadcast("NETTIMER " + context.NetworkMessage + " " + Math.Min(context.Countdown, Math.Max(0, Math.Round(endCountdown.Subtract(DateTime.Now).TotalSeconds))));
        }
    }
}
