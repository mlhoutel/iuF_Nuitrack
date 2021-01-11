using nuitrack.device;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace iuF {
    public partial class MainWindow : INotifyPropertyChanged {
        private Reader _reader;

        private ObservableCollection<string> _cameras;
        private int _camera;

        private ObservableCollection<string> _configurations;
        private int _configuration;

        private bool _license_activated;
        private string _license;

        private string _address;

        private string _port_skeleton;
        private bool _send_skeleton;

        private string _port_pixels;
        private bool _send_pixels;

        private string _console_datas;

        public MainWindow() {
            _reader = new Reader();
            _port_skeleton = "8080";
            _port_pixels = "8081";
            _address = "127.0.0.1";
            _license = "";
            _send_skeleton = true;
            _send_pixels = true;
            _cameras = new ObservableCollection<string>();
            _configurations = new ObservableCollection<string>();

            // Initialize the window
            DataContext = this;
            InitializeComponent();

            PrintLog("Console Initialized...");
            PrintLog(_reader.Initialize());

            LoadCameras();
        }
        private void Bar_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); } }
        private void Minimize_MouseClick(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        private void Close_MouseClick(object sender, RoutedEventArgs e) { Close(); }

        private void LoadCameras() {
            PrintLog("Loading Devices...");
            Cameras.Clear();
            ObservableCollection<string> temp_cameras = new ObservableCollection<string>();
            PrintLog(_reader.getDevices(out temp_cameras));
            for (int i = 0; i < temp_cameras.Count; i++) { Cameras.Add(temp_cameras[i]); }
        }
        private void LoadConfigurations() {
            Configurations.Clear();
            ObservableCollection<string> temp_configurations = new ObservableCollection<string>();
            PrintLog(_reader.getConfigurations(out temp_configurations));
            for (int i = 0; i < temp_configurations.Count; i++) { Configurations.Add(temp_configurations[i]); }
        }
        private void LoadLicense() {
            string temp_license = "";
            PrintLog(_reader.getLicense(out temp_license));
            License = temp_license;
            if (License == "Device Activated") { LicenseText.IsEnabled = false; LicenseText.Opacity = 0.7; RightPannel.IsEnabled = true; } else { LicenseText.IsEnabled = true; LicenseText.Opacity = 1; }
        }
        private void SendLicense() { PrintLog(_reader.setLicense(_license)); }
        public void PrintLog(string output) {
            Console.AppendText("\r\n" + output);
            Console.ScrollToEnd();
        }
        public ObservableCollection<string> Cameras {
            get { return _cameras; }
            set { if (_cameras != value) { _cameras = value; OnPropertyChanged(); } }
        }
        public int Camera {
            get { return _camera; }
            set { if (_camera != value) { _camera = value; OnPropertyChanged(); } }
        }
        public ObservableCollection<string> Configurations {
            get { return _configurations; }
            set { if (_configurations != value) { _configurations = value; OnPropertyChanged(); } }
        }
        public int Configuration {
            get { return _configuration; }
            set { if (_configuration != value) { _configuration = value; OnPropertyChanged(); } }
        }
        public string Address {
            get { return _address; }
            set { if (_address != value) { _address = value; OnPropertyChanged(); } }
        }
        public string Port_Skeleton {
            get { return _port_skeleton; }
            set { if (_port_skeleton != value) { _port_skeleton = value; OnPropertyChanged(); } }
        }
        public bool Send_Skeleton {
            get { return _send_skeleton; }
            set { if (_send_skeleton != value) { _send_skeleton = value; OnPropertyChanged(); } }
        }
        public string Port_Pixels {
            get { return _port_pixels; }
            set { if (_port_pixels != value) { _port_pixels = value; OnPropertyChanged(); } }
        }
        public bool Send_Pixels {
            get { return _send_pixels; }
            set { if (_send_pixels != value) { _send_pixels = value; OnPropertyChanged(); } }
        }
        public string License {
            get { return _license; }
            set { if (_license != value) { _license = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Cameras_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            PrintLog("Camera [" + _camera + "](" + _cameras[_camera] + ") Selected");
            PrintLog(_reader.setDevice(_camera));
            LoadConfigurations();
        }
        private void Configurations_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            PrintLog("Configuration [" + _configuration + "](" + _configurations[_configuration] + ") Selected");
            PrintLog(_reader.setConfiguration(_configuration));
            LoadLicense();
        }
        private void License_LostFocus(object sender, RoutedEventArgs e) {
            if (LicenseText.IsEnabled == true) { SendLicense(); }
        }
        private void License_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && LicenseText.IsEnabled == true) { SendLicense(); }
        }
        private void Send_Click(object sender, RoutedEventArgs e) {
            bool connected = false;
            PrintLog(_reader.setStreamer(_address, _port_skeleton, _port_pixels, out connected));

            if (connected) {
                _reader.Send_Joints = _send_skeleton;
                _reader.Send_Pixels = _send_pixels;

                string msg = "Streaming datas: Sending Joints [";
                if (_send_skeleton) { msg += "X";  } else { msg += " "; }
                msg += "] Sending Pixels [";
                if (_send_pixels) { msg += "X"; } else { msg += " "; }
                msg += "]";

                LeftPannel.IsEnabled = false;
                RightPannel.IsEnabled = false;

                Stop_Button.IsEnabled = true; Stop_Button.Opacity = 1;
                Send_Button.IsEnabled = false; Send_Button.Opacity = 0.8;

                _reader.Setup();
                PrintLog(msg);
            }
        }
        private void Stop_Click(object sender, RoutedEventArgs e) {
            PrintLog("Stop...");
            LeftPannel.IsEnabled = true;
            RightPannel.IsEnabled = true;

            Stop_Button.IsEnabled = false; Stop_Button.Opacity = 0.8;
            Send_Button.IsEnabled = true; Send_Button.Opacity = 1;
        }
    }
}
