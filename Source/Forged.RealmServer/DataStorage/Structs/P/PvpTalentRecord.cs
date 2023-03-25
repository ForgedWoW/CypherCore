// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class PvpTalentRecord
{
	public string Description;
	public uint Id;
	public int SpecID;
	public uint SpellID;
	public uint OverridesSpellID;
	public int Flags;
	public int ActionBarSpellID;
	public int PvpTalentCategoryID;
	public int LevelRequired;
	public int PlayerConditionID;
}