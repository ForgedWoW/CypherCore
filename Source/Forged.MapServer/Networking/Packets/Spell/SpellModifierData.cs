﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct SpellModifierData
{
	public double ModifierValue;
	public byte ClassIndex;

	public void Write(WorldPacket data)
	{
		data.WriteFloat((float)ModifierValue);
		data.WriteUInt8(ClassIndex);
	}
}