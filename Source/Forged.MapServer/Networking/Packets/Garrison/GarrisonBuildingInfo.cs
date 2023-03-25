// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class GarrisonBuildingInfo
{
	public uint GarrPlotInstanceID;
	public uint GarrBuildingID;
	public long TimeBuilt;
	public uint CurrentGarSpecID;
	public long TimeSpecCooldown = 2288912640; // 06/07/1906 18:35:44 - another in the series of magic blizz dates
	public bool Active;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(GarrPlotInstanceID);
		data.WriteUInt32(GarrBuildingID);
		data.WriteInt64(TimeBuilt);
		data.WriteUInt32(CurrentGarSpecID);
		data.WriteInt64(TimeSpecCooldown);
		data.WriteBit(Active);
		data.FlushBits();
	}
}