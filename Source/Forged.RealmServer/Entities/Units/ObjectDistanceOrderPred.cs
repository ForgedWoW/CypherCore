// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.Entities;

public class ObjectDistanceOrderPred : IComparer<WorldObject>
{
	readonly WorldObject _refObj;
	readonly bool _ascending;

	public ObjectDistanceOrderPred(WorldObject pRefObj, bool ascending = true)
	{
		_refObj = pRefObj;
		_ascending = ascending;
	}

	public int Compare(WorldObject pLeft, WorldObject pRight)
	{
		return (_ascending ? _refObj.GetDistanceOrder(pLeft, pRight) : !_refObj.GetDistanceOrder(pLeft, pRight)) ? 1 : 0;
	}
}