// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Item;

internal class ReadItemResultFailed : ServerPacket
{
	public ObjectGuid Item;
	public byte Subcode;
	public uint Delay;
	public ReadItemResultFailed() : base(ServerOpcodes.ReadItemResultFailed) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Item);
		_worldPacket.WriteUInt32(Delay);
		_worldPacket.WriteBits(Subcode, 2);
		_worldPacket.FlushBits();
	}
}