using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NetJungleTimer.UI
{
    public delegate void TimerExpiryHandler(object sender, EventArgs e);
    public delegate void TimerFinalCountHandler(object sender, EventArgs e);

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

    internal class NetworkedTimer : IUIElement
    {
        public NetworkedTimerContext context;

        Label timerLabel;
        DateTime beganCountdown;
        DateTime endCountdown;
        bool Spinning { get { return this.endCountdown > DateTime.Now; } }
        Brush bgBrush;
        bool triggeredPreWarning = false;
        int flashingLastSecond = 0;

        bool triggeredPreWarningTts = false;
        bool triggeredEndWarningTts = false;
        TimeSpan preWarningTtsAdvance;
        TimeSpan endWarningTtsAdvance;

        Networking.INetProto currentNetProto;

        private Color DEFAULT_BRUSH_COLOR = (Color)((new ColorConverter()).ConvertFrom("#aa000000"));
        private Color PRE_WARNING_BRUSH_COLOR = (Color)((new ColorConverter()).ConvertFrom("#aaff0000"));
        public const int PRE_WARNING = 30;

        public event TimerExpiryHandler TimerExpired;
        public event TimerFinalCountHandler TimerFinalCountdownReached;


        internal NetworkedTimer(NetworkedTimerContext ntc, Networking.INetProto currentNetProto)
        {
            this.context = ntc;
            this.currentNetProto = currentNetProto;

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

        ~NetworkedTimer()
        {
            timerLabel.Dispatcher.Invoke(DispatcherPriority.Normal, new Action<Label>((Label l) => ((Grid)l.Parent).Children.Remove(l)), timerLabel);
            timerLabel = null;
        }

        public void RemoveCreatedLabel(Label timerLabel)
        {
            ((Grid)(timerLabel.Parent)).Children.Remove(timerLabel);
        }

        public void StartCountdown(int time)
        {
            beganCountdown = DateTime.Now;
            timerLabel.Visibility = System.Windows.Visibility.Visible;
            timerLabel.Background = new SolidColorBrush(DEFAULT_BRUSH_COLOR);
            endCountdown = beganCountdown.AddSeconds(time);
            triggeredPreWarning = false;

            triggeredPreWarningTts = false;
            triggeredEndWarningTts = false;
            preWarningTtsAdvance = TextToSpeech.Instance.TimerExpiryPreTime(this);
            endWarningTtsAdvance = TextToSpeech.Instance.TimerFinalCountdownPreTime(this);
        }

        public void SyncCountdown(int time)
        {
            if (time == 0)
                EndCountdown();
            else
            {
                if (!Spinning)
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
            if (Spinning && TimerExpired != null)
                TimerExpired(this, EventArgs.Empty);
        }

        public void UpdateComponent(DateTime now)
        {
            timerLabel.Visibility = Spinning ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

            if (!Spinning)
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

                if (TimerFinalCountdownReached != null)
                    TimerFinalCountdownReached(this, EventArgs.Empty);

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

            if (!triggeredPreWarningTts && remaining.TotalSeconds < (preWarningTtsAdvance.TotalSeconds + PRE_WARNING))
            {
                TextToSpeech.Instance.TellTimerFinalCountdown(this);
                triggeredPreWarningTts = true;
            }

            if (!triggeredEndWarningTts && remaining.TotalSeconds < endWarningTtsAdvance.TotalSeconds)
            {
                TextToSpeech.Instance.TellTimerExpiry(this);
                triggeredEndWarningTts = true;
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
                if (message_split[2] == "CANCEL")
                {
                    endCountdown = DateTime.Now;
                    EndCountdown();
                }
                else
                {
                    try
                    {
                        SyncCountdown(int.Parse(message_split[2]));
                    }
                    catch {} // if it failed, we don't care. if it's an actual message it'll be resent
                }
            }
        }

        public bool GotKey(KeyboardManager.KMKey hotKey)
        {
            KeyboardManager.KMKey cancelKey = (KeyboardManager.KMKey)context.Hotkey.Clone();
            cancelKey.InvertCtrlDown();
            

            if (hotKey.Equals(context.Hotkey))
            {
                // yay
                this.currentNetProto.SendMessage("NETTIMER " + context.NetworkMessage + " " + context.Countdown.ToString());
                StartCountdown(context.Countdown);
                return true;
            }
            else if (hotKey.Equals(cancelKey))
            {
                this.currentNetProto.SendMessage("NETTIMER " + context.NetworkMessage + " CANCEL");
                endCountdown = DateTime.Now;
                EndCountdown();
                return true;
            }

            return false;
        }

        public void SyncData()
        {
            this.currentNetProto.SendMessage("NETTIMER " + context.NetworkMessage + " " + Math.Min(context.Countdown, Math.Max(0, Math.Round(endCountdown.Subtract(DateTime.Now).TotalSeconds))));
        }
    }
}
