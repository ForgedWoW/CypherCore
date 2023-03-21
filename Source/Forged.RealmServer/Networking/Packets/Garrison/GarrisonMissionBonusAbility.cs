// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets;

struct GarrisonMissionBonusAbility
{
	public void Write(WorldPacket data)
	{
		data.WriteInt64(StartTime);
		data.WriteUInt32(GarrMssnBonusAbilityID);
	}

	public uint GarrMssnBonusAbilityID;
	public long StartTime;
}