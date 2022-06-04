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
        public ASeeVRDataHandler(
            EyeTracker eyeTracker,
            UDPSender oscSender,
            OSCEyeTracker configData
        )
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
        private readonly EyeTracker _eyeTracker;

        /// <summary>
        /// OSC Sender object.
        /// </summary>
        private readonly UDPSender _oscSender;

        #endregion

        #region Public Properties

        /// <summary>
        /// Configuration data object.
        /// </summary>
        public OSCEyeTracker ConfigData { get; private set; }

        #endregion

        #region Private Properties

        /// <summary>
        /// Store the previous good eye positions
        /// </summary>
        float lastGoodLeftX, lastGoodRightX, lastGoodLeftY, lastGoodRightY;

        /// <summary>
        /// Timers
        /// </summary>
        /// integers incrementing each frames under certain conditions to time stuff.
        float blinkTimer, trackingLossTimerLeft, trackingLossTimerRight;

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the eye tracking values being sent to VRChat.
        /// </summary>
        public void UpdateValues()
        {
            Queue<OscMessage> messages = new Queue<OscMessage>();

            // Get the eyes' positions
            float x_Left = _eyeTracker.GetEyeParameter(
                _eyeTracker.LeftEye.Eye,
                EyeParameter.PupilCenterX
            );
            float x_Right = _eyeTracker.GetEyeParameter(
                _eyeTracker.RightEye.Eye,
                EyeParameter.PupilCenterX
            );
            float y_Left = _eyeTracker.GetEyeParameter(
                _eyeTracker.LeftEye.Eye,
                EyeParameter.PupilCenterY
            );
            float y_Right = _eyeTracker.GetEyeParameter(
                _eyeTracker.RightEye.Eye,
                EyeParameter.PupilCenterY
            );

            // Check if the eyes lost tracking
            bool lostTrackingLeft = x_Left == 0;
            bool lostTrackingRight = x_Right == 0;

            // Blinking
            if (lostTrackingLeft && lostTrackingRight)
            {
                blinkTimer++;
                if (blinkTimer >= ConfigData._blinkTime)
                {
                    messages.Enqueue(new OscMessage(ConfigData.EyeLidLeftAddress, 0));
                    messages.Enqueue(new OscMessage(ConfigData.EyeLidRightAddress, 0));
                }
                else
                {
                    messages.Enqueue(new OscMessage(ConfigData.EyeLidLeftAddress, 1));
                    messages.Enqueue(new OscMessage(ConfigData.EyeLidRightAddress, 1));
                }
            }
            else
            {
                blinkTimer = 0;
                messages.Enqueue(new OscMessage(ConfigData.EyeLidLeftAddress, 1));
                messages.Enqueue(new OscMessage(ConfigData.EyeLidRightAddress, 1));
            }

            // Eye positions when losing tracking
            if (lostTrackingLeft)
            {
                trackingLossTimerLeft = 0;
                if (!lostTrackingRight)
                {
                    x_Left = x_Right;
                    y_Left = y_Right;
                }
                else
                {
                    x_Left = lastGoodLeftX;
                    y_Left = lastGoodLeftY;
                }
            }
            else
            {
                if (trackingLossTimerLeft < ConfigData._movingAverageBufferSize)
                {
                    trackingLossTimerLeft++;
                    if (!lostTrackingRight)
                    {
                        x_Left = x_Right;
                        y_Left = y_Right;
                    }
                    else
                    {
                        x_Left = lastGoodLeftX;
                        y_Left = lastGoodLeftY;
                    }
                }
                else
                {
                    lastGoodLeftX = x_Left;
                    lastGoodLeftY = y_Left;
                }
            }

            if (lostTrackingRight)
            {
                trackingLossTimerRight = 0;
                if (!lostTrackingLeft)
                {
                    x_Right = x_Left;
                    y_Right = y_Left;
                }
                else
                {
                    x_Right = lastGoodRightX;
                    y_Right = lastGoodRightY;
                }
            }
            else
            {
                if (trackingLossTimerRight < ConfigData._movingAverageBufferSize)
                {
                    trackingLossTimerRight++;
                    if (!lostTrackingLeft)
                    {
                        x_Right = x_Left;
                        y_Right = y_Left;
                    }
                    else
                    {
                        x_Right = lastGoodRightX;
                        y_Right = lastGoodRightY;
                    }
                }
                else
                {
                    lastGoodRightX = x_Right;
                    lastGoodRightY = y_Right;
                }
            }

            // Add the new values to the moving average
            x_Left = ConfigData.MovingAverageLeftX.Update(x_Left);
            x_Right = ConfigData.MovingAverageRightX.Update(x_Right);
            y_Left = ConfigData.MovingAverageLeftY.Update(y_Left);
            y_Right = ConfigData.MovingAverageRightY.Update(y_Right);

            // Normalize the eye positions according to their ranges
            x_Left = NormalizeFloatAroundZero(x_Left, ConfigData._xLeftRange);
            x_Right = NormalizeFloatAroundZero(x_Right, ConfigData._xRightRange);
            y_Left = NormalizeFloatAroundZero(y_Left, ConfigData._yLeftRange);
            y_Right = NormalizeFloatAroundZero(y_Right, ConfigData._yRightRange);

            // Add all messages to the queue
            messages.Enqueue(new OscMessage(ConfigData.EyeXLeftAddress, x_Left * ConfigData._movementMultiplierX));
            messages.Enqueue(new OscMessage(ConfigData.EyeXRightAddress, x_Right * ConfigData._movementMultiplierX));
            messages.Enqueue(new OscMessage(ConfigData.EyeYAddress, -(y_Left + y_Right / 2) * ConfigData._movementMultiplierY));

            SendMessages(messages);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Normalizes <paramref name="input"/> that exists within range <paramref name="inputRange"/> to the range -1 to 1.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="inputRange"></param>
        private static float NormalizeFloatAroundZero(float input, MinMaxRange inputRange)
        {
            float slope = 2 / (inputRange.Max - inputRange.Min);
            return -1 + slope * (input - inputRange.Min);
        }

        /// <summary>
        /// Sends the messages in the queue passed into the method.
        /// </summary>
        /// <param name="messages">The queue of messages to send</param>
        private void SendMessages(Queue<OscMessage> messages)
        {
            while (messages.Count > 0)
            {
                _oscSender.Send(messages.Dequeue());
            }
        }

        #endregion
    }
}
