// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Query;

public class NameCacheUnused920
{
	public uint Unused1;
	public ObjectGuid Unused2;
	public string Unused3 = "";

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(Unused1);
		data.WritePackedGuid(Unused2);
		data.WriteBits(Unused3.GetByteCount(), 7);
		data.FlushBits();

		data.WriteString(Unused3);
	}
}