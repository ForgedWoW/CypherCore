// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

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
