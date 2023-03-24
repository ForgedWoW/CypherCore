// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Guild;

public class GuildEventPlayerLeft : ServerPacket
{
	public ObjectGuid LeaverGUID;
	public string LeaverName;
	public uint LeaverVirtualRealmAddress;
	public ObjectGuid RemoverGUID;
	public string RemoverName;
	public uint RemoverVirtualRealmAddress;
	public bool Removed;
	public GuildEventPlayerLeft() : base(ServerOpcodes.GuildEventPlayerLeft) { }

	public override void Write()
	{
		_worldPacket.WriteBit(Removed);
		_worldPacket.WriteBits(LeaverName.GetByteCount(), 6);

		if (Removed)
		{
			_worldPacket.WriteBits(RemoverName.GetByteCount(), 6);
			_worldPacket.WritePackedGuid(RemoverGUID);
			_worldPacket.WriteUInt32(RemoverVirtualRealmAddress);
			_worldPacket.WriteString(RemoverName);
		}

		_worldPacket.WritePackedGuid(LeaverGUID);
		_worldPacket.WriteUInt32(LeaverVirtualRealmAddress);
		_worldPacket.WriteString(LeaverName);
	}
}
