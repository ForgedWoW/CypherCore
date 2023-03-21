// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Collision;

public class ManagedModel
{
	WorldModel _model;
	int _count;

	public ManagedModel()
	{
		_model = new WorldModel();
		_count = 0;
	}

	public void SetModel(WorldModel model)
	{
		_model = model;
	}

	public WorldModel GetModel()
	{
		return _model;
	}

	public void IncRefCount()
	{
		++_count;
	}

	public int DecRefCount()
	{
		return --_count;
	}
}