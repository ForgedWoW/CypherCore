// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.Entities.Creatures;

public class PointOfInterest
{
    public Vector3 Pos;
    public uint Flags { get; set; }
    public uint Icon { get; set; }
    public uint Id { get; set; }
    public uint Importance { get; set; }
    public string Name { get; set; }
    public uint WmoGroupId { get; set; }
}