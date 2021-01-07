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

		private DepthSensor _depthSensor;
		private ColorSensor _colorSensor;
		private UserTracker _userTracker;
		private SkeletonTracker _skeletonTracker;
		private GestureRecognizer _gestureRecognizer;
		private HandTracker _handTracker;

		private DepthFrame _depthFrame;
		private SkeletonData _skeletonData;
		private HandTrackerData _handTrackerData;
		private IssuesData _issuesData;

		private float[] _lhand; // {X, Y, Z} position of the Left hand
		private float[] _rhand; // {X, Y, Z} position of the Right hand
		private List<float> _joints; // {User, Type, XReal, YReal, ZReal} position for each joints


		public Reader(Streamer streamer) {
			_streamer = streamer;
			_running = false;
		}

		public void Run() {
			Initialize();
			while (_running) { Step(); }
			Stop();
		}
		
		private void Initialize()
        {
			Nuitrack.Init("");

			// Select the device
			SelectDevice();

			// Select video configurations
			SelectVideoMode(StreamType.DEPTH);
			SelectVideoMode(StreamType.COLOR);

			// Activate the license
			ActivateDevice();

			Nuitrack.SetDevice(_device);

			// Add modules Sensors
			_depthSensor = DepthSensor.Create();
			_colorSensor = ColorSensor.Create();
			_userTracker = UserTracker.Create();
			_skeletonTracker = SkeletonTracker.Create();
			_handTracker = HandTracker.Create();

			// Add modules Events Handlers
			_depthSensor.OnUpdateEvent += onDepthSensorUpdate;
			_colorSensor.OnUpdateEvent += onColorSensorUpdate;
			_userTracker.OnUpdateEvent += onUserTrackerUpdate;
			_userTracker.OnNewUserEvent += onUserTrackerNewUser;
			_userTracker.OnLostUserEvent += onUserTrackerLostUser;
			_skeletonTracker.OnSkeletonUpdateEvent += onSkeletonUpdate;
			_handTracker.OnUpdateEvent += onHandTrackerUpdate;

			// Run Nuitrack
			Nuitrack.Run();
			_running = true;

			Console.WriteLine("Nuitrack is Running...");
		}

		private void SelectDevice() {

			// List availaible devices
			List<NuitrackDevice> devices = Nuitrack.GetDeviceList();
			int devices_count = devices.Count;

			Console.Clear();
			if (devices_count == 0) { Console.WriteLine("Error: there is no connected devices."); return; }
			Console.WriteLine("Connected devices:");
			for (int i = 0; i < devices_count; i++) {
				Console.WriteLine("Device {0}:\n * Camera name: {1}\n * Serial number: {2}\n * Provider name: {3}\n * Activated: {4}", i, devices[i].GetInfo(DeviceInfoType.DEVICE_NAME), devices[i].GetInfo(DeviceInfoType.SERIAL_NUMBER), devices[i].GetInfo(DeviceInfoType.PROVIDER_NAME), devices[i].GetActivationStatus().ToString());
			}

			// Select a device
			Console.WriteLine("Select a device (Write a number or just press ENTER to select the camera 0 ({0})", devices[0].GetInfo(DeviceInfoType.DEVICE_NAME));
			int selected = -1;
			while (selected == -1) {
				String input = Console.ReadLine();
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
			_device = devices[selected];
			Console.WriteLine("Selected device: {0}", selected);
		}

		private void SelectVideoMode(StreamType stream) {

			// List availaible video modes
			List<VideoMode> modes = _device.GetAvailableVideoModes(stream);
			int modes_count = modes.Count;

			Console.Clear();
			if (modes_count == 0) { Console.WriteLine("Error: there is no video mode for this device."); return; }
			Console.WriteLine("Available {0} modes:", stream.ToString());
			for (int i = 0; i < modes_count; i++) {
				Console.WriteLine(" * Mode {0}: \t({1}px x {2}px) @ {3}fps", i, modes[i].width, modes[i].height, modes[i].fps);
			}

			// Select a Mode
			Console.WriteLine("Select a Mode (Write a number or just press ENTER to select the mode 0");
			int selected = -1;
			while (selected == -1) {
				String input = Console.ReadLine();
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
			_device.SetVideoMode(stream, modes[selected]);
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
				_handTracker.OnUpdateEvent -= onHandTrackerUpdate;

				Nuitrack.Release();
			}
			catch (System.Exception exception) {
				Console.WriteLine("Nuitrack release failed.");
				throw exception;
			}
		}

		public void Step()
        {
			/*
			try
			{
				Nuitrack.WaitUpdate(_skeletonTracker);
				Nuitrack.WaitUpdate(_handTracker);
			}
			catch (LicenseNotAcquiredException exception)
			{
				Console.WriteLine("LicenseNotAcquired exception. Exception: ", exception);
				throw exception;
			}
			catch (System.Exception exception)
			{
				Console.WriteLine("Nuitrack update failed. Exception: ", exception);
			}

			// Skeleton joints
			if (_skeletonData != null)
			{
				_joints = new List<float>();
				for (int i = 0; i < _skeletonData.NumUsers; i++)
                {
					Skeleton skeleton = _skeletonData.Skeletons[i];
					for (int j = 0; j < skeleton.Joints.Length; j++)
                    {
						Joint joint = skeleton.Joints[j];
						_joints.Add(i);                  // User Id
						_joints.Add((float)joint.Type);  // Joint Type 
						_joints.Add(joint.Proj.X);       // Joint PosX
						_joints.Add(joint.Proj.Y);       // Joint PosY
						_joints.Add(joint.Proj.Z);       // Joint Pos2

					}
                }
			}

			_streamer.SendSkeleton(_joints);
			*/
			/*
			// Hand pointers
			if (_handTrackerData != null)
			{
				foreach (var userHands in _handTrackerData.UsersHands)
				{
					if (userHands.LeftHand != null)
					{
						HandContent hand = userHands.LeftHand.Value;
						_lhand = new int[3] { hand.XReal, hand.YReal, hand.ZReal };
					}

					if (userHands.RightHand != null)
					{
						HandContent hand = userHands.RightHand.Value;
						int handSize = hand.Click ? 20 : 30;
						Console.WriteLine(hand.X + "," + hand.Y + " " + handSize);
					}
				}
			}
			*/
		}

        private void onHandTrackerUpdate(HandTrackerData handTrackerData)
		{
			if (_handTrackerData != null) { _handTrackerData.Dispose(); }
			_handTrackerData = (HandTrackerData)handTrackerData.Clone();
		}

        private void onSkeletonUpdate(SkeletonData skeletonData)
		{
			if (_skeletonData != null) { _skeletonData.Dispose(); }
			_skeletonData = (SkeletonData)skeletonData.Clone();
		}

        private void onUserTrackerLostUser(int userID) {
			Console.WriteLine("Lost User {0}", userID);
		}

        private void onUserTrackerNewUser(int userID) {
			Console.WriteLine("New User {0}", userID);
		}

        private void onUserTrackerUpdate(UserFrame frame)
        {
            //throw new NotImplementedException();
        }

        private void onColorSensorUpdate(ColorFrame frame)
        {
            //throw new NotImplementedException();
        }

        private void onDepthSensorUpdate(DepthFrame depthFrame)
        {
			if (_depthFrame != null)
				_depthFrame.Dispose();
			_depthFrame = (DepthFrame)depthFrame.Clone();
		}
    }
}
