// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Creatures;

public class CreatureModelInfo
{
    public float BoundingRadius { get; set; }
    public float CombatReach { get; set; }
    public uint DisplayIdOtherGender { get; set; }
    public sbyte Gender { get; set; }
    public bool IsTrigger { get; set; }
}