﻿using ASeeVROSCServer.ASeeVRInterface.Utilites;
using System.IO;
using System.Text.Json;

namespace ASeeVROSCServer.ASeeVRInterface
{
    /// <summary>
    /// Data object that handles configuration values for the eye tracker
    /// </summary>
    public class OSCEyeTracker
    {
        #region Constructor

        /// <summary>
        /// Constructor.
        /// </summary>
        public OSCEyeTracker()
        {
            _averageSteps = 6;
            _movingAverageBufferSize = 10;
            _movementMultiplierX = 2;
            _movementMultiplierY = 10;
            _xLeftRange = new MinMaxRange(0, 1);
            _xRightRange = new MinMaxRange(0, 1);
            _yLeftRange = new MinMaxRange(0, 1);
            _xRightRange = new MinMaxRange(0, 1);
        }

        #endregion

        #region Public Properties


        #region OSC Addresses

        /// <summary>
        /// String addresses for OSC message sending.
        /// </summary>
        public string EyeXLeftAddress { get; set; }
        public string EyeXRightAddress { get; set; }
        public string EyeYLeftAddress { get; set; }
        public string EyeYRightAddress { get; set; }
        public string EyeYAddress { get; set; }
        public string EyeLidLeftAddress { get; set; }
        public string EyeLidRightAddress { get; set; }

        #endregion

        #region Min Max Ranges

        /// <summary>
        /// Input data ranges.
        /// </summary>
        public MinMaxRange _xLeftRange { get; private set; }
        public MinMaxRange _xRightRange { get; private set; }
        public MinMaxRange _yLeftRange { get; private set; }
        public MinMaxRange _yRightRange { get; private set; }

        #endregion

        #region Movement Multipliers

        /// <summary>
        /// Eye Tracking Multipliers.
        /// </summary>
        public int _movementMultiplierX { get; private set; }
        public int _movementMultiplierY { get; private set; }

        #endregion

        #region Moving Averages

        /// <summary>
        /// Moving average objects for smoothing.
        /// </summary>
        public SimpleMovingAverage MovingAverageLeftX;
        public SimpleMovingAverage MovingAverageRightX;
        public SimpleMovingAverage MovingAverageLeftY;
        public SimpleMovingAverage MovingAverageRightY;

        #endregion

        #endregion

        #region Private Properties

        /// <summary>
        /// Moving Average config.
        /// </summary>
        private int _averageSteps;
        private int _movingAverageBufferSize;        

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the tracking config data based on values read from a JSON config file.
        /// </summary>
        /// <param name="filepath">File path to the JSON config file</param>
        public void InitializeTrackingParams(string filepath)
        {
            string text = File.ReadAllText(filepath);
            JsonDocument doc = JsonDocument.Parse(text);
            JsonElement root = doc.RootElement;
            _averageSteps = root.GetProperty("AverageSteps").GetInt32();
            _movingAverageBufferSize = root.GetProperty("MovingAverageBufferSize").GetInt32();
            _movementMultiplierX = root.GetProperty("MovementMultiplierX").GetInt32();
            _movementMultiplierY = root.GetProperty("MovementMultiplierY").GetInt32();
            EyeXLeftAddress = root.GetProperty("EyeXLeftAddress").GetString();
            EyeXRightAddress = root.GetProperty("EyeXRightAddress").GetString();
            EyeYLeftAddress = root.GetProperty("EyeYLeftAddress").GetString();
            EyeYRightAddress = root.GetProperty("EyeYRightAddress").GetString();
            EyeYAddress = root.GetProperty("EyeYAddress").GetString();
            EyeLidLeftAddress = root.GetProperty("EyeLidLeftAddress").GetString();
            EyeLidRightAddress = root.GetProperty("EyeLidRightAddress").GetString();
            _xLeftRange = new MinMaxRange(root.GetProperty("xLeftRange"));
            _yLeftRange = new MinMaxRange(root.GetProperty("yLeftRange"));
            _xRightRange = new MinMaxRange(root.GetProperty("xRightRange"));
            _yRightRange = new MinMaxRange(root.GetProperty("yRightRange"));

            InitializeMovingArrays();
        }

        /// <summary>
        /// Initialize the moving arrays with the steps read in from the JSON file.
        /// </summary>
        public void InitializeMovingArrays()
        {
            MovingAverageLeftX = new SimpleMovingAverage(_averageSteps);
            MovingAverageRightX = new SimpleMovingAverage(_averageSteps);
            MovingAverageLeftY = new SimpleMovingAverage(_averageSteps);
            MovingAverageRightY = new SimpleMovingAverage(_averageSteps);
        }

        #endregion

    }
}
