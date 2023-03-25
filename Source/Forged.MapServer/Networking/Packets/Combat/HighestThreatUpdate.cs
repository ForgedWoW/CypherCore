﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class HighestThreatUpdate : ServerPacket
{
	public ObjectGuid UnitGUID;
	public List<ThreatInfo> ThreatList = new();
	public ObjectGuid HighestThreatGUID;
	public HighestThreatUpdate() : base(ServerOpcodes.HighestThreatUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(UnitGUID);
		_worldPacket.WritePackedGuid(HighestThreatGUID);
		_worldPacket.WriteInt32(ThreatList.Count);

		foreach (var threatInfo in ThreatList)
		{
			_worldPacket.WritePackedGuid(threatInfo.UnitGUID);
			_worldPacket.WriteInt64(threatInfo.Threat);
		}
	}
}