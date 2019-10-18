using System;
using System.Windows;

namespace Jeeprom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        EepromPort portWatcher = new EepromPort();

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
