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
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            SetStatusLabel("");
            if (!connected)
            {
                actionButton.Content = "Connecting...";
                this.Connect();
                serverHost.IsEnabled = false;
                gameName.IsEnabled = false;
                actionButton.IsEnabled = false;
            }
            else
            {
                connected = false;
                serverHost.IsEnabled = true;
                gameName.IsEnabled = true;
                actionButton.Content = "Connect";
                App.Current.MainWindow = this;
                mw.Disconnect();
                mw.GoAway();
                mw = null;
            }
        }


        private void Connect()
        {

            String[] remote_server_port_bits = serverHost.Text.Split(new char[] { ':' }, 2);
            String RemoteServer = remote_server_port_bits[0];
            int RemotePort = (int)uint.Parse(remote_server_port_bits[1]);
            String RemoteRoom = gameName.Text;

            netJungleProto = new NetProto((NetProtoUI)this, RemoteServer, RemotePort, RemoteRoom);
            netJungleProto.Go();
        }


        public void OnNetworkMessage(String message)
        {
            Console.WriteLine(message);
            if (message == "&CONN")
            {
                connected = true;
                // woo
                actionButton.Content = "Disconnect";
                serverHost.IsEnabled = false;
                gameName.IsEnabled = false;
                actionButton.IsEnabled = true;
                mw = new MainWindow(this.m, this, this.netJungleProto);
                App.Current.MainWindow = mw;
                netJungleProto.parent = (NetProtoUI)mw;
                //this.Visibility = Visibility.Hidden;
                mw.Show();
                SetStatusLabel("Connected! The timer UI should now appear in game.\nEnsure you are in BORDERLESS or WINDOWED mode.");
            }
            else if (message == "!RECONNECT 4")
            {
                SetStatusLabel("Error: couldn't connect to server");
                actionButton.Content = "Connect";
                serverHost.IsEnabled = true;
                gameName.IsEnabled = true;
                actionButton.IsEnabled = true;

            }
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
                mw = null;
            }
        }

        public void SetStatusLabel(String newText)
        {
            statusLabel.Content = newText;
        }
    }
}
