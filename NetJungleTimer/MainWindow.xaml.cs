using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace NetJungleTimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, NetProtoUI
    {
        const int UI_TIMER_TICK = 100;
        const int PROCESSING_TIMER_TICK = 250;
        const int NET_TIMER_TICK = 500;
        const int KEYBOARD_TIMER_TICK = 20;

        // our blue, our red, their blue, their red, dragon, baron
        // buffs: 5 mins
        // dragon: 6 mins (2:30)
        // baron: 7 mins (15:00)
        //const int BUFF_TIME = 5 * 60;
        const int BUFF_TIME = 5 * 60;
        const int DRAGON_TIME = 6 * 60;
        const int BARON_TIME = 7 * 60;
        const int WARD_TIME = 3 * 60;
        const int INHIBITOR_TIME = 5 * 60;

        DispatcherTimer uiTimer;
        DispatcherTimer processingTimer;

        IntPtr leagueOfLegendsWindowHndl;

        NetProto netJungleProto;
        NetworkedTimer[] networkedTimers;
        public KeyboardManager keyboardManager;

        WelcomeWindow welWin;
        private Mutex m;
        private bool propagateExit = false;

        DateTime masterLastResyncedData;

        SpeechSynthesizer synth = new SpeechSynthesizer();

        public MainWindow(Mutex mut, WelcomeWindow welWin, NetProto netJungleProto)
        {
            this.m = mut;

            InitializeComponent();

            this.welWin = welWin;
            this.netJungleProto = netJungleProto;

            ResetState();
        }

        ~MainWindow()
        {
            GoAway();
        }

        public void GoAway()
        {
            if (uiTimer != null)
                uiTimer.Stop();
            uiTimer = null;

            if (processingTimer != null)
                processingTimer.Stop();
            processingTimer = null;

            if (synth != null)
                synth.Dispose();
            synth = null;

            networkedTimers = null; // bye ;D

            netJungleProto = null;
            welWin = null;
            keyboardManager = null;
        }

        protected void ResetState()
        {
            keyboardManager = null;
            networkedTimers = null;

            keyboardManager = new KeyboardManager(this);

            // let's go
            networkedTimers = new NetworkedTimer[6];
            networkedTimers[0] = new NetworkedTimer(this, new NetworkedTimerContext(ourBlueImg, BUFF_TIME, "OUR_BLUE", new KeyboardManager.KMKey(Key.NumPad7), "Our blue"));
            networkedTimers[1] = new NetworkedTimer(this, new NetworkedTimerContext(ourRedImg, BUFF_TIME, "OUR_RED", new KeyboardManager.KMKey(Key.NumPad4), "Our red"));
            networkedTimers[2] = new NetworkedTimer(this, new NetworkedTimerContext(baronImg, BARON_TIME, "BARON", new KeyboardManager.KMKey(Key.NumPad8), "Baron"));
            networkedTimers[3] = new NetworkedTimer(this, new NetworkedTimerContext(dragonImg, DRAGON_TIME, "DRAGON", new KeyboardManager.KMKey(Key.NumPad5), "Dragon"));
            networkedTimers[4] = new NetworkedTimer(this, new NetworkedTimerContext(theirBlueImg, BUFF_TIME, "THEIR_BLUE", new KeyboardManager.KMKey(Key.NumPad9), "Their blue"));
            networkedTimers[5] = new NetworkedTimer(this, new NetworkedTimerContext(theirRedImg, BUFF_TIME, "THEIR_RED", new KeyboardManager.KMKey(Key.NumPad6), "Their red"));

            // now for our quit hotkey...
            keyboardManager.ListenToKey(new KeyboardManager.KMKey(Key.NumLock, true, true, false));

            keyboardManager.ListenToKey(new KeyboardManager.KMKey(Key.F9, true, true, false));
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);


            // this has to happen *after* all the Windows initialisation is done.
            WindowInteropHelper wiHelper = new WindowInteropHelper(this);
            WindowsApi.SetWindowNoActivate(wiHelper.Handle);
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;

            uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(UI_TIMER_TICK);
            uiTimer.Tick += new EventHandler(uiTimer_Tick);
            uiTimer.Start();

            processingTimer = new DispatcherTimer();
            processingTimer.Interval = TimeSpan.FromMilliseconds(PROCESSING_TIMER_TICK);
            processingTimer.Tick += new EventHandler(processingTimer_Tick);
            processingTimer.Start();
        }

        public void OnNetworkMessage(String message)
        {
            if (message.StartsWith("NETTIMER "))
            {
                foreach (NetworkedTimer netTimer in networkedTimers)
                {
                    netTimer.GotMessage(message);
                }
            }
            else if (message.StartsWith("!DISCONNECT"))
            {
                connectionStatusText.Content = "Connection lost";
            }
            else if (message.StartsWith("!RECONNECT"))
            {
                String[] messageSplit = message.Split(new[] { ' ' });
                connectionStatusText.Content = String.Format("Connect attempt #{0}", messageSplit[1]);
            }
            else if (message.StartsWith("CONN") && netJungleProto.IsMaster)
            {
                SyncData();
            }

            if (!message.StartsWith("!")) // internal message
            {
                if (netJungleProto.IsMaster)
                    connectionStatusText.Content = "Connected [MASTER]";
                else
                    connectionStatusText.Content = "Connected";
            }
        }

        private void SyncData()
        {
            if (!netJungleProto.IsMaster)
                return;

            masterLastResyncedData = DateTime.Now;

            foreach (NetworkedTimer jt in networkedTimers)
            {
                jt.SyncData();
            }
        }

        private void uiTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visibility != Visibility.Visible)
                return;

            DateTime now = DateTime.Now;
            foreach (NetworkedTimer jt in networkedTimers)
            {
                jt.UpdateComponent(now);
            }
        }

        private void processingTimer_Tick(object sender, EventArgs e)
        {
            if (propagateExit)
            {
                keyboardManager.EnsureNumLockEnabled();
                System.Environment.Exit(0);
            }


            StringBuilder sb = new StringBuilder(256);
            IntPtr tempHandle = WindowsApi.GetForegroundWindowHandle();
            String windowCaption = WindowsApi.GetWindowCaption(tempHandle);

            uint windowStyle = (uint)WindowsApi.GetWindowStyle(tempHandle);

            // okay, so now we have the name of this mystery foreground window...
            if (windowCaption.ToLower().Contains("league of legends (tm) client"))
            {
                keyboardManager.EnsureNumLockEnabled();
                DateTime start = DateTime.Now;

                // let's go move ourselves to the right place
                Rect leagueOfLegendsWindowDimensions = WindowsApi.GetWindowDims(tempHandle);

                if (leagueOfLegendsWindowDimensions.Width > 1000) // have we got actual dimensions yet?
                {
                    int topMod = 0;
                    int leftMod = 0;
                    int botMod = 0;
                    int rightMod = 0;
                    if ((windowStyle & (WindowsApi.WindowStyle.WS_EX_LAYERED)) == WindowsApi.WindowStyle.WS_EX_LAYERED)
                    {
                        botMod = leftMod = rightMod = (System.Windows.Forms.SystemInformation.FrameBorderSize.Width / 2);
                        topMod = System.Windows.Forms.SystemInformation.CaptionHeight + leftMod;
                    }
                    this.Left = leagueOfLegendsWindowDimensions.Left + leftMod;
                    this.Top = leagueOfLegendsWindowDimensions.Top + topMod;
                    this.Width = leagueOfLegendsWindowDimensions.Width - leftMod - rightMod;
                    this.Height = leagueOfLegendsWindowDimensions.Height - topMod - botMod;
                    this.Visibility = Visibility.Visible;

                    leagueOfLegendsWindowHndl = tempHandle; // we found it <3
                }
            }
            else
            {
                this.Visibility = Visibility.Hidden;
            }

            sb = null; // nuke the stringbuilder


            if (leagueOfLegendsWindowHndl != IntPtr.Zero) // if we've already FOUND the LoL window...
            {
                uint lolWindowStyle = WindowsApi.GetWindowStyle(leagueOfLegendsWindowHndl);
                if (lolWindowStyle == 0) // if it doesn't seem to exist
                {
                    this.ResetState();
                }
            }

            if (netJungleProto.IsMaster && masterLastResyncedData.Add(new TimeSpan(0, 0, 20)) < DateTime.Now)
            {
                this.SyncData();
            }

        }

        public bool OnHotKeyHandler(KeyboardManager.KMKey key)
        {
            if (key.Equals(new KeyboardManager.KMKey(Key.NumLock, true, true, false)))
            {
                propagateExit = true;
                return true;
            }
            else if (key.Equals(new KeyboardManager.KMKey(Key.F9, true, true, false)))
            {
                if (leagueOfLegendsWindowHndl != IntPtr.Zero) // if we've already FOUND the LoL window...
                {
                    WindowsApi.MoveWindowToSensibleLocation(leagueOfLegendsWindowHndl);
                    return false;
                }
            }

            if (this.Visibility != Visibility.Visible)
                return false;

            bool suppress = false;

            foreach (NetworkedTimer jt in networkedTimers)
            {
                if (jt.GotKey(key))
                    suppress = true;
            }
            return suppress;
        }

        public void NetBroadcast(String what)
        {
            netJungleProto.SendMessage(what);
        }

        internal void Disconnect()
        {
            netJungleProto.Stop();
        }

        public Dispatcher GetDispatcher()
        {
            return this.Dispatcher;
        }

        internal void OnTimerExpiry(NetworkedTimer thisTimer)
        {
            /*if (netJungleProto.IsMaster)
            {
                keyboardManager.SendAlliedChatMessage(thisTimer.context.ChatMessageComplete);
            }*/
            if (this.UseSpeechSynth)
                synth.SpeakAsync(String.Format("{0}'s up.", thisTimer.context.ChatMessageComplete));
        }

        internal void OnTimerFinalCountdown(NetworkedTimer thisTimer, int finalLength)
        {
            if (this.UseSpeechSynth)
                synth.SpeakAsync(String.Format("{0} will be up in {1} seconds.", thisTimer.context.ChatMessageComplete, finalLength));
        }

        public bool UseSpeechSynth { get; set; }
    }
}
