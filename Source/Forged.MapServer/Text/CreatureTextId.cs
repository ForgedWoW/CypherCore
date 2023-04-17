// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Text;

public class CreatureTextId
{
    public CreatureTextId(uint e, uint g, uint i)
    {
        Entry = e;
        TextGroup = g;
        TextId = i;
    }

    public uint Entry { get; set; }
    public uint TextGroup { get; set; }
    public uint TextId { get; set; }
}