// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class ThreatClear : ServerPacket
{
	public ObjectGuid UnitGUID;
	public ThreatClear() : base(ServerOpcodes.ThreatClear) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
	}
}