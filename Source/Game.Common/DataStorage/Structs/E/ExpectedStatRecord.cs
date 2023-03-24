// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.E;

public sealed class ExpectedStatRecord
{
	public uint Id;
	public int ExpansionID;
	public float CreatureHealth;
	public float PlayerHealth;
	public float CreatureAutoAttackDps;
	public float CreatureArmor;
	public float PlayerMana;
	public float PlayerPrimaryStat;
	public float PlayerSecondaryStat;
	public float ArmorConstant;
	public float CreatureSpellDamage;
	public uint Lvl;
}
