﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct QuestCompleteDisplaySpell
{
	public uint SpellID;
	public uint PlayerConditionID;

	public QuestCompleteDisplaySpell(uint spellID, uint playerConditionID)
	{
		SpellID = spellID;
		PlayerConditionID = playerConditionID;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(SpellID);
		data.WriteUInt32(PlayerConditionID);
	}
}