// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.World;

public class WorldStateTemplate
{
    public List<uint> AreaIds { get; set; } = new();
    public int DefaultValue { get; set; }
    public int Id { get; set; }
    public List<int> MapIds { get; set; } = new();
    public uint ScriptId { get; set; }
}