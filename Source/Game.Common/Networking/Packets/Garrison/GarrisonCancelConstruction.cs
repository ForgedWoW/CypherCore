// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Objects;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


namespace Game.Common.Networking.Packets.Garrison;

public class GarrisonCancelConstruction : ClientPacket
{
	public ObjectGuid NpcGUID;
	public uint PlotInstanceID;
	public GarrisonCancelConstruction(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		NpcGUID = _worldPacket.ReadPackedGuid();
		PlotInstanceID = _worldPacket.ReadUInt32();
	}
}
