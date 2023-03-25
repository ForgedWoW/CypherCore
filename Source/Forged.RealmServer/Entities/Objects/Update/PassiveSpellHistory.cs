// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Forged.RealmServer.Entities;

public class PassiveSpellHistory
{
	public int SpellID;
	public int AuraSpellID;

	public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
	{
		data.WriteInt32(SpellID);
		data.WriteInt32(AuraSpellID);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
	{
		data.WriteInt32(SpellID);
		data.WriteInt32(AuraSpellID);
	}
}