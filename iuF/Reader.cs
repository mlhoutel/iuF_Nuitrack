using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Text;


using nuitrack;
using nuitrack.device;
using nuitrack.issues;

namespace iuF {
    public class Reader {
		private Streamer _streamer;
		private NuitrackDevice _device;
		private bool _running;
		private VideoMode _configVideo;

		private DepthSensor _depthSensor;
		private ColorSensor _colorSensor;
		private UserTracker _userTracker;
		private SkeletonTracker _skeletonTracker;

		private DepthFrame _depthFrame;
		private ColorFrame _colorFrame;
		private UserFrame _userFrame;
		private SkeletonData _skeletonData;

		private byte[] _joints; // {User, Type, XReal, YReal, ZReal} for each joints
		private byte[] _pixels; // {X, Y, Depth, ColorR, ColorG, ColorB} for each pixel

		/* Selection of the Datas to Send*/
		private bool _send_joints;
		private bool _send_pixels;
		public bool Send_Joints {
			get { return _send_joints; }
			set { _send_joints = value; }
		}
		public bool Send_Pixels {
			get { return _send_pixels; }
			set { _send_pixels = value; }
		}

		/* Default values at Instanciation */
		public Reader() {
			_running = false;
			_send_joints = true;
			_send_pixels = true;
		}

		/* Initialize the Nuitrack Environment */
		public string Initialize() {
			try { Nuitrack.Init(""); }
			catch (nuitrack.Exception e) { return FormatError("Error: Cannot initialize Nuitrack (" + e.ToString() + ")"); }
			return "Nuitrack Initialized...";
		}

		/* Setup the Events Handlers and Run Nuitrack */
		public string Setup() {
			// Add modules Sensors
			_depthSensor = DepthSensor.Create();
			_colorSensor = ColorSensor.Create();
			_userTracker = UserTracker.Create();
			_skeletonTracker = SkeletonTracker.Create();

			// Add modules Events Handlers
			_depthSensor.OnUpdateEvent += onDepthSensorUpdate;
			_colorSensor.OnUpdateEvent += onColorSensorUpdate;
			_userTracker.OnUpdateEvent += onUserTrackerUpdate;
			_skeletonTracker.OnSkeletonUpdateEvent += onSkeletonUpdate;

			// Run Nuitrack
			try { Nuitrack.Run(); _running = true; }
			catch (nuitrack.Exception e) { return FormatError("An Error Occured during the launching of Nuitrack."); }

			return "Nuitrack is Running...";
		}

		/* Main Loop : Process and Send Datas */
		public void Run() {
			while (_running) { Step(); }
			Stop();
		}

		/* Test the Address then Instanciate and Connect the Streamer */
		public string setStreamer(string address, string port_skeleton, string port_pixels, out bool connecting) {
			connecting = false;
			int temp_port_skeleton;
			int temp_port_pixels;

			// Parsing the Strings to valid formats
			if (!IPAddress.TryParse(address, out System.Net.IPAddress IPtemp)) { return FormatError("Error, the Address (" + address + ") could not be parsed as an IP (ex: 127.0.0.1)"); }
			if (!int.TryParse(port_skeleton, out temp_port_skeleton)) { return FormatError("Error, the Skeleton port (" + port_skeleton + ") could not be parsed as an integer (ex: 8080)"); }
			if (!int.TryParse(port_pixels, out temp_port_pixels)) { return FormatError("Error, the Pixels port (" + port_pixels + ") could not be parsed as an integer (ex: 8081)"); }

			// Instanciating a new Streamer
			Streamer streamer = new Streamer(address, temp_port_skeleton, temp_port_pixels);

			// Test if the connections informations are valid and Connect Streamer
			if (streamer.TryConnect()) {
				connecting = true;
				_streamer = streamer;
				return "Succefully connected to " + address + ":" + temp_port_skeleton + " and " + address + ":" + temp_port_pixels;
			}
			return FormatError("Error, could not reach " + address + ":" + temp_port_skeleton + " or " + address + ":" + temp_port_pixels);
		}

