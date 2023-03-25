// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Combat;

public class ThreatUpdate : ServerPacket
{
	public ObjectGuid UnitGUID;
	public List<ThreatInfo> ThreatList = new();
	public ThreatUpdate() : base(ServerOpcodes.ThreatUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WriteInt32(ThreatList.Count);

		foreach (var threatInfo in ThreatList)
		{
			_worldPacket.WritePackedGuid(threatInfo.UnitGUID);
			_worldPacket.WriteInt64(threatInfo.Threat);
		}
	}
}