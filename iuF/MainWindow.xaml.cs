using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
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

            // Initialize the window
            DataContext = this;
            InitializeComponent(); 
        }
        private void Bar_MouseDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) { DragMove(); }}
        private void Minimize_MouseClick(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        private void Close_MouseClick(object sender, RoutedEventArgs e) { Close(); }


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
            get { return _port_pixels.ToString(); }
            set { if (_port_pixels != value) { _port_pixels = value; OnPropertyChanged(); } }
        }
        public bool Send_Pixels {
            get { return _send_pixels; }
            set { if (_send_pixels != value) { _send_pixels = value; OnPropertyChanged(); } }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
