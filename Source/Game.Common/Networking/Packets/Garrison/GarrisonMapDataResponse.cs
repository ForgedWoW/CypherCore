﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Garrison;

namespace Game.Common.Networking.Packets.Garrison;

public class GarrisonMapDataResponse : ServerPacket
{
	public List<GarrisonBuildingMapData> Buildings = new();
	public GarrisonMapDataResponse() : base(ServerOpcodes.GarrisonMapDataResponse, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Buildings.Count);

		foreach (var landmark in Buildings)
			landmark.Write(_worldPacket);
	}
}
