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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Deployment.Application;

namespace NetJungleTimer
{
    /// <summary>
    /// Interaction logic for WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window
    {
        private Mutex m;
        private MainWindow mw;

        private INetProto NetJungleProto;

        private bool RunningMain = false;

        private bool UseLocalMode
        {
            get
            {
                return (LocalMode.IsChecked.HasValue && (bool)LocalMode.IsChecked);
            }
        }

        public WelcomeWindow()
        {
            // go go mutex
            bool createdNew;
            m = new Mutex(true, "NetJungleTimer", out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("You're already running me. ):", "You Retard", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                this.Close();
            }

            InitializeComponent();

            if (ApplicationDeployment.IsNetworkDeployed)
            {
                Version currentVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion;
#if DEBUG
                string whatBuild = "+D";
#elif RELEASE
                string whatBuild = "";
#else
                string whatBuild = "+?";
#endif
                versionLabel.Content = String.Format("v{0}{1}", currentVersion.ToString(), whatBuild);

                SetFormFieldsEnabled(false);
                SetStatusLabel("Checking for updates...");

                ForceCheckForUpdate();
            }
            else
            {
#if DEBUG
                string whatBuild = "DEBUG";
#elif RELEASE
                string whatBuild = "RELEASE";
#else
                string whatBuild = "???";
#endif
                versionLabel.Content = String.Format("<<DEVELOPMENT BUILD: {0}>>", whatBuild);
            }

            LoadLastConnectedSettings();
        }

        private void ForceCheckForUpdate()
        {
            ApplicationDeployment.CurrentDeployment.CheckForUpdateCompleted += new System.Deployment.Application.CheckForUpdateCompletedEventHandler(CheckForUpdateCompleted);
            ApplicationDeployment.CurrentDeployment.CheckForUpdateAsync();
        }

        private void CheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
        {
            if (e.UpdateAvailable)
            {
                MessageBox.Show("An update is available and is now being downloaded.");
                BeginUpdate();
            }
            else
            {
                SetStatusLabel("Ready.");
                SetFormFieldsEnabled(true);
            }
        }

        private void BeginUpdate()
        {
            ApplicationDeployment ad = ApplicationDeployment.CurrentDeployment;
            ad.UpdateCompleted += new System.ComponentModel.AsyncCompletedEventHandler(UpdateCompleted);
            ad.UpdateProgressChanged += new DeploymentProgressChangedEventHandler(UpdateProgressChanged);
            ad.UpdateAsync();
        }

        private void UpdateCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            MessageBox.Show("Update complete.");
            Application.Current.Shutdown(0);
        }

        private void UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            SetStatusLabel(String.Format("Updating... {0:D}%", e.ProgressPercentage));
        }

        private void SetFormFieldsEnabled(bool GloballyEnabled)
        {
            SetFormFieldsEnabled(GloballyEnabled, GloballyEnabled, GloballyEnabled);
        }

        private void SetFormFieldsEnabled(bool GloballyEnabled, bool ButtonEnabled)
        {
            SetFormFieldsEnabled(GloballyEnabled, ButtonEnabled, GloballyEnabled);
        }

        private void SetFormFieldsEnabled(bool GloballyEnabled, bool ButtonEnabled, bool NetworkingEnabled)
        {
            UserName.IsEnabled = NetworkingEnabled;
            ServerHost.IsEnabled = NetworkingEnabled;
            GameName.IsEnabled = NetworkingEnabled;
            ServerPassword.IsEnabled = NetworkingEnabled;

            LocalMode.IsEnabled = GloballyEnabled;
            SpeechSynth.IsEnabled = GloballyEnabled;

            ActionButton.IsEnabled = ButtonEnabled;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            GameName.SetCurrentValue(BackgroundProperty, DependencyProperty.UnsetValue);
            ServerHost.SetCurrentValue(BackgroundProperty, DependencyProperty.UnsetValue);
            UserName.SetCurrentValue(BackgroundProperty, DependencyProperty.UnsetValue);
            ServerPassword.SetCurrentValue(BackgroundProperty, DependencyProperty.UnsetValue);

            // do some validation
            int fieldsMustSpecify = 0;
            if (!UseLocalMode)
            {
                if (GameName.Text.Length == 0)
                {
                    GameName.Background = new SolidColorBrush(Colors.PaleVioletRed);
                    GameName.Focus();
                    SetStatusLabel("You must specify a NetJungle game lobby.");
                    fieldsMustSpecify++;
                }
                if (ServerHost.Text.Length == 0)
                {
                    ServerHost.Background = new SolidColorBrush(Colors.PaleVioletRed);
                    ServerHost.Focus();
                    SetStatusLabel("You must specify a NetJungle server.");
                    fieldsMustSpecify++;
                }
                if (UserName.Text.Length == 0)
                {
                    UserName.Background = new SolidColorBrush(Colors.PaleVioletRed);
                    UserName.Focus();
                    SetStatusLabel("You must specify a username.");
                    fieldsMustSpecify++;
                }
            }
            if (fieldsMustSpecify > 1)
                SetStatusLabel("You must fill in all the form fields.");
            if (fieldsMustSpecify != 0)
                return;

            if (!RunningMain)
            {
                if (UseLocalMode)
                    SetStatusLabel("Running...");
                else
                    SetStatusLabel("Connecting...");

                SetFormFieldsEnabled(false);

                this.StartMain();
            }
            else
            {
                if (UseLocalMode)
                    SetStatusLabel("Closing...");
                else
                    SetStatusLabel("Disconnecting...");

                RidSelfOfMainWindow();

                ActionButton.Content = (UseLocalMode) ? "Start" : "Connect";
                SetFormFieldsEnabled(true);
                SetStatusLabel("Ready.");
            }
        }

