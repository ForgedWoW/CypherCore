﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class VoidItemSwapResponse : ServerPacket
{
	public ObjectGuid VoidItemA;
	public ObjectGuid VoidItemB;
	public uint VoidItemSlotA;
	public uint VoidItemSlotB;
	public VoidItemSwapResponse() : base(ServerOpcodes.VoidItemSwapResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(VoidItemA);
		_worldPacket.WriteUInt32(VoidItemSlotA);
		_worldPacket.WritePackedGuid(VoidItemB);
		_worldPacket.WriteUInt32(VoidItemSlotB);
	}
}