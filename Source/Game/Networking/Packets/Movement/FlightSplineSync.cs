// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

class FlightSplineSync : ServerPacket
{
	public ObjectGuid Guid;
	public float SplineDist;
	public FlightSplineSync() : base(ServerOpcodes.FlightSplineSync, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
		_worldPacket.WriteFloat(SplineDist);
	}
}