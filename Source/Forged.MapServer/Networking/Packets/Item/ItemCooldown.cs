// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

class ItemCooldown : ServerPacket
{
	public ObjectGuid ItemGuid;
	public uint SpellID;
	public uint Cooldown;
	public ItemCooldown() : base(ServerOpcodes.ItemCooldown) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ItemGuid);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteUInt32(Cooldown);
	}
}