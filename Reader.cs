using System;
using System.Collections.Generic;
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

		public Reader() {
			_running = false;
		}

		public void Run() {
			Initialize();
			while (_running) { Step(); }
			Stop();
		}
		
		private void Initialize() {
			try {
				Nuitrack.Init("");
			}
			catch (nuitrack.Exception exception) {
				Console.WriteLine("Cannot initialize Nuitrack.");
				throw exception;
			}
			// Select the device
			_device = SelectDevice();

			// Select video configurations
			_configVideo = SelectVideoMode();


			// Activate the license
			ActivateDevice();

			Nuitrack.SetDevice(_device);

			// Add modules Sensors
			_depthSensor = DepthSensor.Create();
			_colorSensor = ColorSensor.Create();
			_userTracker = UserTracker.Create();
			_skeletonTracker = SkeletonTracker.Create();

			// Add modules Events Handlers
			_depthSensor.OnUpdateEvent += onDepthSensorUpdate;
			_colorSensor.OnUpdateEvent += onColorSensorUpdate;
			_userTracker.OnUpdateEvent += onUserTrackerUpdate;
			_userTracker.OnNewUserEvent += onUserTrackerNewUser;
			_userTracker.OnLostUserEvent += onUserTrackerLostUser;
			_skeletonTracker.OnSkeletonUpdateEvent += onSkeletonUpdate;

			// Connect to remote
			_streamer = SelectStreamer();


			// Run Nuitrack
			Nuitrack.Run();
			_running = true;
			
			Console.WriteLine("Nuitrack is Running...");
		}
		private Streamer SelectStreamer() {
			Console.Clear();
			Console.WriteLine("Enter the address to stream to as IP_ADRESS:PORT_SKELETON:PORT_PIXELS (ex: 127.0.0.1:8080:8081)\nOr just press ENTER to select 127.0.0.1:8080 (skeleton) / 127.0.0.1:8081 (pixels)");

			string address = "127.0.0.1";
			int port_skeleton = 8080;
			int port_pixels = 8081;

			while (true) {
				string input = Console.ReadLine();
				bool connecting = false;
				if (input == "") {
					address = "127.0.0.1";
					port_skeleton = 8080;
					port_pixels = 8081;
					connecting = true;
				}
				string[] inputs = input.Split(':');
				if (inputs.Length == 3) {

					if (System.Net.IPAddress.TryParse(inputs[0], out System.Net.IPAddress IPtemp) && int.TryParse(inputs[1], out port_skeleton) && int.TryParse(inputs[2], out port_pixels)) {
						address = inputs[0];
						connecting = true;
					}
				}

				Console.SetCursorPosition(0, 2); Console.Write(new string(' ', Console.WindowWidth));
				Console.SetCursorPosition(0, 3); Console.Write(new string(' ', Console.WindowWidth));
				Console.SetCursorPosition(0, 2);

				if (connecting) {
					Streamer streamer = new Streamer(address, port_skeleton, port_pixels);
					if (streamer.TryConnect()) { return streamer; }
				} else {
					Console.WriteLine("Bad entry: the address must be in the format IP_ADRESS:PORT_SKELETON:PORT_PIXELS (ex: 127.0.0.1:8080:8081)");
				}
			}
		}

		private NuitrackDevice SelectDevice() {

			// List availaible devices
			List<NuitrackDevice> devices = Nuitrack.GetDeviceList();
			int devices_count = devices.Count;

			Console.Clear();
			if (devices_count == 0) { Console.WriteLine("Error: there is no connected devices."); throw new nuitrack.Exception("Error: there is no connected devices."); }
			Console.WriteLine("Connected devices:");
			for (int i = 0; i < devices_count; i++) {
				Console.WriteLine("Device {0}:\n * Camera name: {1}\n * Serial number: {2}\n * Provider name: {3}\n * Activated: {4}", i, devices[i].GetInfo(DeviceInfoType.DEVICE_NAME), devices[i].GetInfo(DeviceInfoType.SERIAL_NUMBER), devices[i].GetInfo(DeviceInfoType.PROVIDER_NAME), devices[i].GetActivationStatus().ToString());
			}

			// Select a device
			Console.WriteLine("Select a device (Write a number or just press ENTER to select the camera 0 ({0})", devices[0].GetInfo(DeviceInfoType.DEVICE_NAME));
			int selected = -1;
			while (selected == -1) {
                string input = Console.ReadLine();
				if (input == "") {
					selected = 0;
					break;
				}
				if (int.TryParse(input, out selected)) {
					if (selected > devices_count - 1 || selected < 0) {
						selected = -1;
						Console.SetCursorPosition(0, 2 + devices_count * 5); Console.Write(new string(' ', Console.WindowWidth));
						Console.SetCursorPosition(0, 3 + devices_count * 5); Console.Write(new string(' ', Console.WindowWidth));
						Console.SetCursorPosition(0, 2 + devices_count * 5);
						Console.WriteLine("Bad entry: there is only {0} connected devices. Enter a number equal or lower than {1}.", devices_count, devices_count - 1);
					}
				} else {
					selected = -1;
					Console.SetCursorPosition(0, 2 + devices_count * 5); Console.Write(new string(' ', Console.WindowWidth));
					Console.SetCursorPosition(0, 3 + devices_count * 5); Console.Write(new string(' ', Console.WindowWidth));
					Console.SetCursorPosition(0, 2 + devices_count * 5);
					Console.WriteLine("Bad entry: you must write an integer equal or lower than {0}", devices_count - 1);
				}
			}
			Console.WriteLine("Selected device: {0}", selected);
			return devices[selected];
		}

		private VideoMode SelectVideoMode() {
			StreamType stream = StreamType.DEPTH;
			// List availaible video modes
			List<VideoMode> modes = _device.GetAvailableVideoModes(stream);
			int modes_count = modes.Count;

			Console.Clear();
			if (modes_count == 0) { Console.WriteLine("Error: there is no video mode for this device."); throw new nuitrack.Exception("Error: there is no video mode for this device."); }
			Console.WriteLine("Available {0} modes:", stream.ToString());
			for (int i = 0; i < modes_count; i++) {
				Console.WriteLine(" * Mode {0}: \t({1}px x {2}px) @ {3}fps", i, modes[i].width, modes[i].height, modes[i].fps);
			}

			// Select a Mode
			Console.WriteLine("Select a Mode (Write a number or just press ENTER to select the mode 0");
			int selected = -1;
			while (selected == -1) {
				string input = Console.ReadLine();
				if (input == "") {
					selected = 0;
					break;
				}
				if (int.TryParse(input, out selected)) {
					if (selected > modes_count - 1 || selected < 0) {
						selected = -1;
						Console.SetCursorPosition(0, 2 + modes_count); Console.Write(new string(' ', Console.WindowWidth));
						Console.SetCursorPosition(0, 3 + modes_count); Console.Write(new string(' ', Console.WindowWidth));
						Console.SetCursorPosition(0, 2 + modes_count);
						Console.WriteLine("Bad entry: there is only {0} modes fo this device. Enter a number equal or lower than {1}.", modes_count, modes_count - 1);
					}
				} else {
					selected = -1;
					Console.SetCursorPosition(0, 2 + modes_count); Console.Write(new string(' ', Console.WindowWidth));
					Console.SetCursorPosition(0, 3 + modes_count); Console.Write(new string(' ', Console.WindowWidth));
					Console.SetCursorPosition(0, 2 + modes_count);
					Console.WriteLine("Bad entry: you must write an integer equal or lower than {0}", modes_count - 1);
				}
			}
			_device.SetVideoMode(StreamType.DEPTH, modes[selected]);
			_device.SetVideoMode(StreamType.COLOR, modes[selected]);
			return modes[selected];
		}
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
		public void Stop()
        {
			// Release Nuitrack and remove all modules
			try {
				_depthSensor.OnUpdateEvent -= onDepthSensorUpdate;
				_colorSensor.OnUpdateEvent -= onColorSensorUpdate;
				_userTracker.OnUpdateEvent -= onUserTrackerUpdate;
				_userTracker.OnNewUserEvent -= onUserTrackerNewUser;
				_userTracker.OnLostUserEvent -= onUserTrackerLostUser;
				_skeletonTracker.OnSkeletonUpdateEvent -= onSkeletonUpdate;

				Nuitrack.Release();
			}
			catch (System.Exception exception) {
				Console.WriteLine("Nuitrack release failed.");
				throw exception;
			}
		}

		public void Step() {

			// We wait for the arrays to be populated
			try {
				Nuitrack.WaitUpdate(_depthSensor);
				Nuitrack.WaitUpdate(_colorSensor);
				Nuitrack.WaitUpdate(_skeletonTracker);
			}
			catch (LicenseNotAcquiredException exception) { Console.WriteLine("LicenseNotAcquired exception. Exception: {0}", exception.ToString()); }
			catch (System.Exception exception) { Console.WriteLine("Nuitrack update failed. Exception: {0}", exception.ToString()); }

			// We then format the datas in an array of bytes
			ExtractJoints();
			ExtractPixels();

			// Finaly we give the data to the Streamer
			_streamer.SendSkeleton(_pixels, _skeletonData.Timestamp);
			_streamer.SendPixels(_pixels, _skeletonData.Timestamp);
		}
		private void ExtractJoints() {
			if (_skeletonData != null) {
				int nb_skeletons = _skeletonData.NumUsers;
				int size_joint = 13; // Type: [1 byte] Position: [3 * 4 bytes]
				int size_skeletons = nb_skeletons * 24 * size_joint;

				_joints = new byte[size_skeletons];
				for (int i = 0; i < nb_skeletons; i++) {
					Skeleton skeleton = _skeletonData.Skeletons[i];
					for (int j = 0; j < skeleton.Joints.Length; j++) {
						Joint joint = skeleton.Joints[j];
						//Console.WriteLine("[{0}] Type: {1} ({2}, {3}, {4})", i, joint.Type, joint.Proj.X, joint.Proj.Y, joint.Proj.Z);

						//byte[] user_id = BitConverter.GetBytes(j);
						byte[] joint_type = BitConverter.GetBytes((byte)joint.Type);
						byte[] joint_x = BitConverter.GetBytes(joint.Proj.X);
						byte[] joint_y = BitConverter.GetBytes(joint.Proj.Y);
						byte[] joint_z = BitConverter.GetBytes(joint.Proj.Z);

						//Array.Copy(user_id, _joints, user_id.Length);
						Array.Copy(joint_type, _joints, joint_type.Length);
						Array.Copy(joint_x, _joints, joint_x.Length);
						Array.Copy(joint_y, _joints, joint_y.Length);
						Array.Copy(joint_z, _joints, joint_z.Length);
					}
				}
			}
		}

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

							depth_cursor += 2; // Only the Depth (uint16) is stored in the depth arraèy

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
							_pixels[pixel_cursor] = (byte)0;
							pixel_cursor++;
							//Console.WriteLine("[{0}] Depth: ({1}, {2}, {3}) Color: ({4}, {5}, {6})", index, BitConverter.ToUInt16(new byte[] { _pixels[pixel_cursor - 9], _pixels[pixel_cursor - 8] }, 0), BitConverter.ToUInt16(new byte[] { _pixels[pixel_cursor - 7], _pixels[pixel_cursor - 6] }, 0), BitConverter.ToUInt16(new byte[] { _pixels[pixel_cursor - 5], _pixels[pixel_cursor - 4] }, 0), _pixels[pixel_cursor - 3], _pixels[pixel_cursor - 2], _pixels[pixel_cursor - 1]);
						}
					}
				}
			}
		}

        private void onSkeletonUpdate(SkeletonData skeletonData) {
			if (_skeletonData != null) { _skeletonData.Dispose(); }
			_skeletonData = (SkeletonData)skeletonData.Clone();
		}

        private void onUserTrackerLostUser(int userID) { Console.WriteLine("Lost User {0}", userID); }
        private void onUserTrackerNewUser(int userID) { Console.WriteLine("New User {0}", userID); }
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
    }
}
