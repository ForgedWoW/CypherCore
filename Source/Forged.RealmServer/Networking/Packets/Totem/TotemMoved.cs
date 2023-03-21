// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class TotemMoved : ServerPacket
{
	public ObjectGuid Totem;
	public byte Slot;
	public byte NewSlot;
	public TotemMoved() : base(ServerOpcodes.TotemMoved) { }

	public override void Write()
	{
		_worldPacket.WriteUInt8(Slot);
		_worldPacket.WriteUInt8(NewSlot);
		_worldPacket.WritePackedGuid(Totem);
	}
}