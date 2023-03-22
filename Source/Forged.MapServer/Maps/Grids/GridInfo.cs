// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Maps.Grids;

public class GridInfo
{
	readonly PeriodicTimer _visUpdate;
	readonly TimeTracker _timer;

	ushort _unloadActiveLockCount; // lock from active object spawn points (prevent clone loading)
	bool _unloadExplicitLock;      // explicit manual lock or config setting

	public GridInfo()
	{
		_timer = new TimeTracker(0);
		_visUpdate = new PeriodicTimer(0, RandomHelper.IRand(0, 1000));
		_unloadActiveLockCount = 0;
		_unloadExplicitLock = false;
	}

	public GridInfo(long expiry, bool unload = true)
	{
		_timer = new TimeTracker((uint)expiry);
		_visUpdate = new PeriodicTimer(0, RandomHelper.IRand(0, 1000));
		_unloadActiveLockCount = 0;
		_unloadExplicitLock = !unload;
	}

	public TimeTracker GetTimeTracker()
	{
		return _timer;
	}

	public bool GetUnloadLock()
	{
		return _unloadActiveLockCount != 0 || _unloadExplicitLock;
	}

	public void SetUnloadExplicitLock(bool on)
	{
		_unloadExplicitLock = on;
	}

	public void IncUnloadActiveLock()
	{
		++_unloadActiveLockCount;
	}

	public void DecUnloadActiveLock()
	{
		if (_unloadActiveLockCount != 0) --_unloadActiveLockCount;
	}

	public void ResetTimeTracker(long interval)
	{
		_timer.Reset((uint)interval);
	}

	public void UpdateTimeTracker(long diff)
	{
		_timer.Update((uint)diff);
	}

	public PeriodicTimer GetRelocationTimer()
	{
		return _visUpdate;
	}
}