// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Diagnostics;
using Serilog;

namespace Framework.Metrics;

public class MeteredMetric : IDisposable
{
    public ConsoleColor MetricColor = ConsoleColor.Magenta;
    private readonly Stopwatch _stopwatch = new();
    private readonly uint _logEvery;
    private readonly string _name;
    private readonly bool _recordlessThanOnems;
    private readonly bool _log;
    private uint _loops = 0;
    private TimeSpan _total = TimeSpan.Zero;
    private TimeSpan _max = TimeSpan.Zero;
    private TimeSpan _min = TimeSpan.MaxValue;

    public MeteredMetric(string name, uint logEveryXmarks = 1, bool recordlessThanOnems = true, bool log = false)
    {
        _logEvery = logEveryXmarks;
        _name = name;
        _recordlessThanOnems = recordlessThanOnems;
        _log = log;
    }

    public void Dispose()
    {
        StopMark();
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
            Console.ForegroundColor = MetricColor;
            var msg = $"<{_name}> Avg: {(_total / _loops).TotalMilliseconds}ms, Max: {_max.TotalMilliseconds}ms, Min: {_min.TotalMilliseconds}ms, Num loops: {_loops}, Total: {_total.TotalMilliseconds}ms";
            Console.WriteLine(msg);

            if (_log)
                Log.Logger.Debug(msg);

            Console.ForegroundColor = ConsoleColor.Green;
            _total = TimeSpan.Zero;
            _loops = 0;
            _max = TimeSpan.Zero;
            _min = TimeSpan.MaxValue;
        }
    }
}