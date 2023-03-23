// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Garrison;

public class GarrisonDeleteResult : ServerPacket
{
	public GarrisonError Result;
	public uint GarrSiteID;
	public GarrisonDeleteResult() : base(ServerOpcodes.GarrisonDeleteResult, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32((uint)Result);
		_worldPacket.WriteUInt32(GarrSiteID);
	}
}
