using Data_Transfer.Model;
using Data_Transfer.Utils;
using System;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Data_Transfer.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private HerculesDataModel _dataModel;
        private SerialPort _serialPort;

        public string InputData
        {
            get => _dataModel.InputData;
            set
            {
                if (value.Length <= 16 && IsNumeric(value))
                {
                    _dataModel.InputData = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ReceivedData
        {
            get => _dataModel.ReceivedData;
            set
            {
                _dataModel.ReceivedData = value;
                OnPropertyChanged();
            }
        }

        public ICommand SendDataCommand { get; }
        public ICommand ReceiveDataCommand { get; }

        public MainWindowViewModel()
        {
            _dataModel = new HerculesDataModel();
            _serialPort = new SerialPort
            {
                PortName = "COM4", // Set to the detected COM port
                BaudRate = 9600,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                ReadTimeout = 5000,  // Increased Timeout
                WriteTimeout = 5000, // Increased Timeout
                Handshake = Handshake.None,
                DtrEnable = false, // Enable DTR (if required)
                RtsEnable = false  // Enable RTS (if required)
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
            OpenConnection();

            SendDataCommand = new RelayCommand(SendData, CanSendData);
            ReceiveDataCommand = new RelayCommand(ReceiveData);

            // Check available COM ports
            string availablePorts = string.Join(", ", SerialPort.GetPortNames());
            MessageBox.Show("Available COM Ports: " + availablePorts);
        }

        public void OpenConnection()
        {
            if (_serialPort == null)
            {
                _serialPort = new SerialPort
                {
                    PortName = "COM4",
                    BaudRate = 9600,
                    Parity = Parity.None,
                    DataBits = 8,
                    StopBits = StopBits.One,
                    ReadTimeout = 5000,
                    WriteTimeout = 5000,
                    Handshake = Handshake.None,
                    DtrEnable = false,
                    RtsEnable = false
                };
            }

            if (!_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open();
                    MessageBox.Show("Serial Port Opened Successfully");
                }
                catch (UnauthorizedAccessException)
                {
                    MessageBox.Show("Access Denied! Another program might be using COM4.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening serial port: {ex.Message}");
                }
            }
        }

        private bool CanSendData(object parameter) => !string.IsNullOrEmpty(InputData) && InputData.Length == 16;

        private void SendData(object parameter)
        {
            try
            {
                if (!_serialPort.IsOpen)
                {
                    MessageBox.Show("COM Port is not open!");
                    return;
                }

                Thread.Sleep(500); // Small delay before sending data
                _serialPort.WriteLine(InputData); // Send data
                MessageBox.Show("Data Sent Successfully");
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Write timeout: Unable to send data in time. Check Hercules Terminal settings!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending data: " + ex.Message);
            }
        }

        private void ReceiveData(object parameter)
        {
            if (!_serialPort.IsOpen)
            {
                MessageBox.Show("COM Port is not open!");
                return;
            }

            try
            {
                string received = _serialPort.ReadLine();
                ReceivedData = received;
                MessageBox.Show($"Data Received: {ReceivedData}");
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Read timeout: No data received in time.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error receiving data: " + ex.Message);
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = _serialPort.ReadLine();
                Application.Current.Dispatcher.Invoke(() => { ReceivedData = data; });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error receiving data: " + ex.Message);
            }
        }

        private bool IsNumeric(string value) => long.TryParse(value, out _);

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
