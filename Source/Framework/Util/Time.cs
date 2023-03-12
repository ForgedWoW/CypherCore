// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Text;

public enum TimeFormat
{
	FullText,  // 1 Days 2 Hours 3 Minutes 4 Seconds
	ShortText, // 1d 2h 3m 4s
	Numeric    // 1:2:3:4
}

public static class Time
{
	public const int Minute = 60;
	public const int Hour = Minute * 60;
	public const int Day = Hour * 24;
	public const int Week = Day * 7;
	public const int Month = Day * 30;
	public const int Year = Month * 12;
	public const int InMilliseconds = 1000;

	public static readonly DateTime ApplicationStartTime = DateTime.Now;

    /// <summary>
    ///  Gets the current Unix time.
    /// </summary>
    public static long UnixTime
	{
		get { return DateTimeToUnixTime(DateTime.Now); }
	}

    /// <summary>
    ///  Gets the current Unix time, in milliseconds.
    /// </summary>
    public static long UnixTimeMilliseconds
	{
		get { return ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds(); }
	}


    /// <summary>
    ///  Gets the system uptime.
    /// </summary>
    /// <returns> the system uptime in milliseconds </returns>
    public static uint SystemTime => (uint)Environment.TickCount;

	public static uint MSTime => (uint)(DateTime.Now - ApplicationStartTime).TotalMilliseconds;

	public static uint GetMSTimeDiff(uint oldMSTime, uint newMSTime)
	{
		if (oldMSTime > newMSTime)
			return (0xFFFFFFFF - oldMSTime) + newMSTime;
		else
			return newMSTime - oldMSTime;
	}

	public static uint GetMSTimeDiff(uint oldMSTime, DateTime newTime)
	{
		var newMSTime = (uint)(newTime - ApplicationStartTime).TotalMilliseconds;

		return GetMSTimeDiff(oldMSTime, newMSTime);
	}

	public static uint GetMSTimeDiffToNow(uint oldMSTime)
	{
		var newMSTime = MSTime;

		if (oldMSTime > newMSTime)
			return (0xFFFFFFFF - oldMSTime) + newMSTime;
		else
			return newMSTime - oldMSTime;
	}

	public static DateTime UnixTimeToDateTime(long unixTime)
	{
		return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime;
	}

	public static long DateTimeToUnixTime(DateTime dateTime)
	{
		return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
	}

	public static long GetNextResetUnixTime(int hours)
	{
		return DateTimeToUnixTime((DateTime.Now.Date + new TimeSpan(hours, 0, 0)));
	}

	public static long GetNextResetUnixTime(int days, int hours)
	{
		return DateTimeToUnixTime((DateTime.Now.Date + new TimeSpan(days, hours, 0, 0)));
	}

	public static long GetNextResetUnixTime(int months, int days, int hours)
	{
		return DateTimeToUnixTime((DateTime.Now.Date + new TimeSpan(months + days, hours, 0)));
	}

	public static long GetLocalHourTimestamp(long time, uint hour, bool onlyAfterTime = true)
	{
		var timeLocal = UnixTimeToDateTime(time);
		timeLocal = new DateTime(timeLocal.Year, timeLocal.Month, timeLocal.Day, 0, 0, 0, timeLocal.Kind);
		var midnightLocal = DateTimeToUnixTime(timeLocal);
		var hourLocal = midnightLocal + hour * Hour;

		if (onlyAfterTime && hourLocal <= time)
			hourLocal += Day;

		return hourLocal;
	}

	public static long LocalTimeToUTCTime(long time)
	{
		return DateTimeToUnixTime(UnixTimeToDateTime(time).ToUniversalTime());
	}

	public static string secsToTimeString(ulong timeInSecs, TimeFormat timeFormat = TimeFormat.FullText, bool hoursOnly = false)
	{
		var secs = timeInSecs % Minute;
		var minutes = timeInSecs % Hour / Minute;
		var hours = timeInSecs % Day / Hour;
		var days = timeInSecs / Day;

		if (timeFormat == TimeFormat.Numeric)
		{
			if (days != 0)
				return $"{days}:{hours}:{minutes}:{secs:2}";
			else if (hours != 0)
				return $"{hours}:{minutes}:{secs:2}";
			else if (minutes != 0)
				return $"{minutes}:{secs:2}";
			else
				return $"0:{secs:2}";
		}

		StringBuilder ss = new();

		if (days != 0)
		{
			ss.Append(days);

			switch (timeFormat)
			{
				case TimeFormat.ShortText:
					ss.Append("d");

					break;
				case TimeFormat.FullText:
					if (days == 1)
						ss.Append(" Day ");
					else
						ss.Append(" Days ");

					break;
				default:
					return "<Unknown time format>";
			}
		}

		if (hours != 0 || hoursOnly)
		{
			ss.Append(hours);

			switch (timeFormat)
			{
				case TimeFormat.ShortText:
					ss.Append("h");

					break;
				case TimeFormat.FullText:

					if (hours <= 1)
						ss.Append(" Hour ");
					else
						ss.Append(" Hours ");

					break;
				default:
					return "<Unknown time format>";
			}
		}

		if (!hoursOnly)
		{
			if (minutes != 0)
			{
				ss.Append(minutes);

				switch (timeFormat)
				{
					case TimeFormat.ShortText:
						ss.Append("m");

						break;
					case TimeFormat.FullText:
						if (minutes == 1)
							ss.Append(" Minute ");
						else
							ss.Append(" Minutes ");

						break;
					default:
						return "<Unknown time format>";
				}
			}

			if (secs != 0 || (days == 0 && hours == 0 && minutes == 0))
			{
				ss.Append(secs);

				switch (timeFormat)
				{
					case TimeFormat.ShortText:
						ss.Append("s");

						break;
					case TimeFormat.FullText:
						if (secs <= 1)
							ss.Append(" Second.");
						else
							ss.Append(" Seconds.");

						break;
					default:
						return "<Unknown time format>";
				}
			}
		}

		return ss.ToString();
	}

