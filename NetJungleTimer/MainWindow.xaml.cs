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
using NetJungleTimer.Networking;
using NetJungleTimer.UI;

namespace NetJungleTimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
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

        INetProto netJungleProto;
        //UI.NetworkedTimer[] networkedTimers;
        List<IUIElement> uiElements;
        public KeyboardManager KeyboardManager;

        WelcomeWindow welWin;
        private Mutex m;
        private bool propagateExit = false;

        DateTime masterLastResyncedData;

        SpeechSynthesizer synth = new SpeechSynthesizer();

        private bool showHideWindow = false;
        int konamiCodeSteps = 0;
        Key[] konamiCode = new Key[] {
            Key.Up,
            Key.Up,
            Key.Down,
            Key.Down,
            Key.Left,
            Key.Right,
            Key.Left,
            Key.Right,
            Key.B,
            Key.A
        };

        public MainWindow(Mutex mut, WelcomeWindow welWin, INetProto netJungleProto)
        {
            this.m = mut;

            InitializeComponent();

            this.welWin = welWin;
            this.netJungleProto = netJungleProto;

            netJungleProto.NewNetworkMessage += new NewNetworkMessageHandler(this.OnNetworkMessage);

            KeyboardManager.Instance.HotKeyPressed += new HotKeyPressedEventHandler(OnHotKeyHandlerWrapper);

            ResetState();

            if (netJungleProto is MockupNetProto)
            {
                this.layoutGrid.RowDefinitions[0].Height = new GridLength(0);
            }
        }

        ~MainWindow()
        {
            GoAway();
        }

        public void GoAway()
        {
            leagueOfLegendsWindowHndl = IntPtr.Zero;

            if (uiTimer != null)
                uiTimer.Stop();
            uiTimer = null;

            if (processingTimer != null)
                processingTimer.Stop();
            processingTimer = null;

            if (synth != null)
                synth.Dispose();
            synth = null;

            if (uiElements != null)
                uiElements.Clear();
            uiElements = null; // bye ;D

            if (netJungleProto != null)
                netJungleProto.NewNetworkMessage -= new NewNetworkMessageHandler(this.OnNetworkMessage);
            netJungleProto = null;

            KeyboardManager.Instance.HotKeyPressed -= new HotKeyPressedEventHandler(OnHotKeyHandlerWrapper);

            welWin = null;
        }

        protected void ResetState()
        {
            if (uiElements != null)
                uiElements.Clear();
            uiElements = null;

            // let's go
            uiElements = new List<IUIElement>
            {
                new NetworkedTimer(new NetworkedTimerContext(ourBlueImg, BUFF_TIME, "OUR_BLUE", new KeyboardManager.KMKey(Key.NumPad7), "Our blue"), this.netJungleProto),
                new NetworkedTimer(new NetworkedTimerContext(ourRedImg, BUFF_TIME, "OUR_RED", new KeyboardManager.KMKey(Key.NumPad4), "Our red"), this.netJungleProto),
                new NetworkedTimer(new NetworkedTimerContext(baronImg, BARON_TIME, "BARON", new KeyboardManager.KMKey(Key.NumPad8), "Baron"), this.netJungleProto),
                new NetworkedTimer(new NetworkedTimerContext(dragonImg, DRAGON_TIME, "DRAGON", new KeyboardManager.KMKey(Key.NumPad5), "Dragon"), this.netJungleProto),
                new NetworkedTimer(new NetworkedTimerContext(theirBlueImg, BUFF_TIME, "THEIR_BLUE", new KeyboardManager.KMKey(Key.NumPad9), "Their blue"), this.netJungleProto),
                new NetworkedTimer(new NetworkedTimerContext(theirRedImg, BUFF_TIME, "THEIR_RED", new KeyboardManager.KMKey(Key.NumPad6), "Their red"), this.netJungleProto)
            };

            foreach (IUIElement iui in uiElements)
            {
                if (!(iui is NetworkedTimer))
                    continue;
                var nt = iui as NetworkedTimer;
                nt.TimerExpired += new TimerExpiryHandler(OnTimerExpiry);
                nt.TimerFinalCountdownReached += new TimerFinalCountHandler(OnTimerFinalCountdown);
            }
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
            if (!this.showHideWindow)
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

        public void OnNetworkMessage(object sender, NewNetworkMessageEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(new Action<object, NewNetworkMessageEventArgs>(OnNetworkMessage), sender, e);
                return;
            }

            string message = e.NetworkMessage;

            if (message.StartsWith("NETTIMER "))
            {
                foreach (UI.IUIElement iui in uiElements)
                {
                    if (!(iui is NetworkedTimer))
                        continue;

                    var netTimer = iui as NetworkedTimer;
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

            foreach (IUIElement uie in uiElements)
            {
                uie.SyncData();
            }
        }

        private void uiTimer_Tick(object sender, EventArgs e)
        {
            if (this.Visibility != Visibility.Visible)
                return;

            DateTime now = DateTime.Now;
            foreach (IUIElement uie in uiElements)
            {
                uie.UpdateComponent(now);
            }
        }

        private void processingTimer_Tick(object sender, EventArgs e)
        {
            if (propagateExit)
            {
                KeyboardManager.Instance.EnsureNumLockEnabled();
                System.Environment.Exit(0);
            }


            StringBuilder sb = new StringBuilder(256);
            IntPtr tempHandle = WindowsApi.GetForegroundWindowHandle();
            String windowCaption = WindowsApi.GetWindowCaption(tempHandle);

            uint windowStyle = (uint)WindowsApi.GetWindowStyle(tempHandle);

            // okay, so now we have the name of this mystery foreground window...
            if (windowCaption.ToLower().Contains("league of legends (tm) client"))
            {
                KeyboardManager.Instance.EnsureNumLockEnabled();
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

                    if (!this.showHideWindow)
                        this.Visibility = Visibility.Visible;

                    leagueOfLegendsWindowHndl = tempHandle; // we found it <3
                }
            }
            else
            {
                if (!this.showHideWindow)
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

        private void OnHotKeyHandlerWrapper(object sender, HotKeyPressedEventArgs e)
        {
            OnHotKeyHandler(sender, e);
        }

        private bool OnHotKeyHandler(object sender, HotKeyPressedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(new Func<object, HotKeyPressedEventArgs, bool>(OnHotKeyHandler), sender, e);
                return false;
            }

            KeyboardManager.KMKey key = e.Key;

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

            // konami code check
            if (konamiCode[konamiCodeSteps] == key.Key)
            {
                if (++konamiCodeSteps == konamiCode.Length)
                {
                    konamiCodeSteps = 0;
                    showHideWindow = !showHideWindow;
                    connectionStatusText.Content = "KONAMI CODE HACKTIVATED";
                    this.Visibility = Visibility.Visible;
                    this.Top = 0;
                    this.Left = 0;
                    this.Width = System.Windows.SystemParameters.FullPrimaryScreenWidth;
                    this.Height = System.Windows.SystemParameters.FullPrimaryScreenHeight;
                }
            }
            else
            {
                konamiCodeSteps = 0;
            }

            
            if (this.Visibility != Visibility.Visible)
                return false;

            bool suppress = false;

            foreach (IUIElement iui in uiElements)
            {
                if (iui.GotKey(key))
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

        internal void OnTimerExpiry(object sender, EventArgs e)
        {
            NetworkedTimer nt = sender as NetworkedTimer;

            if (this.UseSpeechSynth)
                synth.SpeakAsync(String.Format("{0}'s up.", nt.context.ChatMessageComplete));
        }

        internal void OnTimerFinalCountdown(object sender, EventArgs e)
        {
            NetworkedTimer nt = sender as NetworkedTimer;

            if (this.UseSpeechSynth)
                synth.SpeakAsync(String.Format("{0} will be up in {1} seconds.", nt.context.ChatMessageComplete, NetworkedTimer.PRE_WARNING));
        }

        public bool UseSpeechSynth { get; set; }
    }
}
