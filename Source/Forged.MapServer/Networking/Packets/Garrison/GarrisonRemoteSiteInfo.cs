// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

class GarrisonRemoteSiteInfo
{
	public uint GarrSiteLevelID;
	public List<GarrisonRemoteBuildingInfo> Buildings = new();

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(GarrSiteLevelID);
		data.WriteInt32(Buildings.Count);

		foreach (var building in Buildings)
			building.Write(data);
	}
}