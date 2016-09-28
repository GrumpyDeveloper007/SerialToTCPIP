using SerialToTCPIP.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SerialToTCPIP
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Mapper worker = new Mapper();
        bool _Started;
        int _logLines;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void butStart_Click(object sender, RoutedEventArgs e)
        {
            if (_Started == false)
            {
                worker.TriggerLog += Worker_TriggerLog;
                worker.OpenPort(txtIPAddress.Text, int.Parse(txtTCPIPPort.Text), txtSerialPort.Text);
                butStart.Content = "Stop";
                _Started = true;
            }
            else
            {
                worker.TriggerLog -= Worker_TriggerLog;
                worker.ClosePort();
                butStart.Content = "Start";
                _Started = false;
            }
        }

        private void Worker_TriggerLog(string eventMessage)
        {
            this.Dispatcher.Invoke(() =>
            {
                if (_logLines>1000)
                {
                    txtLog.Text = "";
                    _logLines = 0;
                }
                txtLog.AppendText(eventMessage + "\r\n");
                _logLines++;
                txtLog.ScrollToEnd();
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_Started==true )
            {
                worker.TriggerLog -= Worker_TriggerLog;
                worker.ClosePort();
                _Started = false;
            }
        }
    }
}
