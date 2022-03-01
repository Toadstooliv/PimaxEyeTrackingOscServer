using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASeeVROSCServer.ASeeVRInterface
{
    public class SimpleMovingAverage
    {
        private readonly int _k;
        private readonly float[] _values;

        private int _index=0;
        private float _sum = 0;
        private int _count = 0;
        private int _trail = 0;
        private int _totalBuffer = 0;
        private float _floatingSum = 0;

        public SimpleMovingAverage(int k)
        {
            if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), "Must be greater than 0");
            
            _k = k;
            _totalBuffer = 20;
            _values = new float[_k];
        }

        public float Update(float nextInput)
        {
            if (_count < _totalBuffer && nextInput > 0.00001f) _count++;
            else if (_count > 0 && nextInput < 0.00001f) _count--;
            
            // calculate the new sum
            _sum = _sum - _values[_index] + nextInput;
            if(_count==_totalBuffer)
            _floatingSum = _sum;

            // overwrite the old value with the new one
            _values[_index] = nextInput;

            // increment the index (wrapping back to 0)
            _index = (_index + 1) % (_k);

            // calculate the average
            if (_sum/(float)_k <= .00001f || _count < _totalBuffer)
            {
                //if(_count > _totalBuffer - _k)
                //{
                //    return (_floatingSum) / (float)_k;
                //}
                return 0.0f;
            }
            else
                return (_floatingSum) / (float)_k;
        }
    }
}
