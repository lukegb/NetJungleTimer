using System;
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

namespace NetJungleTimer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int UI_TIMER_TICK = 500;
        const int PROCESSING_TIMER_TICK = 500;
        const int NET_TIMER_TICK = 500;
        const int KEYBOARD_TIMER_TICK = 20;

        const String REMOTE_SERVER = "guy.deserves.a.hug.at.lukegb.com";
        const int REMOTE_PORT = 9446;

        DispatcherTimer uiTimer;
        DispatcherTimer processingTimer;
        DispatcherTimer netTimer;
        DispatcherTimer kbdTimer;

        // our blue, our red, their blue, their red, dragon, baron
        // buffs: 5 mins
        // dragon: 6 mins (2:30)
        // baron: 7 mins (15:00)
        //const int BUFF_TIME = 5 * 60;
        const int BUFF_TIME = 5 * 60;
        const int DRAGON_TIME = 6 * 60;
        const int BARON_TIME = 7 * 60;

        IntPtr leagueOfLegendsWindowHndl;

        NetProto netJungleProto;
        JungleTimer[] jungleTimers;
        public KeyboardManager keyboardManager;

        YesImRunningWindow yirw;
        private Mutex m;

        private bool _isMaster;
        public bool isMaster
        {
            get
            {
                return this._isMaster;
            }
            private set
            {
                this._isMaster = value;
            }
        }

        public MainWindow()
        {
            netJungleProto = new NetProto(REMOTE_SERVER, REMOTE_PORT);

            InitializeComponent();

            yirw = new YesImRunningWindow();
            yirw.Left = 0;
            yirw.Top = 0;
            yirw.Show();
            yirw.Left = 0;
            yirw.Top = 0;

            ResetState();
            
        }

        protected void ResetState()
        {
            keyboardManager = new KeyboardManager(this);

            // let's go
            jungleTimers = new JungleTimer[6];
            jungleTimers[0] = new JungleTimer(this, ourBlueImg, BUFF_TIME, "OUR_BLUE", Key.NumPad7);
            jungleTimers[1] = new JungleTimer(this, ourRedImg, BUFF_TIME, "OUR_RED", Key.NumPad4);
            jungleTimers[2] = new JungleTimer(this, theirBlueImg, BUFF_TIME, "THEIR_BLUE", Key.NumPad9);
            jungleTimers[3] = new JungleTimer(this, theirRedImg, BUFF_TIME, "THEIR_RED", Key.NumPad6);
            jungleTimers[4] = new JungleTimer(this, dragonImg, DRAGON_TIME, "DRAGON", Key.NumPad5);
            jungleTimers[5] = new JungleTimer(this, baronImg, BARON_TIME, "BARON", Key.NumPad8);

            // now for our quit hotkey...
            keyboardManager.ListenToKey(Key.NumLock);
        }

        protected void Connect()
        {
            if (netJungleProto.Connected)
                netJungleProto.Disconnect();

            connectionStatusText.Content = "Connecting...";
            try
            {
                netJungleProto.Connect();
                connectionStatusText.Content = "Connected.";
            }
            catch (Exception err)
            {
                connectionStatusText.Content = "FAILED: " + err.Message;
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);


            // this has to happen *after* all the Windows initialisation is done.
            WindowInteropHelper wiHelper = new WindowInteropHelper(this);
            WindowsApi.SetWindowNoActivate(wiHelper.Handle);

            wiHelper = new WindowInteropHelper(yirw);
            WindowsApi.SetWindowNoActivate(wiHelper.Handle);
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Hidden;

            // go go mutex
            bool createdNew;
            m = new Mutex(true, "NetJungleTimer", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("You're already running me. ):", "You Retard", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                this.Close();
            }

            uiTimer = new DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromMilliseconds(UI_TIMER_TICK);
            uiTimer.Tick += new EventHandler(uiTimer_Tick);
            uiTimer.Start();

            processingTimer = new DispatcherTimer();
            processingTimer.Interval = TimeSpan.FromMilliseconds(PROCESSING_TIMER_TICK);
            processingTimer.Tick += new EventHandler(processingTimer_Tick);
            processingTimer.Start();

            netTimer = new DispatcherTimer();
            netTimer.Interval = TimeSpan.FromMilliseconds(NET_TIMER_TICK);
            netTimer.Tick += new EventHandler(netTimer_Tick);
            netTimer.Start();

            kbdTimer = new DispatcherTimer();
            kbdTimer.Interval = TimeSpan.FromMilliseconds(KEYBOARD_TIMER_TICK);
            kbdTimer.Tick += new EventHandler(kbdTimer_Tick);
            kbdTimer.Start();
        }

        private void netTimer_Tick(object sender, EventArgs e)
        {
            String retData = netJungleProto.ReadData();
            if (retData == "RECONNECT")
            {
                Connect();
            }
            else if (retData != null)
            {
                connectionStatusText.Content = String.Format("Received: {0}", retData);

                if (retData.StartsWith("JUNGLETIMER "))
                {
                    foreach (JungleTimer jt in jungleTimers)
                    {
                        jt.GotMessage(retData);
                    }
                }

            }
        }

        private void uiTimer_Tick(object sender, EventArgs e)
        {
            foreach (JungleTimer jt in jungleTimers)
            {
                jt.UpdateComponent();
            }
        }

        private void kbdTimer_Tick(object sender, EventArgs e)
        {
            keyboardManager.Update();
        }

        private void processingTimer_Tick(object sender, EventArgs e)
        {
            StringBuilder sb = new StringBuilder(256);
            IntPtr tempHandle = WindowsApi.GetForegroundWindowHandle();
            String windowCaption = WindowsApi.GetWindowCaption(tempHandle);

            uint windowStyle = (uint)WindowsApi.GetWindowStyle(tempHandle);

            // okay, so now we have the name of this mystery foreground window...
            if (windowCaption.ToLower().Contains("league of legends (tm) client"))
            {
                yirw.Visibility = Visibility.Hidden;
                yirw.Left = 0;
                yirw.Top = 0;
                this.Visibility = Visibility.Visible;
                DateTime start = DateTime.Now;

                // let's go move ourselves to the right place
                Rect leagueOfLegendsWindowDimensions = WindowsApi.GetWindowDims(tempHandle);
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

                leagueOfLegendsWindowHndl = tempHandle; // we found it <3
            }
            else
            {
                yirw.Visibility = Visibility.Visible;
                this.Visibility = Visibility.Hidden;
            }

            sb = null; // nuke the stringbuilder

            
            if (leagueOfLegendsWindowHndl != IntPtr.Zero) // if we've already FOUND the LoL window...
            {
                uint lolWindowStyle = WindowsApi.GetWindowStyle(leagueOfLegendsWindowHndl);
                if (lolWindowStyle == 0) // if it doesn't seem to exist
                {
                    self.ResetState();
                }
            }
            
        }

        public void OnHotKeyHandler(Key key)
        {
            if (key == Key.NumLock)
            {

                System.Environment.Exit(0);
            }

            foreach (JungleTimer jt in jungleTimers)
            {
                jt.GotKey(key);
            }
        }

        public void NetBroadcast(String what)
        {
            netJungleProto.SendMessage(what);
        }
    }
}
