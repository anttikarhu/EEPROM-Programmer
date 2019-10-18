using Jeeprom.Connection;
using System;
using System.Collections.Generic;
using System.IO.Ports;
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

namespace Jeeprom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        EepromPortWatcher portWatcher = new EepromPortWatcher();

        public MainWindow()
        {
            InitializeComponent();

            portWatcher.FoundBoard += PortWatcher_FoundBoard;
            portWatcher.LostBoard += PortWatcher_LostBoard;

            portWatcher.Scan();
        }

        private void PortWatcher_FoundBoard(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                testLabel.Content = "Found programmer :)";
            });
        }

        private void PortWatcher_LostBoard(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                testLabel.Content = "Please connect programmer";
            });
        }
    }
}