        private void RidSelfOfMainWindow()
        {
            RunningMain = false;
            App.Current.MainWindow = this;
            mw.Disconnect();
            mw.GoAway();
            mw.Close();
            mw = null;
        }


        private void StartMain()
        {
            if (!UseLocalMode)
            {

                String[] remote_server_port_bits = ServerHost.Text.Split(new char[] { ':' }, 2);
                String RemoteServer = remote_server_port_bits[0];
                int RemotePort = (int)uint.Parse(remote_server_port_bits[1]);
                String RemoteRoom = GameName.Text;

                NetJungleProto = (INetProto)new LiveNetProto(RemoteServer, RemotePort, UserName.Text, ServerPassword.Password, RemoteRoom);
                NetJungleProto.NewNetworkMessage += new NewNetworkMessageHandler(this.OnNetworkMessage);
                NetJungleProto.Go();
            }
            else
            {
                NetJungleProto = (INetProto)new MockupNetProto();
                this.OnNetworkMessage(this, new NewNetworkMessageEventArgs("&CONN")); // mock a connected message
            }
        }

        public void OnNetworkMessage(object sender, NewNetworkMessageEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(new Action<object, NewNetworkMessageEventArgs>(OnNetworkMessage), sender, e);
                return;
            }

            string message = e.NetworkMessage;

            if (message == "&CONN")
            {
                SetStatusLabel("Logging in...");
            }
            else if (message == "&LOGGEDIN")
            {
                mw = new MainWindow(this.m, this, this.NetJungleProto);
                mw.UseSpeechSynth = (bool)SpeechSynth.IsChecked;
                App.Current.MainWindow = mw;
                NetJungleProto.NewNetworkMessage -= new NewNetworkMessageHandler(this.OnNetworkMessage);
                mw.Show();

                SaveLastConnectedSettings();
                SetStatusLabel("Ready! The timer UI should now appear in game.\nEnsure you are in BORDERLESS or WINDOWED mode.");

                RunningMain = true;
                // woo

                ActionButton.Content = UseLocalMode ? "Stop" : "Disconnect";
                SetFormFieldsEnabled(false, true);
            }
            else if (message == "&BADPASS")
            {
                RunningMain = false;

                NetJungleProto.Stop();
                NetJungleProto = null;

                SetStatusLabel("Error: password incorrect.");
                ActionButton.Content = "Connect";
                SetFormFieldsEnabled(true, true);

                ServerPassword.Background = new SolidColorBrush(Colors.PaleVioletRed);
                ServerPassword.Focus();

            }
            else if (message == "!RECONNECT 4")
            {
                RunningMain = false;

                NetJungleProto.Stop();
                NetJungleProto = null;

                SetStatusLabel("Error: couldn't connect to server");
                ActionButton.Content = "Connect";
                SetFormFieldsEnabled(true, true);

            }
        }

        public void SaveLastConnectedSettings()
        {
            NetJungleTimer.Properties.Settings.Default.username = UserName.Text;
            NetJungleTimer.Properties.Settings.Default.hostname = ServerHost.Text;
            NetJungleTimer.Properties.Settings.Default.hostpassword = ServerPassword.Password;
            NetJungleTimer.Properties.Settings.Default.roomName = GameName.Text;
            NetJungleTimer.Properties.Settings.Default.useSpeechSynth = (bool)SpeechSynth.IsChecked;
            NetJungleTimer.Properties.Settings.Default.Save();
        }

        public void LoadLastConnectedSettings()
        {
            NetJungleTimer.Properties.Settings.Default.Reload();
            UserName.Text = NetJungleTimer.Properties.Settings.Default.username;
            ServerHost.Text = NetJungleTimer.Properties.Settings.Default.hostname;
            ServerPassword.Password = NetJungleTimer.Properties.Settings.Default.hostpassword;
            GameName.Text = NetJungleTimer.Properties.Settings.Default.roomName;
            SpeechSynth.IsChecked = NetJungleTimer.Properties.Settings.Default.useSpeechSynth;
        }

        public Dispatcher GetDispatcher()
        {
            return this.Dispatcher;
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mw != null)
            {
                if (NetJungleProto != null)
                    NetJungleProto.Stop();
                NetJungleProto = null;

                RidSelfOfMainWindow();
            }
            NetJungleTimer.Properties.Settings.Default.Save();
        }

        public void SetStatusLabel(String newText)
        {
            statusLabel.Content = String.Format("Status: {0}", newText);
        }

        private void LocalMode_Click(object sender, RoutedEventArgs e)
        {
            SetFormFieldsEnabled(true, true, !UseLocalMode);

            if (UseLocalMode)
                ActionButton.Content = "Start";
            else
                ActionButton.Content = "Connect";
        }
    }
}
