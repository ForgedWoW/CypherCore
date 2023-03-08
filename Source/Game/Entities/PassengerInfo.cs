// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

public struct PassengerInfo
{
	public ObjectGuid Guid;
	public bool IsUninteractible;
	public bool IsGravityDisabled;

	public void Reset()
	{
		Guid = ObjectGuid.Empty;
		IsUninteractible = false;
		IsGravityDisabled = false;
	}
}