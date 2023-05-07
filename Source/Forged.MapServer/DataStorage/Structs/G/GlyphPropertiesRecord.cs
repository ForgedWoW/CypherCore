// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed record GlyphPropertiesRecord
{
    public byte GlyphExclusiveCategoryID;
    public byte GlyphType;
    public uint Id;
    public uint SpellIconID;
    public uint SpellID;
}