// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Taxi;

class TaxiNodeStatusPkt : ServerPacket
{
	public TaxiNodeStatus Status; // replace with TaxiStatus enum
	public ObjectGuid Unit;
	public TaxiNodeStatusPkt() : base(ServerOpcodes.TaxiNodeStatus) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Unit);
		_worldPacket.WriteBits(Status, 2);
		_worldPacket.FlushBits();
	}
}