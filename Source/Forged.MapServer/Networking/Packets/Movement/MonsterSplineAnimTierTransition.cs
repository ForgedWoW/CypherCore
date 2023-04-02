// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Movement;

public struct MonsterSplineAnimTierTransition
{
    public byte AnimTier;
    public uint EndTime;
    public uint StartTime;
    public int TierTransitionID;
    public void Write(WorldPacket data)
    {
        data.WriteInt32(TierTransitionID);
        data.WriteUInt32(StartTime);
        data.WriteUInt32(EndTime);
        data.WriteUInt8(AnimTier);
    }
}