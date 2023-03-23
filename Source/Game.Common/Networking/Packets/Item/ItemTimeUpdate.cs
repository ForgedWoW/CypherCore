// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Item;

public class ItemTimeUpdate : ServerPacket
{
	public ObjectGuid ItemGuid;
	public uint DurationLeft;
	public ItemTimeUpdate() : base(ServerOpcodes.ItemTimeUpdate) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ItemGuid);
		_worldPacket.WriteUInt32(DurationLeft);
	}
}
