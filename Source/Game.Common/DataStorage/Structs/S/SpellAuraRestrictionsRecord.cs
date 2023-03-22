// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.DataStorage;

public sealed class SpellAuraRestrictionsRecord
{
	public uint Id;
	public uint DifficultyID;
	public int CasterAuraState;
	public int TargetAuraState;
	public int ExcludeCasterAuraState;
	public int ExcludeTargetAuraState;
	public uint CasterAuraSpell;
	public uint TargetAuraSpell;
	public uint ExcludeCasterAuraSpell;
	public uint ExcludeTargetAuraSpell;
	public int CasterAuraType;
	public int TargetAuraType;
	public int ExcludeCasterAuraType;
	public int ExcludeTargetAuraType;
	public uint SpellID;
}