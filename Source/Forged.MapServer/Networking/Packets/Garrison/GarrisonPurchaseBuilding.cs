// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Garrison;

class GarrisonPurchaseBuilding : ClientPacket
{
	public ObjectGuid NpcGUID;
	public uint BuildingID;
	public uint PlotInstanceID;
	public GarrisonPurchaseBuilding(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		NpcGUID = _worldPacket.ReadPackedGuid();
		PlotInstanceID = _worldPacket.ReadUInt32();
		BuildingID = _worldPacket.ReadUInt32();
	}
}