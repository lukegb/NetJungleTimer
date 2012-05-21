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
    public partial class WelcomeWindow : Window, NetProtoUI
    {
        private Mutex m;
        private MainWindow mw;

        private NetProto netJungleProto;

        private bool connected = false;

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
                versionLabel.Content = String.Format("v{0}", currentVersion.ToString());

                SetFormFieldsEnabled(false);
                SetStatusLabel("Checking for updates...");

                ForceCheckForUpdate();
            }
            else
            {
                versionLabel.Content = "<<DEVELOPMENT BUILD>>";
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
            SetFormFieldsEnabled(GloballyEnabled, GloballyEnabled);
        }

        private void SetFormFieldsEnabled(bool GloballyEnabled, bool ButtonEnabled)
        {
            username.IsEnabled = GloballyEnabled;
            serverHost.IsEnabled = GloballyEnabled;
            gameName.IsEnabled = GloballyEnabled;
            speechSynth.IsEnabled = GloballyEnabled;

            actionButton.IsEnabled = ButtonEnabled;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            gameName.SetCurrentValue(BackgroundProperty, DependencyProperty.UnsetValue);
            serverHost.SetCurrentValue(BackgroundProperty, DependencyProperty.UnsetValue);
            username.SetCurrentValue(BackgroundProperty, DependencyProperty.UnsetValue);

            // do some validation
            int fieldsMustSpecify = 0;
            if (gameName.Text.Length == 0)
            {
                gameName.Background = new SolidColorBrush(Colors.PaleVioletRed);
                gameName.Focus();
                SetStatusLabel("You must specify a NetJungle game lobby.");
                fieldsMustSpecify++;
            }
            if (serverHost.Text.Length == 0)
            {
                serverHost.Background = new SolidColorBrush(Colors.PaleVioletRed);
                serverHost.Focus();
                SetStatusLabel("You must specify a NetJungle server.");
                fieldsMustSpecify++;
            }
            if (username.Text.Length == 0)
            {
                username.Background = new SolidColorBrush(Colors.PaleVioletRed);
                username.Focus();
                SetStatusLabel("You must specify a username.");
                fieldsMustSpecify++;
            }
            if (fieldsMustSpecify > 1)
                SetStatusLabel("You must fill in all the form fields.");
            if (fieldsMustSpecify != 0)
                return;

            if (!connected)
            {
                SetStatusLabel("Connecting...");
                this.Connect();
                SetFormFieldsEnabled(false);
            }
            else
            {
                SetStatusLabel("Disconnecting...");
                connected = false;
                App.Current.MainWindow = this;
                mw.Disconnect();
                mw.GoAway();
                mw = null;
                actionButton.Content = "Connect";
                SetFormFieldsEnabled(true);
                SetStatusLabel("Ready.");
            }
        }


        private void Connect()
        {

            String[] remote_server_port_bits = serverHost.Text.Split(new char[] { ':' }, 2);
            String RemoteServer = remote_server_port_bits[0];
            int RemotePort = (int)uint.Parse(remote_server_port_bits[1]);
            String RemoteRoom = gameName.Text;

            netJungleProto = new NetProto((NetProtoUI)this, RemoteServer, RemotePort, username.Text, RemoteRoom);
            netJungleProto.Go();
        }


        public void OnNetworkMessage(String message)
        {
            Console.WriteLine(message);
            if (message == "&CONN")
            {
                mw = new MainWindow(this.m, this, this.netJungleProto);
                mw.UseSpeechSynth = (bool)speechSynth.IsChecked;
                App.Current.MainWindow = mw;
                netJungleProto.parent = (NetProtoUI)mw;
                mw.Show();

                SaveLastConnectedSettings();
                SetStatusLabel("Connected! The timer UI should now appear in game.\nEnsure you are in BORDERLESS or WINDOWED mode.");

                connected = true;
                // woo
                actionButton.Content = "Disconnect";
                SetFormFieldsEnabled(false, true);
            }
            else if (message == "!RECONNECT 4")
            {
                connected = false;

                SetStatusLabel("Error: couldn't connect to server");
                actionButton.Content = "Connect";
                SetFormFieldsEnabled(true, true);

            }
        }

        public void SaveLastConnectedSettings()
        {
            NetJungleTimer.Properties.Settings.Default.username = username.Text;
            NetJungleTimer.Properties.Settings.Default.hostname = serverHost.Text;
            NetJungleTimer.Properties.Settings.Default.roomName = gameName.Text;
            NetJungleTimer.Properties.Settings.Default.useSpeechSynth = (bool)speechSynth.IsChecked;
            NetJungleTimer.Properties.Settings.Default.Save();
        }

        public void LoadLastConnectedSettings()
        {
            NetJungleTimer.Properties.Settings.Default.Reload();
            username.Text = NetJungleTimer.Properties.Settings.Default.username;
            serverHost.Text = NetJungleTimer.Properties.Settings.Default.hostname;
            gameName.Text = NetJungleTimer.Properties.Settings.Default.roomName;
            speechSynth.IsChecked = NetJungleTimer.Properties.Settings.Default.useSpeechSynth;
        }

        public Dispatcher GetDispatcher()
        {
            return this.Dispatcher;
        }

        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (mw != null)
            {
                mw.Disconnect();
                mw.GoAway();
                mw.Close();
                App.Current.MainWindow = this;
                mw = null;
            }
        }

        public void SetStatusLabel(String newText)
        {
            statusLabel.Content = String.Format("Status: {0}", newText);
        }
    }
}
