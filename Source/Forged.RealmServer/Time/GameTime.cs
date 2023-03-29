// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

public class GameTime
{
	readonly long _startTime = Time.UnixTime;

	long _gameTime = Time.UnixTime;
	uint _gameMSTime = 0;

	DateTime _gameTimeSystemPoint = System.DateTime.MinValue;
	DateTime _gameTimeSteadyPoint = System.DateTime.MinValue;

	DateTime _dateTime;

	public long StartTime => _startTime;

    public long CurrentGameTime => _gameTime;

	public uint GameTimeMS => _gameMSTime;

	public DateTime GetSystemTime => _gameTimeSystemPoint;

	public DateTime Now => _gameTimeSteadyPoint;

    public uint GetUptime => (uint)(_gameTime - _startTime);

	public DateTime DateTime => _dateTime;

	public void UpdateGameTimers()
	{
		_gameTime = Time.UnixTime;
		_gameMSTime = Time.MSTime;
        _gameTimeSystemPoint = System.DateTime.Now;
        _gameTimeSteadyPoint = System.DateTime.Now;

		_dateTime = Time.UnixTimeToDateTime(_gameTime);
	}
}