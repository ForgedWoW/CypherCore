// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Networking;

namespace Forged.RealmServer.DataStorage;

public struct HotfixId
{
	public int PushID;
	public uint UniqueID;

	public void Write(WorldPacket data)
	{
		data.WriteInt32(PushID);
		data.WriteUInt32(UniqueID);
	}

	public void Read(WorldPacket data)
	{
		PushID = data.ReadInt32();
		UniqueID = data.ReadUInt32();
	}
}