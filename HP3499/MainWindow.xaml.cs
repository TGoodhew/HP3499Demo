using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using NationalInstruments.Visa;

namespace HP3499
{
    /// <summary>
    /// Card ListView type
    /// </summary>
    public class CardListItem
    {
        public int Slot { get; set; }
        public string CardType { get; set; }
    }

    ///
    /// A ToggleButton that acts like a RadioButton but allows unchecking
    /// 
    class RadioToggleButton : RadioButton
    {
        protected override void OnToggle()
        {
            if (IsChecked == true) IsChecked = IsThreeState ? (bool?)null : (bool?)false;
            else IsChecked = IsChecked.HasValue;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GpibSession gpibSession3499;
        private GpibSession gpibSession53131;
        private ResourceManager resManager;
        private string gpibAddress3499 = @"GPIB0::9::INSTR";
        private string gpibAddress53131 = @"GPIB0::23::INSTR";
        private DispatcherTimer ReadTimer;

        public MainWindow()
        {
            InitializeComponent();

            // Setup the VISA connection to the controller
            resManager = new ResourceManager();

            // Connect to the 3499A
            gpibSession3499 = (GpibSession)resManager.Open(gpibAddress3499);
            gpibSession3499.TerminationCharacterEnabled = true;
            gpibSession3499.Clear();

            // Reset and clear the device
            SendCommand(gpibSession3499, "*RST");
            SendCommand(gpibSession3499, "*CLS");
            SendCommand(gpibSession3499, "*SRE 0");
            SendCommand(gpibSession3499, "*ESE 0");
            SendCommand(gpibSession3499, ":STAT:PRES");

            // Connect to the 53131
            gpibSession53131 = (GpibSession)resManager.Open(gpibAddress53131);
            gpibSession53131.TerminationCharacterEnabled = true;
            gpibSession53131.Clear();

            // Reset and clear the device
            SendCommand(gpibSession53131, "*RST");
            SendCommand(gpibSession53131, "*CLS");
            SendCommand(gpibSession53131, "*SRE 0");
            SendCommand(gpibSession53131, "*ESE 0");
            SendCommand(gpibSession53131, ":STAT:PRES");

            // Initialize Timer
            ReadTimer = new DispatcherTimer();
            ReadTimer.Interval = TimeSpan.FromSeconds(2);
            ReadTimer.Tick += Timer_Tick;

            // Configure 53131 to read frequency on channel 1
            SendCommand(gpibSession53131, "CONFigure:VOLTage:FREQuency (@1)");
        }

        /// <summary>
        /// Reads the frequency from channel 1 on the 53131A
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            SendCommand(gpibSession53131, "Read?");
            FrequencyValue.Text = ReadStringLine(gpibSession53131);
        }

        /// <summary>
        /// Send a command to the device
        /// </summary>
        /// <param name="gpibSession">GpibSession for the specific device</param>
        /// <param name="command">String for the command</param>
        private void SendCommand(GpibSession gpibSession, string command)
        {
            gpibSession.FormattedIO.WriteLine(command);
        }

        /// <summary>
        /// Read a string from the device and trim off trailing new line characters
        /// </summary>
        /// <param name="gpibSession">GpibSession for the specific device</param>
        /// <returns></returns>
        private string ReadStringLine(GpibSession gpibSession)
        {
            string result = gpibSession.FormattedIO.ReadLine().TrimEnd(Environment.NewLine.ToCharArray());
            return result;
        }

        /// <summary>
        /// Get the list of installed cards in the 3499A
        /// </summary>
        private void GetCardList()
        {
            for (int i = 0; i < 6; i++)
            {
                SendCommand(gpibSession3499, "SYSTem:CTYPE? " + i.ToString());
                var CardEntry = ReadStringLine(gpibSession3499);
                CardListView.Items.Add(new CardListItem { Slot = i, CardType = CardEntry });
            }
        }

        /// <summary>
        /// Handler for the click event to get the list of installed cards in the 3499A
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetInstalledCards_Click(object sender, RoutedEventArgs e)
        {
            GetCardList();
        }

        /// <summary>
        /// Send the close command to the 3499A card for the specific channel. The channel ID is held in the Content property of the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            var radioToggleButton = sender as RadioToggleButton;

            // The button content contains the channel number
            string channel = "close (@1" + radioToggleButton.Content.ToString() + ")";

            SendCommand(gpibSession3499, channel);
        }

        /// <summary>
        /// Send the open command to the 3499A card for the specific channel. The channel ID is held in the Content property of the control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var radioToggleButton = sender as RadioToggleButton;

            // The button content contains the channel number
            string channel = "open (@1" + radioToggleButton.Content.ToString() + ")";

            SendCommand(gpibSession3499,channel);
        }

        /// <summary>
        /// Frequency reading button click handler to start the timer and setup the UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartFrequencyReading_Click(object sender, RoutedEventArgs e)
        {
            // Flip flop on the button state
            if (ReadTimer.IsEnabled)
            {
                ReadTimer.Stop();
                StartFrequencyReading.Content = "Start";
                FrequencyValue.Text = "No reading";
            }
            else
            {
                ReadTimer.Start();
                StartFrequencyReading.Content = "Stop";
            }
        }
    }
}
