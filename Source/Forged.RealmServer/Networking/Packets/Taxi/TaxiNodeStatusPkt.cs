// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

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