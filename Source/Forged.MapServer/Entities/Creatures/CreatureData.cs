// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureData : SpawnData
{
	public uint Displayid;
	public sbyte EquipmentId;
	public float WanderDistance;
	public uint Currentwaypoint;
	public uint Curhealth;
	public uint Curmana;
	public byte MovementType;
	public ulong Npcflag;
	public uint UnitFlags;  // enum UnitFlags mask values
	public uint UnitFlags2; // enum UnitFlags2 mask values
	public uint UnitFlags3; // enum UnitFlags3 mask values
	public uint Dynamicflags;

	public CreatureData() : base(SpawnObjectType.Creature) { }
}