﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Garrison;

public class GarrisonPlotRemoved : ServerPacket
{
	public uint GarrPlotInstanceID;
	public GarrisonPlotRemoved() : base(ServerOpcodes.GarrisonPlotRemoved, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(GarrPlotInstanceID);
	}
}
