// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Talent;

internal struct GlyphBinding
{
    private readonly ushort GlyphID;

    private readonly uint SpellID;

    public GlyphBinding(uint spellId, ushort glyphId)
    {
        SpellID = spellId;
        GlyphID = glyphId;
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(SpellID);
        data.WriteUInt16(GlyphID);
    }
}