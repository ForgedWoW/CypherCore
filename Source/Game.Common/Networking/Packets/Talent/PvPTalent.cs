// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Talent;

public struct PvPTalent
{
	public ushort PvPTalentID;
	public byte Slot;

	public PvPTalent(WorldPacket data)
	{
		PvPTalentID = data.ReadUInt16();
		Slot = data.ReadUInt8();
	}

	public void Write(WorldPacket data)
	{
		data.WriteUInt16(PvPTalentID);
		data.WriteUInt8(Slot);
	}
}
