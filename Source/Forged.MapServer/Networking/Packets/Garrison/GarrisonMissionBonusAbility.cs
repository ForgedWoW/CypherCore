// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Garrison;

internal struct GarrisonMissionBonusAbility
{
    public uint GarrMssnBonusAbilityID;

    public long StartTime;

    public void Write(WorldPacket data)
    {
        data.WriteInt64(StartTime);
        data.WriteUInt32(GarrMssnBonusAbilityID);
    }
}