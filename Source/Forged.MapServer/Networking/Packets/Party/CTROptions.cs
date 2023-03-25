// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct CTROptions
{
	public uint ContentTuningConditionMask;
	public int Unused901;
	public uint ExpansionLevelMask;

	public void Write(WorldPacket data)
	{
		data.WriteUInt32(ContentTuningConditionMask);
		data.WriteInt32(Unused901);
		data.WriteUInt32(ExpansionLevelMask);
	}
}