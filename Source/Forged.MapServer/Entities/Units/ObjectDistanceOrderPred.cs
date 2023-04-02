// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Entities.Units;

public class ObjectDistanceOrderPred : IComparer<WorldObject>
{
    private readonly bool _ascending;
    private readonly WorldObject _refObj;
    public ObjectDistanceOrderPred(WorldObject pRefObj, bool ascending = true)
    {
        _refObj = pRefObj;
        _ascending = ascending;
    }

    public int Compare(WorldObject pLeft, WorldObject pRight)
    {
        return (_ascending ? _refObj.Location.GetDistanceOrder(pLeft, pRight) : !_refObj.Location.GetDistanceOrder(pLeft, pRight)) ? 1 : 0;
    }
}