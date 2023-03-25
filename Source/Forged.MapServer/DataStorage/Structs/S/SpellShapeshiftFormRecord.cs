// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellShapeshiftFormRecord
{
	public uint Id;
	public string Name;
	public sbyte CreatureType;
	public SpellShapeshiftFormFlags Flags;
	public int AttackIconFileID;
	public sbyte BonusActionBar;
	public ushort CombatRoundTime;
	public float DamageVariance;
	public ushort MountTypeID;
	public uint[] CreatureDisplayID = new uint[4];
	public uint[] PresetSpellID = new uint[SpellConst.MaxShapeshift];
}