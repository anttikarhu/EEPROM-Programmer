using System;
using System.Windows;

namespace Jeeprom
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        EepromPort eepromPort = new EepromPort();

        public MainWindow()
        {
            InitializeComponent();

            hideControls();

            eepromPort.FoundBoard += PortWatcher_FoundBoard;
            eepromPort.LostBoard += PortWatcher_LostBoard;
            eepromPort.EraseProgress += PortWatcher_EraseProgress;
            eepromPort.EraseDone += PortWatcher_EraseDone;
            eepromPort.ReadProgress += EepromPort_ReadProgress;
            eepromPort.ReadDone += EepromPort_ReadDone;

            eepromPort.Scan();
        }

        private void PortWatcher_FoundBoard(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                testLabel.Content = "Found programmer :)";

                showControls();
                enableControls();
            });
        }

        private void PortWatcher_LostBoard(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                testLabel.Content = "Please connect programmer";

                hideControls();
            });
        }

        private void eraseButton_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            disableControls();
            statusLabel.Content = "Erasing (filling with 0xFF)...";

            eepromPort.Erase();
        }

        private void zeroButton_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            disableControls();
            statusLabel.Content = "Writing all zeros...";

            eepromPort.Zero();
        }

        private void PortWatcher_EraseProgress(object sender, EraseProgressEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Value = e.Progress;
            });
        }
        private void PortWatcher_EraseDone(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Visibility = Visibility.Hidden;
                progressBar.Value = 0;
                enableControls();
                statusLabel.Content = "";
            });
        }

        private void readButton_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            dataTextBox.Text = "";
            dataTextBox.Visibility = Visibility.Visible;
            disableControls();
            statusLabel.Content = "Reading contents...";

            eepromPort.Read();
        }

        private void EepromPort_ReadProgress(object sender, ReadProgressEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Value = e.Progress;
                dataTextBox.Text += e.Data;
            });
        }

        private void EepromPort_ReadDone(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                progressBar.Visibility = Visibility.Hidden;
                progressBar.Value = 0;
                enableControls();
                statusLabel.Content = "";
            });
        }

        private void writeFileButton_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;
            disableControls();
            statusLabel.Content = "Writing file contents...";
        }

        private void disableControls()
        {
            eraseButton.IsEnabled = false;
            zeroButton.IsEnabled = false;
            readButton.IsEnabled = false;
            writeFileButton.IsEnabled = false;
        }

        private void enableControls()
        {
            eraseButton.IsEnabled = true;
            zeroButton.IsEnabled = true;
            readButton.IsEnabled = true;
            writeFileButton.IsEnabled = true;
        }

        private void hideControls()
        {
            eraseButton.Visibility = Visibility.Hidden;
            zeroButton.Visibility = Visibility.Hidden;
            progressBar.Visibility = Visibility.Hidden;
            readButton.Visibility = Visibility.Hidden;
            writeFileButton.Visibility = Visibility.Hidden;
            dataTextBox.Visibility = Visibility.Hidden;
            dataTextBox.Text = "";
            statusLabel.Visibility = Visibility.Hidden;
            statusLabel.Content = "";
        }

        private void showControls()
        {
            eraseButton.Visibility = Visibility.Visible;
            zeroButton.Visibility = Visibility.Visible;
            readButton.Visibility = Visibility.Visible;
            writeFileButton.Visibility = Visibility.Visible;
        }
    }
}
