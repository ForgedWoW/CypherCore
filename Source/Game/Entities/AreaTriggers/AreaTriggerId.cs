﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Entities;

public struct AreaTriggerId
{
	public uint Id;
	public bool IsServerSide;

	public AreaTriggerId(uint id, bool isServerSide)
	{
		Id = id;
		IsServerSide = isServerSide;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode() ^ IsServerSide.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		return this == (AreaTriggerId)obj;
	}

	public static bool operator ==(AreaTriggerId left, AreaTriggerId right)
	{
		return left.Id == right.Id && left.IsServerSide == right.IsServerSide;
	}

	public static bool operator !=(AreaTriggerId left, AreaTriggerId right)
	{
		return !(left == right);
	}
}