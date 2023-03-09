using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.Metrics
{
    public class MetricFactory
    {
        private uint _logEvery;
        private bool _recordlessThanOnems;
        public MetricFactory(uint logEvery, bool recordlessThanOnems) 
        { 
            _logEvery= logEvery;
            _recordlessThanOnems = recordlessThanOnems;
        }

        Dictionary<string, MeteredMetric> _meteredMetrics = new();

        public MeteredMetric Meter(string name)
        {
            return _meteredMetrics.GetOrAdd(name, () => new MeteredMetric(name, _logEvery, _recordlessThanOnems));
        }
    }
}
