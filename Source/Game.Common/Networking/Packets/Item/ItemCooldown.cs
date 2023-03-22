// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class ItemCooldown : ServerPacket
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