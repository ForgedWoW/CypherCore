// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Forged.RealmServer.Entities;

public class UnitChannel
{
	public uint SpellID;
	public uint SpellXSpellVisualID;
	public SpellCastVisualField SpellVisual = new();

	public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
	{
		data.WriteUInt32(SpellID);
		SpellVisual.WriteCreate(data, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
	{
		data.WriteUInt32(SpellID);
		SpellVisual.WriteUpdate(data, ignoreChangesMask, owner, receiver);
	}
}