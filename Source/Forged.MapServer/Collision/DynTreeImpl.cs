﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Collision;

public class DynTreeImpl : RegularGrid2D<GameObjectModel, BIHWrap<GameObjectModel>>
{
	readonly TimeTracker _rebalanceTimer;
	int _unbalancedTimes;

	public DynTreeImpl()
	{
		_rebalanceTimer = new TimeTracker(200);
		_unbalancedTimes = 0;
	}

	public override void Insert(GameObjectModel mdl)
	{
		base.Insert(mdl);
		++_unbalancedTimes;
	}

	public override void Remove(GameObjectModel mdl)
	{
		base.Remove(mdl);
		++_unbalancedTimes;
	}

	public override void Balance()
	{
		base.Balance();
		_unbalancedTimes = 0;
	}

	public void Update(uint difftime)
	{
		if (Empty())
			return;

		_rebalanceTimer.Update(difftime);

		if (_rebalanceTimer.Passed)
		{
			_rebalanceTimer.Reset(200);

			if (_unbalancedTimes > 0)
				Balance();
		}
	}
}