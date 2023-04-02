﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class GossipMenuItem
{
    public uint Action { get; set; }
    // action data
    public uint ActionMenuId { get; set; }

    public uint ActionPoiId { get; set; }
    public bool BoxCoded { get; set; }
    public uint BoxMoney { get; set; }
    public string BoxText { get; set; }
    public GossipOptionFlags Flags { get; set; }
    public int? GossipNpcOptionId { get; set; }
    public int GossipOptionId { get; set; }
    public uint Language { get; set; }
    public GossipOptionNpc OptionNpc { get; set; }
    public string OptionText { get; set; }
    public uint OrderIndex { get; set; }
    public int? OverrideIconId { get; set; }
    // additional scripting identifiers
    public uint Sender { get; set; }

    public int? SpellId { get; set; }
}