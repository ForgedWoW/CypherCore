// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Spells;

public class SpellTargetPosition
{
    public float Orientation { get; set; }
    public uint TargetMapId { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}