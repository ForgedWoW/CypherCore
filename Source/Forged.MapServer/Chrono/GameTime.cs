// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Chrono;

public class GameTime
{
    private static readonly long StartTime = Time.UnixTime;

    public static long CurrentTime { get; private set; } = Time.UnixTime;

    public static uint CurrentTimeMS { get; private set; }

    public static DateTime DateAndTime { get; private set; }

    public static DateTime Now { get; private set; } = DateTime.MinValue;

    public static DateTime SystemTime { get; private set; } = DateTime.MinValue;

    public static long Uptime => CurrentTime - StartTime;

    public static long GetStartTime()
    {
        return StartTime;
    }
    public static void UpdateGameTimers()
    {
        CurrentTime = Time.UnixTime;
        CurrentTimeMS = Time.MSTime;
        SystemTime = DateTime.Now;
        Now = DateTime.Now;

        DateAndTime = Time.UnixTimeToDateTime(CurrentTime);
    }
}