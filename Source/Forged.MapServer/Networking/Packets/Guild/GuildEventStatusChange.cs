// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class GuildEventStatusChange : ServerPacket
{
	public ObjectGuid Guid;
	public bool AFK;
	public bool DND;
	public GuildEventStatusChange() : base(ServerOpcodes.GuildEventStatusChange) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteBit(AFK);
		_worldPacket.WriteBit(DND);
		_worldPacket.FlushBits();
	}
}