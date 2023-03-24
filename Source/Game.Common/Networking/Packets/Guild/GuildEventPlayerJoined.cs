// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Guild;

public class GuildEventPlayerJoined : ServerPacket
{
	public ObjectGuid Guid;
	public string Name;
	public uint VirtualRealmAddress;
	public GuildEventPlayerJoined() : base(ServerOpcodes.GuildEventPlayerJoined) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteUInt32(VirtualRealmAddress);

		_worldPacket.WriteBits(Name.GetByteCount(), 6);
		_worldPacket.WriteString(Name);
	}
}
