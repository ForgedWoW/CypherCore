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
    public const int DAY = HOUR * 24;
    public const int HOUR = MINUTE * 60;
    public const int IN_MILLISECONDS = 1000;
    public const int MINUTE = 60;
    public const int MONTH = DAY * 30;
    public const int WEEK = DAY * 7;
    public const int YEAR = MONTH * 12;
    public static readonly DateTime ApplicationStartTime = DateTime.Now;

    public static uint MSTime => (uint)(DateTime.Now - ApplicationStartTime).TotalMilliseconds;

    /// <summary>
    ///     Gets the system uptime.
    /// </summary>
    /// <returns> the system uptime in milliseconds </returns>
    public static uint SystemTime => (uint)Environment.TickCount;

    /// <summary>
    ///     Gets the current Unix time.
    /// </summary>
    public static long UnixTime => DateTimeToUnixTime(DateTime.Now);

    /// <summary>
    ///     Gets the current Unix time, in milliseconds.
    /// </summary>
    public static long UnixTimeMilliseconds => ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();

    public static long DateTimeToUnixTime(DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    public static long GetLocalHourTimestamp(long time, uint hour, bool onlyAfterTime = true)
    {
        var timeLocal = UnixTimeToDateTime(time);
        timeLocal = new DateTime(timeLocal.Year, timeLocal.Month, timeLocal.Day, 0, 0, 0, timeLocal.Kind);
        var midnightLocal = DateTimeToUnixTime(timeLocal);
        var hourLocal = midnightLocal + hour * HOUR;

        if (onlyAfterTime && hourLocal <= time)
            hourLocal += DAY;

        return hourLocal;
    }

    public static uint GetMSTimeDiff(uint oldMSTime, uint newMSTime)
    {
        if (oldMSTime > newMSTime)
            return (0xFFFFFFFF - oldMSTime) + newMSTime;

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

        return newMSTime - oldMSTime;
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

    public static uint GetPackedTimeFromDateTime(DateTime now)
    {
        return Convert.ToUInt32((now.Year - 2000) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute);
    }

    public static uint GetPackedTimeFromUnixTime(long unixTime)
    {
        var now = UnixTimeToDateTime(unixTime);

        return Convert.ToUInt32((now.Year - 2000) << 24 | (now.Month - 1) << 20 | (now.Day - 1) << 14 | (int)now.DayOfWeek << 11 | now.Hour << 6 | now.Minute);
    }

    public static string GetTimeString(long time)
    {
        var days = time / DAY;
        var hours = (time % DAY) / HOUR;
        var minute = (time % HOUR) / MINUTE;

        return $"Days: {days} Hours: {hours} Minutes: {minute}";
    }

    public static long GetUnixTimeFromPackedTime(uint packedDate)
    {
        var time = new DateTime((int)((packedDate >> 24) & 0x1F) + 2000, (int)((packedDate >> 20) & 0xF) + 1, (int)((packedDate >> 14) & 0x3F) + 1, (int)(packedDate >> 6) & 0x1F, (int)(packedDate & 0x3F), 0);

        return (uint)DateTimeToUnixTime(time);
    }

    public static long LocalTimeToUtcTime(long time)
    {
        return DateTimeToUnixTime(UnixTimeToDateTime(time).ToUniversalTime());
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

    public static string SecsToTimeString(ulong timeInSecs, TimeFormat timeFormat = TimeFormat.FullText, bool hoursOnly = false)
    {
        var secs = timeInSecs % MINUTE;
        var minutes = timeInSecs % HOUR / MINUTE;
        var hours = timeInSecs % DAY / HOUR;
        var days = timeInSecs / DAY;

        if (timeFormat == TimeFormat.Numeric)
        {
            if (days != 0)
                return $"{days}:{hours}:{minutes}:{secs:2}";

            if (hours != 0)
                return $"{hours}:{minutes}:{secs:2}";

            return minutes != 0 ? $"{minutes}:{secs:2}" : $"0:{secs:2}";
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
                    ss.Append(days == 1 ? " Day " : " Days ");

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

                    ss.Append(hours <= 1 ? " Hour " : " Hours ");

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
                        ss.Append(minutes == 1 ? " Minute " : " Minutes ");

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
                        ss.Append(secs <= 1 ? " Second." : " Seconds.");

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

        foreach (var c in timestring)
            if (char.IsDigit(c))
            {
                buffer *= 10;
                buffer += c - '0';
            }
            else
            {
                int multiplier;

                switch (c)
                {
                    case 'd':
                        multiplier = DAY;

                        break;

                    case 'h':
                        multiplier = HOUR;

                        break;

                    case 'm':
                        multiplier = MINUTE;

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

    public static DateTime UnixTimeToDateTime(long unixTime)
    {
        return DateTimeOffset.FromUnixTimeSeconds(unixTime).LocalDateTime;
    }
}

public class IntervalTimer
{
    public long Current { get; set; }
    public long Interval { get; set; }
    public bool Passed => Current >= Interval;

    public void Reset()
    {
        if (Current >= Interval)
            Current %= Interval;
    }

    public void Update(long diff)
    {
        Current += diff;

        if (Current < 0)
            Current = 0;
    }
}

public class PeriodicTimer
{
    private int _expireTime;
    private int _period;

    public PeriodicTimer(int period, int startTime)
    {
        _period = period;
        _expireTime = startTime;
    }

    // Tracker interface
    public void Modify(int diff)
    {
        _expireTime -= diff;
    }

    public bool Passed()
    {
        return _expireTime <= 0;
    }

    public void Reset(int diff, int period)
    {
        _expireTime += period > diff ? period : diff;
    }

    public void SetPeriodic(int period, int startTime)
    {
        _expireTime = startTime;
        _period = period;
    }

    public bool Update(int diff)
    {
        if ((_expireTime -= diff) > 0)
            return false;

        _expireTime += _period > diff ? _period : diff;

        return true;
    }
}

public class TimeTracker
{
    private TimeSpan _expiryTime;

    public TimeSpan Expiry => _expiryTime;
    public bool Passed => _expiryTime <= TimeSpan.Zero;

    public TimeTracker(uint expiry = 0)
    {
        _expiryTime = TimeSpan.FromMilliseconds(expiry);
    }

    public TimeTracker(TimeSpan expiry)
    {
        _expiryTime = expiry;
    }

    public void Reset(uint expiry)
    {
        Reset(TimeSpan.FromMilliseconds(expiry));
    }

    public void Reset(TimeSpan expiry)
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
}