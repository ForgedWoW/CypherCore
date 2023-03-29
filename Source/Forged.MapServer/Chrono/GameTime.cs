// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Chrono;

public class GameTime
{
    private static readonly long StartTime = Time.UnixTime;

    private static long _gameTime = Time.UnixTime;
    private static uint _gameMSTime = 0;

    private static DateTime _gameTimeSystemPoint = DateTime.MinValue;
    private static DateTime _gameTimeSteadyPoint = DateTime.MinValue;

    private static DateTime _dateTime;

    public static long GetStartTime()
    {
        return StartTime;
    }

    public static long GetGameTime()
    {
        return _gameTime;
    }

    public static uint GetGameTimeMS()
    {
        return _gameMSTime;
    }

    public static DateTime GetSystemTime()
    {
        return _gameTimeSystemPoint;
    }

    public static DateTime Now()
    {
        return _gameTimeSteadyPoint;
    }

    public static uint GetUptime()
    {
        return (uint)(_gameTime - StartTime);
    }

    public static DateTime GetDateAndTime()
    {
        return _dateTime;
    }

    public static void UpdateGameTimers()
    {
        _gameTime = Time.UnixTime;
        _gameMSTime = Time.MSTime;
        _gameTimeSystemPoint = DateTime.Now;
        _gameTimeSteadyPoint = DateTime.Now;

        _dateTime = Time.UnixTimeToDateTime(_gameTime);
    }
}