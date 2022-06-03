using ASeeVROSCServer.ASeeVRInterface.Utilites;
using Pimax.EyeTracking;
using SharpOSC;
using System;
using System.Collections.Generic;

namespace ASeeVROSCServer.ASeeVRInterface
{
    /// <summary>
    /// Class that handles the data transfer from the PimaxEyeTracker ASeeVR wrapper to VRChat over OSC.
    /// </summary>
    public class ASeeVRDataHandler
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="eyeTracker">Eye tracker data source object.</param>
        /// <param name="oscSender">OSC sending object.</param>
        /// <param name="averageSteps">Step count for the rolling average.</param>
        public ASeeVRDataHandler(EyeTracker eyeTracker, UDPSender oscSender, int averageSteps, OSCEyeTracker configData)
        {
            _eyeTracker = eyeTracker;
            _oscSender = oscSender;
            ConfigData = configData;
            eyeTracker.OnUpdate += UpdateValues;
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

        #endregion

        #region Public Properties

        /// <summary>
        /// Configuration data object.
        /// </summary>
        public OSCEyeTracker ConfigData { get; private set; }

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
            //Console.WriteLine(_eyeTracker.GetEyeParameter(_eyeTracker.LeftEye.Eye, EyeParameter.GazeX));

            // Add the new values to the moving average
            x_Left = ConfigData.MovingAverageLeftX.Update(x_Left);
            x_Right = ConfigData.MovingAverageRightX.Update(x_Right);

            Console.WriteLine(_eyeTracker.GetEyeExpression(_eyeTracker.LeftEye.Eye, EyeExpression.PupilCenterX));

            // Determine if we are blinking
            if(x_Left <= float.Epsilon && x_Right <= float.Epsilon)
            {
                messages.Enqueue(new OscMessage(ConfigData.EyeLidLeftAddress, 0));
                messages.Enqueue(new OscMessage(ConfigData.EyeLidRightAddress, 0));
            } else
            {
                messages.Enqueue(new OscMessage(ConfigData.EyeLidLeftAddress, 1));
                messages.Enqueue(new OscMessage(ConfigData.EyeLidRightAddress, 1));
            }

            // Place the Y values in their respective moving averages
            y_Left = ConfigData.MovingAverageLeftY.Update(y_Left);
            y_Right = ConfigData.MovingAverageRightY.Update(y_Right);

            // Normalize the axes from 0-1 to -1 to 1
            normalizeAxes(ref x_Left, ref x_Right, ConfigData._xLeftRange);
            float y_combined = normalizeAxes(ref y_Left, ref y_Right, ConfigData._yLeftRange);

            // Add all messages to the queue
            messages.Enqueue(new OscMessage(ConfigData.EyeXLeftAddress, x_Left * ConfigData._movementMultiplierX));
            messages.Enqueue(new OscMessage(ConfigData.EyeXRightAddress, x_Right * ConfigData._movementMultiplierX));
            messages.Enqueue(new OscMessage(ConfigData.EyeYAddress, -(y_combined) * ConfigData._movementMultiplierY));

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
        private float normalizeAxes(ref float left, ref float right, MinMaxRange inputRange)
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
                right = normalizeFloatAroundZero(right, inputRange);
                left = right;
                return right;
            }
            // If the right eye has lost tracking, use the left eye's data
            else if (right <= float.Epsilon)
            {
                left = normalizeFloatAroundZero(left, inputRange);
                right = left;
                return left;
            }
            // Otherwise normalize both eyes
            else
            {
                left = normalizeFloatAroundZero(left, inputRange);
                right = normalizeFloatAroundZero(right, inputRange);
                return (left * right) / 2f;
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

        /// <summary>
        /// Normalizes <paramref name="input"/> that exists within range <paramref name="inputRange"/> to the range -1 to 1.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputRange"></param>
        /// <returns></returns>
        private float normalizeFloatAroundZero(float input, MinMaxRange inputRange)
        {
            float slope = 2 / (inputRange.Max - inputRange.Min);
            return -1 + slope * (input-inputRange.Min);
        }

        #endregion
    }
}
