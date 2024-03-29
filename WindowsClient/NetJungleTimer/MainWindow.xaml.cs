﻿using System;
using System.Collections.Generic;
using System.Linq;
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

        private bool _spectatorModeActive = false;
        public bool SpectatorModeActive
        {
            get 
            {
                return _spectatorModeActive;
            }

            set
            {
                _spectatorModeActive = (bool)value;

                UpdateJungleTimerOffsets();
            }
        }
        private const int SPECTATOR_OFFSET = 3 * 60;

        MapMode currentMapMode;

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

        internal Dictionary<MapMode, RowDefinition> rowDefs;

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

        private bool alwaysShowWindow = false;
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

        public MainWindow(Mutex mut, WelcomeWindow welWin, INetProto netJungleProto, MapMode selectedMapMode)
        {
            this.m = mut;
            this.currentMapMode = selectedMapMode;

            InitializeComponent();

            rowDefs = new Dictionary<MapMode, RowDefinition>
            {
                { MapMode.SUMMONERS_RIFT, summonersRiftRowDefinition }
            };

            this.welWin = welWin;
            this.netJungleProto = netJungleProto;

            netJungleProto.NewNetworkMessage += new NewNetworkMessageHandler(this.OnNetworkMessage);

            KeyboardManager.Instance.HotKeyPressed += new HotKeyPressedEventHandler(OnHotKeyHandlerWrapper);

            ResetState();

            if (netJungleProto is MockupNetProto)
            {
                nowPlayingRowDefinition.Height = new GridLength(0);
                statusRowDefinition.Height = new GridLength(0);
            }

            // okay, here goes nothing
            foreach (KeyValuePair<MapMode, RowDefinition> rowDefKvp in rowDefs)
            {
                if (rowDefKvp.Key != selectedMapMode)
                {
                    rowDefKvp.Value.Height = new GridLength(0);
                }
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
            switch (currentMapMode)
            {
                case MapMode.SUMMONERS_RIFT:
                    uiElements = new List<IUIElement>
                    {
                        new NetworkedTimer(new NetworkedTimerContext(ourBlueImg, BUFF_TIME, "OUR_BLUE", new KeyboardManager.KMKey(Key.NumPad7), "Our blue"), this.netJungleProto),
                        new NetworkedTimer(new NetworkedTimerContext(ourRedImg, BUFF_TIME, "OUR_RED", new KeyboardManager.KMKey(Key.NumPad4), "Our red"), this.netJungleProto),
                        new NetworkedTimer(new NetworkedTimerContext(baronImg, BARON_TIME, "BARON", new KeyboardManager.KMKey(Key.NumPad8), "Baron"), this.netJungleProto),
                        new NetworkedTimer(new NetworkedTimerContext(dragonImg, DRAGON_TIME, "DRAGON", new KeyboardManager.KMKey(Key.NumPad5), "Dragon"), this.netJungleProto),
                        new NetworkedTimer(new NetworkedTimerContext(theirBlueImg, BUFF_TIME, "THEIR_BLUE", new KeyboardManager.KMKey(Key.NumPad9), "Their blue"), this.netJungleProto),
                        new NetworkedTimer(new NetworkedTimerContext(theirRedImg, BUFF_TIME, "THEIR_RED", new KeyboardManager.KMKey(Key.NumPad6), "Their red"), this.netJungleProto),

                        new MuseBotNP(nowPlayingText),
                    };
                    break;
                default:
                    // wtf
                    throw new Exception("WTF");
                    break;
            }



            foreach (IUIElement iui in uiElements)
            {
                if (!(iui is NetworkedTimer))
                    continue;

                var nt = iui as NetworkedTimer;
                nt.TimerExpired += new TimerExpiryHandler(OnTimerExpiry);
                nt.TimerFinalCountdownReached += new TimerFinalCountHandler(OnTimerFinalCountdown);
                TextToSpeech.Instance.PrecacheTimer(nt);
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
            if (!this.alwaysShowWindow)
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

        private void SetStatusLine(string NewStatusLine)
        {
            StringBuilder sb = new StringBuilder(NewStatusLine);

            if (netJungleProto.Connected && netJungleProto.IsMaster)
            {
                sb.Append(" [MAST]");
            }

            if (SpectatorModeActive)
            {
                sb.Append(" [SPEC]");
            }

            connectionStatusText.Content = sb.ToString();
        }

        public void OnNetworkMessage(object sender, NewNetworkMessageEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(new Action<object, NewNetworkMessageEventArgs>(OnNetworkMessage), sender, e);
                return;
            }

            string message = e.NetworkMessage;

            if (message.StartsWith("NETTIMER ") || message.StartsWith("MUSEBOTNP "))
            {
                foreach (UI.IUIElement iui in uiElements)
                {
                    if (message.StartsWith("NETTIMER ") && !(iui is NetworkedTimer))
                        continue;
                    else if (message.StartsWith("^MUSEBOTNP ") && !(iui is MuseBotNP))
                        continue;

                    iui.GotMessage(message);
                }
            }
            else if (message.StartsWith("!DISCONNECT"))
            {
                SetStatusLine("Connection lost");
            }
            else if (message.StartsWith("!RECONNECT"))
            {
                String[] messageSplit = message.Split(new[] { ' ' });
                SetStatusLine(String.Format("Connect attempt #{0}", messageSplit[1]));
            }
            else if (message == "!BADLINEVER")
            {
                welWin.mainWindowAbort("Bad NetJungleTimer version for room.");
            }
            else if (message.StartsWith("CONN") && netJungleProto.IsMaster)
            {
                SyncData();
            }

            if (!message.StartsWith("!")) // internal message
            {
                SetStatusLine("Connected");
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

                    if (!this.alwaysShowWindow)
                        this.Visibility = Visibility.Visible;

                    leagueOfLegendsWindowHndl = tempHandle; // we found it <3
                }
            }
            else
            {
                if (!this.alwaysShowWindow)
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
            else if (key.Equals(new KeyboardManager.KMKey(Key.F10, true, true, false)))
            {
                if (leagueOfLegendsWindowHndl != IntPtr.Zero) // if we've already FOUND the LoL window...
                {
                    KeyboardManager.Instance.SendAlliedChatMessage("test");
                }
            }

            // konami code check
            if (konamiCode[konamiCodeSteps] == key.Key)
            {
                if (++konamiCodeSteps == konamiCode.Length)
                {
                    konamiCodeSteps = 0;
                    alwaysShowWindow = !alwaysShowWindow;
                    SetStatusLine("KONAMI CODE HACKTIVATED");
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
        }

        internal void OnTimerFinalCountdown(object sender, EventArgs e)
        {
            NetworkedTimer nt = sender as NetworkedTimer;
        }

        private void UpdateJungleTimerOffsets()
        {
            foreach (IUIElement iui in uiElements)
            {
                if (!(iui is NetworkedTimer))
                    continue;

                var nt = iui as NetworkedTimer;
                nt.TimingOffset = SpectatorModeActive ? SPECTATOR_OFFSET : 0;
            }
        }

        public bool UseSpeechSynth { get; set; }
    }
}
