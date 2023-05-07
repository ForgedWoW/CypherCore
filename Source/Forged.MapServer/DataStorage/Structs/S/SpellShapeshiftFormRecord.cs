// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellShapeshiftFormRecord
{
    public int AttackIconFileID;
    public sbyte BonusActionBar;
    public ushort CombatRoundTime;
    public uint[] CreatureDisplayID = new uint[4];
    public sbyte CreatureType;
    public float DamageVariance;
    public SpellShapeshiftFormFlags Flags;
    public uint Id;
    public ushort MountTypeID;
    public string Name;
    public uint[] PresetSpellID = new uint[SpellConst.MaxShapeshift];
}