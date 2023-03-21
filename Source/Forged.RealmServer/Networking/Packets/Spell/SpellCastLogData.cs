// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer.Networking.Packets;

public class SpellCastLogData
{
	readonly List<SpellLogPowerData> PowerData = new();

	long Health;
	double AttackPower;
	double SpellPower;
	uint Armor;

	public void Initialize(Unit unit)
	{
		Health = unit.Health;
		AttackPower = unit.GetTotalAttackPowerValue(unit.Class == PlayerClass.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
		SpellPower = unit.SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
		Armor = unit.GetArmor();
		PowerData.Add(new SpellLogPowerData((int)unit.DisplayPowerType, unit.GetPower(unit.DisplayPowerType), 0));
	}

	public void Initialize(Spell spell)
	{
		var unitCaster = spell.Caster.AsUnit;

		if (unitCaster != null)
		{
			Health = unitCaster.Health;
			AttackPower = unitCaster.GetTotalAttackPowerValue(unitCaster.Class == PlayerClass.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
			SpellPower = unitCaster.SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
			Armor = unitCaster.GetArmor();
			var primaryPowerType = unitCaster.DisplayPowerType;
			var primaryPowerAdded = false;

			foreach (var cost in spell.PowerCost)
			{
				PowerData.Add(new SpellLogPowerData((int)cost.Power, unitCaster.GetPower(cost.Power), (int)cost.Amount));

				if (cost.Power == primaryPowerType)
					primaryPowerAdded = true;
			}

			if (!primaryPowerAdded)
				PowerData.Insert(0, new SpellLogPowerData((int)primaryPowerType, unitCaster.GetPower(primaryPowerType), 0));
		}
	}

	public void Write(WorldPacket data)
	{
		data.WriteInt64(Health);
		data.WriteInt32((int)AttackPower);
		data.WriteInt32((int)SpellPower);
		data.WriteUInt32(Armor);
		data.WriteBits(PowerData.Count, 9);
		data.FlushBits();

		foreach (var powerData in PowerData)
		{
			data.WriteInt32(powerData.PowerType);
			data.WriteInt32(powerData.Amount);
			data.WriteInt32(powerData.Cost);
		}
	}
}