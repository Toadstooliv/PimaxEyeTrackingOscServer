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
            _movingAverageVert = new SimpleMovingAverage(averageSteps);
            _eyeTracker.OnUpdate += UpdateValues;

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
        SimpleMovingAverage _movingAverageVert;

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the eye tracking values being sent to VRChat.
        /// </summary>
        public void UpdateValues()
        {
            // Get the X eye components
            float x_Left = _eyeTracker.GetEyeParameter(_eyeTracker.LeftEye.Eye, EyeParameter.PupilCenterX);
            float x_Right = _eyeTracker.GetEyeParameter(_eyeTracker.RightEye.Eye, EyeParameter.PupilCenterX);

            // Get the y eye components
            float y_Left = _eyeTracker.GetEyeParameter(_eyeTracker.LeftEye.Eye, EyeParameter.PupilCenterY);
            float y_Right = _eyeTracker.GetEyeParameter(_eyeTracker.RightEye.Eye, EyeParameter.PupilCenterY);

            Queue<OscMessage> messages = new Queue<OscMessage>();

            x_Left = _movingAverageLeftX.Update(x_Left);
            x_Right = _movingAverageRightX.Update(x_Right);

            if(x_Left <= float.Epsilon && x_Right <= float.Epsilon)
            {
                messages.Enqueue(new OscMessage("/avatar/parameters/Blink", 1));
            } else
            {
                messages.Enqueue(new OscMessage("/avatar/parameters/Blink", 0));
            }

            normalizeAxes(ref x_Left, ref x_Right);
            float y_combined = normalizeAxes(ref y_Left, ref y_Right);

            y_combined = _movingAverageVert.Update(y_combined);


            messages.Enqueue(new OscMessage("/avatar/parameters/EyeHorizontal_Left", x_Left*2));
            messages.Enqueue(new OscMessage("/avatar/parameters/EyeHorizontal_Right", x_Right * 2));
            messages.Enqueue(new OscMessage("/avatar/parameters/Eye", y_combined * 2));

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
            if (left <= float.Epsilon && right <= float.Epsilon)
            {
                left = 0;
                right = 0;
                return 0;
            }
            else if (left <= float.Epsilon)
            {
                right = right * 2.0f - 1.0f;
                left = right;
                return right;
            }
            else if (right <= float.Epsilon)
            {
                left = left * 2.0f - 1.0f;
                right = left;
                return left;
            }
            else
            {
                left = left * 2.0f - 1.0f;
                right = right * 2.0f - 1.0f;
                return (left * right)/2;
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
