// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells;

public class SpellChainNode
{
    public SpellInfo First { get; set; }
    public SpellInfo Last { get; set; }
    public SpellInfo Next { get; set; }
    public SpellInfo Prev { get; set; }
    public byte Rank { get; set; }
}