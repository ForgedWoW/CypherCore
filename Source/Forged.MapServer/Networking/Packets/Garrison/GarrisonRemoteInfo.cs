// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Garrison;

internal class GarrisonRemoteInfo : ServerPacket
{
	public List<GarrisonRemoteSiteInfo> Sites = new();
	public GarrisonRemoteInfo() : base(ServerOpcodes.GarrisonRemoteInfo, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Sites.Count);

		foreach (var site in Sites)
			site.Write(_worldPacket);
	}
}