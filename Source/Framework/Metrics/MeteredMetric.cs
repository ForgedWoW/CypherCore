// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Diagnostics;

namespace Framework.Metrics;

public class MeteredMetric : IDisposable
{
	public ConsoleColor MetricColor = ConsoleColor.Magenta;
	readonly Stopwatch _stopwatch = new();
	private readonly uint _logEvery;
	readonly string _name;
	uint _loops = 0;
	TimeSpan _total = TimeSpan.Zero;
	TimeSpan _max = TimeSpan.Zero;
	TimeSpan _min = TimeSpan.MaxValue;
	readonly bool _recordlessThanOnems;
	readonly bool _log;

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
				Log.outDebug(LogFilter.Metric, msg);

			Console.ForegroundColor = ConsoleColor.Green;
			_total = TimeSpan.Zero;
			_loops = 0;
			_max = TimeSpan.Zero;
			_min = TimeSpan.MaxValue;
		}
	}
}