		/* ----------------------------------------------------------------------- */
		/*	Get the plugged devices and Output it into the parameter device_list   */
		/*	@return: String of Errors                                              */
		/* ----------------------------------------------------------------------- */
		public string getDevices(out ObservableCollection<string> device_list) {
			device_list = new ObservableCollection<string>();

			// List the Availaible Devices
			List<NuitrackDevice> devices = Nuitrack.GetDeviceList();
			int devices_count = devices.Count;

			// Check for an Empty List
			if (devices_count == 0) { return FormatError("Error: there is no connected devices."); }

			// Format the devices to String and output the array
			for (int i = 0; i < devices_count; i++) { device_list.Add(devices[i].GetInfo(DeviceInfoType.DEVICE_NAME)); }
			return"\nDevices loaded (" + devices_count + " devices detected)";
		}

		/* ----------------------------------------------------------------------- */
		/*	Set the Selected Devices with the index passed by parameter            */
		/*	@return: String of Errors                                              */
		/* ----------------------------------------------------------------------- */
		public string setDevice(int index) {

			// List the Availaible Devices
			List<NuitrackDevice> devices = Nuitrack.GetDeviceList();
			int devices_count = devices.Count;

			// Update the selected Device
			if (index >= 0 && index < devices_count) { 
				_device = devices[index];
				return "\nLoading Configurations...";
			}

			// Check for Invalid Index
			return FormatError("Error during the device selection: The selected device must have been unplugged");
		}

		/* ----------------------------------------------------------------------- */
		/*	Get the Configurations of the selected device and Output it            */
		/*	@return: String of Errors                                              */
		/* ----------------------------------------------------------------------- */
		public string getConfigurations(out ObservableCollection<string> configurations_list) {
			configurations_list = new ObservableCollection<string>();

			// List availaible devices
			List<VideoMode> modes = _device.GetAvailableVideoModes(StreamType.DEPTH);
			int modes_count = modes.Count;
			if (modes_count == 0) { return FormatError("Error: there is no video mode for this device."); }
			for (int i = 0; i < modes_count; i++) { configurations_list.Add(modes[i].width + "x" + modes[i].height + "@" + modes[i].fps + "fps"); }
			return "Configurations loaded (" + modes_count + " configurations detected)";
		}

		/* ----------------------------------------------------------------------- */
		/*	Set the Selected Configuration with the index passed by parameter      */
		/*	@return: String of Errors                                              */
		/* ----------------------------------------------------------------------- */
		public string setConfiguration(int index) {

			List<VideoMode> modes = _device.GetAvailableVideoModes(StreamType.DEPTH);
			int modes_count = modes.Count;
			if (index >= 0 && index < modes_count) {
				_configVideo = modes[index];
				return "\nChecking License...";
			}
			return FormatError("Error during the configurations selection: The selected device must have been unplugged");
		}

		/* ----------------------------------------------------------------------- */
		/*	Get the Configurations of the selected device and Output it            */
		/*	@return: String of Errors                                              */
		/* ----------------------------------------------------------------------- */
		public string getLicense(out string license) {
			bool device_activated = Convert.ToBoolean(_device.GetActivationStatus());
			license = "";
			if (device_activated) { license = "Device Activated"; return "Device Activated (" + _device.GetActivationStatus().ToString() + ")"; }
			return "The device is not activated. Enter your license key (or get one at https://cognitive.3divi.com/app/nuitrack/dashboard/)";
			
		}

		/* ----------------------------------------------------------------------- */
		/*	Set the Selected Configuration with the index passed by parameter      */
		/*	@return: String of Errors                                              */
		/* ----------------------------------------------------------------------- */
		public string setLicense(string license) {
			bool device_activated = false;
			try {
				_device.Activate(license);
				device_activated = Convert.ToBoolean(_device.GetActivationStatus());
				if (device_activated) { return "Device activated succefully"; }
			}
			catch { }
			
			return FormatError("Error during the device activation. Check your license key (or get one at https://cognitive.3divi.com/app/nuitrack/dashboard/)");
		}

