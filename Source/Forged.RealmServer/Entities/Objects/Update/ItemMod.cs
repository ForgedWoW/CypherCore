// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Networking;

namespace Forged.RealmServer.Entities;

public class ItemMod
{
	public uint Value;
	public byte Type;

	public void WriteCreate(WorldPacket data, Item owner, Player receiver)
	{
		data.WriteUInt32(Value);
		data.WriteUInt8(Type);
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
	{
		data.WriteUInt32(Value);
		data.WriteUInt8(Type);
	}
}