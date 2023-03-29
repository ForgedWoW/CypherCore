// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Reputation;

internal struct FactionStandingData
{
    public FactionStandingData(int index, int standing)
    {
        Index = index;
        Standing = standing;
    }

    public void Write(WorldPacket data)
    {
        data.WriteInt32(Index);
        data.WriteInt32(Standing);
    }

    private readonly int Index;
    private readonly int Standing;
}