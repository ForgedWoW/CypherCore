﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Networking;

namespace Game.Entities;

public class SpellCastVisualField
{
	public uint SpellXSpellVisualID;
	public uint ScriptVisualID;

	public void WriteCreate(WorldPacket data, WorldObject owner, Player receiver)
	{
		data.WriteUInt32(SpellXSpellVisualID);
		data.WriteUInt32(ScriptVisualID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, WorldObject owner, Player receiver)
	{
		data.WriteUInt32(SpellXSpellVisualID);
		data.WriteUInt32(ScriptVisualID);
	}
}