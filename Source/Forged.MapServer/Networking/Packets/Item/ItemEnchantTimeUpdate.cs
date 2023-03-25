// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

class ItemEnchantTimeUpdate : ServerPacket
{
	public ObjectGuid OwnerGuid;
	public ObjectGuid ItemGuid;
	public uint DurationLeft;
	public uint Slot;
	public ItemEnchantTimeUpdate() : base(ServerOpcodes.ItemEnchantTimeUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ItemGuid);
		_worldPacket.WriteUInt32(DurationLeft);
		_worldPacket.WriteUInt32(Slot);
		_worldPacket.WritePackedGuid(OwnerGuid);
	}
}