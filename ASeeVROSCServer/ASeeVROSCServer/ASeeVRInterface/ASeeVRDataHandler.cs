using Pimax.EyeTracking;
using SharpOSC;
using System;
using System.Collections.Generic;

namespace ASeeVROSCServer.ASeeVRInterface
{
    public class ASeeVRDataHandler
    {
        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="eyeTracker">Eye tracker data source object.</param>
        /// <param name="oscSender">OSC sending object.</param>
        /// <param name="averageSteps">Step count for the rolling average.</param>
        public ASeeVRDataHandler(EyeTracker eyeTracker, UDPSender oscSender, int averageSteps)
        {
            _eyeTracker = eyeTracker;
            _oscSender = new UDPSender("127.0.0.1", 9000);
            _movingAverageLeftX = new SimpleMovingAverage(averageSteps);
            _movingAverageRightX = new SimpleMovingAverage(averageSteps);
            _movingAverageLeftY = new SimpleMovingAverage(averageSteps);
            _movingAverageRightY = new SimpleMovingAverage(averageSteps);
            _eyeTracker.OnUpdate += UpdateValues;
            EyeTrackingHorizontalLeftAddress = "/avatar/parameters/LeftX";
            EyeTrackingHorizontalRightAddress = "/avatar/parameters/RightX";
            EyeTrackingVerticalAddress = "/avatar/parameters/EyesY";
            EyeTrackingBlinkLeftAddress = "/avatar/parameters/LeftEyeLid";
            EyeTrackingBlinkRightAddress = "/avatar/parameters/RightEyeLid";

        }

        #endregion

        #region Fields

        /// <summary>
        /// Eye tracker object.
        /// </summary>
        private EyeTracker _eyeTracker;

        
        /// <summary>
        /// OSC Sender object.
        /// </summary>
        UDPSender _oscSender;

        /// <summary>
        /// Moving Averages for smoothing.
        /// </summary>
        SimpleMovingAverage _movingAverageLeftX;
        SimpleMovingAverage _movingAverageRightX;
        SimpleMovingAverage _movingAverageLeftY;
        SimpleMovingAverage _movingAverageRightY;

        #endregion

        #region Public Properties

        public string EyeTrackingHorizontalLeftAddress { get; set; }
        public string EyeTrackingHorizontalRightAddress { get; set; }
        public string EyeTrackingVerticalAddress { get; set; }
        public string EyeTrackingBlinkLeftAddress { get; set; }
        public string EyeTrackingBlinkRightAddress { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the eye tracking values being sent to VRChat.
        /// </summary>
        public void UpdateValues()
        {
            Queue<OscMessage> messages = new Queue<OscMessage>();
            // Get the X eye components
            float x_Left = _eyeTracker.GetEyeParameter(_eyeTracker.LeftEye.Eye, EyeParameter.PupilCenterX);
            float x_Right = _eyeTracker.GetEyeParameter(_eyeTracker.RightEye.Eye, EyeParameter.PupilCenterX);

            // Get the y eye components
            float y_Left = _eyeTracker.GetEyeParameter(_eyeTracker.LeftEye.Eye, EyeParameter.PupilCenterY);
            float y_Right = _eyeTracker.GetEyeParameter(_eyeTracker.RightEye.Eye, EyeParameter.PupilCenterY);

            //Console.WriteLine(y_Left);

            // Add the new values to the moving average
            x_Left = _movingAverageLeftX.Update(x_Left);
            x_Right = _movingAverageRightX.Update(x_Right);

            // Determine if we are blinking
            if(x_Left <= float.Epsilon && x_Right <= float.Epsilon)
            {
                messages.Enqueue(new OscMessage(EyeTrackingBlinkRightAddress, 0));
                messages.Enqueue(new OscMessage(EyeTrackingBlinkLeftAddress, 0));
            } else
            {
                messages.Enqueue(new OscMessage(EyeTrackingBlinkRightAddress, 1));
                messages.Enqueue(new OscMessage(EyeTrackingBlinkLeftAddress, 1));
            }

            y_Left = _movingAverageLeftY.Update(y_Left);
            y_Right = _movingAverageRightY.Update(y_Right);

            // Normalize the axes from 0-1 to -1 to 1
            normalizeAxes(ref x_Left, ref x_Right);
            float y_combined = normalizeAxes(ref y_Left, ref y_Right);
            Console.WriteLine(y_Left);

            // Add the new y value to its moving average
            //y_combined = _movingAverageVert.Update(y_combined);

            // Add all messages to the queue
            messages.Enqueue(new OscMessage(EyeTrackingHorizontalLeftAddress, x_Left*2));
            messages.Enqueue(new OscMessage(EyeTrackingVerticalAddress, (y_Left)));
            messages.Enqueue(new OscMessage(EyeTrackingHorizontalRightAddress, x_Right * 2));


            sendMessages(messages);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Normalizes between two eye inputs and handles tracking loss per eye.  If either eye cannot be tracked, 
        /// tracking falls back to the other eye, if both eyes lose tracking both eyes go to center.
        /// </summary>
        /// <param name="left">Left eye value.</param>
        /// <param name="right">Right eye value.</param>
        /// <returns>Returns an average of the two eyes or whichever is tracking.</returns>
        private float normalizeAxes(ref float left, ref float right)
        {
            // If both eyes are lost center the view
            if (left <= float.Epsilon && right <= float.Epsilon)
            {
                left = 0;
                right = 0;
                return 0;
            }
            // If the left eye has lost tracking, use the right eye's data
            else if (left <= float.Epsilon)
            {
                right = right * 2.0f - 1.0f;
                left = right;
                return right;
            }
            // If the right eye has lost tracking, use the left eye's data
            else if (right <= float.Epsilon)
            {
                left = left * 2.0f - 1.0f;
                right = left;
                return left;
            }
            // Otherwise normalize both eyes
            else
            {
                left = left * 2.0f - 1.0f;
                right = right * 2.0f - 1.0f;
                return (left * right)/2f;
            }
        }

        /// <summary>
        /// Sends the messages in the queue passed into the method.
        /// </summary>
        /// <param name="messages">The queue of messages to send</param>
        private void sendMessages(Queue<OscMessage> messages)
        {
            while(messages.Count>0)
            {
                _oscSender.Send(messages.Dequeue());
            }
        }

        #endregion
    }
}
