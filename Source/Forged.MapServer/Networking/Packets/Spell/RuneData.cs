// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Spell;

public class RuneData
{
    public byte Start;
    public byte Count;
    public List<byte> Cooldowns = new();

    public void Write(WorldPacket data)
    {
        data.WriteUInt8(Start);
        data.WriteUInt8(Count);
        data.WriteInt32(Cooldowns.Count);

        foreach (var cd in Cooldowns)
            data.WriteUInt8(cd);
    }
}