		/* ----------------------------------------------------------------------- */
		/*	Check the License of the Selected Device                               */
		/*	@return: String of the License activation status                       */
		/* ----------------------------------------------------------------------- */
		private void ActivateDevice() {
			bool device_activated = Convert.ToBoolean(_device.GetActivationStatus());
			Console.Clear();
			while (!device_activated) {
				Console.WriteLine("Your device license is not activated\nEnter the activation key (or get one at https://cognitive.3divi.com/app/nuitrack/dashboard/): ");
				string activationKey = Console.ReadLine();
				_device.Activate(activationKey);
				device_activated = Convert.ToBoolean(_device.GetActivationStatus());
			}
			Console.WriteLine("Activation status: {0}", _device.GetActivationStatus().ToString());
		}

		/* Stop the Main Loop and Reset the Events Handlers */
		public void Stop() {
			try {
				// Remove all modules
				_depthSensor.OnUpdateEvent -= onDepthSensorUpdate;
				_colorSensor.OnUpdateEvent -= onColorSensorUpdate;
				_userTracker.OnUpdateEvent -= onUserTrackerUpdate;
				_skeletonTracker.OnSkeletonUpdateEvent -= onSkeletonUpdate;

				// Release Nuitrack
				Nuitrack.Release();
			}
			catch (System.Exception exception) {
				Console.WriteLine("Nuitrack release failed.");
				throw exception;
			}
		}

		/* Extract the datas when the Handlers are Updated then Send them via the Streamer */
		public void Step() {

			// We wait for the arrays to be populated
			try { Nuitrack.WaitUpdate(_skeletonTracker); }
			catch (LicenseNotAcquiredException exception) { Console.WriteLine("LicenseNotAcquired exception. Exception: {0}", exception.ToString()); }
			catch (System.Exception exception) { Console.WriteLine("Nuitrack update failed. Exception: {0}", exception.ToString()); }

			// We then format the datas in an array of bytes
			if (_send_joints) { ExtractJoints(); }
			if (_send_pixels) { ExtractPixels(); }

			// Finaly we give the data to the Streamer
			if (_send_joints) { _streamer.SendSkeleton(_pixels, _skeletonData.Timestamp); }
			if (_send_pixels) { _streamer.SendPixels(_pixels, _skeletonData.Timestamp); }
		}

		/* Extract and format the Joints into an array of bytes */
		private void ExtractJoints() {
			if (_skeletonData != null) {
				int nb_skeletons = _skeletonData.NumUsers;
				int size_joint = 13; // Type: [1 byte] Position: [3 * 4 bytes]
				int size_skeletons = nb_skeletons * 24 * size_joint;
				_joints = new byte[size_skeletons];

				// For each detected skeleton, extract the datas in an Skeleton object
				for (int i = 0; i < nb_skeletons; i++) {
					Skeleton skeleton = _skeletonData.Skeletons[i];

					// For each Joint of the Skeleton object, extract and format the datas in an array of bytes
					for (int j = 0; j < skeleton.Joints.Length; j++) {
						Joint joint = skeleton.Joints[j];
						//Console.WriteLine("[{0}] Type: {1} ({2}, {3}, {4})", i, joint.Type, joint.Proj.X, joint.Proj.Y, joint.Proj.Z);

						// byte[] user_id = BitConverter.GetBytes(j); 
						byte[] joint_type = BitConverter.GetBytes((byte)joint.Type);   // Type: joint type  (1 byte)
						byte[] joint_x = BitConverter.GetBytes(joint.Proj.X);          // X: x position		(4 byte)
						byte[] joint_y = BitConverter.GetBytes(joint.Proj.Y);          // Y: y position		(4 byte)
						byte[] joint_z = BitConverter.GetBytes(joint.Proj.Z);          // Z: z position		(4 byte)

						//Array.Copy(user_id, _joints, user_id.Length);
						Array.Copy(joint_type, _joints, joint_type.Length);
						Array.Copy(joint_x, _joints, joint_x.Length);
						Array.Copy(joint_y, _joints, joint_y.Length);
						Array.Copy(joint_z, _joints, joint_z.Length);
					}
				}
			}
		}

