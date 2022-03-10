using System.Text.Json;

namespace ASeeVROSCServer.ASeeVRInterface.Utilites
{
    /// <summary>
    /// Represents a minimum and maximum
    /// </summary>
    public class MinMaxRange
    {
        /// <summary>
        /// Values
        /// </summary>
        public float Max;
        public float Min;

        /// <summary>
        /// Constructor.
        /// </summary>
        public MinMaxRange(float min, float max)
        {
            Max = max;
            Min = min;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MinMaxRange(JsonElement root)
        {
            Max = (float)root.GetProperty("Max").GetDecimal();
            Min = (float)root.GetProperty("Min").GetDecimal();
        }
    }
}
