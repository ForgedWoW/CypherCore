// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

class NearestCheckCustomizer : NoopCheckCustomizer
{
	readonly WorldObject _obj;
	float _range;

	public NearestCheckCustomizer(WorldObject obj, float range)
	{
		_obj = obj;
		_range = range;
	}

	public override bool Test(WorldObject o)
	{
		return _obj.IsWithinDist(o, _range);
	}

	public override void Update(WorldObject o)
	{
		_range = _obj.GetDistance(o);
	}
}