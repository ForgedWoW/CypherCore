// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Reputation;

internal struct ForcedReaction
{
    public void Write(WorldPacket data)
    {
        data.WriteInt32(Faction);
        data.WriteInt32(Reaction);
    }

    public int Faction;
    public int Reaction;
}