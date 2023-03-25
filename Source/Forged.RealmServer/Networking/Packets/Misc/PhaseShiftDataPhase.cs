// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

struct PhaseShiftDataPhase
{
	public PhaseShiftDataPhase(uint phaseFlags, uint id)
	{
		PhaseFlags = (ushort)phaseFlags;
		Id = (ushort)id;
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt16(PhaseFlags);
		data.WriteUInt16(Id);
	}

	public ushort PhaseFlags;
	public ushort Id;
}