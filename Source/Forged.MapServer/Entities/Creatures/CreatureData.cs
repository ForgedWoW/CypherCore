// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureData : SpawnData
{
    // enum UnitFlags3 mask values
    public CreatureData() : base(SpawnObjectType.Creature) { }

    public uint Curhealth { get; set; }
    public uint Curmana { get; set; }
    public uint Currentwaypoint { get; set; }
    public uint Displayid { get; set; }
    public uint Dynamicflags { get; set; }
    public sbyte EquipmentId { get; set; }
    public byte MovementType { get; set; }
    public ulong Npcflag { get; set; }

    public uint UnitFlags { get; set; }

    // enum UnitFlags mask values
    public uint UnitFlags2 { get; set; }

    // enum UnitFlags2 mask values
    public uint UnitFlags3 { get; set; }

    public float WanderDistance { get; set; }
}