		/* Extract and format the Pixels into an array of bytes */
		private void ExtractPixels() {
			if (_depthFrame != null && _colorFrame != null) {
				int nb_pixels = _configVideo.height * _configVideo.width;
				int size_pixel = 16; // Depth:  [3 * 4 bytes], Color: [3 * 1 bytes] (+ 1?)
				int size_frame = nb_pixels * size_pixel;
				_pixels = new byte[size_frame];

				int depth_cursor = 0;
				int color_cursor = 0;
				int pixel_cursor = 0;

				unsafe {
					byte* depths = (byte*)_depthFrame.Data.ToPointer();
					byte* colors = (byte*)_colorFrame.Data.ToPointer();

					for (int i = 0; i < _configVideo.height; i++) {
						for (int j = 0; j < _configVideo.width; j++) {
							int index = j + i * _configVideo.width;

							byte[] depth_X = BitConverter.GetBytes((float)i);
							byte[] depth_Y = BitConverter.GetBytes((float)j);
							byte[] depth_Z = BitConverter.GetBytes((float)BitConverter.ToUInt16(new byte[2] { depths[depth_cursor], depths[depth_cursor + 1] }, 0));

							byte[] depth_pos = new byte[4 * 3] { depth_X[0], depth_X[1], depth_X[2], depth_X[3], depth_Y[0], depth_Y[1], depth_Y[2], depth_Y[3], depth_Z[0], depth_Z[1], depth_Z[2], depth_Z[3] };

							depth_cursor += 2; // Only the Depth (uint16/2bytes) is stored in the depth array

							// Depth [X, Y, Z]
							for (int k = 0; k < 12; k++) {
								_pixels[pixel_cursor] = depth_pos[k];
								pixel_cursor++;
							}

							// Color [R, G, B]
							for (int k = 0; k < 3; k++) {
								_pixels[pixel_cursor] = colors[color_cursor];
								color_cursor++;  pixel_cursor++;
							}
							_pixels[pixel_cursor] = (byte)0; // Empty 16th byte
							pixel_cursor++;
							//Console.WriteLine("[{0}] Depth: ({1}, {2}, {3}) Color: ({4}, {5}, {6})", index, BitConverter.ToUInt16(new byte[] { _pixels[pixel_cursor - 9], _pixels[pixel_cursor - 8] }, 0), BitConverter.ToUInt16(new byte[] { _pixels[pixel_cursor - 7], _pixels[pixel_cursor - 6] }, 0), BitConverter.ToUInt16(new byte[] { _pixels[pixel_cursor - 5], _pixels[pixel_cursor - 4] }, 0), _pixels[pixel_cursor - 3], _pixels[pixel_cursor - 2], _pixels[pixel_cursor - 1]);
						}
					}
				}
			}
		}

		/* Handlers for the Trackers Updates : Extract datas */
        private void onSkeletonUpdate(SkeletonData skeletonData) {
			if (_skeletonData != null) { _skeletonData.Dispose(); }
			_skeletonData = (SkeletonData)skeletonData.Clone();
		}
        private void onUserTrackerUpdate(UserFrame userFrame) {
			if (_userFrame != null) { _userFrame.Dispose(); }
			_userFrame = (UserFrame)userFrame.Clone();
		}

        private void onColorSensorUpdate(ColorFrame colorFrame) {
			if (_colorFrame != null) { _colorFrame.Dispose(); }
			_colorFrame = (ColorFrame)colorFrame.Clone();
		}

        private void onDepthSensorUpdate(DepthFrame depthFrame){
			if (_depthFrame != null) {_depthFrame.Dispose(); }
			_depthFrame = (DepthFrame)depthFrame.Clone();
		}
		/* Formatting the Errors in the Console */
		private string FormatError(string message) { return "\n______________________________________________________________________________\n\n /!\\ " + message + "\n______________________________________________________________________________\n"; }
	}
}
