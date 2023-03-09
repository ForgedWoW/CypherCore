using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Metrics
{
    public class MeteredMetric
    {
        private uint _logEvery;
        string _name;
        uint _loops = 0;
        TimeSpan _total = TimeSpan.Zero;
        TimeSpan _max = TimeSpan.Zero;
        TimeSpan _min = TimeSpan.MaxValue;
        readonly Stopwatch _stopwatch = new Stopwatch();
        bool _recordlessThanOnems;

        public MeteredMetric(string name, uint logEveryXmarks, bool recordlessThanOnems) 
        { 
            _logEvery= logEveryXmarks;
            _name = name;
            _recordlessThanOnems = recordlessThanOnems;
        }

        public void StartMark()
        {
            _stopwatch.Restart();
        }

        public void StopMark()
        {
            _stopwatch.Stop();

            if (!_recordlessThanOnems && _stopwatch.ElapsedMilliseconds <= 0)
                return;

            _loops++;
            _total += _stopwatch.Elapsed;

            if (_stopwatch.Elapsed > _max)
                _max = _stopwatch.Elapsed;

            if (_stopwatch.Elapsed < _min && _stopwatch.Elapsed != TimeSpan.Zero)
                _min = _stopwatch.Elapsed;

            if (_loops % _logEvery == 0)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"<{_name}> Avg: {(_total / _loops).TotalMilliseconds}ms, Max: {_max.TotalMilliseconds}ms, Min: {_min.TotalMilliseconds}ms, Num loops: {_loops}, Total: {_total.TotalMilliseconds}ms");
                Console.ForegroundColor = ConsoleColor.Green;
                _total = TimeSpan.Zero;
                _loops = 0;
                _max = TimeSpan.Zero;
                _min = TimeSpan.MaxValue;
            }
        }
    }
}