	public static uint TimeStringToSecs(string timestring)
	{
		var secs = 0;
		var buffer = 0;
		int multiplier;

		foreach (var c in timestring)
			if (char.IsDigit(c))
			{
				buffer *= 10;
				buffer += c - '0';
			}
			else
			{
				switch (c)
				{
					case 'd':
						multiplier = Day;

						break;
					case 'h':
						multiplier = Hour;

						break;
					case 'm':
						multiplier = Minute;

						break;
					case 's':
						multiplier = 1;

						break;
					default:
						return 0; //bad format
				}

				buffer *= multiplier;
				secs += buffer;
				buffer = 0;
			}

		return (uint)secs;
	}

	public static string GetTimeString(long time)
	{
		var days = time / Day;
		var hours = (time % Day) / Hour;
		var minute = (time % Hour) / Minute;

		return $"Days: {days} Hours: {hours} Minutes: {minute}";
	}

	public static long GetUnixTimeFromPackedTime(uint packedDate)
	{
		var time = new DateTime((int)((packedDate >> 24) & 0x1F) + 2000, (int)((packedDate >> 20) & 0xF) + 1, (int)((packedDate >> 14) & 0x3F) + 1, (int)(packedDate >> 6) & 0x1F, (int)(packedDate & 0x3F), 0);

		return (uint)DateTimeToUnixTime(time);
	}

	public static uint GetPackedTimeFromUnixTime(long unixTime)
	{
		var now = UnixTimeToDateTime(unixTime);

		return Convert.ToUInt32((now.Year - 2000) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute);
	}

	public static uint GetPackedTimeFromDateTime(DateTime now)
	{
		return Convert.ToUInt32((now.Year - 2000) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute);
	}

	public static void Profile(string description, int iterations, Action func)
	{
		//Run at highest priority to minimize fluctuations caused by other processes/threads
		System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.High;
		System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

		// warm up 
		func();

		var watch = new System.Diagnostics.Stopwatch();

		// clean up
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		watch.Start();

		for (var i = 0; i < iterations; i++)
			func();

		watch.Stop();
		Console.Write(description);
		Console.WriteLine(" Time Elapsed {0} ms", watch.Elapsed.TotalMilliseconds);
	}
}

public class TimeTracker
{
	TimeSpan _expiryTime;

	public bool Passed => _expiryTime <= TimeSpan.Zero;

	public TimeSpan Expiry => _expiryTime;

	public TimeTracker(uint expiry = 0)
	{
		_expiryTime = TimeSpan.FromMilliseconds(expiry);
	}

	public TimeTracker(TimeSpan expiry)
	{
		_expiryTime = expiry;
	}

	public void Update(uint diff)
	{
		Update(TimeSpan.FromMilliseconds(diff));
	}

	public void Update(TimeSpan diff)
	{
		_expiryTime -= diff;
	}

	public void Reset(uint expiry)
	{
		Reset(TimeSpan.FromMilliseconds(expiry));
	}

	public void Reset(TimeSpan expiry)
	{
		_expiryTime = expiry;
	}
}

public class IntervalTimer
{
	long _interval;
	long _current;

	public bool Passed => _current >= _interval;

	public long Interval
	{
		get => _interval;
		set => _interval = value;
	}

	public long Current
	{
		get => _current;
		set => _current = value;
	}

	public void Update(long diff)
	{
		_current += diff;

		if (_current < 0)
			_current = 0;
	}

	public void Reset()
	{
		if (_current >= _interval)
			_current %= _interval;
	}
}

public class PeriodicTimer
{
	int _period;
	int _expireTime;

	public PeriodicTimer(int period, int start_time)
	{
		_period = period;
		_expireTime = start_time;
	}

	public bool Update(int diff)
	{
		if ((_expireTime -= diff) > 0)
			return false;

		_expireTime += _period > diff ? _period : diff;

		return true;
	}

	public void SetPeriodic(int period, int start_time)
	{
		_expireTime = start_time;
		_period = period;
	}

	// Tracker interface
	public void TUpdate(int diff)
	{
		_expireTime -= diff;
	}

	public bool TPassed()
	{
		return _expireTime <= 0;
	}

	public void TReset(int diff, int period)
	{
		_expireTime += period > diff ? period : diff;
	}
}