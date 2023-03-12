// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

struct PositionUpdateInfo
{
	public bool Relocated;
	public bool Turned;

	public void Reset()
	{
		Relocated = false;
		Turned = false;
	}
}