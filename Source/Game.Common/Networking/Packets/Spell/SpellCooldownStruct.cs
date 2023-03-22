﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class SpellCooldownStruct
{
	public uint SrecID;
	public uint ForcedCooldown;
	public float ModRate = 1.0f;

	public SpellCooldownStruct(uint spellId, uint forcedCooldown)
	{
		SrecID = spellId;
		ForcedCooldown = forcedCooldown;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SrecID);
		data.WriteUInt32(ForcedCooldown);
		data.WriteFloat(ModRate);
	}
}