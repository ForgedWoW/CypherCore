// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Spell;

public struct CreatureImmunities
{
    public uint School;
    public uint Value;

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(School);
        data.WriteUInt32(Value);
    }
}