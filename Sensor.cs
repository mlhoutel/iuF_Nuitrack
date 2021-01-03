using System;
using System.Collections.Generic;
using System.Text;


using nuitrack;
using nuitrack.issues;

namespace iuF
{

    public class Sensor
    {

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

		public void Run()
        {
			Initialize();
			while (true)
			{
				Step();
			}
			Stop();
		}
		public void Initialize()
        {
			try
			{
				Nuitrack.Init("");
			}
			catch (System.Exception exception)
			{
				Console.WriteLine("Cannot initialize Nuitrack.");
				throw exception;
			}

			try
			{
				// Create and setup all required modules
				_depthSensor = DepthSensor.Create();
				_colorSensor = ColorSensor.Create();
				_userTracker = UserTracker.Create();
				_skeletonTracker = SkeletonTracker.Create();
				_handTracker = HandTracker.Create();
				_gestureRecognizer = GestureRecognizer.Create();
			}
			catch (System.Exception exception)
			{
				Console.WriteLine("Cannot create Nuitrack module.");
				throw exception;
			}

			//_depthSensor.SetMirror(false);

			// Add event handlers for all modules
			_depthSensor.OnUpdateEvent += onDepthSensorUpdate;
			_colorSensor.OnUpdateEvent += onColorSensorUpdate;
			_userTracker.OnUpdateEvent += onUserTrackerUpdate;
			_userTracker.OnNewUserEvent += onUserTrackerNewUser;
			_userTracker.OnLostUserEvent += onUserTrackerLostUser;
			_skeletonTracker.OnSkeletonUpdateEvent += onSkeletonUpdate;
			_handTracker.OnUpdateEvent += onHandTrackerUpdate;
			_gestureRecognizer.OnNewGesturesEvent += onNewGestures;

			// Add an event handler for the IssueUpdate event
			Nuitrack.onIssueUpdateEvent += onIssueDataUpdate;

			// Create and configure the Bitmap object according to the depth sensor output mode
			OutputMode mode = _depthSensor.GetOutputMode();
			OutputMode colorMode = _colorSensor.GetOutputMode();

			// Run Nuitrack. This starts sensor data processing.
			try
			{
				Nuitrack.Run();
			}
			catch (System.Exception exception)
			{
				Console.WriteLine("Cannot start Nuitrack.");
				throw exception;
			}
		}

		public void Stop()
        {
			// Release Nuitrack and remove all modules
			try
			{
				Nuitrack.onIssueUpdateEvent -= onIssueDataUpdate;

				_depthSensor.OnUpdateEvent -= onDepthSensorUpdate;
				_colorSensor.OnUpdateEvent -= onColorSensorUpdate;
				_userTracker.OnUpdateEvent -= onUserTrackerUpdate;
				_skeletonTracker.OnSkeletonUpdateEvent -= onSkeletonUpdate;
				_handTracker.OnUpdateEvent -= onHandTrackerUpdate;
				_gestureRecognizer.OnNewGesturesEvent -= onNewGestures;

				Nuitrack.Release();
			}
			catch (System.Exception exception)
			{
				Console.WriteLine("Nuitrack release failed.");
				throw exception;
			}
		}

		public void Step()
        {

        }

        private void onIssueDataUpdate(IssuesData issuesData)
        {
            throw new NotImplementedException();
        }

        private void onNewGestures(GestureData gestures)
        {
            throw new NotImplementedException();
        }

        private void onHandTrackerUpdate(HandTrackerData handTrackerData)
        {
            throw new NotImplementedException();
        }

        private void onSkeletonUpdate(SkeletonData skeletonData)
        {
            throw new NotImplementedException();
        }

        private void onUserTrackerLostUser(int userID)
        {
            throw new NotImplementedException();
        }

        private void onUserTrackerNewUser(int userID)
        {
            throw new NotImplementedException();
        }

        private void onUserTrackerUpdate(UserFrame frame)
        {
            throw new NotImplementedException();
        }

        private void onColorSensorUpdate(ColorFrame frame)
        {
            throw new NotImplementedException();
        }

        private void onDepthSensorUpdate(DepthFrame frame)
        {
            throw new NotImplementedException();
        }
    }
}
